using System.Text.Json;
using Confluent.Kafka;
using Dobropay.Contracts.Events;

namespace Dobropay.Messaging.Kafka;

public sealed class KafkaPublisher : IDisposable
{
    private readonly IProducer<string, string> producer;

    public KafkaPublisher(string bootstrapServers) => producer = new ProducerBuilder<string, string>(
        new ProducerConfig { BootstrapServers = bootstrapServers, Acks = Acks.All, EnableIdempotence = true }).Build();

    public Task<DeliveryResult<string, string>> PublishAsync(string topic, EventEnvelope envelope, CancellationToken ct) =>
        producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = envelope.PaymentId.ToString("N"),
            Value = JsonSerializer.Serialize(envelope)
        }, ct);

    public void Dispose() => producer.Dispose();
}

public sealed class KafkaSubscriber : IDisposable
{
    private readonly IConsumer<string, string> consumer;

    public KafkaSubscriber(string bootstrapServers, string groupId, string topic)
    {
        consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        }).Build();
        consumer.Subscribe(topic);
    }

    public ConsumeResult<string, string>? Receive(TimeSpan timeout) => consumer.Consume(timeout);
    public void Commit(ConsumeResult<string, string> result) => consumer.Commit(result);
    public void Dispose() { consumer.Close(); consumer.Dispose(); }
}
