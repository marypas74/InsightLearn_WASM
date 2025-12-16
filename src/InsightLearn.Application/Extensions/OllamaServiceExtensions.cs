using InsightLearn.Application.Interfaces;
using InsightLearn.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Extensions;

/// <summary>
/// Extension methods for registering Ollama services in DI container
/// Allows easy toggling between real and mock services based on configuration
/// </summary>
public static class OllamaServiceExtensions
{
    /// <summary>
    /// Registers Ollama service based on configuration
    /// Uses MockOllamaService when Ollama:UseMock is true or when Ollama URL is unavailable
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddOllamaService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var useMock = configuration.GetValue<bool>("Ollama:UseMock");
        var ollamaUrl = configuration["Ollama:Url"] ?? configuration["Ollama:BaseUrl"];
        var defaultModel = configuration["Ollama:Model"] ?? "qwen2:0.5b";

        if (useMock)
        {
            // Register mock service for testing/development (Scoped to match ChatbotService)
            services.AddScoped<IOllamaService, MockOllamaService>();
            services.AddScoped<IMockOllamaService, MockOllamaService>();

            Console.WriteLine("[CONFIG] Using MockOllamaService (Ollama:UseMock=true)");
        }
        else if (string.IsNullOrWhiteSpace(ollamaUrl))
        {
            // Fallback to mock if no URL configured
            services.AddScoped<IOllamaService, MockOllamaService>();
            services.AddScoped<IMockOllamaService, MockOllamaService>();

            Console.WriteLine("[CONFIG] Using MockOllamaService (No Ollama URL configured)");
        }
        else
        {
            // Register real Ollama service (Scoped to match ChatbotService)
            services.AddScoped<IOllamaService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("ollama");
                var logger = sp.GetRequiredService<ILogger<OllamaService>>();
                return new OllamaService(httpClient, ollamaUrl, defaultModel, logger);
            });

            // Also register mock service for testing endpoints
            services.AddScoped<IMockOllamaService, MockOllamaService>();

            Console.WriteLine($"[CONFIG] Using OllamaService at {ollamaUrl} with model {defaultModel}");
        }

        return services;
    }

    /// <summary>
    /// Registers MockOllamaService explicitly for testing environments
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddMockOllamaService(this IServiceCollection services)
    {
        services.AddScoped<IOllamaService, MockOllamaService>();
        services.AddScoped<IMockOllamaService, MockOllamaService>();

        Console.WriteLine("[CONFIG] Using MockOllamaService (explicit registration)");

        return services;
    }
}
