using Dobropay.Contracts.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dobropay.Notifications.Persistence;

public sealed class NotificationsDesignTimeFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConfigurationKeyNames.DesignTimeConnectionEnv)
            ?? throw new InvalidOperationException($"Missing environment variable: {ConfigurationKeyNames.DesignTimeConnectionEnv}");

        return new NotificationsDbContext(
            new DbContextOptionsBuilder<NotificationsDbContext>()
                .UseNpgsql(connectionString)
                .Options);
    }
}
