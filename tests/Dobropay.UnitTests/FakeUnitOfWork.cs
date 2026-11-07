using Dobropay.Messaging.Database;

namespace Dobropay.UnitTests;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCalls { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCalls += 1;
        return Task.FromResult(1);
    }
}
