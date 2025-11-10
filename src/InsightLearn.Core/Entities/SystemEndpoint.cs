namespace InsightLearn.Core.Entities;

/// <summary>
/// System endpoint configuration stored in database for runtime modification
/// </summary>
public class SystemEndpoint
{
    public int Id { get; set; }

    /// <summary>
    /// Category of the endpoint (e.g., "Auth", "Chat", "Courses")
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint key within category (e.g., "Login", "SendMessage", "GetAll")
    /// </summary>
    public string EndpointKey { get; set; } = string.Empty;

    /// <summary>
    /// Full endpoint path (e.g., "/api/auth/login")
    /// </summary>
    public string EndpointPath { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this endpoint does
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE)
    /// </summary>
    public string HttpMethod { get; set; } = "GET";

    /// <summary>
    /// Whether this endpoint is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this endpoint has been implemented in the API
    /// </summary>
    public bool IsImplemented { get; set; } = false;

    /// <summary>
    /// Last modified timestamp
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who last modified this endpoint
    /// </summary>
    public string? ModifiedBy { get; set; }
}
