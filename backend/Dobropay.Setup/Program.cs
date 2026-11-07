using Dobropay.Contracts.Security;
using Dobropay.Setup.Configuration;
using Dobropay.Setup.Services;

var options = SetupOptions.LoadFromEnvironment();
var seeder = new EfCoreDatabaseSeeder(options, new Sha256HashProvider());
await seeder.MigrateAndSeedAllAsync(CancellationToken.None);
Console.WriteLine("Five isolated service databases migrated and seeded via EF Core.");
