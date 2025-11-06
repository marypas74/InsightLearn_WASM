namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service for managing system endpoints with caching
/// </summary>
public interface IEndpointService
{
    /// <summary>
    /// Get endpoint path by category and key (with caching)
    /// </summary>
    Task<string?> GetEndpointAsync(string category, string endpointKey);

    /// <summary>
    /// Get all endpoints for a category (with caching)
    /// </summary>
    Task<Dictionary<string, string>> GetCategoryEndpointsAsync(string category);

    /// <summary>
    /// Refresh cache (call after updating endpoints)
    /// </summary>
    Task RefreshCacheAsync();

    /// <summary>
    /// Clear cache
    /// </summary>
    void ClearCache();
}
