using System.Text;
using System.Text.Json;
using Dobropay.Core;
using Dobropay.Infrastructure;
using Npgsql;
using NpgsqlTypes;

public sealed class Worker(IConfiguration configuration, ILogger<Worker> logger, IHttpClientFactory clients) : BackgroundService
{
    private readonly string connection = configuration["ConnectionStrings:Worker"]!;

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        while (!stop.IsCancellationRequested)
        {
            try
            {
                await ProcessInbox(stop);
                await QueueOutbox(stop);
                await Deliver(stop);
            }
            catch (Exception e) { logger.LogError(e, "Worker cycle failed"); }
            await Task.Delay(1000, stop);
        }
    }

    private async Task ProcessInbox(CancellationToken ct)
    {
        await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);
        var row = await Read(c, tx, "SELECT id,shop_id,payload::text,attempts FROM provider_inbox WHERE state='pending' AND next_at<=now() ORDER BY received_at LIMIT 1", ct);
        if (row is null) return;
        var id = (Guid)row[0]; var shop = (Guid)row[1];
        await SetShop(c, tx, shop, ct);
        row = await Read(c, tx, "SELECT id,shop_id,payload::text,attempts FROM provider_inbox WHERE id=$1 AND state='pending' AND next_at<=now() FOR UPDATE SKIP LOCKED", ct, id);
        if (row is null) return;
        var e = JsonSerializer.Deserialize<ProviderEvent>((string)row[2], JsonSerializerOptions.Web)!;
        var match = await Read(c, tx, "SELECT a.external_id,p.id,p.amount,p.currency,p.status FROM payment_attempts a JOIN payments p ON p.shop_id=a.shop_id AND p.id=a.payment_id WHERE a.id=$1 AND a.shop_id=$2 FOR UPDATE OF p,a", ct, e.AttemptId, shop);
        var snapshot = match is null ? null : new AttemptSnapshot(shop, (Guid)match[1], match[0] is DBNull ? null : (string)match[0], (long)match[2], (string)match[3], (string)match[4]);
        var decision = PaymentConfirmation.Decide(e, snapshot);
        if (decision == ConfirmationDecision.WaitForAttempt)
        {
            await Execute(c, tx, "UPDATE provider_inbox SET attempts=attempts+1,next_at=now()+make_interval(secs => LEAST(60, power(2,attempts+1)::int)),state=CASE WHEN attempts>=5 THEN 'failed' ELSE 'pending' END,error='attempt_not_ready' WHERE id=$1", ct, id);
            await tx.CommitAsync(ct); return;
        }
        if (decision == ConfirmationDecision.Reject)
        {
            await Execute(c, tx, "UPDATE provider_inbox SET state='rejected',error='data_mismatch',attempts=attempts+1 WHERE id=$1", ct, id);
            await tx.CommitAsync(ct); logger.LogWarning("Rejected provider event {EventId} shop {ShopId}", e.EventId, shop); return;
        }
        if (decision == ConfirmationDecision.Apply)
        {
            var paymentId = snapshot!.PaymentId;
            await Execute(c, tx, "UPDATE payments SET status=$1,updated_at=now() WHERE id=$2", ct, e.Outcome, paymentId);
            await Execute(c, tx, "UPDATE payment_attempts SET state=$1 WHERE id=$2", ct, e.Outcome, e.AttemptId);
            await Execute(c, tx, "INSERT INTO outbox_events(id,shop_id,payment_id,event_type,occurred_at,queued) VALUES ($1,$2,$3,$4,now(),false)", ct, Guid.NewGuid(), shop, paymentId, "payment." + e.Outcome);
            logger.LogInformation("Payment {PaymentId} transitioned to {Status} event {EventId} shop {ShopId}", paymentId, e.Outcome, e.EventId, shop);
        }
        await Execute(c, tx, "UPDATE provider_inbox SET state='processed',attempts=attempts+1,error=NULL WHERE id=$1", ct, id);
        await tx.CommitAsync(ct);
    }

    private async Task QueueOutbox(CancellationToken ct)
    {
        await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);
        var row = await Read(c, tx, "SELECT id,shop_id FROM outbox_events WHERE queued=false ORDER BY occurred_at LIMIT 1", ct);
        if (row is null) return;
        var eventId = (Guid)row[0]; var shop = (Guid)row[1]; await SetShop(c, tx, shop, ct);
        if (await Read(c, tx, "SELECT id FROM outbox_events WHERE id=$1 AND queued=false FOR UPDATE SKIP LOCKED", ct, eventId) is null) return;
        await Execute(c, tx, "INSERT INTO webhook_deliveries(id,shop_id,event_id,subscription_id,state,attempts,next_at) SELECT gen_random_uuid(),$1,$2,id,'pending',0,now() FROM webhook_subscriptions WHERE shop_id=$1 ON CONFLICT(event_id,subscription_id) DO NOTHING", ct, shop, eventId);
        await Execute(c, tx, "UPDATE outbox_events SET queued=true WHERE id=$1", ct, eventId);
        await tx.CommitAsync(ct);
    }

    private async Task Deliver(CancellationToken ct)
    {
        Guid deliveryId, shop;
        await using (var c = new NpgsqlConnection(connection))
        {
            await c.OpenAsync(ct); await using var tx = await c.BeginTransactionAsync(ct);
            var row = await Read(c, tx, "SELECT id,shop_id FROM webhook_deliveries WHERE (state='pending' AND next_at<=now()) OR (state='processing' AND locked_until<now()) ORDER BY next_at LIMIT 1", ct);
            if (row is null) return;
            deliveryId = (Guid)row[0]; shop = (Guid)row[1]; await SetShop(c, tx, shop, ct);
            if (await Read(c, tx, "SELECT id FROM webhook_deliveries WHERE id=$1 AND ((state='pending' AND next_at<=now()) OR (state='processing' AND locked_until<now())) FOR UPDATE SKIP LOCKED", ct, deliveryId) is null) return;
            await Execute(c, tx, "UPDATE webhook_deliveries SET state='processing',attempts=attempts+1,locked_until=now()+interval '30 seconds' WHERE id=$1", ct, deliveryId);
            await tx.CommitAsync(ct);
        }
        string url, signingSecret; byte[] body; Guid eventId; int attempt;
        await using (var c = new NpgsqlConnection(connection))
        {
            await c.OpenAsync(ct); await using var tx = await c.BeginTransactionAsync(ct); await SetShop(c, tx, shop, ct);
            var row = (await Read(c, tx, "SELECT s.url,e.id,e.event_type,e.occurred_at,p.id,p.status,p.amount,p.currency,d.attempts,s.signing_secret FROM webhook_deliveries d JOIN webhook_subscriptions s ON s.id=d.subscription_id JOIN outbox_events e ON e.id=d.event_id JOIN payments p ON p.id=e.payment_id AND p.shop_id=e.shop_id WHERE d.id=$1", ct, deliveryId))!;
            url = (string)row[0]; eventId = (Guid)row[1]; attempt = (int)row[8]; signingSecret = (string)row[9];
            body = JsonSerializer.SerializeToUtf8Bytes(new { event_id = eventId, event_type = row[2], occurred_at = row[3], payment = new { id = row[4], status = row[5], amount = row[6], currency = row[7] } });
            await tx.CommitAsync(ct);
        }
        int? status = null; string result;
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var signed = Encoding.UTF8.GetBytes(timestamp + ".").Concat(body).ToArray();
            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new ByteArrayContent(body) };
            req.Content.Headers.ContentType = new("application/json");
            req.Headers.Add("X-Dobropay-Timestamp", timestamp);
            req.Headers.Add("X-Dobropay-Signature", "sha256=" + SandboxCrypto.Hmac(signingSecret, signed));
            using var response = await clients.CreateClient().SendAsync(req, ct);
            status = (int)response.StatusCode; result = response.IsSuccessStatusCode ? "delivered" : "http_error";
        }
        catch (Exception e) { result = e.GetType().Name; }
        await using (var c = new NpgsqlConnection(connection))
        {
            await c.OpenAsync(ct); await using var tx = await c.BeginTransactionAsync(ct); await SetShop(c, tx, shop, ct);
            await Execute(c, tx, "INSERT INTO webhook_delivery_attempts(id,shop_id,delivery_id,attempted_at,http_status,result) VALUES ($1,$2,$3,now(),$4,$5)", ct, Guid.NewGuid(), shop, deliveryId, (object?)status ?? DBNull.Value, result);
            await Execute(c, tx, "UPDATE webhook_deliveries SET state=$1,locked_until=NULL,next_at=now()+make_interval(secs => LEAST(60,power(2,attempts)::int)),last_result=$2 WHERE id=$3", ct, result == "delivered" ? "delivered" : attempt >= 5 ? "failed" : "pending", result, deliveryId);
            await tx.CommitAsync(ct);
        }
        logger.LogInformation("Webhook delivery {DeliveryId} event {EventId} shop {ShopId} result {Result} status {HttpStatus}", deliveryId, eventId, shop, result, status);
    }

    private static async Task SetShop(NpgsqlConnection c, NpgsqlTransaction tx, Guid shop, CancellationToken ct) =>
        await Execute(c, tx, "SELECT set_config('app.shop_id',$1,true)", ct, shop.ToString());
    private static async Task Execute(NpgsqlConnection c, NpgsqlTransaction tx, string sql, CancellationToken ct, params object[] values)
    {
        await using var cmd = new NpgsqlCommand(sql, c, tx);
        foreach (var value in values) Add(cmd, value);
        await cmd.ExecuteNonQueryAsync(ct);
    }
    private static async Task<object[]?> Read(NpgsqlConnection c, NpgsqlTransaction tx, string sql, CancellationToken ct, params object[] values)
    {
        await using var cmd = new NpgsqlCommand(sql, c, tx);
        foreach (var value in values) Add(cmd, value);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        var row = new object[reader.FieldCount]; reader.GetValues(row); return row;
    }
    private static void Add(NpgsqlCommand command, object value)
    {
        if (value == DBNull.Value) command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer, Value = DBNull.Value });
        else command.Parameters.AddWithValue(value);
    }
}
