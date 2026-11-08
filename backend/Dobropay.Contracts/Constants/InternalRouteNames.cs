namespace Dobropay.Contracts.Constants;

public static class InternalRouteNames
{
    public const string ResolveSession = "/internal/v1/sessions/resolve";
    public const string ResolveApiKey = "/internal/v1/keys/resolve";
    public const string ShopById = "/internal/v1/shops/{shopId:guid}";
    public const string ShopByIdPrefix = "/internal/v1/shops/";
    public const string ShopOnboardingApprove = "/internal/v1/shops/{shopId:guid}/onboarding/approve";
    public const string MembershipByAccountAndShop = "/internal/v1/memberships/{accountId:guid}/{shopId:guid}";
    public const string MembershipPrefix = "/internal/v1/memberships/";
    public const string ShopPayments = "/internal/v1/shops/{shopId:guid}/payments";
    public const string ShopPaymentById = "/internal/v1/shops/{shopId:guid}/payments/{id:guid}";
    public const string ShopPaymentCapture = "/internal/v1/shops/{shopId:guid}/payments/{id:guid}/capture";
    public const string ShopPaymentCancel = "/internal/v1/shops/{shopId:guid}/payments/{id:guid}/cancel";
    public const string ShopPaymentRefunds = "/internal/v1/shops/{shopId:guid}/payments/{id:guid}/refunds";
    public const string ShopRefunds = "/internal/v1/shops/{shopId:guid}/refunds";
    public const string ShopDeliveries = "/internal/v1/shops/{shopId:guid}/deliveries";
    public const string ShopDeliveryRetry = "/internal/v1/shops/{shopId:guid}/deliveries/{deliveryId:guid}/retry";
    public const string ShopSubscriptions = "/internal/v1/shops/{shopId:guid}/subscriptions";
    public const string ShopReceipts = "/internal/v1/shops/{shopId:guid}/receipts";
    public const string ShopPaymentReceiptContext = "/internal/v1/shops/{shopId:guid}/payments/{paymentId:guid}/receipt-context";
    public const string SendEmail = "/internal/v1/email";
    public const string PaymentsSuffix = "payments";
    public const string RefundsSuffix = "refunds";
    public const string CaptureSuffix = "capture";
    public const string CancelSuffix = "cancel";
    public const string DeliveriesSuffix = "deliveries";
    public const string RetrySuffix = "retry";
    public const string SubscriptionsSuffix = "subscriptions";
    public const string ReceiptsSuffix = "receipts";
    public const string ReceiptContextSuffix = "receipt-context";
}
