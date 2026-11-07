namespace Dobropay.Auth.Persistence;

public interface IAuthRepository
{
    Task<Account?> FindAccountByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task AddSessionAsync(StaffSession session, CancellationToken cancellationToken);
    Task<StaffSession?> FindActiveSessionAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken);
}
