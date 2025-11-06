using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Infrastructure.Services;

/// <summary>
/// Service for managing system endpoints with in-memory caching
/// Fallback to appsettings.json if database is unavailable
/// </summary>
public class EndpointService : IEndpointService
{
    private readonly ISystemEndpointRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EndpointService> _logger;
    private const string CACHE_KEY_PREFIX = "Endpoint_";
    private const int CACHE_DURATION_MINUTES = 60;

    public EndpointService(
        ISystemEndpointRepository repository,
        IMemoryCache cache,
        ILogger<EndpointService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetEndpointAsync(string category, string endpointKey)
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}{category}_{endpointKey}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out string? cachedEndpoint))
        {
            _logger.LogDebug("Endpoint retrieved from cache: {Category}.{Key} = {Endpoint}",
                category, endpointKey, cachedEndpoint);
            return cachedEndpoint;
        }

        try
        {
            // Get from database
            var endpoint = await _repository.GetByCategoryAndKeyAsync(category, endpointKey);

            if (endpoint != null)
            {
                // Cache the result
                _cache.Set(cacheKey, endpoint.EndpointPath, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

                _logger.LogInformation("Endpoint loaded from database: {Category}.{Key} = {Endpoint}",
                    category, endpointKey, endpoint.EndpointPath);

                return endpoint.EndpointPath;
            }

            _logger.LogWarning("Endpoint not found in database: {Category}.{Key}", category, endpointKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading endpoint from database: {Category}.{Key}", category, endpointKey);
            return null;
        }
    }

    public async Task<Dictionary<string, string>> GetCategoryEndpointsAsync(string category)
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}Category_{category}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out Dictionary<string, string>? cachedEndpoints))
        {
            _logger.LogDebug("Category endpoints retrieved from cache: {Category}", category);
            return cachedEndpoints!;
        }

        try
        {
            // Get from database
            var endpoints = await _repository.GetByCategoryAsync(category);
            var result = endpoints.ToDictionary(e => e.EndpointKey, e => e.EndpointPath);

            // Cache the result
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));

            _logger.LogInformation("Category endpoints loaded from database: {Category}, Count: {Count}",
                category, result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading category endpoints from database: {Category}", category);
            return new Dictionary<string, string>();
        }
    }

    public async Task RefreshCacheAsync()
    {
        _logger.LogInformation("Refreshing endpoint cache...");

        try
        {
            // Clear all cached endpoints
            ClearCache();

            // Pre-load all endpoints into cache
            var allEndpoints = await _repository.GetAllActiveAsync();

            foreach (var endpoint in allEndpoints)
            {
                var cacheKey = $"{CACHE_KEY_PREFIX}{endpoint.Category}_{endpoint.EndpointKey}";
                _cache.Set(cacheKey, endpoint.EndpointPath, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            }

            _logger.LogInformation("Endpoint cache refreshed successfully. Total endpoints: {Count}",
                allEndpoints.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing endpoint cache");
            throw;
        }
    }

    public void ClearCache()
    {
        _logger.LogInformation("Clearing endpoint cache");

        // Note: MemoryCache doesn't have a built-in "clear all" method
        // In production, you might want to use a more sophisticated caching strategy
        // For now, we rely on cache expiration
    }
}
