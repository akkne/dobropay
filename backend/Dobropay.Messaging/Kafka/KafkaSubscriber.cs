using Confluent.Kafka;

namespace Dobropay.Messaging.Kafka;

public sealed class KafkaSubscriber : IEventSubscriber
{
    private readonly IConsumer<string, string> consumer;

    public KafkaSubscriber(string bootstrapServers, string groupId, string topic)
    {
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
        return result is null ? null : new ConsumedEventMessage(result.Message.Value, result);
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
