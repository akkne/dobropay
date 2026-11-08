using System.Diagnostics.Metrics;
using Dobropay.Contracts.Constants;
using Dobropay.Messaging.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Dobropay.UnitTests;

[Collection("MetricsRecorderTests")]
public sealed class MetricsRecorderTests : IDisposable
{
    private readonly OpenTelemetryMetricsRecorder recorder = new();

    public void Dispose()
    {
        recorder.Dispose();
    }

    [Fact]
    public void RecordPayment_IncrementsCountAndPreservesTags()
    {
        recorder.RecordPayment("succeeded", "RUB");
        recorder.RecordPayment("SUCCEEDED", "rub");
        recorder.RecordPayment("waiting_for_capture", "RUB");

        Assert.Equal(2, recorder.GetPaymentCount("succeeded", "RUB"));
        Assert.Equal(1, recorder.GetPaymentCount("waiting_for_capture", "RUB"));
        Assert.Equal(0, recorder.GetPaymentCount("canceled", "RUB"));
    }

    [Fact]
    public void RecordWebhookDeliveryFailure_IncrementsCountPerReason()
    {
        recorder.RecordWebhookDeliveryFailure("rate_limited");
        recorder.RecordWebhookDeliveryFailure("rate_limited");
        recorder.RecordWebhookDeliveryFailure("http_500");

        Assert.Equal(2, recorder.GetWebhookDeliveryFailureCount("rate_limited"));
        Assert.Equal(1, recorder.GetWebhookDeliveryFailureCount("http_500"));
        Assert.Equal(0, recorder.GetWebhookDeliveryFailureCount("timeout"));
    }

    [Fact]
    public void RecordOutboxPendingCount_UpdatesGaugeValueAndClampsNegative()
    {
        recorder.RecordOutboxPendingCount(42);
        Assert.Equal(42, recorder.OutboxPendingCount);

        recorder.RecordOutboxPendingCount(-5);
        Assert.Equal(0, recorder.OutboxPendingCount);
    }

    [Fact]
    public void RecordKafkaConsumerLag_UpdatesLagPerTopicAndGroup()
    {
        recorder.RecordKafkaConsumerLag("dobropay.payment.events", "payments-group", 120);
        recorder.RecordKafkaConsumerLag("dobropay.provider.events", "notifications-group", 5);

        Assert.Equal(120, recorder.GetKafkaConsumerLag("dobropay.payment.events", "payments-group"));
        Assert.Equal(5, recorder.GetKafkaConsumerLag("dobropay.provider.events", "notifications-group"));
        Assert.Equal(0, recorder.GetKafkaConsumerLag("dobropay.payment.events", "other-group"));
    }

    [Fact]
    public void MeterListener_CapturesMeasurementsAndTags()
    {
        using var meterListener = new MeterListener();
        var capturedMeasurements = new List<(string InstrumentName, long Value, Dictionary<string, object?> Tags)>();
        var syncLock = new object();

        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == OpenTelemetryMetricsRecorder.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            var dict = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                dict[tag.Key] = tag.Value;
            }

            lock (syncLock)
            {
                capturedMeasurements.Add((instrument.Name, measurement, dict));
            }
        });

        meterListener.Start();

        using var localRecorder = new OpenTelemetryMetricsRecorder();

        localRecorder.RecordPayment("succeeded", "RUB");
        localRecorder.RecordWebhookDeliveryFailure("rate_limited");
        localRecorder.RecordOutboxPendingCount(15);
        localRecorder.RecordKafkaConsumerLag("events-topic", "test-group", 7);

        meterListener.RecordObservableInstruments();

        lock (syncLock)
        {
            Assert.Contains(capturedMeasurements, m =>
                m.InstrumentName == MetricNames.PaymentsTotal &&
                m.Value == 1 &&
                (string?)m.Tags["status"] == "succeeded" &&
                (string?)m.Tags["currency"] == "RUB");

            Assert.Contains(capturedMeasurements, m =>
                m.InstrumentName == MetricNames.WebhookDeliveryFailuresTotal &&
                m.Value == 1 &&
                (string?)m.Tags["reason"] == "rate_limited");

            Assert.Contains(capturedMeasurements, m =>
                m.InstrumentName == MetricNames.OutboxPendingCount &&
                m.Value == 15);

            Assert.Contains(capturedMeasurements, m =>
                m.InstrumentName == MetricNames.KafkaConsumerLag &&
                m.Value == 7 &&
                (string?)m.Tags["topic"] == "events-topic" &&
                (string?)m.Tags["consumer_group"] == "test-group");
        }
    }

    [Fact]
    public void AddDobropayMetrics_RegistersSingletonServicesInContainer()
    {
        var services = new ServiceCollection();
        services.AddDobropayMetrics();

        using var provider = services.BuildServiceProvider();
        var recorderFromInterface = provider.GetRequiredService<IMetricsRecorder>();
        var recorderFromClass = provider.GetRequiredService<OpenTelemetryMetricsRecorder>();

        Assert.NotNull(recorderFromInterface);
        Assert.Same(recorderFromInterface, recorderFromClass);
    }
}
