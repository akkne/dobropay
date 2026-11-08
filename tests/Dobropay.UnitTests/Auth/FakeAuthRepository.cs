using Dobropay.Auth.Persistence;

namespace Dobropay.UnitTests.Auth;

public sealed class FakeAuthRepository : IAuthRepository
{
    public List<Account> Accounts { get; } = [];
    public List<StaffSession> Sessions { get; } = [];

    public Task<Account?> FindAccountByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.SingleOrDefault(x => x.Email == normalizedEmail));

    public Task<Account?> FindAccountByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Accounts.SingleOrDefault(x => x.Id == id));

    public Task AddAccountAsync(Account account, CancellationToken cancellationToken)
    {
        Accounts.Add(account);
        return Task.CompletedTask;
    }

    public Task AddSessionAsync(StaffSession session, CancellationToken cancellationToken)
    {
        Sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task<StaffSession?> FindActiveSessionAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult(Sessions.SingleOrDefault(x => x.TokenHash == tokenHash && !x.IsRevoked && x.ExpiresAt > now));

    public Task<StaffSession?> FindActiveSessionByRefreshTokenAsync(string refreshTokenHash, DateTimeOffset now, CancellationToken cancellationToken) =>
        Task.FromResult(Sessions.SingleOrDefault(x => x.RefreshTokenHash == refreshTokenHash && !x.IsRevoked && x.RefreshExpiresAt > now));

    public Task<StaffSession?> FindSessionByTokenOrRefreshTokenAsync(string hash, CancellationToken cancellationToken) =>
        Task.FromResult(Sessions.SingleOrDefault(x => (x.TokenHash == hash || x.RefreshTokenHash == hash) && !x.IsRevoked));
}
