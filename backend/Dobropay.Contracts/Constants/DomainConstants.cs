namespace Dobropay.Contracts.Constants;

public static class DomainConstants
{
    public const string DefaultCurrency = "RUB";
    public const string CreatePaymentOperation = "create_payment";
    public const string CapturePaymentOperation = "capture_payment";
    public const string CancelPaymentOperation = "cancel_payment";
    public const string CreateRefundOperation = "create_refund";
    public const string OwnerRole = "owner";
    public const string ApplicationJsonContentType = "application/json";
    public const string TextHtmlUtf8ContentType = "text/html; charset=utf-8";
    public const string EmptyJsonArray = "[]";
    public const string EmptyJsonObject = "{}";
    public const string ShopApiKeyPrefix = "dp_live_";
    public const string SimulatorAttemptPrefix = "sim-";
    public const string SimulatorRefundPrefix = "sim-ref-";
    public const string SbpAttemptPrefix = "sbp-";
    public const string SbpRefundPrefix = "sbp-ref-";
    public const string SbpQrPrefix = "qrc-";
    public const string SbpQrBaseUrl = "https://qr.nspk.ru/";
    public const string CardAttemptPrefix = "card-";
    public const string CardRefundPrefix = "card-ref-";
    public const string CardProviderTokenPrefix = "pm_tok_";
    public const string CryptoAttemptPrefix = "crypto-";
    public const string CryptoRefundPrefix = "crypto-ref-";
    public const string CryptoDepositAddressPrefix = "dp_crypto_";
    public const string CryptoConfirmingNetworkState = "confirming_network";
    public const string DefaultCardBrand = "Mir";
    public const string WebhookRateLimitPrefix = "webhook-limit:";
    public const int DefaultTaxSystemCode = 1;
    public const int DefaultVatCode = 1;
    public const int MinTaxSystemCode = 1;
    public const int MaxTaxSystemCode = 6;
    public const int MinVatCode = 1;
    public const int MaxVatCode = 6;
    public const string DefaultReceiptItemQuantity = "1.000";
    public const string DefaultReceiptItemDescription = "Оплата заказа";
    public const string DefaultRefundReceiptItemDescription = "Возврат оплаты заказа";
    public const string SandboxFiscalStoragePrefix = "99990789020";
    public const string DefaultOfdBaseUrl = "https://ofd.sandbox.dobropay.local/check";
}
