using Dobropay.Auth.Persistence;
using Dobropay.DeveloperApi.Persistence;
using Dobropay.Notifications.Persistence;
using Dobropay.Payments.Persistence;
using Dobropay.Receipts.Persistence;
using Dobropay.Setup.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Dobropay.Setup.Services;

public sealed class EfCoreDatabaseSeeder(SetupOptions options) : IDatabaseSeeder
{
    public async Task MigrateAndSeedAllAsync(CancellationToken cancellationToken)
    {
        await using (var context = new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql(options.AuthDbConnectionString).Options))
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        await using (var context = new MerchantDbContext(new DbContextOptionsBuilder<MerchantDbContext>()
            .UseNpgsql(options.MerchantDevDbConnectionString).Options))
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        await using (var context = new PaymentsDbContext(new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql(options.PaymentsDbConnectionString).Options))
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        await using (var context = new NotificationsDbContext(new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(options.NotificationsDbConnectionString).Options))
        {
            await context.Database.MigrateAsync(cancellationToken);
        }

        await using (var context = new ReceiptsDbContext(new DbContextOptionsBuilder<ReceiptsDbContext>()
            .UseNpgsql(options.ReceiptsDbConnectionString).Options))
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
    }
}
