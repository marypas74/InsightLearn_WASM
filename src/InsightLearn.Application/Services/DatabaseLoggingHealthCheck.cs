using Microsoft.Extensions.Diagnostics.HealthChecks;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

public class DatabaseLoggingHealthCheck : IHealthCheck
{
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<DatabaseLoggingHealthCheck> _logger;

    public DatabaseLoggingHealthCheck(InsightLearnDbContext context, ILogger<DatabaseLoggingHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if we can connect to the database
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            // Check if logging tables exist
            var logEntriesTableExists = await TableExistsAsync("LogEntries", cancellationToken);
            var errorLogsTableExists = await TableExistsAsync("ErrorLogs", cancellationToken);
            var accessLogsTableExists = await TableExistsAsync("AccessLogs", cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["LogEntriesTable"] = logEntriesTableExists,
                ["ErrorLogsTable"] = errorLogsTableExists,
                ["AccessLogsTable"] = accessLogsTableExists,
                ["DatabaseConnection"] = canConnect
            };

            if (!logEntriesTableExists || !errorLogsTableExists || !accessLogsTableExists)
            {
                return HealthCheckResult.Degraded("Some logging tables are missing", data: data);
            }

            // Test logging functionality
            try
            {
                var testLogEntry = new Core.Entities.LogEntry
                {
                    Id = Guid.NewGuid(),
                    Level = "Information",
                    Message = "Health check test log entry",
                    Application = "HealthCheck",
                    Timestamp = DateTime.UtcNow,
                    Logger = "DatabaseLoggingHealthCheck"
                };

                _context.LogEntries.Add(testLogEntry);
                await _context.SaveChangesAsync(cancellationToken);

                // Clean up test entry
                _context.LogEntries.Remove(testLogEntry);
                await _context.SaveChangesAsync(cancellationToken);

                data["LoggingTest"] = "Successful";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Logging test failed during health check");
                data["LoggingTest"] = $"Failed: {ex.Message}";
                return HealthCheckResult.Degraded("Database logging test failed", data: data);
            }

            return HealthCheckResult.Healthy("Database logging is working correctly", data: data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return HealthCheckResult.Unhealthy($"Health check failed: {ex.Message}");
        }
    }

    private async Task<bool> TableExistsAsync(string tableName, CancellationToken cancellationToken)
    {
        try
        {
            // Simplified table existence check - directly check if we can access the table
            if (tableName == "ErrorLogs")
            {
                var count = await _context.ErrorLogs.CountAsync(cancellationToken);
                return true;
            }
            else if (tableName == "LogEntries")
            {
                var count = await _context.LogEntries.CountAsync(cancellationToken);
                return true;
            }
            else if (tableName == "AccessLogs")
            {
                var count = await _context.AccessLogs.CountAsync(cancellationToken);
                return true;
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
}