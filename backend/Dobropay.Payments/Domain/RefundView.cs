namespace Dobropay.Payments.Domain;

public sealed record RefundView(
    Guid Id,
    Guid PaymentId,
    string Status,
    long Amount,
    string Currency,
    string? Reason,
    string? ExternalRefundId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);
