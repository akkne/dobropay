namespace Dobropay.Messaging.Kafka;

public interface IKafkaClientFactory
{
    IEventPublisher CreatePublisher();
    IEventSubscriber CreateSubscriber(string groupId, string topic);
}
