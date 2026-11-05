using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Net.Http.Json;
using Dobropay.Payments.Domain;
using Dobropay.Payments.Persistence;
using Dobropay.Payments.Provider;
using Dobropay.Contracts.Events;
using Dobropay.Payments.Workers;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
string required(string key) => builder.Configuration[key] ?? throw new InvalidOperationException($"Missing {key}");
var connection = required("ConnectionStrings:Runtime");
var developerApi = required("Services:DeveloperApi");
var serviceKey = required("INTERNAL_SERVICE_KEY");
var checkoutSecret = required("CHECKOUT_SECRET");
var providerSecret = required("PROVIDER_SECRET");
var publicBase = builder.Configuration["Sandbox:PublicBaseUrl"] ?? "http://localhost:8080";
var internalBase = builder.Configuration["Sandbox:InternalBaseUrl"] ?? "http://api:8080";
builder.Services.AddDbContext<PaymentsDbContext>(o => o.UseNpgsql(connection));
builder.Services.AddSingleton<IPaymentProvider>(new SandboxProvider(providerSecret));
builder.Services.AddHostedService<PaymentEventWorker>();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(x => x.OperationFilter<PaymentHeadersFilter>());
var app = builder.Build();
app.UseSwagger(); app.UseSwaggerUI();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (CancellationToken ct) => { try { await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct); return Results.Ok(new { status = "ready" }); } catch { return Results.StatusCode(503); } });

app.MapPost("/v1/payments", async (HttpContext http, CreatePaymentRequest body, PaymentsDbContext db, CancellationToken ct) =>
{
    var shop = await Authenticate(http, ct);
    if (shop is null) return Results.Unauthorized();
    var key = http.Request.Headers["Idempotency-Key"].ToString();
    if (key.Length is < 1 or > 200) return Results.BadRequest(new { error = "idempotency_key_required" });
    if (body.Amount <= 0 || body.Currency != "RUB" || body.ExternalOrderId?.Length > 200) return Results.BadRequest(new { error = "invalid_payment" });
    var order = body.ExternalOrderId?.Trim();
    var fingerprint = SandboxCrypto.Sha256(JsonSerializer.Serialize(new { body.Amount, body.Currency, ExternalOrderId = order }));
    try
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await SetShop(db, shop.Value, ct);
        var old = await db.IdempotencyRecords.AsNoTracking().SingleOrDefaultAsync(x => x.ShopId == shop && x.Operation == "create_payment" && x.IdempotencyKey == key, ct);
        if (old is not null)
        {
            if (old.RequestHash != fingerprint) return Results.Conflict(new { error = "idempotency_conflict" });
            var previous = await db.Payments.AsNoTracking().SingleAsync(x => x.Id == old.PaymentId, ct);
            await tx.CommitAsync(ct);
            return Results.Ok(Response(previous));
        }
        var p = new Payment { Id = Guid.NewGuid(), ShopId = shop.Value, Amount = body.Amount, Currency = "RUB", ExternalOrderId = order };
        db.Payments.Add(p); await db.SaveChangesAsync(ct);
        db.IdempotencyRecords.Add(new IdempotencyRecord { ShopId = shop.Value, Operation = "create_payment", IdempotencyKey = key, RequestHash = fingerprint, PaymentId = p.Id });
        db.CheckoutLinks.Add(new CheckoutLink { TokenHash = SandboxCrypto.Sha256(Token(p.Id)), ShopId = shop.Value, PaymentId = p.Id, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30) });
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        app.Logger.LogInformation("Payment created {PaymentId} shop {ShopId} correlation {CorrelationId}", p.Id, shop, http.TraceIdentifier);
        return Results.Created($"/v1/payments/{p.Id}", Response(p));
    }
    catch (DbUpdateException e) when (e.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        db.ChangeTracker.Clear();
        await using var tx = await db.Database.BeginTransactionAsync(ct); await SetShop(db, shop.Value, ct);
        var old = await db.IdempotencyRecords.AsNoTracking().SingleAsync(x => x.ShopId == shop && x.Operation == "create_payment" && x.IdempotencyKey == key, ct);
        if (old.RequestHash != fingerprint) return Results.Conflict(new { error = "idempotency_conflict" });
        var previous = await db.Payments.AsNoTracking().SingleAsync(x => x.Id == old.PaymentId, ct);
        await tx.CommitAsync(ct); return Results.Ok(Response(previous));
    }
}).WithTags("Payments").WithSummary("Create a RUB payment").Produces<PaymentResponse>(201).Produces<PaymentResponse>(200).Produces(400).Produces(401).Produces(409);

