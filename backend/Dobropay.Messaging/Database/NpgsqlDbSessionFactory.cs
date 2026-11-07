using Npgsql;

namespace Dobropay.Messaging.Database;

public sealed class NpgsqlDbSessionFactory : IDbSessionFactory, IAsyncDisposable
{
    private readonly NpgsqlDataSource dataSource;

    public NpgsqlDbSessionFactory(string connectionString) =>
        dataSource = NpgsqlDataSource.Create(connectionString);

    public async Task<IDbSession> OpenSessionAsync(CancellationToken cancellationToken)
    {
        var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return new NpgsqlDbSession(connection);
    }

    public async Task<IDbSession> OpenShopSessionAsync(Guid shopId, CancellationToken cancellationToken)
    {
        var session = await OpenSessionAsync(cancellationToken);
        try
        {
            await session.BeginShopTransactionAsync(shopId, cancellationToken);
            return session;
        }
        catch
        {
            await session.DisposeAsync();
            throw;
        }
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ValueTask DisposeAsync() => dataSource.DisposeAsync();
}
