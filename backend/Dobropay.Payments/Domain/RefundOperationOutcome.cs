namespace Dobropay.Payments.Domain;

public enum RefundOperationOutcome
{
    Success,
    PaymentNotFound,
    RefundNotFound,
    IdempotencyConflict,
    InvalidState,
    InvalidAmount,
    AmountExceeded
}
