using Dobropay.Contracts.Constants;
using Npgsql;

namespace Dobropay.Messaging.Database;

public sealed class NpgsqlDbSession(NpgsqlConnection connection) : IDbSession
{
    public NpgsqlConnection Connection { get; } = connection;
    public NpgsqlTransaction? Transaction { get; private set; }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken) =>
        Transaction = await Connection.BeginTransactionAsync(cancellationToken);

    public async Task BeginShopTransactionAsync(Guid shopId, CancellationToken cancellationToken)
    {
        Transaction = await Connection.BeginTransactionAsync(cancellationToken);
        await ExecuteNonQueryAsync(PostgresSessionVariables.SetShopIdSql, cancellationToken, shopId.ToString());
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (Transaction is not null)
        {
            await Transaction.CommitAsync(cancellationToken);
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken, params object?[] parameters)
    {
        await using var command = CreateCommand(sql, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<object?> ExecuteScalarAsync(string sql, CancellationToken cancellationToken, params object?[] parameters)
    {
        await using var command = CreateCommand(sql, parameters);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    public NpgsqlCommand CreateCommand(string sql, params object?[] parameters)
    {
        var command = new NpgsqlCommand(sql, Connection, Transaction);
        foreach (var parameter in parameters)
        {
            if (parameter is NpgsqlParameter npgsqlParameter)
            {
                command.Parameters.Add(npgsqlParameter);
            }
            else
            {
                command.Parameters.AddWithValue(parameter ?? DBNull.Value);
            }
        }
        return command;
    }

    public async ValueTask DisposeAsync()
    {
        if (Transaction is not null)
        {
            await Transaction.DisposeAsync();
        }
        await Connection.DisposeAsync();
    }
}
