using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace InsightLearn.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add basic application services here when ready
        // Currently using existing services from the project
        
        return services;
    }
}