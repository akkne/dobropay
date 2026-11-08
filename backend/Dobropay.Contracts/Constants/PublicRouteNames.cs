namespace Dobropay.Contracts.Constants;

public static class PublicRouteNames
{
    public const string AuthLogin = "/v1/auth/login";
    public const string AuthRegister = "/v1/auth/register";
    public const string AuthRefresh = "/v1/auth/refresh";
    public const string AuthLogout = "/v1/auth/logout";
    public const string AuthMe = "/v1/auth/me";
    public const string DeveloperShops = "/v1/developer/shops";
    public const string DeveloperShopOnboarding = "/v1/developer/shops/{shopId:guid}/onboarding";
    public const string DeveloperShopOnboardingSubmit = "/v1/developer/shops/{shopId:guid}/onboarding/submit";
    public const string DeveloperShopKeys = "/v1/developer/shops/{shopId:guid}/keys";
    public const string DeveloperShopKeyDelete = "/v1/developer/shops/{shopId:guid}/keys/{keyId:guid}";
    public const string DeveloperCreateShopKey = "/v1/developer/shops/{shopId:guid}/keys";
    public const string DeveloperConfigureWebhook = "/v1/developer/shops/{shopId:guid}/webhooks";
    public const string Payments = "/v1/payments";
    public const string PaymentById = "/v1/payments/{id:guid}";
    public const string PaymentCapture = "/v1/payments/{id:guid}/capture";
    public const string PaymentCancel = "/v1/payments/{id:guid}/cancel";
    public const string PaymentRefunds = "/v1/payments/{id:guid}/refunds";
    public const string PaymentsOpenApiPrefix = "v1/payments";
    public const string Refunds = "/v1/refunds";
    public const string RefundById = "/v1/refunds/{id:guid}";
    public const string RefundsOpenApiPrefix = "v1/refunds";
    public const string CheckoutPrefix = "/checkout/";
    public const string CheckoutByToken = "/checkout/{token}";
    public const string CheckoutStart = "/checkout/{token}/start";
    public const string CheckoutSimulate = "/checkout/{token}/simulate/{outcome}";
    public const string ProviderSandboxEvents = "/v1/provider/sandbox/events";
    public const string ProviderSbpEvents = "/v1/provider/sbp/events";
    public const string ProviderCard3dsCallback = "/v1/provider/card/3ds-callback";
    public const string ProviderCryptoEvents = "/v1/provider/crypto/events";
    public const string PaymentMethods = "/v1/payment-methods";
    public const string PaymentMethodById = "/v1/payment-methods/{id:guid}";
    public const string LocalWebhookReceiver = "/local/webhooks/receiver";
    public const string LocalWebhooksReceived = "/local/webhooks/received";
    public const string AdminShopPayments = "/v1/admin/shops/{shopId:guid}/payments";
    public const string AdminShopPaymentById = "/v1/admin/shops/{shopId:guid}/payments/{id:guid}";
    public const string AdminShopPaymentCapture = "/v1/admin/shops/{shopId:guid}/payments/{id:guid}/capture";
    public const string AdminShopPaymentCancel = "/v1/admin/shops/{shopId:guid}/payments/{id:guid}/cancel";
    public const string AdminShopPaymentRefunds = "/v1/admin/shops/{shopId:guid}/payments/{id:guid}/refunds";
    public const string AdminShopRefunds = "/v1/admin/shops/{shopId:guid}/refunds";
    public const string AdminShopDeliveries = "/v1/admin/shops/{shopId:guid}/deliveries";
    public const string AdminDeliveryRetry = "/v1/admin/shops/{shopId:guid}/deliveries/{deliveryId:guid}/retry";
    public const string AdminShopReceipts = "/v1/admin/shops/{shopId:guid}/receipts";
    public const string ReceiptsLookup = "/v1/receipts/lookup";
    public const string ReceiptById = "/v1/receipts/{id:guid}";
}
