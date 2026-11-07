using Microsoft.EntityFrameworkCore;

namespace Dobropay.Messaging.Database;

public sealed class EfDatabaseHealthChecker<TContext>(TContext dbContext) : IDatabaseHealthChecker
    where TContext : DbContext
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken) =>
        dbContext.Database.CanConnectAsync(cancellationToken);
}
