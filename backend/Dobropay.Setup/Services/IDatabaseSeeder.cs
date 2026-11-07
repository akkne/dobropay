namespace Dobropay.Setup.Services;

public interface IDatabaseSeeder
{
    Task MigrateAndSeedAllAsync(CancellationToken cancellationToken);
}
