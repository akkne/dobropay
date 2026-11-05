using System.Net.Http.Json;
using System.Text.Json;
using Dobropay.Contracts.Events;
using Dobropay.Messaging.Kafka;
using Npgsql;

namespace Dobropay.Receipts.Workers;

public sealed class ReceiptWorker(IConfiguration config, ILogger<ReceiptWorker> log, IHttpClientFactory clients) : BackgroundService
{
    private readonly string connection = config.GetConnectionString("Runtime")!;

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        var broker = config["Kafka:BootstrapServers"] ?? throw new InvalidOperationException("Kafka:BootstrapServers missing");
        using var subscriber = new KafkaSubscriber(broker, KafkaContracts.ReceiptsPaymentGroup, KafkaContracts.PaymentEvents);
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
                await SendNext(stop);
            }
            catch (Exception e) when (!stop.IsCancellationRequested) { log.LogError(e, "Receipt cycle failed"); }
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
        await Execute(c, tx, "INSERT INTO receipt_inbox(event_id,shop_id,payment_id,received_at) VALUES ($1,$2,$3,now()) ON CONFLICT(event_id) DO NOTHING",
            ct, e.EventId, e.ShopId, e.PaymentId);
        if (e.EventType == "payment.succeeded")
        {
            var amount = e.Payload.GetProperty("amount").GetInt64();
            var currency = e.Payload.GetProperty("currency").GetString()!;
            await Execute(c, tx, """
                INSERT INTO demo_receipts(id,shop_id,payment_id,event_id,amount,currency,email_state,created_at)
                VALUES ($1,$2,$3,$4,$5,$6,'pending',now()) ON CONFLICT(event_id) DO NOTHING
                """, ct, Guid.NewGuid(), e.ShopId, e.PaymentId, e.EventId, amount, currency);
        }
        await tx.CommitAsync(ct);
        log.LogInformation("Recorded receipt event {EventId} shop {ShopId} payment {PaymentId} correlation {CorrelationId}",
            e.EventId, e.ShopId, e.PaymentId, e.CorrelationId);
    }

    private async Task SendNext(CancellationToken ct)
    {
        await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct);
        Guid id, shop;
        await using (var cmd = new NpgsqlCommand("SELECT id,shop_id FROM next_receipt()", c))
        await using (var r = await cmd.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct)) return;
            id = r.GetGuid(0); shop = r.GetGuid(1);
        }
        Guid payment; long amount;
        await using (var tx = await c.BeginTransactionAsync(ct))
        {
            await SetShop(c, tx, shop, ct);
            await using var cmd = new NpgsqlCommand("SELECT payment_id,amount FROM demo_receipts WHERE id=$1 AND email_state='pending' FOR UPDATE SKIP LOCKED", c, tx);
            cmd.Parameters.AddWithValue(id);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct)) return;
            payment = r.GetGuid(0); amount = r.GetInt64(1);
            await r.DisposeAsync();
            await Execute(c, tx, "UPDATE demo_receipts SET email_state='sending' WHERE id=$1", ct, id);
            await tx.CommitAsync(ct);
        }
        var client = clients.CreateClient();
        client.DefaultRequestHeaders.Add("X-Internal-Key", config["INTERNAL_SERVICE_KEY"]);
        string state;
        try
        {
            using var response = await client.PostAsJsonAsync(config["Services:Email"] + "/internal/v1/email",
                new { ReceiptId = id, To = config["Demo:ReceiptEmail"], Subject = "Демо-квитанция Dobropay",
                    Text = $"Демонстрационная квитанция. Платёж {payment}, сумма {amount} копеек RUB. Не является фискальным чеком." }, ct);
            state = response.IsSuccessStatusCode ? "sent" : "failed";
        }
        catch (Exception e) { log.LogWarning(e, "Email outcome unknown for receipt {ReceiptId}", id); return; }
        await using var finish = await c.BeginTransactionAsync(ct);
        await SetShop(c, finish, shop, ct);
        await Execute(c, finish, "UPDATE demo_receipts SET email_state=$1 WHERE id=$2 AND email_state='sending'", ct, state, id);
        await finish.CommitAsync(ct);
        log.LogInformation("Receipt email {ReceiptId} shop {ShopId} payment {PaymentId} state {State}", id, shop, payment, state);
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
