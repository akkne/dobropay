namespace Dobropay.Payments.Domain;

public enum PaymentOperationOutcome
{
    Success,
    NotFound,
    IdempotencyConflict,
    InvalidState,
    InvalidAmount
}
