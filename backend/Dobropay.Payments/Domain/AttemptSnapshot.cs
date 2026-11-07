namespace Dobropay.Payments.Domain;

public sealed record AttemptSnapshot(
    Guid ShopId,
    Guid PaymentId,
    string? ExternalId,
    long Amount,
    string Currency,
    string PaymentStatus);
