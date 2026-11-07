using Dobropay.Contracts.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dobropay.Auth.Persistence;

public sealed class AuthDesignTimeFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConfigurationKeyNames.DesignTimeConnectionEnv)
            ?? throw new InvalidOperationException($"Missing environment variable: {ConfigurationKeyNames.DesignTimeConnectionEnv}");

        return new AuthDbContext(
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseNpgsql(connectionString)
                .Options);
    }
}
