using Npgsql;

namespace Dobropay.Messaging.Database;

public interface IDbSession : IAsyncDisposable
{
    NpgsqlConnection Connection { get; }
    NpgsqlTransaction? Transaction { get; }
    Task BeginTransactionAsync(CancellationToken cancellationToken);
    Task BeginShopTransactionAsync(Guid shopId, CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
    Task<int> ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken, params object?[] parameters);
    Task<object?> ExecuteScalarAsync(string sql, CancellationToken cancellationToken, params object?[] parameters);
    NpgsqlCommand CreateCommand(string sql, params object?[] parameters);
}
