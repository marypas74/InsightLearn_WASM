using InsightLearn.Core.Entities;

namespace InsightLearn.Application.Interfaces;

public interface IErrorLoggingService
{
    Task<Guid> LogErrorAsync(
        Exception exception,
        string? requestId = null,
        string? correlationId = null,
        string? sessionId = null,
        Guid? userId = null,
        string? email = null,
        string? requestPath = null,
        string? httpMethod = null,
        string? requestData = null,
        int? responseStatusCode = null,
        string? ipAddress = null,
        string? userAgent = null,
        string severity = "Error",
        string? component = null,
        string? additionalData = null,
        int? retryCount = null);

    Task<Guid> LogCustomErrorAsync(
        string errorType,
        string errorMessage,
        string? requestId = null,
        string? correlationId = null,
        string? sessionId = null,
        Guid? userId = null,
        string? email = null,
        string? requestPath = null,
        string? httpMethod = null,
        string? requestData = null,
        int? responseStatusCode = null,
        string? ipAddress = null,
        string? userAgent = null,
        string severity = "Error",
        string? component = null,
        string? additionalData = null,
        string? stackTrace = null);

    Task<List<ErrorLog>> GetRecentErrorsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? severity = null,
        string? component = null,
        Guid? userId = null,
        bool? resolvedOnly = null,
        int limit = 100);

    Task<Dictionary<string, int>> GetErrorStatsByTypeAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<Dictionary<string, int>> GetErrorStatsBySeverityAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<Dictionary<string, int>> GetErrorStatsByComponentAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task<List<(string Component, int ErrorCount, DateTime LastOccurrence)>> GetTopErrorSourcesAsync(
        int topCount = 10,
        TimeSpan? timeWindow = null);

    Task<(int TotalErrors, int CriticalErrors, int UnresolvedErrors, decimal ErrorRate)> GetErrorSummaryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    Task MarkErrorResolvedAsync(Guid errorId, Guid resolvedByUserId, string? resolutionNotes = null);

    Task<List<ErrorLog>> GetUnresolvedCriticalErrorsAsync();

    Task<List<ErrorLog>> GetErrorsByCorrelationIdAsync(string correlationId);

    Task<List<ErrorLog>> GetUserErrorsAsync(Guid userId, int limit = 50);

    Task CleanupOldErrorsAsync(TimeSpan? retentionPeriod = null);

    Task<bool> ShouldSendNotificationAsync(string errorType, string severity);

    Task MarkNotificationSentAsync(Guid errorId);

    Task<List<(string IpAddress, int ErrorCount, DateTime LastError)>> GetFrequentErrorIpAddressesAsync(
        int minErrorCount = 10,
        TimeSpan? timeWindow = null);
}