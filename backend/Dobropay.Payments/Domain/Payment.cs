namespace Dobropay.Payments.Domain;

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

public sealed record AttemptSnapshot(Guid ShopId, Guid PaymentId, string? ExternalId,
    long Amount, string Currency, string PaymentStatus);
public enum ConfirmationDecision { WaitForAttempt, Reject, Ignore, Apply }

public static class PaymentConfirmation
{
    public static ConfirmationDecision Decide(ProviderEvent incoming, AttemptSnapshot? attempt)
    {
        if (attempt is null || attempt.ExternalId is null) return ConfirmationDecision.WaitForAttempt;
        if (incoming.Outcome is not (PaymentRules.Succeeded or PaymentRules.Canceled) ||
            incoming.ShopId != attempt.ShopId || incoming.ExternalAttemptId != attempt.ExternalId ||
            incoming.Amount != attempt.Amount || incoming.Currency != attempt.Currency)
            return ConfirmationDecision.Reject;
        return PaymentRules.CanApply(attempt.PaymentStatus, incoming.Outcome)
            ? ConfirmationDecision.Apply : ConfirmationDecision.Ignore;
    }
}

public interface IPaymentProvider
{
    Task<string> CreateAttemptAsync(Guid attemptId, Guid operationKey, CancellationToken cancellationToken);
    Task<string?> GetAttemptStateAsync(string externalId, CancellationToken cancellationToken);
    bool VerifyEvent(ReadOnlySpan<byte> body, string? signature);
}
