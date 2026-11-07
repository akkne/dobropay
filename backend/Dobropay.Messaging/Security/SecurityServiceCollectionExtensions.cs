using Dobropay.Contracts.Constants;
using Dobropay.Contracts.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dobropay.Messaging.Security;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddInternalSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var internalKey = configuration[ConfigurationKeyNames.InternalServiceKey]
            ?? throw new InvalidOperationException($"Missing configuration: {ConfigurationKeyNames.InternalServiceKey}");

        services.AddSingleton<IHashProvider, Sha256HashProvider>();
        services.AddSingleton<IInternalKeyValidator>(provider =>
            new InternalKeyValidator(internalKey, provider.GetRequiredService<IHashProvider>()));
        return services;
    }
}
