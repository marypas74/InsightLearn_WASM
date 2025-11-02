namespace InsightLearn.Application.Interfaces;

public interface ILoggingService
{
    Task LogErrorAsync(
        string exceptionType,
        string exceptionMessage,
        string? stackTrace,
        string? requestPath = null,
        string? httpMethod = null,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string severity = "Error",
        string? source = null,
        string? additionalData = null);

    Task LogAccessAsync(
        string requestPath,
        string httpMethod,
        int statusCode,
        long? responseTimeMs = null,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? referer = null,
        string? sessionId = null,
        string? additionalData = null);

    Task LogInformationAsync(
        string message,
        string? logger = null,
        Guid? userId = null,
        string? correlationId = null,
        object? properties = null);

    Task LogWarningAsync(
        string message,
        string? logger = null,
        Guid? userId = null,
        string? correlationId = null,
        object? properties = null);

    Task LogDebugAsync(
        string message,
        string? logger = null,
        Guid? userId = null,
        string? correlationId = null,
        object? properties = null);

    Task CleanupOldLogsAsync(int retentionDays = 30);
    
    Task LogAdminActionAsync(
        string adminUserName,
        string action,
        string? entityType = null,
        Guid? entityId = null,
        string? description = null,
        string severity = "Information",
        string? ipAddress = null);
}