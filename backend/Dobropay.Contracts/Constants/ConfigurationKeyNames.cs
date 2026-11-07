namespace Dobropay.Contracts.Constants;

public static class ConfigurationKeyNames
{
    public const string RuntimeConnectionString = "Runtime";
    public const string FullRuntimeConnectionString = "ConnectionStrings:Runtime";
    public const string InternalServiceKey = "INTERNAL_SERVICE_KEY";
    public const string CheckoutSecret = "CHECKOUT_SECRET";
    public const string ProviderSecret = "PROVIDER_SECRET";
    public const string WebhookSecret = "WEBHOOK_SECRET";
    public const string KafkaBootstrapServers = "Kafka:BootstrapServers";
    public const string RedisConnection = "Redis:Connection";
    public const string SandboxEnabled = "Sandbox:Enabled";
    public const string SandboxPublicBaseUrl = "Sandbox:PublicBaseUrl";
    public const string SandboxInternalBaseUrl = "Sandbox:InternalBaseUrl";
    public const string ServiceAuthUrl = "Services:Auth";
    public const string ServiceDeveloperApiUrl = "Services:DeveloperApi";
    public const string ServicePaymentsUrl = "Services:Payments";
    public const string ServiceNotificationsUrl = "Services:Notifications";
    public const string ServiceReceiptsUrl = "Services:Receipts";
    public const string ServiceEmailUrl = "Services:Email";
    public const string SmtpHost = "Smtp:Host";
    public const string SmtpPort = "Smtp:Port";
    public const string SmtpFrom = "Smtp:From";
    public const string DemoReceiptEmail = "Demo:ReceiptEmail";
    public const string DesignTimeConnectionEnv = "DESIGN_TIME_CONNECTION_STRING";
    public const string ReconciliationIntervalSeconds = "Reconciliation:IntervalSeconds";
    public const string ReconciliationAttemptStaleSeconds = "Reconciliation:AttemptStaleSeconds";
    public const string PaymentExpirationMinutes = "Payments:ExpirationMinutes";
    public const string PaymentCaptureExpirationMinutes = "Payments:CaptureExpirationMinutes";
    public const string FiscalProviderMode = "Fiscal:ProviderMode";
    public const string FiscalCloudKktBaseUrl = "Fiscal:CloudKktBaseUrl";
    public const string FiscalCloudKktApiKey = "Fiscal:CloudKktApiKey";
    public const string FiscalOfdBaseUrl = "Fiscal:OfdBaseUrl";
}
