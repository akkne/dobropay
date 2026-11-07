using Dobropay.Contracts.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dobropay.DeveloperApi.Persistence;

public sealed class MerchantDesignTimeFactory : IDesignTimeDbContextFactory<MerchantDbContext>
{
    public MerchantDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConfigurationKeyNames.DesignTimeConnectionEnv)
            ?? throw new InvalidOperationException($"Missing environment variable: {ConfigurationKeyNames.DesignTimeConnectionEnv}");

        return new MerchantDbContext(
            new DbContextOptionsBuilder<MerchantDbContext>()
                .UseNpgsql(connectionString)
                .Options);
    }
}
