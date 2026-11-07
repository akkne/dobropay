namespace Dobropay.Payments.Domain;

public sealed record PaymentView(
    Guid Id,
    string Status,
    long Amount,
    string Currency,
    string? ExternalOrderId,
    bool Capture = true,
    long? CapturedAmount = null,
    long RefundedAmount = 0,
    string? CancellationParty = null,
    string? CancellationReason = null);
