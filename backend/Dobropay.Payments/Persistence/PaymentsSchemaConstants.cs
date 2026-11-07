namespace Dobropay.Payments.Persistence;

public static class PaymentsSchemaConstants
{
    public const string PaymentsTable = "payments";
    public const string CheckoutLinksTable = "checkout_links";
    public const string PaymentAttemptsTable = "payment_attempts";
    public const string IdempotencyRecordsTable = "idempotency_records";
    public const string ProviderInboxTable = "provider_inbox";
    public const string OutboxEventsTable = "outbox_events";
    public const string RefundsTable = "refunds";
    public const string JsonbColumnType = "jsonb";
    public const string NowDefaultValueExpression = "now()";
    public const int CurrencyMaxLength = 3;
    public const int StatusMaxLength = 32;
    public const int ExternalOrderIdMaxLength = 200;
    public const int CancellationPartyMaxLength = 32;
    public const int CancellationReasonMaxLength = 64;
    public const int RefundReasonMaxLength = 200;
}
