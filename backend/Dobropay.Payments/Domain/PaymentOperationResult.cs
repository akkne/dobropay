namespace Dobropay.Payments.Domain;

public sealed record PaymentOperationResult(
    PaymentOperationOutcome Outcome,
    PaymentView? Payment = null,
    string? ErrorCode = null);
