export class ApiRoutes {
  public static readonly AuthLogin = '/v1/auth/login';
  public static readonly Payments = '/v1/payments';
  public static readonly PaymentById = (id: string) => `/v1/payments/${id}`;
  public static readonly CheckoutStatus = (token: string) => `/checkout/${token}/status`;
  public static readonly CheckoutStart = (token: string) => `/checkout/${token}/start`;
  public static readonly CheckoutSimulate = (token: string, outcome: string) => `/checkout/${token}/simulate/${outcome}`;

  public static readonly AdminShopPayments = (shopId: string) => `/v1/admin/shops/${shopId}/payments`;
  public static readonly AdminShopPaymentById = (shopId: string, id: string) => `/v1/admin/shops/${shopId}/payments/${id}`;
  public static readonly AdminShopPaymentCapture = (shopId: string, id: string) => `/v1/admin/shops/${shopId}/payments/${id}/capture`;
  public static readonly AdminShopPaymentCancel = (shopId: string, id: string) => `/v1/admin/shops/${shopId}/payments/${id}/cancel`;
  public static readonly AdminShopPaymentRefunds = (shopId: string, id: string) => `/v1/admin/shops/${shopId}/payments/${id}/refunds`;
  public static readonly AdminShopDeliveries = (shopId: string) => `/v1/admin/shops/${shopId}/deliveries`;
  public static readonly AdminShopReceipts = (shopId: string) => `/v1/admin/shops/${shopId}/receipts`;

  public static readonly DeveloperShopKeys = (shopId: string) => `/v1/developer/shops/${shopId}/keys`;
  public static readonly DeveloperShopWebhooks = (shopId: string) => `/v1/developer/shops/${shopId}/webhooks`;
  public static readonly DeveloperShopOnboarding = (shopId: string) => `/v1/developer/shops/${shopId}/onboarding`;
}
