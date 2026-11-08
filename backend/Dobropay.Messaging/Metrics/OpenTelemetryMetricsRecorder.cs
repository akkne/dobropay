using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Dobropay.Contracts.Constants;

namespace Dobropay.Messaging.Metrics;

public sealed class OpenTelemetryMetricsRecorder : IMetricsRecorder, IDisposable
{
    public const string MeterName = "Dobropay";

    private readonly Meter meter;
    private readonly Counter<long> paymentsCounter;
    private readonly Counter<long> webhookFailuresCounter;
    private readonly ObservableGauge<long> outboxGauge;
    private readonly ObservableGauge<long> consumerLagGauge;

    private readonly ConcurrentDictionary<(string Status, string Currency), long> paymentsTotals = new();
    private readonly ConcurrentDictionary<string, long> webhookFailures = new();
    private readonly ConcurrentDictionary<(string Topic, string ConsumerGroup), long> consumerLags = new();
    private long outboxPendingCount;

    public OpenTelemetryMetricsRecorder()
    {
        meter = new Meter(MeterName, "1.0.0");

        paymentsCounter = meter.CreateCounter<long>(
            MetricNames.PaymentsTotal,
            unit: "{payments}",
            description: "Total number of payments recorded by status and currency");

        webhookFailuresCounter = meter.CreateCounter<long>(
            MetricNames.WebhookDeliveryFailuresTotal,
            unit: "{failures}",
            description: "Total number of webhook delivery failures by reason");

        outboxGauge = meter.CreateObservableGauge(
            MetricNames.OutboxPendingCount,
            () => Volatile.Read(ref outboxPendingCount),
            unit: "{events}",
            description: "Number of pending outbox events");

        consumerLagGauge = meter.CreateObservableGauge(
            MetricNames.KafkaConsumerLag,
            () => consumerLags.Select(entry => new Measurement<long>(
                entry.Value,
                new KeyValuePair<string, object?>("topic", entry.Key.Topic),
                new KeyValuePair<string, object?>("consumer_group", entry.Key.ConsumerGroup))),
            unit: "{messages}",
            description: "Kafka consumer lag by topic and consumer group");
    }

    public long OutboxPendingCount => Volatile.Read(ref outboxPendingCount);

    public void RecordPayment(string status, string currency)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? "unknown" : status.Trim().ToLowerInvariant();
        var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? "unknown" : currency.Trim().ToUpperInvariant();

        paymentsTotals.AddOrUpdate((normalizedStatus, normalizedCurrency), 1, (_, current) => current + 1);

        paymentsCounter.Add(
            1,
            new KeyValuePair<string, object?>("status", normalizedStatus),
            new KeyValuePair<string, object?>("currency", normalizedCurrency));
    }

    public void RecordWebhookDeliveryFailure(string reason)
    {
        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim();

        webhookFailures.AddOrUpdate(normalizedReason, 1, (_, current) => current + 1);

        webhookFailuresCounter.Add(
            1,
            new KeyValuePair<string, object?>("reason", normalizedReason));
    }

    public void RecordOutboxPendingCount(long count)
    {
        Volatile.Write(ref outboxPendingCount, Math.Max(0, count));
    }

    public void RecordKafkaConsumerLag(string topic, string consumerGroup, long lag)
    {
        var normalizedTopic = string.IsNullOrWhiteSpace(topic) ? "unknown" : topic.Trim();
        var normalizedGroup = string.IsNullOrWhiteSpace(consumerGroup) ? "unknown" : consumerGroup.Trim();
        consumerLags[(normalizedTopic, normalizedGroup)] = Math.Max(0, lag);
    }

    public long GetPaymentCount(string status, string currency)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? "unknown" : status.Trim().ToLowerInvariant();
        var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? "unknown" : currency.Trim().ToUpperInvariant();
        return paymentsTotals.TryGetValue((normalizedStatus, normalizedCurrency), out var count) ? count : 0;
    }

    public long GetWebhookDeliveryFailureCount(string reason)
    {
        var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "unknown" : reason.Trim();
        return webhookFailures.TryGetValue(normalizedReason, out var count) ? count : 0;
    }

    public long GetKafkaConsumerLag(string topic, string consumerGroup)
    {
        var normalizedTopic = string.IsNullOrWhiteSpace(topic) ? "unknown" : topic.Trim();
        var normalizedGroup = string.IsNullOrWhiteSpace(consumerGroup) ? "unknown" : consumerGroup.Trim();
        return consumerLags.TryGetValue((normalizedTopic, normalizedGroup), out var lag) ? lag : 0;
    }

    public void Dispose()
    {
        meter.Dispose();
    }
}
