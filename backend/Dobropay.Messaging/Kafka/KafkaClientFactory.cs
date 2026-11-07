namespace Dobropay.Messaging.Kafka;

public sealed class KafkaClientFactory(string bootstrapServers) : IKafkaClientFactory
{
    public IEventPublisher CreatePublisher() => new KafkaPublisher(bootstrapServers);

    public IEventSubscriber CreateSubscriber(string groupId, string topic) =>
        new KafkaSubscriber(bootstrapServers, groupId, topic);
}
