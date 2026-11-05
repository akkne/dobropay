using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dobropay.Contracts.Events;
using Dobropay.Messaging.Kafka;
using Npgsql;
using NpgsqlTypes;
using StackExchange.Redis;

namespace Dobropay.Notifications.Workers;

public sealed class NotificationWorker(IConfiguration config, ILogger<NotificationWorker> log, IHttpClientFactory clients) : BackgroundService
{
    private readonly string connection = config.GetConnectionString("Runtime")!;

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        var broker = config["Kafka:BootstrapServers"] ?? throw new InvalidOperationException("Kafka:BootstrapServers missing");
        using var subscriber = new KafkaSubscriber(broker, KafkaContracts.NotificationsPaymentGroup, KafkaContracts.PaymentEvents);
        while (!stop.IsCancellationRequested)
        {
            try
            {
                var result = subscriber.Receive(TimeSpan.FromMilliseconds(100));
                if (result is not null)
                {
                    await SaveEvent(result.Message.Value, stop);
                    subscriber.Commit(result);
                }
                await Deliver(stop);
            }
            catch (Exception e) when (!stop.IsCancellationRequested) { log.LogError(e, "Notification cycle failed"); }
            await Task.Delay(500, stop);
        }
    }

    private async Task SaveEvent(string json, CancellationToken ct)
    {
        var e = JsonSerializer.Deserialize<EventEnvelope>(json) ?? throw new JsonException("Empty event");
        if (e.SchemaVersion != 1 || e.EventType is not ("payment.succeeded" or "payment.canceled"))
            throw new JsonException("Unsupported payment event");
        await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);
        await SetShop(c, tx, e.ShopId, ct);
        await Execute(c, tx, "INSERT INTO notification_inbox(event_id,shop_id,payment_id,envelope,received_at) VALUES ($1,$2,$3,$4::jsonb,now()) ON CONFLICT(event_id) DO NOTHING",
            ct, e.EventId, e.ShopId, e.PaymentId, json);
        await Execute(c, tx, """
            INSERT INTO webhook_deliveries(id,shop_id,event_id,subscription_id,state,attempts,next_at)
            SELECT gen_random_uuid(),$1,$2,id,'pending',0,now() FROM webhook_subscriptions WHERE shop_id=$1
            ON CONFLICT(event_id,subscription_id) DO NOTHING
            """, ct, e.ShopId, e.EventId);
        await tx.CommitAsync(ct);
        log.LogInformation("Queued webhook event {EventId} shop {ShopId} payment {PaymentId} correlation {CorrelationId}",
            e.EventId, e.ShopId, e.PaymentId, e.CorrelationId);
    }

    private async Task Deliver(CancellationToken ct)
    {
        await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct);
        Guid id, shop;
        await using (var cmd = new NpgsqlCommand("SELECT id,shop_id FROM next_delivery()", c))
        await using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct)) return;
            id = r.GetGuid(0); shop = r.GetGuid(1);
        }
        string url, secret, envelopeJson; int attempts;
        await using (var tx = await c.BeginTransactionAsync(ct))
        {
            await SetShop(c, tx, shop, ct);
            await using var cmd = new NpgsqlCommand("""
                SELECT s.url,s.signing_secret,i.envelope::text,d.attempts
                FROM webhook_deliveries d JOIN webhook_subscriptions s ON s.id=d.subscription_id AND s.shop_id=d.shop_id
                JOIN notification_inbox i ON i.event_id=d.event_id AND i.shop_id=d.shop_id
                WHERE d.id=$1 AND ((d.state='pending' AND d.next_at<=now()) OR (d.state='processing' AND d.locked_until<now()))
                FOR UPDATE OF d SKIP LOCKED
                """, c, tx);
            cmd.Parameters.AddWithValue(id);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return;
            url = r.GetString(0); secret = r.GetString(1); envelopeJson = r.GetString(2); attempts = r.GetInt32(3) + 1;
            await r.DisposeAsync();
            await Execute(c, tx, "UPDATE webhook_deliveries SET state='processing',attempts=$1,locked_until=now()+interval '30 seconds' WHERE id=$2", ct, attempts, id);
            await tx.CommitAsync(ct);
        }
        var envelope = JsonSerializer.Deserialize<EventEnvelope>(envelopeJson)!;
        var payment = envelope.Payload;
        var body = JsonSerializer.SerializeToUtf8Bytes(new { event_id = envelope.EventId, event_type = envelope.EventType,
            occurred_at = envelope.OccurredAt, payment });
        int? status = null; string outcome;
        try
        {
            using var redis = await ConnectionMultiplexer.ConnectAsync(config["Redis:Connection"] ?? "redis:6379");
            var allowed = await redis.GetDatabase().StringSetAsync("webhook-limit:" + shop, "1", TimeSpan.FromSeconds(1), When.NotExists);
            if (!allowed) { await Defer(c, shop, id, "rate_limited", ct); return; }
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var signed = Encoding.UTF8.GetBytes(timestamp + ".").Concat(body).ToArray();
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new ByteArrayContent(body) };
            request.Content.Headers.ContentType = new("application/json");
            request.Headers.Add("X-Dobropay-Timestamp", timestamp);
            request.Headers.Add("X-Dobropay-Signature", "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), signed)).ToLowerInvariant());
            using var response = await clients.CreateClient().SendAsync(request, ct);
            status = (int)response.StatusCode;
            outcome = response.IsSuccessStatusCode ? "delivered" : "http_error";
        }
        catch (RedisConnectionException) { await Defer(c, shop, id, "redis_unavailable", ct); return; }
        catch (Exception e) { outcome = e.GetType().Name; }
        await using (var tx = await c.BeginTransactionAsync(ct))
        {
            await SetShop(c, tx, shop, ct);
            await using var insert = new NpgsqlCommand("INSERT INTO webhook_delivery_attempts(id,shop_id,delivery_id,attempted_at,http_status,result) VALUES ($1,$2,$3,now(),$4,$5)", c, tx);
            insert.Parameters.AddWithValue(Guid.NewGuid()); insert.Parameters.AddWithValue(shop); insert.Parameters.AddWithValue(id);
            insert.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer, Value = (object?)status ?? DBNull.Value });
            insert.Parameters.AddWithValue(outcome); await insert.ExecuteNonQueryAsync(ct);
            await Execute(c, tx, """
                UPDATE webhook_deliveries SET state=$1,locked_until=NULL,
                next_at=now()+make_interval(secs => LEAST(60,power(2,attempts)::int)),last_result=$2 WHERE id=$3
                """, ct, outcome == "delivered" ? "delivered" : attempts >= 5 ? "failed" : "pending", outcome, id);
            await tx.CommitAsync(ct);
        }
        log.LogInformation("Webhook delivery {DeliveryId} event {EventId} shop {ShopId} result {Result} status {HttpStatus}",
            id, envelope.EventId, shop, outcome, status);
    }

    private static async Task Defer(NpgsqlConnection c, Guid shop, Guid id, string reason, CancellationToken ct)
    {
        await using var tx = await c.BeginTransactionAsync(ct);
        await SetShop(c, tx, shop, ct);
        await Execute(c, tx, "UPDATE webhook_deliveries SET state='pending',attempts=attempts-1,locked_until=NULL,next_at=now()+interval '5 seconds',last_result=$1 WHERE id=$2", ct, reason, id);
        await tx.CommitAsync(ct);
    }

    private static async Task SetShop(NpgsqlConnection c, NpgsqlTransaction tx, Guid shop, CancellationToken ct) =>
        await Execute(c, tx, "SELECT set_config('app.shop_id',$1,true)", ct, shop.ToString());

    private static async Task Execute(NpgsqlConnection c, NpgsqlTransaction tx, string sql, CancellationToken ct, params object[] values)
    {
        await using var cmd = new NpgsqlCommand(sql, c, tx);
        foreach (var v in values) cmd.Parameters.AddWithValue(v);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
