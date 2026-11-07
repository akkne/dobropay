using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Dobropay.Payments.Domain;

namespace Dobropay.Payments.Provider;

public sealed class SbpPaymentProvider(
    string secret,
    IHashProvider hashProvider,
    ISbpQrGenerator qrGenerator) : IPaymentProvider
{
    public string PaymentMethodType => PaymentMethodTypes.Sbp;

    public Task<string> CreateAttemptAsync(Guid attemptId, Guid operationKey, CancellationToken cancellationToken) =>
        Task.FromResult(DomainConstants.SbpAttemptPrefix + attemptId.ToString("N"));

    public Task<string?> GetAttemptStateAsync(string externalId, CancellationToken cancellationToken) =>
        Task.FromResult<string?>(null);

    public Task<RefundProviderResult> CreateRefundAsync(RefundProviderRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new RefundProviderResult(
            DomainConstants.SbpRefundPrefix + request.RefundId.ToString("N"),
            RefundStatuses.Succeeded));

    public bool VerifyEvent(ReadOnlySpan<byte> body, string? signature) =>
        !string.IsNullOrEmpty(signature) &&
        hashProvider.FixedTimeEqualsAscii(hashProvider.ComputeHmacSha256Hex(secret, body), signature);

    public SbpQrPayload GenerateQrPayload(Guid attemptId, long amount, string currency, DateTimeOffset expiresAt) =>
        qrGenerator.GenerateQr(attemptId, amount, currency, expiresAt);
}
