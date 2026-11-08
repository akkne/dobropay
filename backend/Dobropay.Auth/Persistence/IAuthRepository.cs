namespace Dobropay.Auth.Persistence;

public interface IAuthRepository
{
    Task<Account?> FindAccountByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<Account?> FindAccountByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAccountAsync(Account account, CancellationToken cancellationToken);
    Task AddSessionAsync(StaffSession session, CancellationToken cancellationToken);
    Task<StaffSession?> FindActiveSessionAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken);
    Task<StaffSession?> FindActiveSessionByRefreshTokenAsync(string refreshTokenHash, DateTimeOffset now, CancellationToken cancellationToken);
    Task<StaffSession?> FindSessionByTokenOrRefreshTokenAsync(string hash, CancellationToken cancellationToken);
}
