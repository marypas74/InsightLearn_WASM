using InsightLearn.Application.Interfaces;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InsightLearn.Application.Services;

public class LoggingService : ILoggingService
{
    private readonly IDbContextFactory<InsightLearnDbContext> _contextFactory;
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(IDbContextFactory<InsightLearnDbContext> contextFactory, ILogger<LoggingService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    private static string? TruncateString(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    public async Task LogErrorAsync(
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
        string? additionalData = null)
    {
        try
        {
            // 🔥 CRITICAL FIX: Use DbContextFactory to prevent threading issues
            using var context = _contextFactory.CreateDbContext();
            
            // Validate UserId exists if provided
            Guid? validUserId = null;
            if (userId != null)
            {
                var userExists = await context.Users.AnyAsync(u => u.Id == userId.Value);
                if (userExists)
                {
                    validUserId = userId;
                }
            }

            var errorLog = new ErrorLog
            {
                Id = Guid.NewGuid(),
                UserId = validUserId,
                ExceptionType = TruncateString(exceptionType, 500),
                ExceptionMessage = TruncateString(exceptionMessage, 2000),
                StackTrace = stackTrace,
                RequestPath = TruncateString(requestPath, 1000),
                HttpMethod = TruncateString(httpMethod, 20),
                IpAddress = TruncateString(ipAddress, 45),
                UserAgent = TruncateString(userAgent, 1000),
                Severity = TruncateString(severity, 20),
                Source = TruncateString(source, 100),
                LoggedAt = DateTime.UtcNow,
                AdditionalData = additionalData
            };

            context.ErrorLogs.Add(errorLog);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log error to database");
        }
    }

    public async Task LogAccessAsync(
        string requestPath,
        string httpMethod,
        int statusCode,
        long? responseTimeMs = null,
        Guid? userId = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? referer = null,
        string? sessionId = null,
        string? additionalData = null)
    {
        try
        {
            // 🔥 CRITICAL FIX: Use DbContextFactory to prevent threading issues
            using var context = _contextFactory.CreateDbContext();
            
            var accessLog = new AccessLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                IpAddress = ipAddress ?? "unknown",
                RequestPath = requestPath,
                HttpMethod = httpMethod,
                UserAgent = userAgent,
                Referer = referer,
                StatusCode = statusCode,
                ResponseTimeMs = responseTimeMs,
                AccessedAt = DateTime.UtcNow,
                SessionId = sessionId,
                AdditionalData = additionalData
            };

            context.AccessLogs.Add(accessLog);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log access to database");
        }
    }

    public async Task LogInformationAsync(
        string message,
        string? logger = null,
        Guid? userId = null,
        string? correlationId = null,
        object? properties = null)
    {
        await LogEntryAsync("Information", message, logger, userId, correlationId, properties);
    }

    public async Task LogWarningAsync(
        string message,
        string? logger = null,
        Guid? userId = null,
        string? correlationId = null,
        object? properties = null)
    {
        await LogEntryAsync("Warning", message, logger, userId, correlationId, properties);
    }

    public async Task LogDebugAsync(
        string message,
        string? logger = null,
        Guid? userId = null,
        string? correlationId = null,
        object? properties = null)
    {
        await LogEntryAsync("Debug", message, logger, userId, correlationId, properties);
    }

    private async Task LogEntryAsync(
        string level,
        string message,
        string? logger = null,
        Guid? userId = null,
        string? correlationId = null,
        object? properties = null)
    {
        try
        {
            // 🔥 CRITICAL FIX: Use DbContextFactory to prevent threading issues
            using var context = _contextFactory.CreateDbContext();
            
            var logEntry = new LogEntry
            {
                Id = Guid.NewGuid(),
                Level = level,
                Message = message,
                Logger = logger,
                Application = "InsightLearn",
                MachineName = Environment.MachineName,
                UserId = userId,
                CorrelationId = correlationId,
                Timestamp = DateTime.UtcNow,
                Properties = properties != null ? JsonSerializer.Serialize(properties) : null,
                ProcessId = Environment.ProcessId.ToString()
            };

            context.LogEntries.Add(logEntry);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log entry to database");
        }
    }

    public async Task CleanupOldLogsAsync(int retentionDays = 30)
    {
        try
        {
            // 🔥 CRITICAL FIX: Use DbContextFactory to prevent threading issues
            using var context = _contextFactory.CreateDbContext();
            
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            // Clean up old log entries
            var oldLogEntries = await context.LogEntries
                .Where(l => l.Timestamp < cutoffDate)
                .ToListAsync();

            if (oldLogEntries.Any())
            {
                context.LogEntries.RemoveRange(oldLogEntries);
                _logger.LogInformation($"Cleaned up {oldLogEntries.Count} old log entries");
            }

            // Clean up old access logs
            var oldAccessLogs = await context.AccessLogs
                .Where(l => l.AccessedAt < cutoffDate)
                .ToListAsync();

            if (oldAccessLogs.Any())
            {
                context.AccessLogs.RemoveRange(oldAccessLogs);
                _logger.LogInformation($"Cleaned up {oldAccessLogs.Count} old access logs");
            }

            // Clean up resolved error logs older than retention period
            var oldErrorLogs = await context.ErrorLogs
                .Where(l => l.LoggedAt < cutoffDate && l.IsResolved)
                .ToListAsync();

            if (oldErrorLogs.Any())
            {
                context.ErrorLogs.RemoveRange(oldErrorLogs);
                _logger.LogInformation($"Cleaned up {oldErrorLogs.Count} old resolved error logs");
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old logs");
        }
    }

    public async Task LogAdminActionAsync(
        string adminUserName,
        string action,
        string? entityType = null,
        Guid? entityId = null,
        string? description = null,
        string severity = "Information",
        string? ipAddress = null)
    {
        try
        {
            // Note: This is a simplified implementation. In a real scenario, you'd need to resolve the username to UserId
            // For now, we'll skip this implementation as it requires UserManager dependency
            _logger.LogInformation("Admin action logged: {Action} by {AdminUser}", action, adminUserName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log admin action: {Action} by {AdminUser}", action, adminUserName);
        }
    }
}