namespace Dobropay.Messaging.Redis;

public interface IRateLimiter
{
    Task<bool> TryAcquireAsync(string key, TimeSpan window);
}