app.MapGet("/v1/payments/{id:guid}", async (HttpContext http, Guid id, PaymentsDbContext db, CancellationToken ct) =>
{
    var shop = await Authenticate(http, ct); if (shop is null) return Results.Unauthorized();
    await using var tx = await db.Database.BeginTransactionAsync(ct); await SetShop(db, shop.Value, ct);
    var p = await db.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); await tx.CommitAsync(ct);
    return p is null ? Results.NotFound(new { error = "payment_not_found" }) : Results.Ok(new PaymentView(p.Id, p.Status, p.Amount, p.Currency, p.ExternalOrderId));
}).WithTags("Payments").WithSummary("Get own payment").Produces<PaymentView>().Produces(401).Produces(404);

app.MapGet("/internal/v1/shops/{shopId:guid}/payments/{id:guid}", async (HttpContext http, Guid shopId, Guid id, PaymentsDbContext db, CancellationToken ct) =>
{
    if (!InternalAuthorized(http)) return Results.Unauthorized();
    await using var tx = await db.Database.BeginTransactionAsync(ct); await SetShop(db, shopId, ct);
    var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.ShopId == shopId, ct);
    await tx.CommitAsync(ct);
    return payment is null ? Results.NotFound() : Results.Ok(new PaymentView(payment.Id, payment.Status, payment.Amount, payment.Currency, payment.ExternalOrderId));
});

app.MapGet("/internal/v1/shops/{shopId:guid}/payments", async (HttpContext http, Guid shopId, PaymentsDbContext db, CancellationToken ct) =>
{
    if (!InternalAuthorized(http)) return Results.Unauthorized();
    await using var tx = await db.Database.BeginTransactionAsync(ct); await SetShop(db, shopId, ct);
    var payments = await db.Payments.AsNoTracking().Where(x => x.ShopId == shopId).OrderByDescending(x => x.CreatedAt)
        .Take(100).Select(x => new PaymentView(x.Id, x.Status, x.Amount, x.Currency, x.ExternalOrderId)).ToListAsync(ct);
    await tx.CommitAsync(ct);
    return Results.Ok(payments);
});

app.MapGet("/checkout/{token}", async (string token, PaymentsDbContext db, CancellationToken ct) =>
{
    var link = await Lookup(token, ct); if (link is null) return Results.NotFound();
    if (link.Value.ExpiresAt < DateTimeOffset.UtcNow) return Results.StatusCode(410);
    await using var tx = await db.Database.BeginTransactionAsync(ct); await SetShop(db, link.Value.ShopId, ct);
    var p = await db.Payments.AsNoTracking().SingleAsync(x => x.Id == link.Value.PaymentId, ct);
    var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
    client.DefaultRequestHeaders.Add("X-Internal-Key", serviceKey);
    var shop = await client.GetFromJsonAsync<ResolvedShop>(developerApi + "/internal/v1/shops/" + link.Value.ShopId, ct);
    var attempt = await db.PaymentAttempts.AsNoTracking().SingleOrDefaultAsync(x => x.PaymentId == p.Id, ct);
    await tx.CommitAsync(ct);
    var actions = p.Status != "pending" ? "" : attempt is null
        ? $"<form method='post' action='/checkout/{token}/start'><button>Перейти к оплате</button></form>"
        : $"<form method='post' action='/checkout/{token}/simulate/succeeded'><button>Успешно</button></form><form method='post' action='/checkout/{token}/simulate/canceled'><button>Отменить</button></form>";
    return Results.Content($"<!doctype html><html lang='ru'><meta charset='utf-8'><title>Dobropay</title><main><h1>{HtmlEncoder.Default.Encode(shop?.Name ?? "Магазин")}</h1><p>К оплате: {p.Amount / 100m:0.00} ₽</p><p>Статус: {p.Status}</p>{actions}</main></html>", "text/html; charset=utf-8");
});

