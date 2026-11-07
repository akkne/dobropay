using Dobropay.Contracts.Constants;
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
        services.AddSingleton<IKafkaClientFactory>(new KafkaClientFactory(options.BootstrapServers));
        services.AddSingleton<IEventPublisher>(provider => provider.GetRequiredService<IKafkaClientFactory>().CreatePublisher());
        return services;
    }
}
