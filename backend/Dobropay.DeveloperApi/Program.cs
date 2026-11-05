using System.Security.Cryptography;
using System.Text;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Runtime") ?? throw new InvalidOperationException("ConnectionStrings:Runtime missing");
var serviceKey = builder.Configuration["INTERNAL_SERVICE_KEY"] ?? throw new InvalidOperationException("INTERNAL_SERVICE_KEY missing");
var app = builder.Build();

app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (CancellationToken ct) =>
{
    try { await using var db = new NpgsqlConnection(connection); await db.OpenAsync(ct); return Results.Ok(new { status = "ready" }); }
    catch { return Results.StatusCode(503); }
});

app.MapPost("/internal/v1/keys/resolve", async (HttpContext http, ResolveKeyRequest body, CancellationToken ct) =>
{
    if (!Authorized(http)) return Results.Unauthorized();
    if (body.KeyHash.Length != 64) return Results.BadRequest();
    await using var db = new NpgsqlConnection(connection); await db.OpenAsync(ct);
    await using var cmd = new NpgsqlCommand("SELECT shop_id,shop_name FROM resolve_api_key($1)", db);
    cmd.Parameters.AddWithValue(body.KeyHash);
    await using var row = await cmd.ExecuteReaderAsync(ct);
    if (!await row.ReadAsync(ct)) return Results.NotFound();
    return Results.Ok(new ResolvedShop(row.GetGuid(0), row.GetString(1)));
});

app.MapGet("/internal/v1/shops/{shopId:guid}", async (HttpContext http, Guid shopId, CancellationToken ct) =>
{
    if (!Authorized(http)) return Results.Unauthorized();
    await using var db = new NpgsqlConnection(connection); await db.OpenAsync(ct);
    await using var tx = await db.BeginTransactionAsync(ct);
    await using (var set = new NpgsqlCommand("SELECT set_config('app.shop_id',$1,true)", db, tx))
    { set.Parameters.AddWithValue(shopId.ToString()); await set.ExecuteNonQueryAsync(ct); }
    await using var cmd = new NpgsqlCommand("SELECT name FROM shops WHERE id=$1", db, tx);
    cmd.Parameters.AddWithValue(shopId);
    var name = await cmd.ExecuteScalarAsync(ct) as string;
    await tx.CommitAsync(ct);
    return name is null ? Results.NotFound() : Results.Ok(new ResolvedShop(shopId, name));
});

app.MapGet("/internal/v1/memberships/{accountId:guid}/{shopId:guid}", async (HttpContext http, Guid accountId, Guid shopId, CancellationToken ct) =>
{
    if (!Authorized(http)) return Results.Unauthorized();
    await using var db = new NpgsqlConnection(connection); await db.OpenAsync(ct);
    await using var tx = await db.BeginTransactionAsync(ct);
    await using (var set = new NpgsqlCommand("SELECT set_config('app.shop_id',$1,true)", db, tx))
    { set.Parameters.AddWithValue(shopId.ToString()); await set.ExecuteNonQueryAsync(ct); }
    await using var cmd = new NpgsqlCommand("SELECT role FROM shop_memberships WHERE shop_id=$1 AND account_id=$2", db, tx);
    cmd.Parameters.AddWithValue(shopId); cmd.Parameters.AddWithValue(accountId);
    var role = await cmd.ExecuteScalarAsync(ct) as string;
    await tx.CommitAsync(ct);
    return role is null ? Results.NotFound() : Results.Ok(new { shop_id = shopId, role });
});

app.Run();

bool Authorized(HttpContext http)
{
    var supplied = http.Request.Headers["X-Internal-Key"].ToString();
    return CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(serviceKey)), SHA256.HashData(Encoding.UTF8.GetBytes(supplied)));
}

public sealed record ResolveKeyRequest(string KeyHash);
public sealed record ResolvedShop(Guid ShopId, string Name);
