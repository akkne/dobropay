namespace Dobropay.Contracts.Constants;

public static class PublicRouteNames
{
    public const string AuthLogin = "/v1/auth/login";
    public const string DeveloperCreateShopKey = "/v1/developer/shops/{shopId:guid}/keys";
    public const string DeveloperConfigureWebhook = "/v1/developer/shops/{shopId:guid}/webhooks";
    public const string Payments = "/v1/payments";
    public const string PaymentById = "/v1/payments/{id:guid}";
    public const string PaymentsOpenApiPrefix = "v1/payments";
    public const string CheckoutPrefix = "/checkout/";
    public const string CheckoutByToken = "/checkout/{token}";
    public const string CheckoutStart = "/checkout/{token}/start";
    public const string CheckoutSimulate = "/checkout/{token}/simulate/{outcome}";
    public const string ProviderSandboxEvents = "/v1/provider/sandbox/events";
    public const string LocalWebhookReceiver = "/local/webhooks/receiver";
    public const string LocalWebhooksReceived = "/local/webhooks/received";
    public const string AdminShopPayments = "/v1/admin/shops/{shopId:guid}/payments";
    public const string AdminShopPaymentById = "/v1/admin/shops/{shopId:guid}/payments/{id:guid}";
    public const string AdminShopDeliveries = "/v1/admin/shops/{shopId:guid}/deliveries";
    public const string AdminShopReceipts = "/v1/admin/shops/{shopId:guid}/receipts";
}
