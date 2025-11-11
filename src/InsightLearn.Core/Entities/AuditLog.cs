namespace InsightLearn.Core.Entities;

/// <summary>
/// Entity for persistent audit logging (GDPR Article 30, SOC 2 compliance)
/// Stores audit trail of sensitive operations (authentication, authorization, admin actions)
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Unique identifier for the audit log entry
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Timestamp when the event occurred (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Event type (AUTH_LOGIN_SUCCESS, ADMIN_UPDATE, PAYMENT_OPERATION, etc.)
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Entity type affected by the action (User, Course, Enrollment, Payment, etc.)
    /// Nullable for system-level events (login, logout)
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Entity ID affected by the action (user ID, course ID, etc.)
    /// Nullable for system-level events
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// User ID who performed the action (null for anonymous/unauthenticated requests)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// User email (redacted as u***r@example.com for GDPR compliance)
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// User roles at the time of action (JSON array: ["Admin", "Instructor"])
    /// </summary>
    public string? UserRoles { get; set; }

    /// <summary>
    /// Client IP address (proxy-aware: X-Forwarded-For, X-Real-IP)
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// HTTP method (GET, POST, PUT, DELETE, etc.)
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// Request path (/api/auth/login, /api/admin/users/123, etc.)
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code (200, 401, 403, 500, etc.)
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Request duration in milliseconds (for performance monitoring)
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Additional details (JSON object with request/response metadata)
    /// Sensitive data redacted (passwords, tokens, credit cards)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// User-Agent header (sanitized, max 500 chars)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Referer header (sanitized, max 500 chars)
    /// </summary>
    public string? Referer { get; set; }

    /// <summary>
    /// Request ID for correlation (TraceIdentifier)
    /// </summary>
    public string? RequestId { get; set; }
}
