using System.ComponentModel.DataAnnotations;
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
    private const int SaltByteLength = 32;
    private const int AccessTokenLifetimeSeconds = 28800;
    private const int MinPasswordLength = 8;
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    public async Task<RegisterResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            !new EmailAddressAttribute().IsValid(request.Email.Trim()) ||
            !request.Email.Contains('@'))
        {
            return RegisterResult.InvalidEmail();
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < MinPasswordLength)
        {
            return RegisterResult.InvalidPassword();
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existingAccount = await repository.FindAccountByEmailAsync(normalizedEmail, cancellationToken);
        if (existingAccount is not null)
        {
            return RegisterResult.AlreadyExists();
        }

        var saltBytes = RandomNumberGenerator.GetBytes(SaltByteLength);
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            request.Password,
            saltBytes,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            HashByteLength);

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordSalt = Convert.ToBase64String(saltBytes),
            PasswordHash = Convert.ToBase64String(hashBytes)
        };

        await repository.AddAccountAsync(account, cancellationToken);
        var sessionResponse = await CreateSessionAsync(account.Id, cancellationToken);

        return RegisterResult.Success(sessionResponse);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
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

        return await CreateSessionAsync(account.Id, cancellationToken);
    }

    public async Task<AuthResponse?> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return null;
        }

        var refreshTokenHash = hashProvider.ComputeSha256Hex(request.RefreshToken.Trim());
        var session = await repository.FindActiveSessionByRefreshTokenAsync(refreshTokenHash, DateTimeOffset.UtcNow, cancellationToken);
        if (session is null)
        {
            return null;
        }

        session.IsRevoked = true;

        return await CreateSessionAsync(session.AccountId, cancellationToken);
    }

    public async Task<bool> LogoutAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var hash = hashProvider.ComputeSha256Hex(token.Trim());
        var session = await repository.FindSessionByTokenOrRefreshTokenAsync(hash, cancellationToken);
        if (session is null)
        {
            return false;
        }

        session.IsRevoked = true;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AuthMeResponse?> GetMeAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenHash = hashProvider.ComputeSha256Hex(token.Trim());
        var session = await repository.FindActiveSessionAsync(tokenHash, DateTimeOffset.UtcNow, cancellationToken);
        if (session is null)
        {
            return null;
        }

        var account = await repository.FindAccountByIdAsync(session.AccountId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        return new AuthMeResponse(account.Id, account.Email);
    }

    public async Task<ResolveSessionResponse?> ResolveSessionAsync(
        ResolveSessionRequest request,
        CancellationToken cancellationToken)
    {
        var tokenHash = hashProvider.ComputeSha256Hex(request.Token);
        var session = await repository.FindActiveSessionAsync(tokenHash, DateTimeOffset.UtcNow, cancellationToken);
        return session is null ? null : new ResolveSessionResponse(session.AccountId);
    }

    private async Task<AuthResponse> CreateSessionAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var accessToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(HashByteLength)).ToLowerInvariant();
        var accessTokenHash = hashProvider.ComputeSha256Hex(accessToken);

        var refreshToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(HashByteLength)).ToLowerInvariant();
        var refreshTokenHash = hashProvider.ComputeSha256Hex(refreshToken);

        var now = DateTimeOffset.UtcNow;
        var session = new StaffSession
        {
            TokenHash = accessTokenHash,
            AccountId = accountId,
            ExpiresAt = now.AddSeconds(AccessTokenLifetimeSeconds),
            RefreshTokenHash = refreshTokenHash,
            RefreshExpiresAt = now.Add(RefreshTokenLifetime),
            IsRevoked = false
        };

        await repository.AddSessionAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            AuthSchemeNames.Bearer,
            AccessTokenLifetimeSeconds,
            refreshToken,
            accountId);
    }
}
