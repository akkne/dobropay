using Dobropay.Contracts.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dobropay.Receipts.Persistence;

public sealed class ReceiptsDesignTimeFactory : IDesignTimeDbContextFactory<ReceiptsDbContext>
{
    public ReceiptsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConfigurationKeyNames.DesignTimeConnectionEnv)
            ?? throw new InvalidOperationException($"Missing environment variable: {ConfigurationKeyNames.DesignTimeConnectionEnv}");

        return new ReceiptsDbContext(
            new DbContextOptionsBuilder<ReceiptsDbContext>()
                .UseNpgsql(connectionString)
                .Options);
    }
}
