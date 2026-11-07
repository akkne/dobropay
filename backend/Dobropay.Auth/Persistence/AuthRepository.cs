using Microsoft.EntityFrameworkCore;

namespace Dobropay.Auth.Persistence;

public sealed class AuthRepository(AuthDbContext dbContext) : IAuthRepository
{
    public Task<Account?> FindAccountByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

    public async Task AddSessionAsync(StaffSession session, CancellationToken cancellationToken) =>
        await dbContext.StaffSessions.AddAsync(session, cancellationToken);

    public Task<StaffSession?> FindActiveSessionAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.StaffSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash && x.ExpiresAt > now, cancellationToken);
}
