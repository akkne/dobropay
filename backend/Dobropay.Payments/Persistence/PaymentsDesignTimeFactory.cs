using Dobropay.Contracts.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dobropay.Payments.Persistence;

public sealed class PaymentsDesignTimeFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConfigurationKeyNames.DesignTimeConnectionEnv)
            ?? throw new InvalidOperationException($"Missing environment variable: {ConfigurationKeyNames.DesignTimeConnectionEnv}");

        return new PaymentsDbContext(
            new DbContextOptionsBuilder<PaymentsDbContext>()
                .UseNpgsql(connectionString)
                .Options);
    }
}