app.MapPost("/checkout/{token}/start", async (string token, PaymentsDbContext db, IPaymentProvider provider, CancellationToken ct) =>
{
    var link = await Lookup(token, ct); if (link is null) return Results.NotFound();
    if (link.Value.ExpiresAt < DateTimeOffset.UtcNow) return Results.StatusCode(410);
    PaymentAttempt attempt;
    await using (var tx = await db.Database.BeginTransactionAsync(ct))
    {
        await SetShop(db, link.Value.ShopId, ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({link.Value.PaymentId.ToString()},0))", ct);
        var p = await db.Payments.SingleAsync(x => x.Id == link.Value.PaymentId, ct);
        if (p.Status != "pending") return Results.Redirect($"/checkout/{token}");
        attempt = await db.PaymentAttempts.SingleOrDefaultAsync(x => x.PaymentId == p.Id, ct) ?? new PaymentAttempt { Id = Guid.NewGuid(), ShopId = link.Value.ShopId, PaymentId = p.Id, OperationKey = Guid.NewGuid() };
        if (db.Entry(attempt).State == EntityState.Detached) db.PaymentAttempts.Add(attempt);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
    }
    try
    {
        var external = await provider.CreateAttemptAsync(attempt.Id, attempt.OperationKey, ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct); await SetShop(db, link.Value.ShopId, ct);
        await db.PaymentAttempts.Where(x => x.Id == attempt.Id && x.ExternalId == null).ExecuteUpdateAsync(x => x.SetProperty(a => a.ExternalId, external), ct);
        await tx.CommitAsync(ct);
    }
    catch (Exception e) { app.Logger.LogWarning(e, "Provider outcome unknown attempt {AttemptId}", attempt.Id); }
    return Results.Redirect($"/checkout/{token}");
});

if (builder.Configuration.GetValue<bool>("Sandbox:Enabled"))
{
    app.MapPost("/checkout/{token}/simulate/{outcome}", async (string token, string outcome, PaymentsDbContext db, IHttpClientFactory clients, CancellationToken ct) =>
    {
        if (outcome is not ("succeeded" or "canceled")) return Results.BadRequest();
        var link = await Lookup(token, ct); if (link is null) return Results.NotFound();
        if (link.Value.ExpiresAt < DateTimeOffset.UtcNow) return Results.StatusCode(410);
        await using var tx = await db.Database.BeginTransactionAsync(ct); await SetShop(db, link.Value.ShopId, ct);
        var p = await db.Payments.AsNoTracking().SingleAsync(x => x.Id == link.Value.PaymentId, ct);
        var attempt = await db.PaymentAttempts.AsNoTracking().SingleOrDefaultAsync(x => x.PaymentId == p.Id, ct);
        await tx.CommitAsync(ct);
        if (p.Status != "pending" || attempt?.ExternalId is null) return Results.Conflict(new { error = "attempt_not_ready" });
        var incoming = new ProviderEvent(Guid.NewGuid().ToString("N"), link.Value.ShopId, attempt.Id, attempt.ExternalId, p.Amount, p.Currency, outcome);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(incoming, JsonSerializerOptions.Web);
        using var request = new HttpRequestMessage(HttpMethod.Post, internalBase + "/v1/provider/sandbox/events") { Content = new ByteArrayContent(bytes) };
        request.Headers.Add("X-Provider-Signature", SandboxCrypto.Hmac(providerSecret, bytes));
        request.Content.Headers.ContentType = new("application/json");
        var response = await clients.CreateClient().SendAsync(request, ct);
        return response.IsSuccessStatusCode ? Results.Redirect($"/checkout/{token}") : Results.StatusCode(502);
    });

    app.MapPost("/v1/provider/sandbox/events", async (HttpContext http, IPaymentProvider provider, PaymentsDbContext db, CancellationToken ct) =>
    {
        using var stream = new MemoryStream(); await http.Request.Body.CopyToAsync(stream, ct); var bytes = stream.ToArray();
        if (!provider.VerifyEvent(bytes, http.Request.Headers["X-Provider-Signature"])) return Results.Unauthorized();
        ProviderEvent? incoming;
        try { incoming = JsonSerializer.Deserialize<ProviderEvent>(bytes, JsonSerializerOptions.Web); }
        catch (JsonException) { return Results.BadRequest(); }
        if (incoming is null || incoming.ShopId == Guid.Empty || incoming.AttemptId == Guid.Empty ||
            incoming.EventId.Length is < 1 or > 200 || incoming.Amount <= 0 || incoming.Currency != "RUB" ||
            incoming.Outcome is not ("succeeded" or "canceled")) return Results.BadRequest();
        await using var lookup = new NpgsqlConnection(connection); await lookup.OpenAsync(ct);
        await using var find = new NpgsqlCommand("SELECT shop_id,payment_id FROM lookup_attempt_id($1)", lookup);
        find.Parameters.AddWithValue(incoming.AttemptId);
        await using var reader = await find.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return Results.BadRequest(new { error = "unknown_attempt" });
        var shopId = reader.GetGuid(0); var paymentId = reader.GetGuid(1);
        if (incoming.ShopId != shopId) return Results.BadRequest(new { error = "shop_mismatch" });
        await reader.DisposeAsync();
        var id = Guid.NewGuid();
        var envelope = new EventEnvelope(id, "provider.event.accepted", 1, DateTimeOffset.UtcNow,
            shopId, paymentId, http.TraceIdentifier, JsonSerializer.SerializeToElement(incoming, JsonSerializerOptions.Web));
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await SetShop(db, shopId, ct);
            db.ProviderInbox.Add(new ProviderInbox { Id = id, ShopId = shopId, PaymentId = paymentId,
                ProviderEventId = incoming.EventId, Payload = Encoding.UTF8.GetString(bytes), NextAt = DateTimeOffset.UtcNow });
            db.OutboxEvents.Add(new OutboxEvent { Id = id, ShopId = shopId, PaymentId = paymentId,
                Topic = KafkaContracts.ProviderEvents, Envelope = JsonSerializer.Serialize(envelope), OccurredAt = envelope.OccurredAt });
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
        { return Results.Accepted(); }
        app.Logger.LogInformation("Provider event accepted {EventId} shop {ShopId} payment {PaymentId} correlation {CorrelationId}",
            incoming.EventId, shopId, paymentId, http.TraceIdentifier);
        return Results.Accepted();
    });

}
app.Run();

