using Microsoft.EntityFrameworkCore;

namespace Dobropay.Messaging.Database;

public sealed class EfUnitOfWork<TContext>(TContext dbContext) : IUnitOfWork
    where TContext : DbContext
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
