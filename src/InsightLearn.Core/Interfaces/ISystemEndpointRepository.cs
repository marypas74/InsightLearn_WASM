using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Repository for managing system endpoints stored in database
/// </summary>
public interface ISystemEndpointRepository
{
    /// <summary>
    /// Get all active endpoints
    /// </summary>
    Task<IEnumerable<SystemEndpoint>> GetAllActiveAsync();

    /// <summary>
    /// Get endpoint by category and key
    /// </summary>
    Task<SystemEndpoint?> GetByCategoryAndKeyAsync(string category, string endpointKey);

    /// <summary>
    /// Get all endpoints for a specific category
    /// </summary>
    Task<IEnumerable<SystemEndpoint>> GetByCategoryAsync(string category);

    /// <summary>
    /// Update an endpoint
    /// </summary>
    Task<SystemEndpoint> UpdateAsync(SystemEndpoint endpoint);

    /// <summary>
    /// Create a new endpoint
    /// </summary>
    Task<SystemEndpoint> CreateAsync(SystemEndpoint endpoint);

    /// <summary>
    /// Delete an endpoint
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Check if endpoint exists
    /// </summary>
    Task<bool> ExistsAsync(string category, string endpointKey);
}
