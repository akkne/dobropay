namespace Dobropay.Payments.Domain;

public sealed record RefundOperationResult(
    RefundOperationOutcome Outcome,
    RefundView? Refund = null,
    string? ErrorCode = null,
    bool Created = false);
