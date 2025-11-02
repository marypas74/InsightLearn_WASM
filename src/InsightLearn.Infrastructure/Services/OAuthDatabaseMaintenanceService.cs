using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using InsightLearn.Infrastructure.Data;
using System.Diagnostics;

namespace InsightLearn.Infrastructure.Services;

/// <summary>
/// Background service for OAuth database maintenance and performance monitoring.
/// Handles cleanup of expired sessions, token management, and performance analytics.
/// </summary>
public class OAuthDatabaseMaintenanceService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OAuthDatabaseMaintenanceService> _logger;
    private readonly OAuthMaintenanceOptions _options;
    private readonly Timer _cleanupTimer;
    private readonly Timer _performanceTimer;

    public OAuthDatabaseMaintenanceService(
        IServiceProvider serviceProvider,
        ILogger<OAuthDatabaseMaintenanceService> logger,
        IOptions<OAuthMaintenanceOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;

        // Setup timers for different maintenance tasks
        _cleanupTimer = new Timer(PerformCleanup, null, TimeSpan.Zero, TimeSpan.FromHours(1));
        _performanceTimer = new Timer(AnalyzePerformance, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OAuth Database Maintenance Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("OAuth Database Maintenance Service stopped");
    }

    /// <summary>
    /// Performs cleanup of expired OAuth sessions and tokens.
    /// </summary>
    private async void PerformCleanup(object? state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightLearnDbContext>();

            var stopwatch = Stopwatch.StartNew();

            await CleanupExpiredSessions(dbContext);
            await CleanupOldLoginAttempts(dbContext);
            await CleanupExpiredTokens(dbContext);
            await OptimizeIndexes(dbContext);

            stopwatch.Stop();

            _logger.LogInformation(
                "OAuth cleanup completed in {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OAuth database cleanup");
        }
    }

    /// <summary>
    /// Analyzes OAuth performance metrics and logs insights.
    /// </summary>
    private async void AnalyzePerformance(object? state)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightLearnDbContext>();

            await AnalyzeAuthenticationPerformance(dbContext);
            await MonitorSecurityEvents(dbContext);
            await CheckIndexFragmentation(dbContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OAuth performance analysis");
        }
    }

    /// <summary>
    /// Cleans up expired user sessions older than the configured threshold.
    /// </summary>
    private async Task CleanupExpiredSessions(InsightLearnDbContext dbContext)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-_options.SessionTimeoutHours);

        var expiredSessionsCount = await dbContext.Database.ExecuteSqlRawAsync(@"
            UPDATE UserSessions
            SET IsActive = 0,
                EndedAt = GETUTCDATE(),
                EndReason = 'Timeout'
            WHERE IsActive = 1
                AND LastActivityAt < {0}",
            cutoffTime);

        if (expiredSessionsCount > 0)
        {
            _logger.LogInformation(
                "Cleaned up {ExpiredCount} expired OAuth sessions",
                expiredSessionsCount);
        }
    }

    /// <summary>
    /// Removes old login attempts to maintain database performance.
    /// </summary>
    private async Task CleanupOldLoginAttempts(InsightLearnDbContext dbContext)
    {
        var cutoffTime = DateTime.UtcNow.AddDays(-_options.LoginAttemptsRetentionDays);

        var deletedCount = await dbContext.Database.ExecuteSqlRawAsync(@"
            DELETE FROM LoginAttempts
            WHERE AttemptedAt < {0}",
            cutoffTime);

        if (deletedCount > 0)
        {
            _logger.LogInformation(
                "Cleaned up {DeletedCount} old login attempts",
                deletedCount);
        }
    }

    /// <summary>
    /// Cleans up expired OAuth tokens from AspNetUserTokens table.
    /// </summary>
    private async Task CleanupExpiredTokens(InsightLearnDbContext dbContext)
    {
        // Clean up expired Google OAuth refresh tokens
        var cutoffTime = DateTime.UtcNow.AddDays(-_options.TokenRetentionDays);

        var expiredTokensCount = await dbContext.Database.ExecuteSqlRawAsync(@"
            DELETE ut FROM UserTokens ut
            INNER JOIN Users u ON ut.UserId = u.Id
            WHERE ut.LoginProvider = 'Google'
                AND u.GoogleTokenExpiry IS NOT NULL
                AND u.GoogleTokenExpiry < {0}",
            cutoffTime);

        if (expiredTokensCount > 0)
        {
            _logger.LogInformation(
                "Cleaned up {ExpiredCount} expired OAuth tokens",
                expiredTokensCount);
        }
    }

    /// <summary>
    /// Optimizes OAuth-related database indexes for performance.
    /// </summary>
    private async Task OptimizeIndexes(InsightLearnDbContext dbContext)
    {
        try
        {
            // Rebuild fragmented indexes for OAuth tables
            await dbContext.Database.ExecuteSqlRawAsync(@"
                -- Rebuild indexes with fragmentation > 30%
                DECLARE @sql NVARCHAR(MAX) = '';
                SELECT @sql = @sql + 'ALTER INDEX ' + i.name + ' ON ' + t.name + ' REBUILD;' + CHAR(13)
                FROM sys.indexes i
                INNER JOIN sys.tables t ON i.object_id = t.object_id
                INNER JOIN sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ps
                    ON i.object_id = ps.object_id AND i.index_id = ps.index_id
                WHERE t.name IN ('Users', 'UserLogins', 'UserSessions', 'LoginAttempts', 'UserTokens')
                    AND ps.avg_fragmentation_in_percent > 30
                    AND i.name IS NOT NULL;
                EXEC sp_executesql @sql;
            ");

            _logger.LogDebug("OAuth index optimization completed");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not optimize OAuth indexes");
        }
    }

    /// <summary>
    /// Analyzes authentication performance metrics.
    /// </summary>
    private async Task AnalyzeAuthenticationPerformance(InsightLearnDbContext dbContext)
    {
        try
        {
            var metrics = await dbContext.Database
                .SqlQuery<AuthenticationMetric>($@"
                    SELECT
                        LoginMethod,
                        COUNT(*) as TotalAttempts,
                        SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) as SuccessfulAttempts,
                        AVG(CASE WHEN IsSuccess = 1 THEN DATEDIFF(ms, AttemptedAt, DATEADD(second, 1, AttemptedAt)) ELSE NULL END) as AvgResponseTimeMs
                    FROM LoginAttempts
                    WHERE AttemptedAt >= DATEADD(hour, -1, GETUTCDATE())
                    GROUP BY LoginMethod
                ")
                .ToListAsync();

            foreach (var metric in metrics)
            {
                var successRate = (double)metric.SuccessfulAttempts / metric.TotalAttempts * 100;

                _logger.LogInformation(
                    "OAuth {Method}: {Total} attempts, {Success}% success rate, {AvgMs}ms avg response",
                    metric.LoginMethod,
                    metric.TotalAttempts,
                    successRate.ToString("F1"),
                    metric.AvgResponseTimeMs?.ToString("F0") ?? "N/A");

                // Alert on poor performance
                if (successRate < 95 && metric.TotalAttempts > 10)
                {
                    _logger.LogWarning(
                        "Low OAuth success rate detected for {Method}: {Rate}%",
                        metric.LoginMethod,
                        successRate.ToString("F1"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing authentication performance");
        }
    }

    /// <summary>
    /// Monitors OAuth-related security events.
    /// </summary>
    private async Task MonitorSecurityEvents(InsightLearnDbContext dbContext)
    {
        try
        {
            // Check for suspicious login patterns
            var suspiciousIPs = await dbContext.Database
                .SqlQuery<SuspiciousIPMetric>($@"
                    SELECT
                        IpAddress,
                        COUNT(*) as FailedAttempts,
                        COUNT(DISTINCT Email) as UniqueEmails,
                        MAX(AttemptedAt) as LastAttempt
                    FROM LoginAttempts
                    WHERE IsSuccess = 0
                        AND AttemptedAt >= DATEADD(hour, -1, GETUTCDATE())
                        AND LoginMethod LIKE '%Google%'
                    GROUP BY IpAddress
                    HAVING COUNT(*) >= {_options.SecurityAlertThreshold}
                ")
                .ToListAsync();

            foreach (var suspiciousIP in suspiciousIPs)
            {
                _logger.LogWarning(
                    "Suspicious OAuth activity from IP {IP}: {Failures} failed attempts for {Emails} unique emails",
                    suspiciousIP.IpAddress,
                    suspiciousIP.FailedAttempts,
                    suspiciousIP.UniqueEmails);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error monitoring OAuth security events");
        }
    }

    /// <summary>
    /// Checks index fragmentation for OAuth-critical indexes.
    /// </summary>
    private async Task CheckIndexFragmentation(InsightLearnDbContext dbContext)
    {
        try
        {
            var fragmentedIndexes = await dbContext.Database
                .SqlQuery<IndexFragmentationMetric>($@"
                    SELECT
                        t.name as TableName,
                        i.name as IndexName,
                        ps.avg_fragmentation_in_percent as FragmentationPercent
                    FROM sys.indexes i
                    INNER JOIN sys.tables t ON i.object_id = t.object_id
                    INNER JOIN sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ps
                        ON i.object_id = ps.object_id AND i.index_id = ps.index_id
                    WHERE t.name IN ('Users', 'UserLogins', 'UserSessions', 'LoginAttempts')
                        AND i.name LIKE 'IX_%OAuth%' OR i.name LIKE 'IX_%Google%'
                        AND ps.avg_fragmentation_in_percent > 10
                ")
                .ToListAsync();

            foreach (var index in fragmentedIndexes)
            {
                _logger.LogInformation(
                    "OAuth index fragmentation: {Table}.{Index} = {Fragmentation}%",
                    index.TableName,
                    index.IndexName,
                    index.FragmentationPercent.ToString("F1"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not check OAuth index fragmentation");
        }
    }

    public override void Dispose()
    {
        _cleanupTimer?.Dispose();
        _performanceTimer?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Configuration options for OAuth database maintenance.
/// </summary>
public class OAuthMaintenanceOptions
{
    /// <summary>
    /// Hours after which inactive sessions are considered expired.
    /// </summary>
    public int SessionTimeoutHours { get; set; } = 24;

    /// <summary>
    /// Days to retain login attempt records.
    /// </summary>
    public int LoginAttemptsRetentionDays { get; set; } = 30;

    /// <summary>
    /// Days to retain expired OAuth tokens.
    /// </summary>
    public int TokenRetentionDays { get; set; } = 7;

    /// <summary>
    /// Number of failed attempts from single IP to trigger security alert.
    /// </summary>
    public int SecurityAlertThreshold { get; set; } = 10;
}

/// <summary>
/// Authentication performance metrics.
/// </summary>
public class AuthenticationMetric
{
    public string LoginMethod { get; set; } = string.Empty;
    public int TotalAttempts { get; set; }
    public int SuccessfulAttempts { get; set; }
    public double? AvgResponseTimeMs { get; set; }
}

/// <summary>
/// Suspicious IP activity metrics.
/// </summary>
public class SuspiciousIPMetric
{
    public string IpAddress { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public int UniqueEmails { get; set; }
    public DateTime LastAttempt { get; set; }
}

/// <summary>
/// Index fragmentation metrics.
/// </summary>
public class IndexFragmentationMetric
{
    public string TableName { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public double FragmentationPercent { get; set; }
}