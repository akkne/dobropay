namespace Dobropay.Messaging.Metrics;

public interface IMetricsRecorder
{
    void RecordPayment(string status, string currency);

    void RecordWebhookDeliveryFailure(string reason);

    void RecordOutboxPendingCount(long count);

    void RecordKafkaConsumerLag(string topic, string consumerGroup, long lag);

    long OutboxPendingCount { get; }

    long GetPaymentCount(string status, string currency);

    long GetWebhookDeliveryFailureCount(string reason);

    long GetKafkaConsumerLag(string topic, string consumerGroup);
}
