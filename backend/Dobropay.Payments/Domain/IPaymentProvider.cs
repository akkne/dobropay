namespace Dobropay.Payments.Domain;

public interface IPaymentProvider
{
    Task<string> CreateAttemptAsync(Guid attemptId, Guid operationKey, CancellationToken cancellationToken);
    Task<string?> GetAttemptStateAsync(string externalId, CancellationToken cancellationToken);
    bool VerifyEvent(ReadOnlySpan<byte> body, string? signature);
}
