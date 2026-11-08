using Dobropay.Contracts.Constants;
using Dobropay.Messaging.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dobropay.Messaging.Kafka;

public static class KafkaServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var bootstrapServers = configuration[ConfigurationKeyNames.KafkaBootstrapServers]
            ?? throw new InvalidOperationException($"Missing configuration: {ConfigurationKeyNames.KafkaBootstrapServers}");

        var options = new KafkaOptions { BootstrapServers = bootstrapServers };
        services.AddSingleton(options);
        services.AddSingleton<IKafkaClientFactory>(provider =>
            new KafkaClientFactory(options.BootstrapServers, provider.GetService<IMetricsRecorder>()));
        services.AddSingleton<IEventPublisher>(provider => provider.GetRequiredService<IKafkaClientFactory>().CreatePublisher());
        return services;
    }
}
