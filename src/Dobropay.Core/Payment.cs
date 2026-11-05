namespace Dobropay.Core;

public static class PaymentRules
{
    public const string Pending = "pending";
    public const string Succeeded = "succeeded";
    public const string Canceled = "canceled";

    public static bool CanApply(string current, string next) =>
        current == Pending && (next == Succeeded || next == Canceled);
}

public sealed record CreatePaymentRequest(long Amount, string Currency, string? ExternalOrderId);
public sealed record PaymentResponse(Guid Id, string Status, long Amount, string Currency, string CheckoutUrl);
public sealed record PaymentView(Guid Id, string Status, long Amount, string Currency, string? ExternalOrderId);
public sealed record ProviderEvent(string EventId, Guid ShopId, Guid AttemptId, string ExternalAttemptId,
    long Amount, string Currency, string Outcome);

public interface IPaymentProvider
{
    Task<string> CreateAttemptAsync(Guid attemptId, Guid operationKey, CancellationToken cancellationToken);
    Task<string?> GetAttemptStateAsync(string externalId, CancellationToken cancellationToken);
    bool VerifyEvent(ReadOnlySpan<byte> body, string? signature);
}
