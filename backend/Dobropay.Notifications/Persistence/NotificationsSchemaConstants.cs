namespace Dobropay.Notifications.Persistence;

public static class NotificationsSchemaConstants
{
    public const string WebhookSubscriptionsTable = "webhook_subscriptions";
    public const string NotificationInboxTable = "notification_inbox";
    public const string WebhookDeliveriesTable = "webhook_deliveries";
    public const string WebhookDeliveryAttemptsTable = "webhook_delivery_attempts";
    public const string LocalReceiverEventsTable = "local_receiver_events";
    public const string JsonbColumnType = "jsonb";
    public const string StatePending = "pending";
    public const string StateProcessing = "processing";
    public const string StateDelivered = "delivered";
    public const string StateFailed = "failed";
    public const string ResultDelivered = "delivered";
    public const string ResultHttpError = "http_error";
    public const string ResultRateLimited = "rate_limited";
    public const string ResultRedisUnavailable = "redis_unavailable";
}
