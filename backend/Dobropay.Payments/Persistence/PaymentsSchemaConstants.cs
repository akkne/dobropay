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
    public const string SavedPaymentMethodsTable = "saved_payment_methods";
    public const string PaymentReceiptPayloadsTable = "payment_receipt_payloads";
    public const string JsonbColumnType = "jsonb";
    public const string NowDefaultValueExpression = "now()";
    public const int CurrencyMaxLength = 3;
    public const int StatusMaxLength = 32;
    public const int ExternalOrderIdMaxLength = 200;
    public const int CancellationPartyMaxLength = 32;
    public const int CancellationReasonMaxLength = 64;
    public const int RefundReasonMaxLength = 200;
    public const int PaymentMethodTypeMaxLength = 32;
    public const int ConfirmationTypeMaxLength = 32;
    public const int CardFirst6MaxLength = 6;
    public const int CardLast4MaxLength = 4;
    public const int CardBrandMaxLength = 32;
    public const int ProviderTokenMaxLength = 128;
    public const int SbpIdMaxLength = 64;
    public const int CryptoCurrencyMaxLength = 16;
    public const int CryptoNetworkMaxLength = 32;
    public const int CryptoAmountMaxLength = 64;
    public const int DepositAddressMaxLength = 128;
    public const int TxHashMaxLength = 128;
    public const int ReceiptTypeMaxLength = 32;
    public const int CustomerEmailMaxLength = 256;
    public const int CustomerPhoneMaxLength = 64;
    public const int ReceiptItemDescriptionMaxLength = 256;
    public const int ReceiptItemQuantityMaxLength = 32;
}
