namespace Dobropay.Payments.Domain;

public sealed record ProviderEvent(
    string EventId,
    Guid ShopId,
    Guid AttemptId,
    string ExternalAttemptId,
    long Amount,
    string Currency,
    string Outcome);
