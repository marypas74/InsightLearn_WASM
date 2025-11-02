using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using InsightLearn.Infrastructure.Services;

namespace InsightLearn.Infrastructure.Extensions;

/// <summary>
/// Extension methods for configuring OAuth database services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds OAuth database maintenance and monitoring services.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    /// <returns>The updated service collection</returns>
    public static IServiceCollection AddOAuthDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure OAuth maintenance options
        services.Configure<OAuthMaintenanceOptions>(options =>
        {
            configuration.GetSection("OAuthMaintenance").Bind(options);
        });

        // Register the OAuth database maintenance background service
        services.AddHostedService<OAuthDatabaseMaintenanceService>();

        return services;
    }

    /// <summary>
    /// Adds OAuth database maintenance services with custom options.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure OAuth maintenance options</param>
    /// <returns>The updated service collection</returns>
    public static IServiceCollection AddOAuthDatabaseServices(
        this IServiceCollection services,
        Action<OAuthMaintenanceOptions> configureOptions)
    {
        // Configure OAuth maintenance options
        services.Configure(configureOptions);

        // Register the OAuth database maintenance background service
        services.AddHostedService<OAuthDatabaseMaintenanceService>();

        return services;
    }
}