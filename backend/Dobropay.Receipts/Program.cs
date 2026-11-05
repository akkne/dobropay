using System.Security.Cryptography;
using System.Text;
using Dobropay.Receipts.Workers;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Runtime") ?? throw new InvalidOperationException("ConnectionStrings:Runtime missing");
var serviceKey = builder.Configuration["INTERNAL_SERVICE_KEY"] ?? throw new InvalidOperationException("INTERNAL_SERVICE_KEY missing");
builder.Services.AddHttpClient();
builder.Services.AddHostedService<ReceiptWorker>();
var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (CancellationToken ct) =>
{
    try { await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct); return Results.Ok(new { status = "ready" }); }
    catch { return Results.StatusCode(503); }
});
app.MapGet("/internal/v1/shops/{shopId:guid}/receipts", async (HttpContext http, Guid shopId, CancellationToken ct) =>
{
    if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(serviceKey)),
        SHA256.HashData(Encoding.UTF8.GetBytes(http.Request.Headers["X-Internal-Key"].ToString())))) return Results.Unauthorized();
    await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct);
    await using var tx = await c.BeginTransactionAsync(ct);
    await using (var set = new NpgsqlCommand("SELECT set_config('app.shop_id',$1,true)", c, tx))
    { set.Parameters.AddWithValue(shopId.ToString()); await set.ExecuteNonQueryAsync(ct); }
    await using var cmd = new NpgsqlCommand("SELECT coalesce(json_agg(json_build_object('id',id,'payment_id',payment_id,'amount',amount,'currency',currency,'email_state',email_state))::text,'[]') FROM demo_receipts WHERE shop_id=$1", c, tx);
    cmd.Parameters.AddWithValue(shopId);
    var json = (string?)await cmd.ExecuteScalarAsync(ct) ?? "[]";
    await tx.CommitAsync(ct);
    return Results.Content(json, "application/json");
});

app.Run();
