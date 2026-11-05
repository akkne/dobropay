using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dobropay.Notifications.Workers;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Runtime") ?? throw new InvalidOperationException("ConnectionStrings:Runtime missing");
var serviceKey = builder.Configuration["INTERNAL_SERVICE_KEY"] ?? throw new InvalidOperationException("INTERNAL_SERVICE_KEY missing");
builder.Services.AddHttpClient();
builder.Services.AddHostedService<NotificationWorker>();
var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (CancellationToken ct) =>
{
    try { await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct); return Results.Ok(new { status = "ready" }); }
    catch { return Results.StatusCode(503); }
});

app.MapGet("/internal/v1/shops/{shopId:guid}/deliveries", async (HttpContext http, Guid shopId, CancellationToken ct) =>
{
    if (!Authorized(http)) return Results.Unauthorized();
    await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct);
    await using var tx = await c.BeginTransactionAsync(ct);
    await using (var set = new NpgsqlCommand("SELECT set_config('app.shop_id',$1,true)", c, tx))
    { set.Parameters.AddWithValue(shopId.ToString()); await set.ExecuteNonQueryAsync(ct); }
    await using var cmd = new NpgsqlCommand("SELECT coalesce(json_agg(json_build_object('event_id',event_id,'state',state,'attempts',attempts,'last_result',last_result))::text,'[]') FROM webhook_deliveries WHERE shop_id=$1", c, tx);
    cmd.Parameters.AddWithValue(shopId);
    var json = (string?)await cmd.ExecuteScalarAsync(ct) ?? "[]";
    await tx.CommitAsync(ct);
    return Results.Content(json, "application/json");
});

if (builder.Configuration.GetValue<bool>("Sandbox:Enabled"))
{
    var webhookSecret = builder.Configuration["WEBHOOK_SECRET"] ?? throw new InvalidOperationException("WEBHOOK_SECRET missing");
    app.MapPost("/local/webhooks/receiver", async (HttpContext http, CancellationToken ct) =>
    {
        using var stream = new MemoryStream(); await http.Request.Body.CopyToAsync(stream, ct); var body = stream.ToArray();
        var timestamp = http.Request.Headers["X-Dobropay-Timestamp"].ToString();
        var signature = http.Request.Headers["X-Dobropay-Signature"].ToString();
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (!long.TryParse(timestamp, out var seconds) || Math.Abs(now - seconds) > 300) return Results.Unauthorized();
        var signed = Encoding.UTF8.GetBytes(timestamp + ".").Concat(body).ToArray();
        var expected = "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(webhookSecret), signed)).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(signature))) return Results.Unauthorized();
        Guid eventId;
        try { using var doc = JsonDocument.Parse(body); eventId = doc.RootElement.GetProperty("event_id").GetGuid(); }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or FormatException) { return Results.BadRequest(); }
        await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("""
            INSERT INTO local_receiver_events(event_id,attempts,verified,body) VALUES ($1,1,true,$2::jsonb)
            ON CONFLICT(event_id) DO UPDATE SET attempts=local_receiver_events.attempts+1,verified=true,body=EXCLUDED.body RETURNING attempts
            """, c);
        cmd.Parameters.AddWithValue(eventId); cmd.Parameters.AddWithValue(Encoding.UTF8.GetString(body));
        var attempts = (int)(await cmd.ExecuteScalarAsync(ct))!;
        return attempts == 1 ? Results.StatusCode(503) : Results.Ok(new { received = true, event_id = eventId });
    });
    app.MapGet("/local/webhooks/received", async (CancellationToken ct) =>
    {
        await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("SELECT coalesce(json_agg(json_build_object('event_id',event_id,'attempts',attempts,'verified',verified,'body',body))::text,'[]') FROM local_receiver_events", c);
        return Results.Content((string?)await cmd.ExecuteScalarAsync(ct) ?? "[]", "application/json");
    });
}

app.Run();

bool Authorized(HttpContext http) => CryptographicOperations.FixedTimeEquals(
    SHA256.HashData(Encoding.UTF8.GetBytes(serviceKey)),
    SHA256.HashData(Encoding.UTF8.GetBytes(http.Request.Headers["X-Internal-Key"].ToString())));
