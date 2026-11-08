using Microsoft.Extensions.DependencyInjection;

namespace Dobropay.Messaging.Metrics;

public static class MetricsServiceCollectionExtensions
{
    public static IServiceCollection AddDobropayMetrics(this IServiceCollection services)
    {
        services.AddSingleton<OpenTelemetryMetricsRecorder>();
        services.AddSingleton<IMetricsRecorder>(provider => provider.GetRequiredService<OpenTelemetryMetricsRecorder>());
        return services;
    }
}
