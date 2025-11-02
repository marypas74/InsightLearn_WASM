using InsightLearn.Application.Interfaces;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace InsightLearn.Application.Services;

public class ErrorLoggingService : IErrorLoggingService
{
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<ErrorLoggingService> _logger;

    public ErrorLoggingService(InsightLearnDbContext context, ILogger<ErrorLoggingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> LogErrorAsync(
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
        int? retryCount = null)
    {
        try
        {
            var stackTrace = new StackTrace(exception, true);
            var frame = stackTrace.GetFrame(0);
            
            var errorLog = new ErrorLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExceptionType = exception.GetType().Name,
                ExceptionMessage = exception.Message,
                StackTrace = exception.StackTrace,
                RequestPath = requestPath,
                HttpMethod = httpMethod,
                ResponseStatusCode = responseStatusCode,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Severity = severity,
                Source = exception.Source,
                LoggedAt = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new 
                {
                    RequestId = requestId,
                    CorrelationId = correlationId,
                    SessionId = sessionId,
                    Email = email,
                    RequestData = requestData,
                    Component = component ?? GetCallingComponent(),
                    Method = frame?.GetMethod()?.Name,
                    LineNumber = frame?.GetFileLineNumber(),
                    InnerException = exception.InnerException?.ToString(),
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    RetryCount = retryCount
                })
            };

            _context.ErrorLogs.Add(errorLog);
            await _context.SaveChangesAsync();

            _logger.LogError(exception, 
                "Error logged: {ErrorType} - {Message}",
                errorLog.ExceptionType, errorLog.ExceptionMessage);

            return errorLog.Id;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to log error to database. Original exception: {OriginalException}", exception.Message);
            throw;
        }
    }

    public async Task<Guid> LogCustomErrorAsync(
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
        string? stackTrace = null)
    {
        try
        {
            var errorLog = new ErrorLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ExceptionType = errorType,
                ExceptionMessage = errorMessage,
                StackTrace = stackTrace,
                RequestPath = requestPath,
                HttpMethod = httpMethod,
                ResponseStatusCode = responseStatusCode,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Severity = severity,
                Source = "CustomError",
                LoggedAt = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new 
                {
                    RequestId = requestId,
                    CorrelationId = correlationId,
                    SessionId = sessionId,
                    Email = email,
                    RequestData = requestData,
                    Component = component ?? GetCallingComponent(),
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    CustomAdditionalData = additionalData
                })
            };

            _context.ErrorLogs.Add(errorLog);
            await _context.SaveChangesAsync();

            _logger.LogError("Custom error logged: {ErrorType} - {Message}",
                errorLog.ExceptionType, errorLog.ExceptionMessage);

            return errorLog.Id;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to log custom error to database: {ErrorType} - {ErrorMessage}", errorType, errorMessage);
            throw;
        }
    }

    public async Task<List<ErrorLog>> GetRecentErrorsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? severity = null,
        string? component = null,
        Guid? userId = null,
        bool? resolvedOnly = null,
        int limit = 100)
    {
        try
        {
            var query = _context.ErrorLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.LoggedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.LoggedAt <= endDate.Value);

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(e => e.Severity == severity);

            // Component filter temporarily disabled (property moved to AdditionalData)
            // if (!string.IsNullOrEmpty(component))
            //     query = query.Where(e => e.Component == component);

            if (userId.HasValue)
                query = query.Where(e => e.UserId == userId.Value);

            if (resolvedOnly.HasValue)
                query = query.Where(e => e.IsResolved == resolvedOnly.Value);

            return await query
                .OrderByDescending(e => e.LoggedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent errors");
            return new List<ErrorLog>();
        }
    }

    public async Task<Dictionary<string, int>> GetErrorStatsByTypeAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.ErrorLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.LoggedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.LoggedAt <= endDate.Value);

            return await query
                .GroupBy(e => e.ExceptionType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error stats by type");
            return new Dictionary<string, int>();
        }
    }

    public async Task<Dictionary<string, int>> GetErrorStatsBySeverityAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.ErrorLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.LoggedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.LoggedAt <= endDate.Value);

            return await query
                .GroupBy(e => e.Severity)
                .Select(g => new { Severity = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Severity, x => x.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error stats by severity");
            return new Dictionary<string, int>();
        }
    }

    public async Task<Dictionary<string, int>> GetErrorStatsByComponentAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.ErrorLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.LoggedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.LoggedAt <= endDate.Value);

            // Component stats temporarily disabled (property moved to AdditionalData)
            return new Dictionary<string, int>();
            /*
            return await query
                .Where(e => e.Component != null)
                .GroupBy(e => e.Component!)
                .Select(g => new { Component = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Component, x => x.Count);
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error stats by component");
            return new Dictionary<string, int>();
        }
    }

    public async Task<List<(string Component, int ErrorCount, DateTime LastOccurrence)>> GetTopErrorSourcesAsync(
        int topCount = 10,
        TimeSpan? timeWindow = null)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.Subtract(timeWindow ?? TimeSpan.FromDays(7));

            // Top error sources temporarily disabled (Component property moved to AdditionalData)
            return new List<(string Component, int ErrorCount, DateTime LastOccurrence)>();
            /*
            return await _context.ErrorLogs
                .Where(e => e.LoggedAt >= cutoffDate && e.Component != null)
                .GroupBy(e => e.Component!)
                .Select(g => new
                {
                    Component = g.Key,
                    ErrorCount = g.Count(),
                    LastOccurrence = g.Max(x => x.LoggedAt)
                })
                .OrderByDescending(x => x.ErrorCount)
                .Take(topCount)
                .Select(x => ValueTuple.Create(x.Component, x.ErrorCount, x.LastOccurrence))
                .ToListAsync();
            */
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top error sources");
            return new List<(string, int, DateTime)>();
        }
    }

    public async Task<(int TotalErrors, int CriticalErrors, int UnresolvedErrors, decimal ErrorRate)> GetErrorSummaryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.ErrorLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.LoggedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.LoggedAt <= endDate.Value);

            var totalErrors = await query.CountAsync();
            var criticalErrors = await query.CountAsync(e => e.Severity == "Critical");
            var unresolvedErrors = await query.CountAsync(e => !e.IsResolved);

            var totalRequests = await _context.ApiRequestLogs
                .Where(r => (!startDate.HasValue || r.RequestedAt >= startDate.Value) &&
                           (!endDate.HasValue || r.RequestedAt <= endDate.Value))
                .CountAsync();

            var errorRate = totalRequests > 0 ? (decimal)totalErrors / totalRequests * 100 : 0;

            return (totalErrors, criticalErrors, unresolvedErrors, errorRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error summary");
            return (0, 0, 0, 0);
        }
    }

    public async Task MarkErrorResolvedAsync(Guid errorId, Guid resolvedByUserId, string? resolutionNotes = null)
    {
        try
        {
            var errorLog = await _context.ErrorLogs.FindAsync(errorId);
            if (errorLog != null)
            {
                errorLog.IsResolved = true;
                errorLog.ResolvedAt = DateTime.UtcNow;
                errorLog.ResolvedByUserId = resolvedByUserId;
                errorLog.ResolutionNotes = resolutionNotes;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Error {ErrorId} marked as resolved by user {UserId}", errorId, resolvedByUserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark error {ErrorId} as resolved", errorId);
            throw;
        }
    }

    public async Task<List<ErrorLog>> GetUnresolvedCriticalErrorsAsync()
    {
        try
        {
            return await _context.ErrorLogs
                .Where(e => !e.IsResolved && e.Severity == "Critical")
                .OrderByDescending(e => e.LoggedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get unresolved critical errors");
            return new List<ErrorLog>();
        }
    }

    public async Task<List<ErrorLog>> GetErrorsByCorrelationIdAsync(string correlationId)
    {
        try
        {
            // Since CorrelationId is now in AdditionalData, search in JSON field
            return await _context.ErrorLogs
                .Where(e => e.AdditionalData != null && e.AdditionalData.Contains(correlationId))
                .OrderBy(e => e.LoggedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get errors by correlation ID {CorrelationId}", correlationId);
            return new List<ErrorLog>();
        }
    }

    public async Task<List<ErrorLog>> GetUserErrorsAsync(Guid userId, int limit = 50)
    {
        try
        {
            return await _context.ErrorLogs
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.LoggedAt)
                .Take(limit)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get errors for user {UserId}", userId);
            return new List<ErrorLog>();
        }
    }

    public async Task CleanupOldErrorsAsync(TimeSpan? retentionPeriod = null)
    {
        try
        {
            var retention = retentionPeriod ?? TimeSpan.FromDays(90);
            var cutoffDate = DateTime.UtcNow.Subtract(retention);

            var oldErrors = await _context.ErrorLogs
                .Where(e => e.LoggedAt < cutoffDate && e.IsResolved)
                .ToListAsync();

            if (oldErrors.Any())
            {
                _context.ErrorLogs.RemoveRange(oldErrors);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cleaned up {Count} old error logs older than {CutoffDate}", 
                    oldErrors.Count, cutoffDate);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old errors");
        }
    }

    public async Task<bool> ShouldSendNotificationAsync(string errorType, string severity)
    {
        try
        {
            if (severity == "Critical")
                return true;

            // NotificationSent functionality temporarily disabled (property moved to AdditionalData)
            var recentSimilarErrors = await _context.ErrorLogs
                .CountAsync(e => e.ExceptionType == errorType && 
                               e.LoggedAt >= DateTime.UtcNow.AddMinutes(-30));

            return recentSimilarErrors == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check notification requirement for {ErrorType}", errorType);
            return false;
        }
    }

    public async Task MarkNotificationSentAsync(Guid errorId)
    {
        try
        {
            // NotificationSent functionality temporarily disabled (property moved to AdditionalData)
            var errorLog = await _context.ErrorLogs.FindAsync(errorId);
            if (errorLog != null)
            {
                // errorLog.NotificationSent = true;
                // TODO: Update AdditionalData to mark notification as sent
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification sent for error {ErrorId}", errorId);
        }
    }

    public async Task<List<(string IpAddress, int ErrorCount, DateTime LastError)>> GetFrequentErrorIpAddressesAsync(
        int minErrorCount = 10,
        TimeSpan? timeWindow = null)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.Subtract(timeWindow ?? TimeSpan.FromHours(24));

            return await _context.ErrorLogs
                .Where(e => e.LoggedAt >= cutoffDate && e.IpAddress != null)
                .GroupBy(e => e.IpAddress!)
                .Select(g => new
                {
                    IpAddress = g.Key,
                    ErrorCount = g.Count(),
                    LastError = g.Max(x => x.LoggedAt)
                })
                .Where(x => x.ErrorCount >= minErrorCount)
                .OrderByDescending(x => x.ErrorCount)
                .Select(x => ValueTuple.Create(x.IpAddress, x.ErrorCount, x.LastError))
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get frequent error IP addresses");
            return new List<(string, int, DateTime)>();
        }
    }

    private string GetCallingComponent()
    {
        var stackTrace = new StackTrace();
        var frames = stackTrace.GetFrames();
        
        foreach (var frame in frames)
        {
            var method = frame.GetMethod();
            if (method?.DeclaringType != null && 
                method.DeclaringType != typeof(ErrorLoggingService) &&
                !method.DeclaringType.Name.Contains("Logger"))
            {
                return method.DeclaringType.Name;
            }
        }
        
        return "Unknown";
    }
}