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
    try { await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct); return Results.Ok(new { status = "ready" }); }
    catch { return Results.StatusCode(503); }
});
app.MapPost("/v1/auth/login", async (LoginRequest body, CancellationToken ct) =>
{
    await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct);
    await using var cmd = new NpgsqlCommand("SELECT id,password_salt,password_hash FROM accounts WHERE email=$1", c);
    cmd.Parameters.AddWithValue(body.Email.Trim().ToLowerInvariant());
    Guid account; string salt, expected;
    await using (var row = await cmd.ExecuteReaderAsync(ct))
    {
        if (!await row.ReadAsync(ct)) return Results.Unauthorized();
        account = row.GetGuid(0); salt = row.GetString(1); expected = row.GetString(2);
    }
    byte[] hash;
    try { hash = Rfc2898DeriveBytes.Pbkdf2(body.Password, Convert.FromBase64String(salt), 210000, HashAlgorithmName.SHA256, 32); }
    catch (FormatException) { return Results.Unauthorized(); }
    if (!CryptographicOperations.FixedTimeEquals(hash, Convert.FromBase64String(expected))) return Results.Unauthorized();
    var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    await using var insert = new NpgsqlCommand("INSERT INTO staff_sessions(token_hash,account_id,expires_at) VALUES ($1,$2,now()+interval '8 hours')", c);
    insert.Parameters.AddWithValue(tokenHash); insert.Parameters.AddWithValue(account);
    await insert.ExecuteNonQueryAsync(ct);
    return Results.Ok(new { access_token = token, token_type = "Bearer", expires_in = 28800, account_id = account });
});
app.MapPost("/internal/v1/sessions/resolve", async (HttpContext http, ResolveSession body, CancellationToken ct) =>
{
    if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(serviceKey)),
        SHA256.HashData(Encoding.UTF8.GetBytes(http.Request.Headers["X-Internal-Key"].ToString())))) return Results.Unauthorized();
    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body.Token))).ToLowerInvariant();
    await using var c = new NpgsqlConnection(connection); await c.OpenAsync(ct);
    await using var cmd = new NpgsqlCommand("SELECT account_id FROM staff_sessions WHERE token_hash=$1 AND expires_at>now()", c);
    cmd.Parameters.AddWithValue(hash);
    return await cmd.ExecuteScalarAsync(ct) is Guid id ? Results.Ok(new { account_id = id }) : Results.NotFound();
});

app.Run();

public sealed record LoginRequest(string Email, string Password);
public sealed record ResolveSession(string Token);