string Token(Guid id) => SandboxCrypto.CheckoutToken(checkoutSecret, id);
PaymentResponse Response(Payment p) => new(p.Id, p.Status, p.Amount, p.Currency, publicBase + "/checkout/" + Token(p.Id));
static Task SetShop(PaymentsDbContext db, Guid shop, CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync($"SELECT set_config('app.shop_id',{shop.ToString()},true)", ct);
async Task<Guid?> Authenticate(HttpContext http, CancellationToken ct)
{
    var key = http.Request.Headers["X-Api-Key"].ToString();
    if (key.Length < 8) return null;
    var client = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
    client.DefaultRequestHeaders.Add("X-Internal-Key", serviceKey);
    using var response = await client.PostAsJsonAsync(developerApi + "/internal/v1/keys/resolve",
        new { KeyHash = SandboxCrypto.Sha256(key) }, ct);
    if (!response.IsSuccessStatusCode) return null;
    return (await response.Content.ReadFromJsonAsync<ResolvedShop>(cancellationToken: ct))?.ShopId;
}
bool InternalAuthorized(HttpContext http) => SandboxCrypto.Equal(
    SandboxCrypto.Sha256(serviceKey), SandboxCrypto.Sha256(http.Request.Headers["X-Internal-Key"].ToString()));
async Task<(Guid ShopId, Guid PaymentId, DateTimeOffset ExpiresAt)?> Lookup(string token, CancellationToken ct)
{
    if (token.Length != 64) return null;
    await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct);
    await using var cmd = new NpgsqlCommand("SELECT shop_id,payment_id,expires_at FROM lookup_checkout($1)", c); cmd.Parameters.AddWithValue(SandboxCrypto.Sha256(token));
    await using var r = await cmd.ExecuteReaderAsync(ct);
    return await r.ReadAsync(ct) ? (r.GetGuid(0), r.GetGuid(1), r.GetFieldValue<DateTimeOffset>(2)) : null;
}

public sealed record ResolvedShop(Guid ShopId, string Name);
