using Dobropay.Messaging.Kafka;

namespace Dobropay.UnitTests;

public sealed class FakeKafkaClientFactory : IKafkaClientFactory
{
    public FakeEventSubscriber Subscriber { get; } = new();

    public IEventPublisher CreatePublisher() => throw new NotSupportedException();

    public IEventSubscriber CreateSubscriber(string groupId, string topic) => Subscriber;
}
