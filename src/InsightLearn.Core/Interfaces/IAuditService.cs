using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service interface for persistent audit logging (database-backed)
/// Provides compliance with GDPR Article 30, SOC 2, ISO 27001
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs an audit event to the database
    /// </summary>
    /// <param name="action">Event type (AUTH_LOGIN_SUCCESS, ADMIN_UPDATE, etc.)</param>
    /// <param name="entityType">Entity type affected (User, Course, null for system events)</param>
    /// <param name="entityId">Entity ID affected (null for system events)</param>
    /// <param name="userId">User ID who performed the action (null for anonymous)</param>
    /// <param name="userEmail">User email (redacted)</param>
    /// <param name="userRoles">User roles (JSON array)</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="httpMethod">HTTP method (GET, POST, etc.)</param>
    /// <param name="path">Request path</param>
    /// <param name="statusCode">HTTP status code</param>
    /// <param name="durationMs">Request duration in milliseconds</param>
    /// <param name="details">Additional details (JSON, sensitive data redacted)</param>
    /// <param name="userAgent">User-Agent header (sanitized)</param>
    /// <param name="referer">Referer header (sanitized)</param>
    /// <param name="requestId">Request ID for correlation</param>
    Task LogAsync(
        string action,
        string? entityType,
        Guid? entityId,
        Guid? userId,
        string? userEmail,
        string? userRoles,
        string ipAddress,
        string httpMethod,
        string path,
        int statusCode,
        long durationMs,
        string? details = null,
        string? userAgent = null,
        string? referer = null,
        string? requestId = null);

    /// <summary>
    /// Gets recent audit logs with pagination (for Admin dashboard)
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of records per page</param>
    /// <param name="userId">Filter by user ID (optional)</param>
    /// <param name="action">Filter by action type (optional)</param>
    /// <param name="fromDate">Filter from date (optional)</param>
    /// <param name="toDate">Filter to date (optional)</param>
    Task<(List<AuditLog> Logs, int TotalCount)> GetAuditLogsAsync(
        int page = 1,
        int pageSize = 50,
        Guid? userId = null,
        string? action = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Gets audit logs for a specific entity
    /// </summary>
    /// <param name="entityType">Entity type (User, Course, etc.)</param>
    /// <param name="entityId">Entity ID</param>
    /// <param name="limit">Maximum number of records to return</param>
    Task<List<AuditLog>> GetEntityAuditLogsAsync(string entityType, Guid entityId, int limit = 100);

    /// <summary>
    /// Cleans up old audit logs based on retention policy
    /// </summary>
    /// <param name="retentionDays">Number of days to retain (90 for auth, 2555 for payments)</param>
    /// <param name="actionPrefix">Filter by action prefix (e.g., "AUTH_", "PAYMENT_")</param>
    Task<int> CleanupOldLogsAsync(int retentionDays, string? actionPrefix = null);
}
