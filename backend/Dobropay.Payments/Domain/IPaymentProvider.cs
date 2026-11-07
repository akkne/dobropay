namespace Dobropay.Payments.Domain;

public interface IPaymentProvider
{
    string PaymentMethodType { get; }
    Task<string> CreateAttemptAsync(Guid attemptId, Guid operationKey, CancellationToken cancellationToken);
    Task<string?> GetAttemptStateAsync(string externalId, CancellationToken cancellationToken);
    Task<RefundProviderResult> CreateRefundAsync(RefundProviderRequest request, CancellationToken cancellationToken);
    bool VerifyEvent(ReadOnlySpan<byte> body, string? signature);
}
