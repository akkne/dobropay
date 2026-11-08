using Dobropay.Messaging.Redis;

namespace Dobropay.UnitTests;

public sealed class FakeRateLimiter : IRateLimiter
{
    public bool Allowed { get; set; } = true;

    public Task<bool> TryAcquireAsync(string key, TimeSpan window) => Task.FromResult(Allowed);
}
