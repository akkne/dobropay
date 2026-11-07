namespace Dobropay.Contracts.Constants;

public static class ErrorCodes
{
    public const string IdempotencyKeyRequired = "idempotency_key_required";
    public const string InvalidPayment = "invalid_payment";
    public const string IdempotencyConflict = "idempotency_conflict";
    public const string PaymentNotFound = "payment_not_found";
    public const string AttemptNotReady = "attempt_not_ready";
    public const string UnknownAttempt = "unknown_attempt";
    public const string ShopMismatch = "shop_mismatch";
}
