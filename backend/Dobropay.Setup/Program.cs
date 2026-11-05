using System.Security.Cryptography;
using System.Text;
using Dobropay.Auth.Persistence;
using Dobropay.DeveloperApi.Persistence;
using Dobropay.Notifications.Persistence;
using Dobropay.Payments.Persistence;
using Dobropay.Receipts.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

string required(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException($"Missing {name}");
var password = required("POSTGRES_PASSWORD");
var admin = new NpgsqlConnectionStringBuilder { Host = "db", Database = "postgres", Username = "postgres", Password = password };
var specs = new[]
{
    (Database: "auth_db", Prefix: "auth"),
    (Database: "merchant_dev_db", Prefix: "developer_api"),
    (Database: "payments_db", Prefix: "payments"),
    (Database: "notifications_db", Prefix: "notifications"),
    (Database: "receipts_db", Prefix: "receipts")
};
await using (var root = new NpgsqlConnection(admin.ConnectionString))
{
    await root.OpenAsync();
    foreach (var (database, prefix) in specs)
    {
        var owner = prefix + "_owner";
        var migrator = prefix + "_migrator";
        var runtime = prefix + "_runtime";
        await EnsureRole(root, owner, null, false);
        await EnsureRole(root, migrator, required(prefix.ToUpperInvariant() + "_MIGRATOR_PASSWORD"), true);
        await EnsureRole(root, runtime, required(prefix.ToUpperInvariant() + "_RUNTIME_PASSWORD"), true);
        await Execute(root, $"GRANT {owner} TO {migrator}");
        if (!await Exists(root, "SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname=$1)", database))
            await Execute(root, $"CREATE DATABASE {database}");
        await Execute(root, $"REVOKE CONNECT ON DATABASE {database} FROM PUBLIC");
        await Execute(root, $"GRANT CONNECT ON DATABASE {database} TO {runtime},{migrator}");
    }
}
foreach (var (database, prefix) in specs)
{
    admin.Database = database;
    await using var connection = new NpgsqlConnection(admin.ConnectionString);
    await connection.OpenAsync();
    await Execute(connection, "REVOKE CREATE ON SCHEMA public FROM PUBLIC");
    await Execute(connection, $"GRANT USAGE ON SCHEMA public TO {prefix}_runtime,{prefix}_migrator");
    await Execute(connection, $"GRANT CREATE ON SCHEMA public TO {prefix}_migrator");
}
await using (var db = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>().UseNpgsql(For("auth_db")).Options))
    await db.Database.MigrateAsync();
await using (var db = new MerchantDbContext(new DbContextOptionsBuilder<MerchantDbContext>().UseNpgsql(For("merchant_dev_db")).Options))
    await db.Database.MigrateAsync();
await using (var db = new PaymentsDbContext(new DbContextOptionsBuilder<PaymentsDbContext>().UseNpgsql(For("payments_db")).Options))
    await db.Database.MigrateAsync();
await using (var db = new NotificationsDbContext(new DbContextOptionsBuilder<NotificationsDbContext>().UseNpgsql(For("notifications_db")).Options))
    await db.Database.MigrateAsync();
await using (var db = new ReceiptsDbContext(new DbContextOptionsBuilder<ReceiptsDbContext>().UseNpgsql(For("receipts_db")).Options))
    await db.Database.MigrateAsync();

