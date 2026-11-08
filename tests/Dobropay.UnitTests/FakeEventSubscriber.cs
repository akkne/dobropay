using Dobropay.Messaging.Kafka;

namespace Dobropay.UnitTests;

public sealed class FakeEventSubscriber : IEventSubscriber
{
    public ConsumedEventMessage? MessageToReturn { get; set; }

    public ConsumedEventMessage? Receive(TimeSpan timeout) => MessageToReturn;

    public void Commit(ConsumedEventMessage message)
    {
    }

    public void Dispose()
    {
    }
}
