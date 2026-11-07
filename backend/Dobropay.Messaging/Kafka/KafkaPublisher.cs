using System.Text.Json;
using Confluent.Kafka;
using Dobropay.Contracts.Events;

namespace Dobropay.Messaging.Kafka;

public sealed class KafkaPublisher : IEventPublisher
{
    private readonly IProducer<string, string> producer;

    public KafkaPublisher(string bootstrapServers) =>
        producer = new ProducerBuilder<string, string>(
            new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                EnableIdempotence = true
            }).Build();

    public async Task PublishAsync(string topic, EventEnvelope envelope, CancellationToken cancellationToken) =>
        await producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = envelope.PaymentId.ToString("N"),
                Value = JsonSerializer.Serialize(envelope)
            },
            cancellationToken);

    public void Dispose() => producer.Dispose();
}
