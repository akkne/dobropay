using System.Security.Cryptography;
using Dobropay.Auth.Models;
using Dobropay.Auth.Persistence;
using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Messaging.Database;

namespace Dobropay.Auth.Services;

public sealed class AuthService(
    IAuthRepository repository,
    IUnitOfWork unitOfWork,
    IHashProvider hashProvider) : IAuthService
{
    private const int Pbkdf2Iterations = 210000;
    private const int HashByteLength = 32;
    private const int SessionLifetimeSeconds = 28800;

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var account = await repository.FindAccountByEmailAsync(normalizedEmail, cancellationToken);
        if (account is null)
        {
            return null;
        }

        byte[] computedHash;
        byte[] expectedHash;
        try
        {
            var saltBytes = Convert.FromBase64String(account.PasswordSalt);
            expectedHash = Convert.FromBase64String(account.PasswordHash);
            computedHash = Rfc2898DeriveBytes.Pbkdf2(
                request.Password,
                saltBytes,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256,
                HashByteLength);
        }
        catch (FormatException)
        {
            return null;
        }

        if (!CryptographicOperations.FixedTimeEquals(computedHash, expectedHash))
        {
            return null;
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(HashByteLength)).ToLowerInvariant();
        var tokenHash = hashProvider.ComputeSha256Hex(token);
        var session = new StaffSession
        {
            TokenHash = tokenHash,
            AccountId = account.Id,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(SessionLifetimeSeconds)
        };

        await repository.AddSessionAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(token, AuthSchemeNames.Bearer, SessionLifetimeSeconds, account.Id);
    }

    public async Task<ResolveSessionResponse?> ResolveSessionAsync(
        ResolveSessionRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = hashProvider.ComputeSha256Hex(request.Token);
        var session = await repository.FindActiveSessionAsync(tokenHash, DateTimeOffset.UtcNow, cancellationToken);
        return session is null ? null : new ResolveSessionResponse(session.AccountId);
    }
}
