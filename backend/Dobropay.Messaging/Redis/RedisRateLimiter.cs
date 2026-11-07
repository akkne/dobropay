using StackExchange.Redis;

namespace Dobropay.Messaging.Redis;

public sealed class RedisRateLimiter(IRedisConnectionProvider connectionProvider) : IRateLimiter
{
    public async Task<bool> TryAcquireAsync(string key, TimeSpan window)
    {
        var database = await connectionProvider.GetDatabaseAsync();
        return await database.StringSetAsync(key, "1", window, When.NotExists);
    }
}
