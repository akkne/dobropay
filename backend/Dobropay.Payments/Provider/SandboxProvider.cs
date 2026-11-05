using System.Security.Cryptography;
using System.Text;
using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Provider;

public static class SandboxCrypto
{
    public static string Sha256(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    public static string Hmac(string secret, ReadOnlySpan<byte> bytes) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), bytes)).ToLowerInvariant();
    public static string CheckoutToken(string secret, Guid paymentId) => Hmac(secret, Encoding.UTF8.GetBytes(paymentId.ToString("N")));
    public static bool Equal(string a, string b) => CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));
}

public sealed class SandboxProvider(string secret) : IPaymentProvider
{
    public Task<string> CreateAttemptAsync(Guid attemptId, Guid operationKey, CancellationToken cancellationToken) =>
        Task.FromResult("sim-" + attemptId.ToString("N"));
    public Task<string?> GetAttemptStateAsync(string externalId, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
    public bool VerifyEvent(ReadOnlySpan<byte> body, string? signature) =>
        signature is not null && SandboxCrypto.Equal(SandboxCrypto.Hmac(secret, body), signature);
}
