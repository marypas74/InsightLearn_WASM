namespace InsightLearn.Core.DTOs;

/// <summary>
/// Standardized error response DTO for all API errors.
/// Provides consistent error format across the application with environment-aware details.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// General error category or type (e.g., "BadRequest", "Unauthorized", "InternalServerError")
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error message.
    /// In Production: Safe, generic message.
    /// In Development: Detailed exception message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Trace identifier for correlation across logs and distributed systems.
    /// Useful for debugging and support ticket resolution.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// UTC timestamp when the error occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Validation errors (field-level) or additional error details.
    /// In Production: Only validation errors.
    /// In Development: Includes StackTrace and ExceptionType.
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; set; }
}