var staffId = Guid.Parse("33333333-3333-4333-8333-333333333333");
var a = Guid.Parse("11111111-1111-4111-8111-111111111111");
var b = Guid.Parse("22222222-2222-4222-8222-222222222222");
var salt = SHA256.HashData(Encoding.UTF8.GetBytes("dobropay-local-demo-salt"));
var staffHash = Rfc2898DeriveBytes.Pbkdf2(required("DEMO_STAFF_PASSWORD"), salt, 210000, HashAlgorithmName.SHA256, 32);
await using (var db = new NpgsqlConnection(For("auth_db")))
{
    await db.OpenAsync();
    await using var cmd = new NpgsqlCommand("""
        INSERT INTO accounts(id,email,password_salt,password_hash) VALUES ($1,$2,$3,$4)
        ON CONFLICT(id) DO UPDATE SET email=EXCLUDED.email,password_salt=EXCLUDED.password_salt,password_hash=EXCLUDED.password_hash
        """, db);
    cmd.Parameters.AddWithValue(staffId); cmd.Parameters.AddWithValue("demo@dobropay.local");
    cmd.Parameters.AddWithValue(Convert.ToBase64String(salt)); cmd.Parameters.AddWithValue(Convert.ToBase64String(staffHash));
    await cmd.ExecuteNonQueryAsync();
}
await using (var db = new NpgsqlConnection(For("merchant_dev_db")))
{
    await db.OpenAsync();
    foreach (var (id, name, key, credential) in new[]
    {
        (a, "Мастерская Добро", required("DEMO_KEY_A"), Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa")),
        (b, "Второй магазин", required("DEMO_KEY_B"), Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"))
    })
    {
        await using var cmd = new NpgsqlCommand("""
            INSERT INTO shops(id,name) VALUES ($1,$2) ON CONFLICT(id) DO UPDATE SET name=EXCLUDED.name;
            INSERT INTO api_credentials(id,shop_id,key_hash) VALUES ($3,$1,$4)
              ON CONFLICT(id) DO UPDATE SET key_hash=EXCLUDED.key_hash;
            INSERT INTO shop_memberships(shop_id,account_id,role) VALUES ($1,$5,'owner')
              ON CONFLICT(shop_id,account_id) DO NOTHING
            """, db);
        cmd.Parameters.AddWithValue(id); cmd.Parameters.AddWithValue(name); cmd.Parameters.AddWithValue(credential);
        cmd.Parameters.AddWithValue(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant());
        cmd.Parameters.AddWithValue(staffId); await cmd.ExecuteNonQueryAsync();
    }
}
await using (var db = new NpgsqlConnection(For("notifications_db")))
{
    await db.OpenAsync();
    foreach (var (shop, id) in new[] { (a, Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc")), (b, Guid.Parse("dddddddd-dddd-4ddd-8ddd-dddddddddddd")) })
    {
        await using var cmd = new NpgsqlCommand("""
            INSERT INTO webhook_subscriptions(id,shop_id,url,signing_secret) VALUES ($1,$2,$3,$4)
            ON CONFLICT(id) DO UPDATE SET url=EXCLUDED.url,signing_secret=EXCLUDED.signing_secret
            """, db);
        cmd.Parameters.AddWithValue(id); cmd.Parameters.AddWithValue(shop);
        cmd.Parameters.AddWithValue(required("DEMO_WEBHOOK_URL")); cmd.Parameters.AddWithValue(required("WEBHOOK_SECRET"));
        await cmd.ExecuteNonQueryAsync();
    }
}
Console.WriteLine("Five code-first databases migrated; two local shops and one staff account seeded.");

string For(string database) { admin.Database = database; return admin.ConnectionString; }
static async Task<bool> Exists(NpgsqlConnection c, string sql, string value)
{
    await using var cmd = new NpgsqlCommand(sql, c); cmd.Parameters.AddWithValue(value);
    return (bool)(await cmd.ExecuteScalarAsync())!;
}
static async Task Execute(NpgsqlConnection c, string sql)
{
    await using var cmd = new NpgsqlCommand(sql, c); await cmd.ExecuteNonQueryAsync();
}
static async Task EnsureRole(NpgsqlConnection c, string name, string? password, bool login)
{
    if (!await Exists(c, "SELECT EXISTS(SELECT 1 FROM pg_roles WHERE rolname=$1)", name))
    {
        await using var quote = new NpgsqlCommand("SELECT format('CREATE ROLE %I ' || $2 || ' PASSWORD %L', $1, $3)", c);
        quote.Parameters.AddWithValue(name); quote.Parameters.AddWithValue(login ? "LOGIN" : "NOLOGIN");
        quote.Parameters.AddWithValue(password ?? "");
        await Execute(c, (string)(await quote.ExecuteScalarAsync())!);
    }
    else if (login)
    {
        await using var quote = new NpgsqlCommand("SELECT format('ALTER ROLE %I PASSWORD %L', $1, $2)", c);
        quote.Parameters.AddWithValue(name); quote.Parameters.AddWithValue(password!);
        await Execute(c, (string)(await quote.ExecuteScalarAsync())!);
    }
}
