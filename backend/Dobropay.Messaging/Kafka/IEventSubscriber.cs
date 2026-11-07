namespace Dobropay.Messaging.Kafka;

public interface IEventSubscriber : IDisposable
{
    ConsumedEventMessage? Receive(TimeSpan timeout);
    void Commit(ConsumedEventMessage message);
}
