using Microsoft.EntityFrameworkCore;

namespace Dobropay.Auth.Persistence;

public sealed class AuthRepository(AuthDbContext dbContext) : IAuthRepository
{
    public Task<Account?> FindAccountByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

    public Task<Account?> FindAccountByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAccountAsync(Account account, CancellationToken cancellationToken) =>
        await dbContext.Accounts.AddAsync(account, cancellationToken);

    public async Task AddSessionAsync(StaffSession session, CancellationToken cancellationToken) =>
        await dbContext.StaffSessions.AddAsync(session, cancellationToken);

    public Task<StaffSession?> FindActiveSessionAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.StaffSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash && !x.IsRevoked && x.ExpiresAt > now, cancellationToken);

    public Task<StaffSession?> FindActiveSessionByRefreshTokenAsync(string refreshTokenHash, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.StaffSessions
            .SingleOrDefaultAsync(x => x.RefreshTokenHash == refreshTokenHash && !x.IsRevoked && x.RefreshExpiresAt > now, cancellationToken);

    public Task<StaffSession?> FindSessionByTokenOrRefreshTokenAsync(string hash, CancellationToken cancellationToken) =>
        dbContext.StaffSessions
            .SingleOrDefaultAsync(x => (x.TokenHash == hash || x.RefreshTokenHash == hash) && !x.IsRevoked, cancellationToken);
}
