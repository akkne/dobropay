using Confluent.Kafka;
using Dobropay.Messaging.Metrics;

namespace Dobropay.Messaging.Kafka;

public sealed class KafkaSubscriber : IEventSubscriber
{
    private readonly IConsumer<string, string> consumer;
    private readonly string groupId;
    private readonly IMetricsRecorder? metricsRecorder;

    public KafkaSubscriber(
        string bootstrapServers,
        string groupId,
        string topic,
        IMetricsRecorder? metricsRecorder = null)
    {
        this.groupId = groupId;
        this.metricsRecorder = metricsRecorder;
        consumer = new ConsumerBuilder<string, string>(
            new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false
            }).Build();
        consumer.Subscribe(topic);
    }

    public ConsumedEventMessage? Receive(TimeSpan timeout)
    {
        var result = consumer.Consume(timeout);
        if (result is null)
        {
            return null;
        }

        if (metricsRecorder is not null)
        {
            try
            {
                var watermark = consumer.GetWatermarkOffsets(result.TopicPartition);
                var lag = Math.Max(0L, watermark.High.Value - result.Offset.Value - 1);
                metricsRecorder.RecordKafkaConsumerLag(result.Topic, groupId, lag);
            }
            catch
            {
            }
        }

        return new ConsumedEventMessage(result.Message.Value, result);
    }

    public void Commit(ConsumedEventMessage message)
    {
        if (message.NativeReceipt is ConsumeResult<string, string> result)
        {
            consumer.Commit(result);
        }
    }

    public void Dispose()
    {
        consumer.Close();
        consumer.Dispose();
    }
}
