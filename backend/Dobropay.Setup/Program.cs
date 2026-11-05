using Dobropay.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

string required(string key) => Environment.GetEnvironmentVariable(key) ?? throw new InvalidOperationException($"Missing {key}");
var connection = required("ConnectionStrings__Migration");
var keyA = required("DEMO_KEY_A");
var keyB = required("DEMO_KEY_B");
var webhookSecret = required("WEBHOOK_SECRET");
var receiver = required("DEMO_WEBHOOK_URL");
await using var db = new DobropayDbContext(new DbContextOptionsBuilder<DobropayDbContext>().UseNpgsql(connection).Options);
await db.Database.MigrateAsync();
await using (var conn = new NpgsqlConnection(connection))
{
    await conn.OpenAsync();
    foreach (var (role, password) in new[] { ("dobropay_api", required("API_DB_PASSWORD")), ("dobropay_worker", required("WORKER_DB_PASSWORD")), ("dobropay_migrator", required("MIGRATOR_DB_PASSWORD")) })
    {
        await using var quote = new NpgsqlCommand("SELECT format('ALTER ROLE ' || $1 || ' PASSWORD %L', $2)", conn);
        quote.Parameters.AddWithValue(role); quote.Parameters.AddWithValue(password);
        var sql = (string)(await quote.ExecuteScalarAsync())!;
        await using var alter = new NpgsqlCommand(sql, conn);
        await alter.ExecuteNonQueryAsync();
    }
}
var a = Guid.Parse("11111111-1111-4111-8111-111111111111");
var b = Guid.Parse("22222222-2222-4222-8222-222222222222");
await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO shops(id,name) VALUES ({a},'Мастерская Добро'),({b},'Второй магазин') ON CONFLICT(id) DO UPDATE SET name=EXCLUDED.name");
await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO api_credentials(id,shop_id,key_hash) VALUES ({Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa")},{a},{SandboxCrypto.Sha256(keyA)}),({Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb")},{b},{SandboxCrypto.Sha256(keyB)}) ON CONFLICT(id) DO UPDATE SET key_hash=EXCLUDED.key_hash");
await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO webhook_subscriptions(id,shop_id,url,signing_secret) VALUES ({Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc")},{a},{receiver},{webhookSecret}),({Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd")},{b},{receiver},{webhookSecret}) ON CONFLICT(id) DO UPDATE SET url=EXCLUDED.url,signing_secret=EXCLUDED.signing_secret");
Console.WriteLine("Migrations applied; two local demo shops seeded.");
