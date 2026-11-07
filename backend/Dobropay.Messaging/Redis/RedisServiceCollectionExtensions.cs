using Dobropay.Contracts.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dobropay.Messaging.Redis;

public static class RedisServiceCollectionExtensions
{
    public static IServiceCollection AddRedisRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration[ConfigurationKeyNames.RedisConnection]
            ?? throw new InvalidOperationException($"Missing configuration: {ConfigurationKeyNames.RedisConnection}");

        var options = new RedisOptions { ConnectionString = connectionString };
        services.AddSingleton(options);
        services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
        services.AddSingleton<IRateLimiter, RedisRateLimiter>();
        return services;
    }
}
