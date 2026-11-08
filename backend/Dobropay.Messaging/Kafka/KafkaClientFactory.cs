using Dobropay.Messaging.Metrics;

namespace Dobropay.Messaging.Kafka;

public sealed class KafkaClientFactory(string bootstrapServers, IMetricsRecorder? metricsRecorder = null) : IKafkaClientFactory
{
    public IEventPublisher CreatePublisher() => new KafkaPublisher(bootstrapServers);

    public IEventSubscriber CreateSubscriber(string groupId, string topic) =>
        new KafkaSubscriber(bootstrapServers, groupId, topic, metricsRecorder);
}
