using StackExchange.Redis;

namespace Dobropay.Messaging.Redis;

public sealed class RedisConnectionProvider(RedisOptions options) : IRedisConnectionProvider
{
    private readonly SemaphoreSlim mutex = new(1, 1);
    private IConnectionMultiplexer? multiplexer;

    public async Task<IDatabase> GetDatabaseAsync()
    {
        if (multiplexer is { IsConnected: true })
        {
            return multiplexer.GetDatabase();
        }

        await mutex.WaitAsync();
        try
        {
            if (multiplexer is not { IsConnected: true })
            {
                multiplexer?.Dispose();
                multiplexer = await ConnectionMultiplexer.ConnectAsync(options.ConnectionString);
            }

            return multiplexer.GetDatabase();
        }
        finally
        {
            mutex.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (multiplexer is not null)
        {
            await multiplexer.DisposeAsync();
        }

        mutex.Dispose();
    }
}
