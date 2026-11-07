using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Provider;

public sealed class SandboxProvider(string secret, IHashProvider hashProvider) : IPaymentProvider
{
    public Task<string> CreateAttemptAsync(Guid attemptId, Guid operationKey, CancellationToken cancellationToken) =>
        Task.FromResult(DomainConstants.SimulatorAttemptPrefix + attemptId.ToString("N"));

    public Task<string?> GetAttemptStateAsync(string externalId, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(null);

    public Task<RefundProviderResult> CreateRefundAsync(RefundProviderRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new RefundProviderResult(
            DomainConstants.SimulatorRefundPrefix + request.RefundId.ToString("N"),
            RefundStatuses.Succeeded));

    public bool VerifyEvent(ReadOnlySpan<byte> body, string? signature) =>
        !string.IsNullOrEmpty(signature) &&
        hashProvider.FixedTimeEqualsAscii(hashProvider.ComputeHmacSha256Hex(secret, body), signature);
}
