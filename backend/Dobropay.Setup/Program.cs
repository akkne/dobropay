using Dobropay.Setup.Configuration;
using Dobropay.Setup.Services;

var options = SetupOptions.LoadFromEnvironment();
var seeder = new EfCoreDatabaseSeeder(options);
await seeder.MigrateAndSeedAllAsync(CancellationToken.None);
Console.WriteLine("Five isolated service databases migrated via EF Core.");
