namespace Dobropay.Contracts.Constants;

public static class MetricNames
{
    public const string PaymentsTotal = "dobropay_payments_total";
    public const string WebhookDeliveryFailuresTotal = "dobropay_webhook_delivery_failures_total";
    public const string OutboxPendingCount = "dobropay_outbox_pending_count";
    public const string KafkaConsumerLag = "dobropay_kafka_consumer_lag";
}
