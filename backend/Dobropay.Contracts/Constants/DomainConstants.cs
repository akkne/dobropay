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
    public const string WebhookRateLimitPrefix = "webhook-limit:";
}
