using Dobropay.Contracts.Events;

namespace Dobropay.Messaging.Kafka;

public interface IEventPublisher : IDisposable
{
    Task PublishAsync(string topic, EventEnvelope envelope, CancellationToken cancellationToken);
}
