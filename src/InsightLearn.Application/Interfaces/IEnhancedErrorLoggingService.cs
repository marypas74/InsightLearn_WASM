using InsightLearn.Core.Entities;

namespace InsightLearn.Application.Interfaces;

public interface IEnhancedErrorLoggingService
{
    // Enhanced error logging (extends existing ILoggingService)
    Task<Guid> LogApplicationErrorAsync(
        Exception exception,
        string? requestPath = null,
        string? httpMethod = null,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? correlationId = null,
        string? additionalContext = null);

    // API request logging
    Task<Guid> LogApiRequestAsync(
        string requestId,
        string method,
        string path,
        string? queryString = null,
        string? requestHeaders = null,
        string? requestBody = null,
        int responseStatusCode = 0,
        string? responseHeaders = null,
        string? responseBody = null,
        long requestSize = 0,
        long responseSize = 0,
        long durationMs = 0,
        string? ipAddress = null,
        string? userAgent = null,
        string? referer = null,
        Guid? userId = null,
        string? sessionId = null,
        string? correlationId = null,
        string? exception = null,
        bool? cacheHit = null,
        int databaseQueries = 0,
        long databaseDurationMs = 0,
        decimal? memoryUsageMB = null,
        long? cpuUsageMs = null,
        string? apiVersion = null,
        string? clientApp = null,
        string? feature = null);

    // Database error logging
    Task<Guid> LogDatabaseErrorAsync(
        Exception exception,
        string? sqlCommand = null,
        string? parameters = null,
        string? databaseName = null,
        string? tableName = null,
        string? procedureName = null,
        long? executionTimeMs = null,
        int? rowsAffected = null,
        string? transactionId = null,
        string? isolationLevel = null,
        Guid? userId = null,
        string? requestId = null,
        string? correlationId = null);

    // Validation error logging
    Task<Guid> LogValidationErrorAsync(
        string validationSource,
        string fieldName,
        string? fieldValue,
        string validationRule,
        string errorMessage,
        string modelType,
        string? requestPath = null,
        string? httpMethod = null,
        Guid? userId = null,
        string? sessionId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? requestData = null,
        string? correlationId = null,
        bool isClientSideValidated = false,
        string? validationCategory = null,
        string? requestId = null);

    // Performance metrics
    Task LogPerformanceMetricAsync(
        string metricType,
        string metricName,
        decimal value,
        string unit,
        string source,
        string? component = null,
        string? requestId = null,
        Guid? userId = null,
        string? tags = null,
        decimal? threshold = null,
        string environment = "Production");

    // Entity audit logging
    Task LogEntityAuditAsync(
        string entityType,
        string entityId,
        string action,
        string? propertyName = null,
        string? oldValue = null,
        string? newValue = null,
        Guid? userId = null,
        string? userEmail = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? requestId = null,
        string? reason = null,
        string? changeSource = null,
        Guid? batchId = null,
        string? additionalContext = null);

    // Error analysis and reporting
    Task<List<ErrorLog>> GetErrorsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? severity = null,
        string? exceptionType = null,
        bool? resolvedOnly = null,
        int limit = 100);

    Task<Dictionary<string, int>> GetErrorFrequencyByTypeAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<List<(string Path, int ErrorCount, decimal AvgDurationMs)>> GetApiEndpointErrorRatesAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<List<DatabaseErrorLog>> GetDatabaseErrorsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? errorType = null,
        string? tableName = null,
        int limit = 100);

    Task<List<ValidationErrorLog>> GetValidationErrorsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? fieldName = null,
        string? validationRule = null,
        int limit = 100);

    // Performance analysis
    Task<List<PerformanceMetric>> GetPerformanceMetricsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? metricType = null,
        string? component = null,
        bool? alertsOnly = null,
        int limit = 100);

    Task<Dictionary<string, decimal>> GetAveragePerformanceMetricsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? component = null);

    // Correlation and tracing
    Task<List<object>> GetCorrelatedLogsAsync(string correlationId);
    Task<List<object>> GetRequestTraceAsync(string requestId);

    // Error resolution
    Task MarkErrorResolvedAsync(Guid errorId, Guid resolvedByUserId, string resolutionNotes);
    Task MarkMultipleErrorsResolvedAsync(List<Guid> errorIds, Guid resolvedByUserId, string resolutionNotes);

    // Alerts and notifications
    Task CheckErrorThresholdsAsync();
    Task SendErrorAlertAsync(string alertType, string details, string severity = "Medium");

    // Cleanup and maintenance
    Task CleanupOldLogsAsync(string logType, int retentionDays);
    Task ArchiveOldLogsAsync(string logType, int archiveAfterDays);
    Task CompressArchivedLogsAsync();
}