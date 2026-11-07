namespace Dobropay.Messaging.Kafka;

public sealed class KafkaOptions
{
    public required string BootstrapServers { get; init; }
}
