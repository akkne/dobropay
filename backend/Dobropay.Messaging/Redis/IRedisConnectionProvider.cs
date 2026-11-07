using StackExchange.Redis;

namespace Dobropay.Messaging.Redis;

public interface IRedisConnectionProvider : IAsyncDisposable
{
    Task<IDatabase> GetDatabaseAsync();
}
