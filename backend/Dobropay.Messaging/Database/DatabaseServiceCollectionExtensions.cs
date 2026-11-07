using Dobropay.Contracts.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dobropay.Messaging.Database;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TContext : DbContext
    {
        var connectionString = configuration.GetConnectionString(ConfigurationKeyNames.RuntimeConnectionString)
            ?? throw new InvalidOperationException($"Missing configuration: {ConfigurationKeyNames.FullRuntimeConnectionString}");

        var options = new DatabaseOptions { ConnectionString = connectionString };
        services.AddSingleton(options);
        services.AddDbContext<TContext>(builder => builder.UseNpgsql(options.ConnectionString));
        services.AddScoped<IDatabaseHealthChecker, EfDatabaseHealthChecker<TContext>>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork<TContext>>();
        return services;
    }
}
