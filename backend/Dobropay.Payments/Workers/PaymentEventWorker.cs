using System.Text.Json;
using Dobropay.Contracts.Events;
using Dobropay.Messaging.Kafka;
using Dobropay.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dobropay.Payments.Workers;

public sealed class PaymentEventWorker(IConfiguration config, ILogger<PaymentEventWorker> log) : BackgroundService
{
    private readonly string connection = config.GetConnectionString("Runtime")!;

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        var broker = config["Kafka:BootstrapServers"] ?? throw new InvalidOperationException("Kafka:BootstrapServers missing");
        using var publisher = new KafkaPublisher(broker);
        using var subscriber = new KafkaSubscriber(broker, KafkaContracts.PaymentsProviderGroup, KafkaContracts.ProviderEvents);
        while (!stop.IsCancellationRequested)
        {
            try
            {
                await PublishNext(publisher, stop);
                var result = subscriber.Receive(TimeSpan.FromMilliseconds(100));
                if (result is not null && await Handle(result.Message.Value, publisher, stop)) subscriber.Commit(result);
            }
            catch (Exception e) when (!stop.IsCancellationRequested) { log.LogError(e, "Payment event cycle failed"); }
            await Task.Delay(500, stop);
        }
    }

    private async Task PublishNext(KafkaPublisher publisher, CancellationToken ct)
    {
        await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct);
        Guid id, shop;
        await using (var find = new NpgsqlCommand("SELECT id,shop_id FROM next_outbox()", c))
        await using (var r = await find.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct)) return;
            id = r.GetGuid(0); shop = r.GetGuid(1);
        }
        await using var tx = await c.BeginTransactionAsync(ct);
        await SetShop(c, tx, shop, ct);
        await using var cmd = new NpgsqlCommand("SELECT topic,envelope::text FROM outbox_events WHERE id=$1 AND NOT published FOR UPDATE SKIP LOCKED", c, tx);
        cmd.Parameters.AddWithValue(id);
        await using var row = await cmd.ExecuteReaderAsync(ct);
        if (!await row.ReadAsync(ct)) return;
        var topic = row.GetString(0);
        var envelope = JsonSerializer.Deserialize<EventEnvelope>(row.GetString(1))!;
        await row.DisposeAsync();
        await publisher.PublishAsync(topic, envelope, ct);
        await Execute(c, tx, "UPDATE outbox_events SET published=true WHERE id=$1", ct, id);
        await tx.CommitAsync(ct);
        log.LogInformation("Published event {EventId} payment {PaymentId} shop {ShopId} correlation {CorrelationId}",
            envelope.EventId, envelope.PaymentId, envelope.ShopId, envelope.CorrelationId);
    }

    private async Task<bool> Handle(string json, KafkaPublisher publisher, CancellationToken ct)
    {
        EventEnvelope envelope;
        try { envelope = JsonSerializer.Deserialize<EventEnvelope>(json)!; }
        catch (JsonException) { return false; }
        if (envelope.SchemaVersion != 1 || envelope.EventType != "provider.event.accepted")
        {
            await publisher.PublishAsync(KafkaContracts.DeadLetterQueue("payments-provider-events"), envelope, ct);
            return true;
        }
        var incoming = envelope.Payload.Deserialize<ProviderEvent>(JsonSerializerOptions.Web);
        if (incoming is null) return false;
        await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);
        await SetShop(c, tx, envelope.ShopId, ct);
        string? state = null; int attempts = 0;
        await using (var cmd = new NpgsqlCommand("SELECT state,attempts FROM provider_inbox WHERE id=$1 AND shop_id=$2 FOR UPDATE", c, tx))
        {
            cmd.Parameters.AddWithValue(envelope.EventId); cmd.Parameters.AddWithValue(envelope.ShopId);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (await r.ReadAsync(ct)) { state = r.GetString(0); attempts = r.GetInt32(1); }
        }
        if (state is null) { await tx.CommitAsync(ct); return true; }
        if (state != "accepted") { await tx.CommitAsync(ct); return true; }
        AttemptSnapshot? snapshot = null;
        await using (var cmd = new NpgsqlCommand("""
            SELECT a.external_id,p.id,p.amount,p.currency,p.status FROM payment_attempts a
            JOIN payments p ON p.shop_id=a.shop_id AND p.id=a.payment_id
            WHERE a.id=$1 AND a.shop_id=$2 AND p.id=$3 FOR UPDATE OF a,p
            """, c, tx))
        {
            cmd.Parameters.AddWithValue(incoming.AttemptId); cmd.Parameters.AddWithValue(envelope.ShopId); cmd.Parameters.AddWithValue(envelope.PaymentId);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (await r.ReadAsync(ct)) snapshot = new AttemptSnapshot(envelope.ShopId, r.GetGuid(1), r.IsDBNull(0) ? null : r.GetString(0),
                r.GetInt64(2), r.GetString(3), r.GetString(4));
        }
        var decision = PaymentConfirmation.Decide(incoming, snapshot);
        if (decision == ConfirmationDecision.WaitForAttempt && attempts < 5)
        {
            await Execute(c, tx, "UPDATE provider_inbox SET attempts=attempts+1,error='attempt_not_ready' WHERE id=$1", ct, envelope.EventId);
            await tx.CommitAsync(ct);
            return false;
        }
        if (decision == ConfirmationDecision.Apply)
        {
            await Execute(c, tx, "UPDATE payments SET status=$1,updated_at=now() WHERE id=$2", ct, incoming.Outcome, envelope.PaymentId);
            await Execute(c, tx, "UPDATE payment_attempts SET state=$1 WHERE id=$2", ct, incoming.Outcome, incoming.AttemptId);
            var payload = JsonSerializer.SerializeToElement(new { id = envelope.PaymentId, status = incoming.Outcome,
                amount = snapshot!.Amount, currency = snapshot.Currency });
            var business = new EventEnvelope(Guid.NewGuid(), "payment." + incoming.Outcome, 1, DateTimeOffset.UtcNow,
                envelope.ShopId, envelope.PaymentId, envelope.CorrelationId, payload);
            await Execute(c, tx, "INSERT INTO outbox_events(id,shop_id,payment_id,topic,envelope,occurred_at,published) VALUES ($1,$2,$3,$4,$5::jsonb,$6,false)",
                ct, business.EventId, business.ShopId, business.PaymentId, KafkaContracts.PaymentEvents, JsonSerializer.Serialize(business), business.OccurredAt);
        }
        var final = decision == ConfirmationDecision.Reject ? "rejected" : decision == ConfirmationDecision.WaitForAttempt ? "failed" : "processed";
        await Execute(c, tx, "UPDATE provider_inbox SET state=$1,attempts=attempts+1,error=$2 WHERE id=$3", ct,
            final, final == "processed" ? "" : decision.ToString(), envelope.EventId);
        await tx.CommitAsync(ct);
        log.LogInformation("Applied provider event {EventId} payment {PaymentId} shop {ShopId} decision {Decision}",
            envelope.EventId, envelope.PaymentId, envelope.ShopId, decision);
        return true;
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
