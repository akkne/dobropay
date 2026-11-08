using Dobropay.DeveloperApi.Services;

namespace Dobropay.UnitTests.DeveloperApi;

public sealed class FakeAuthSessionResolver : IAuthSessionResolver
{
    private readonly Dictionary<string, Guid> _sessions = new(StringComparer.Ordinal);

    public void SetSession(string token, Guid accountId)
    {
        _sessions[token] = accountId;
    }

    public Task<Guid?> ResolveAccountIdAsync(string token, CancellationToken cancellationToken)
    {
        if (_sessions.TryGetValue(token, out var accountId))
        {
            return Task.FromResult<Guid?>(accountId);
        }

        return Task.FromResult<Guid?>(null);
    }
}
