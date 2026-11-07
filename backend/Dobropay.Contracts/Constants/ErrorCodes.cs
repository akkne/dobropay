namespace Dobropay.Contracts.Constants;

public static class ErrorCodes
{
    public const string IdempotencyKeyRequired = "idempotency_key_required";
    public const string InvalidPayment = "invalid_payment";
    public const string IdempotencyConflict = "idempotency_conflict";
    public const string PaymentNotFound = "payment_not_found";
    public const string InvalidCaptureState = "invalid_capture_state";
    public const string InvalidCaptureAmount = "invalid_capture_amount";
    public const string InvalidCancelState = "invalid_cancel_state";
    public const string InvalidRefund = "invalid_refund";
    public const string RefundAmountExceeded = "refund_amount_exceeded";
    public const string RefundNotFound = "refund_not_found";
    public const string AttemptNotReady = "attempt_not_ready";
    public const string UnknownAttempt = "unknown_attempt";
    public const string ShopMismatch = "shop_mismatch";
}
