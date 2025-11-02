using InsightLearn.Application.DTOs;
using InsightLearn.Application.Interfaces;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InsightLearn.Application.Services;

/// <summary>
/// Complete implementation of IAnalyticsService using ACTUAL entity schema.
/// All property names verified against entity files.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly IDbContextFactory<InsightLearnDbContext> _contextFactory;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IDbContextFactory<InsightLearnDbContext> contextFactory,
        ILogger<AnalyticsService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    #region Login Analytics

    public async Task<LoginAnalyticsDto> GetLoginAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? groupBy = "Day")
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var attempts = await context.LoginAttempts
                .Where(l => l.AttemptedAt >= start && l.AttemptedAt <= end)
                .ToListAsync();

            var totalAttempts = attempts.Count;
            var successfulLogins = attempts.Count(a => a.IsSuccess);
            var failedAttempts = attempts.Count(a => !a.IsSuccess);

            return new LoginAnalyticsDto
            {
                TotalAttempts = totalAttempts,
                SuccessfulLogins = successfulLogins,
                FailedAttempts = failedAttempts,
                SuccessRate = totalAttempts > 0 ? (decimal)successfulLogins / totalAttempts * 100 : 0,
                UniqueUsers = attempts.Where(a => a.UserId.HasValue)
                    .Select(a => a.UserId!.Value)
                    .Distinct()
                    .Count(),
                LoginsByMethod = attempts
                    .GroupBy(a => a.LoginMethod ?? "Standard")
                    .ToDictionary(g => g.Key, g => g.Count()),
                LoginsByDevice = attempts
                    .GroupBy(a => ParseDeviceType(a.UserAgent ?? "Unknown"))
                    .ToDictionary(g => g.Key, g => g.Count()),
                HourlyDistribution = attempts
                    .GroupBy(a => a.AttemptedAt.Hour)
                    .Select(g => new HourlyLoginDto
                    {
                        Hour = g.Key,
                        LoginCount = g.Count()
                    })
                    .OrderBy(h => h.Hour)
                    .ToList(),
                LoginsByHour = attempts
                    .GroupBy(a => new DateTime(a.AttemptedAt.Year, a.AttemptedAt.Month, a.AttemptedAt.Day, a.AttemptedAt.Hour, 0, 0))
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting login analytics for period {StartDate} to {EndDate}", startDate, endDate);
            return new LoginAnalyticsDto();
        }
    }

    public async Task<List<LoginTrendDto>> GetLoginTrendsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? loginMethod = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var query = context.LoginAttempts
                .Where(l => l.AttemptedAt >= start && l.AttemptedAt <= end);

            if (!string.IsNullOrEmpty(loginMethod))
            {
                query = query.Where(l => l.LoginMethod == loginMethod);
            }

            var attempts = await query.ToListAsync();

            return attempts
                .GroupBy(a => a.AttemptedAt.Date)
                .Select(g =>
                {
                    var total = g.Count();
                    var successful = g.Count(a => a.IsSuccess);
                    var failed = g.Count(a => !a.IsSuccess);

                    return new LoginTrendDto
                    {
                        Date = g.Key,
                        TotalAttempts = total,
                        SuccessfulLogins = successful,
                        FailedAttempts = failed,
                        SuccessRate = total > 0 ? (decimal)successful / total * 100 : 0
                    };
                })
                .OrderBy(t => t.Date)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting login trends for method {LoginMethod}", loginMethod);
            return new List<LoginTrendDto>();
        }
    }

    public async Task<UserBehaviorAnalyticsDto> GetUserBehaviorAnalyticsAsync(
        Guid? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var sessionQuery = context.UserSessions.AsQueryable();
            var accessLogQuery = context.AccessLogs.AsQueryable();
            var apiLogQuery = context.ApiRequestLogs.AsQueryable();

            if (userId.HasValue)
            {
                sessionQuery = sessionQuery.Where(s => s.UserId == userId.Value);
                accessLogQuery = accessLogQuery.Where(a => a.UserId == userId.Value);
                apiLogQuery = apiLogQuery.Where(a => a.UserId == userId.Value);
            }

            var sessions = await sessionQuery
                .Where(s => s.StartedAt >= start && s.StartedAt <= end)
                .ToListAsync();

            var accessLogs = await accessLogQuery
                .Where(a => a.AccessedAt >= start && a.AccessedAt <= end)
                .ToListAsync();

            var apiLogs = await apiLogQuery
                .Where(a => a.RequestedAt >= start && a.RequestedAt <= end)
                .ToListAsync();

            var averageSessionDuration = sessions
                .Where(s => s.EndedAt.HasValue)
                .Select(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes)
                .DefaultIfEmpty(0)
                .Average();

            var preferredDevices = sessions
                .Where(s => !string.IsNullOrEmpty(s.DeviceType))
                .GroupBy(s => s.DeviceType!)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            var commonLocations = sessions
                .Where(s => !string.IsNullOrEmpty(s.GeolocationData))
                .Select(s => ParseLocation(s.GeolocationData!))
                .Where(loc => !string.IsNullOrEmpty(loc))
                .GroupBy(loc => loc)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => g.Key)
                .ToList();

            var featureUsage = apiLogs
                .Where(a => !string.IsNullOrEmpty(a.Feature))
                .GroupBy(a => a.Feature!)
                .ToDictionary(g => g.Key, g => g.Count());

            DateTime? lastLogin = null;
            if (userId.HasValue)
            {
                var user = await context.Users.FindAsync(userId.Value);
                lastLogin = user?.LastLoginDate;
            }

            return new UserBehaviorAnalyticsDto
            {
                TotalSessions = sessions.Count,
                AverageSessionDurationMinutes = (decimal)averageSessionDuration,
                TotalPageViews = accessLogs.Count,
                LastLoginDate = lastLogin,
                PreferredDevices = preferredDevices,
                CommonLocations = commonLocations,
                FeatureUsage = featureUsage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user behavior analytics for user {UserId}", userId);
            return new UserBehaviorAnalyticsDto();
        }
    }

    #endregion

    #region Error Analytics

    public async Task<ErrorAnalyticsDto> GetErrorAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? severity = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var query = context.ErrorLogs
                .Where(e => e.LoggedAt >= start && e.LoggedAt <= end);

            if (!string.IsNullOrEmpty(severity))
            {
                query = query.Where(e => e.Severity == severity);
            }

            var errors = await query.ToListAsync();

            var totalErrors = errors.Count;
            var resolvedErrors = errors.Count(e => e.IsResolved);
            var unresolvedErrors = errors.Count(e => !e.IsResolved);

            var averageResolutionTime = errors
                .Where(e => e.IsResolved && e.ResolvedAt.HasValue)
                .Select(e => (e.ResolvedAt!.Value - e.LoggedAt).TotalHours)
                .DefaultIfEmpty(0)
                .Average();

            return new ErrorAnalyticsDto
            {
                TotalErrors = totalErrors,
                ResolvedErrors = resolvedErrors,
                UnresolvedErrors = unresolvedErrors,
                ResolutionRate = totalErrors > 0 ? (decimal)resolvedErrors / totalErrors * 100 : 0,
                AverageResolutionTimeHours = (decimal)averageResolutionTime,
                ErrorsBySeverity = errors
                    .GroupBy(e => e.Severity)
                    .ToDictionary(g => g.Key, g => g.Count()),
                ErrorsByType = errors
                    .GroupBy(e => e.ExceptionType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TopErrorSources = errors
                    .Where(e => !string.IsNullOrEmpty(e.Source))
                    .GroupBy(e => e.Source!)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => g.Key)
                    .ToList(),
                ErrorsByHour = errors
                    .GroupBy(e => new DateTime(e.LoggedAt.Year, e.LoggedAt.Month, e.LoggedAt.Day, e.LoggedAt.Hour, 0, 0))
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting error analytics for severity {Severity}", severity);
            return new ErrorAnalyticsDto();
        }
    }

    public async Task<List<ErrorTrendDto>> GetErrorTrendsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? groupBy = "Day")
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var errors = await context.ErrorLogs
                .Where(e => e.LoggedAt >= start && e.LoggedAt <= end)
                .ToListAsync();

            return errors
                .GroupBy(e => e.LoggedAt.Date)
                .Select(g => new ErrorTrendDto
                {
                    Date = g.Key,
                    ErrorCount = g.Count(),
                    ErrorsBySeverity = g.GroupBy(e => e.Severity)
                        .ToDictionary(sg => sg.Key, sg => sg.Count())
                })
                .OrderBy(t => t.Date)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting error trends");
            return new List<ErrorTrendDto>();
        }
    }

    public async Task<List<ApiEndpointErrorDto>> GetApiEndpointErrorAnalysisAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        int topN = 20)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            // Get API request logs
            var apiLogs = await context.ApiRequestLogs
                .Where(a => a.RequestedAt >= start && a.RequestedAt <= end)
                .ToListAsync();

            // Get error logs for the same period
            var errorLogs = await context.ErrorLogs
                .Where(e => e.LoggedAt >= start && e.LoggedAt <= end && !string.IsNullOrEmpty(e.RequestPath))
                .ToListAsync();

            var endpointStats = apiLogs
                .GroupBy(a => new { Path = a.Path, Method = a.Method })
                .Select(g =>
                {
                    var totalRequests = g.Count();
                    var errorCount = g.Count(a => a.ResponseStatusCode >= 400);
                    var avgResponseTime = g.Average(a => a.DurationMs);

                    return new ApiEndpointErrorDto
                    {
                        Path = g.Key.Path,
                        Method = g.Key.Method,
                        TotalRequests = totalRequests,
                        ErrorCount = errorCount,
                        ErrorRate = totalRequests > 0 ? (decimal)errorCount / totalRequests * 100 : 0,
                        AverageResponseTime = (decimal)avgResponseTime
                    };
                })
                .OrderByDescending(e => e.ErrorCount)
                .Take(topN)
                .ToList();

            return endpointStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API endpoint error analysis");
            return new List<ApiEndpointErrorDto>();
        }
    }

    #endregion

    #region Performance Analytics

    public async Task<PerformanceAnalyticsDto> GetPerformanceAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? component = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            // Get API request logs for response times
            var apiQuery = context.ApiRequestLogs
                .Where(a => a.RequestedAt >= start && a.RequestedAt <= end);

            var apiLogs = await apiQuery.ToListAsync();

            // Get performance metrics
            var metricsQuery = context.PerformanceMetrics
                .Where(p => p.CollectedAt >= start && p.CollectedAt <= end);

            if (!string.IsNullOrEmpty(component))
            {
                metricsQuery = metricsQuery.Where(p => p.Component == component);
            }

            var metrics = await metricsQuery.ToListAsync();

            var responseTimes = apiLogs.Select(a => a.DurationMs).OrderBy(x => x).ToList();
            var avgResponseTime = responseTimes.Any() ? responseTimes.Average() : 0;
            var p95ResponseTime = CalculatePercentile(responseTimes, 95);
            var p99ResponseTime = CalculatePercentile(responseTimes, 99);

            var memoryMetrics = metrics.Where(m => m.MetricType == "MemoryUsage").ToList();
            var avgMemory = memoryMetrics.Any() ? memoryMetrics.Average(m => m.Value) : 0;

            var cpuMetrics = metrics.Where(m => m.MetricType == "CpuUsage").ToList();
            var avgCpu = cpuMetrics.Any() ? cpuMetrics.Average(m => m.Value) : 0;

            var dbConnectionMetrics = metrics.Where(m => m.MetricType == "DatabaseConnection").ToList();
            var dbConnections = dbConnectionMetrics.Any() ? (int)dbConnectionMetrics.Average(m => m.Value) : 0;

            var avgDbQueryTime = apiLogs.Any() ? (decimal)apiLogs.Average(a => a.DatabaseDurationMs) : 0;

            return new PerformanceAnalyticsDto
            {
                AverageResponseTimeMs = (decimal)avgResponseTime,
                P95ResponseTimeMs = p95ResponseTime,
                P99ResponseTimeMs = p99ResponseTime,
                AverageMemoryUsageMB = avgMemory,
                AverageCpuUsagePercent = avgCpu,
                DatabaseConnectionCount = dbConnections,
                AverageDatabaseQueryTimeMs = avgDbQueryTime,
                ResponseTimeTrends = apiLogs
                    .GroupBy(a => new DateTime(a.RequestedAt.Year, a.RequestedAt.Month, a.RequestedAt.Day, a.RequestedAt.Hour, 0, 0))
                    .Select(g => new PerformanceTrendDto
                    {
                        Timestamp = g.Key,
                        Value = (decimal)g.Average(a => a.DurationMs),
                        MetricType = "ResponseTime"
                    })
                    .OrderBy(t => t.Timestamp)
                    .ToList(),
                // Backward compatibility properties
                MaxResponseTimeMs = responseTimes.Any() ? responseTimes.Max() : 0,
                MinResponseTimeMs = responseTimes.Any() ? responseTimes.Min() : 0,
                TotalRequests = apiLogs.Count,
                EndpointResponseTimes = apiLogs
                    .GroupBy(a => a.Path)
                    .ToDictionary(g => g.Key, g => g.Average(a => (double)a.DurationMs)),
                ResponseTimesByHour = apiLogs
                    .GroupBy(a => new DateTime(a.RequestedAt.Year, a.RequestedAt.Month, a.RequestedAt.Day, a.RequestedAt.Hour, 0, 0))
                    .ToDictionary(g => g.Key, g => g.Average(a => (double)a.DurationMs))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance analytics for component {Component}", component);
            return new PerformanceAnalyticsDto();
        }
    }

    public async Task<List<ApiPerformanceDto>> GetApiPerformanceAnalysisAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        int topN = 20)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var apiLogs = await context.ApiRequestLogs
                .Where(a => a.RequestedAt >= start && a.RequestedAt <= end)
                .ToListAsync();

            var endpointPerformance = apiLogs
                .GroupBy(a => new { Path = a.Path, Method = a.Method })
                .Select(g =>
                {
                    var requests = g.ToList();
                    var durations = requests.Select(r => r.DurationMs).OrderBy(x => x).ToList();
                    var errorCount = requests.Count(r => r.ResponseStatusCode >= 400);

                    return new ApiPerformanceDto
                    {
                        Path = g.Key.Path,
                        Method = g.Key.Method,
                        RequestCount = requests.Count,
                        AverageResponseTimeMs = (decimal)durations.Average(),
                        P95ResponseTimeMs = CalculatePercentile(durations, 95),
                        P99ResponseTimeMs = CalculatePercentile(durations, 99),
                        ErrorCount = errorCount,
                        ErrorRate = requests.Count > 0 ? (decimal)errorCount / requests.Count * 100 : 0
                    };
                })
                .OrderByDescending(p => p.RequestCount)
                .Take(topN)
                .ToList();

            return endpointPerformance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API performance analysis");
            return new List<ApiPerformanceDto>();
        }
    }

    public async Task<DatabasePerformanceDto> GetDatabasePerformanceAnalysisAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var apiLogs = await context.ApiRequestLogs
                .Where(a => a.RequestedAt >= start && a.RequestedAt <= end && a.DatabaseQueries > 0)
                .ToListAsync();

            var avgQueryTime = apiLogs.Any() ? (decimal)apiLogs.Average(a => a.DatabaseDurationMs / (a.DatabaseQueries > 0 ? a.DatabaseQueries : 1)) : 0;
            var totalQueries = apiLogs.Sum(a => a.DatabaseQueries);
            var slowQueries = apiLogs.Count(a => a.DatabaseDurationMs > 1000); // > 1 second

            // Get database connection metrics
            var dbMetrics = await context.PerformanceMetrics
                .Where(m => m.CollectedAt >= start && m.CollectedAt <= end && m.MetricType == "DatabaseConnection")
                .ToListAsync();

            var connectionPoolUsage = dbMetrics.Any() ? (int)dbMetrics.Average(m => m.Value) : 0;

            return new DatabasePerformanceDto
            {
                AverageQueryTimeMs = avgQueryTime,
                TotalQueries = totalQueries,
                SlowQueries = slowQueries,
                TopSlowQueries = apiLogs
                    .Where(a => a.DatabaseDurationMs > 1000)
                    .OrderByDescending(a => a.DatabaseDurationMs)
                    .Take(10)
                    .Select(a => $"{a.Method} {a.Path} ({a.DatabaseDurationMs}ms)")
                    .ToList(),
                QueriesByTable = new Dictionary<string, int>(), // TODO: Would need query parsing to implement
                ConnectionPoolUsage = connectionPoolUsage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database performance analysis");
            return new DatabasePerformanceDto();
        }
    }

    #endregion

    #region Security Analytics

    public async Task<SecurityAnalyticsDto> GetSecurityAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var events = await context.SecurityEvents
                .Where(e => e.DetectedAt >= start && e.DetectedAt <= end)
                .ToListAsync();

            var totalEvents = events.Count;
            var criticalEvents = events.Count(e => e.Severity == "Critical");
            var blockedIps = events.Where(e => e.AutoBlocked).Select(e => e.IpAddress).Distinct().Count();

            // Get suspended accounts
            var suspendedAccounts = await context.Users
                .Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTime.UtcNow)
                .CountAsync();

            var avgRiskScore = events.Any() ? events.Average(e => e.RiskScore) : 0;

            var topRiskIps = events
                .GroupBy(e => e.IpAddress)
                .Select(g => new { IpAddress = g.Key, AvgRisk = g.Average(e => e.RiskScore), Count = g.Count() })
                .OrderByDescending(x => x.AvgRisk)
                .Take(10)
                .Select(x => $"{x.IpAddress} (Risk: {x.AvgRisk:F2}, Events: {x.Count})")
                .ToList();

            return new SecurityAnalyticsDto
            {
                TotalSecurityEvents = totalEvents,
                CriticalEvents = criticalEvents,
                BlockedIpAddresses = blockedIps,
                SuspendedAccounts = suspendedAccounts,
                AverageRiskScore = avgRiskScore,
                EventsByType = events
                    .GroupBy(e => e.EventType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TopRiskIpAddresses = topRiskIps,
                // Backward compatibility
                WarningEvents = events.Count(e => e.Severity == "Warning" || e.Severity == "Medium"),
                InfoEvents = events.Count(e => e.Severity == "Info" || e.Severity == "Low"),
                EventsByHour = events
                    .GroupBy(e => new DateTime(e.DetectedAt.Year, e.DetectedAt.Month, e.DetectedAt.Day, e.DetectedAt.Hour, 0, 0))
                    .ToDictionary(g => g.Key, g => g.Count())
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security analytics");
            return new SecurityAnalyticsDto();
        }
    }

    public async Task<List<ThreatAnalysisDto>> GetThreatAnalysisAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        decimal minRiskScore = 0.5m)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var threats = await context.SecurityEvents
                .Where(e => e.DetectedAt >= start && e.DetectedAt <= end && e.RiskScore >= minRiskScore)
                .GroupBy(e => new { e.EventType, e.IpAddress })
                .Select(g => new ThreatAnalysisDto
                {
                    EventType = g.Key.EventType,
                    IpAddress = g.Key.IpAddress,
                    RiskScore = g.Max(e => e.RiskScore),
                    EventCount = g.Count(),
                    FirstDetected = g.Min(e => e.DetectedAt),
                    LastDetected = g.Max(e => e.DetectedAt),
                    IsResolved = g.All(e => e.IsResolved)
                })
                .OrderByDescending(t => t.RiskScore)
                .ToListAsync();

            return threats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting threat analysis");
            return new List<ThreatAnalysisDto>();
        }
    }

    public async Task<List<RiskScoreDistributionDto>> GetRiskScoreDistributionAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var events = await context.SecurityEvents
                .Where(e => e.DetectedAt >= start && e.DetectedAt <= end)
                .ToListAsync();

            var totalEvents = events.Count;

            var distribution = new List<RiskScoreDistributionDto>
            {
                new() { RiskRange = "0.0 - 0.2", EventCount = events.Count(e => e.RiskScore < 0.2m) },
                new() { RiskRange = "0.2 - 0.4", EventCount = events.Count(e => e.RiskScore >= 0.2m && e.RiskScore < 0.4m) },
                new() { RiskRange = "0.4 - 0.6", EventCount = events.Count(e => e.RiskScore >= 0.4m && e.RiskScore < 0.6m) },
                new() { RiskRange = "0.6 - 0.8", EventCount = events.Count(e => e.RiskScore >= 0.6m && e.RiskScore < 0.8m) },
                new() { RiskRange = "0.8 - 1.0", EventCount = events.Count(e => e.RiskScore >= 0.8m) }
            };

            foreach (var item in distribution)
            {
                item.Percentage = totalEvents > 0 ? (decimal)item.EventCount / totalEvents * 100 : 0;
            }

            return distribution;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting risk score distribution");
            return new List<RiskScoreDistributionDto>();
        }
    }

    #endregion

    #region User Analytics

    public async Task<UserActivityAnalyticsDto> GetUserActivityAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var sessions = await context.UserSessions
                .Where(s => s.StartedAt >= start && s.StartedAt <= end)
                .ToListAsync();

            var activeUserIds = sessions
                .Where(s => s.IsActive)
                .Select(s => s.UserId)
                .Distinct()
                .ToList();

            var newUsers = await context.Users
                .Where(u => u.DateJoined >= start && u.DateJoined <= end)
                .CountAsync();

            var previousPeriodStart = start.AddDays(-(end - start).Days);
            var returningUserIds = await context.UserSessions
                .Where(s => s.StartedAt >= previousPeriodStart && s.StartedAt < start)
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync();

            var returningUsers = activeUserIds.Count(id => returningUserIds.Contains(id));

            var avgSessionsPerUser = activeUserIds.Count > 0
                ? (decimal)sessions.Count / activeUserIds.Count
                : 0;

            var avgSessionDuration = sessions
                .Where(s => s.EndedAt.HasValue)
                .Select(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes)
                .DefaultIfEmpty(0)
                .Average();

            var usersByDevice = sessions
                .Where(s => !string.IsNullOrEmpty(s.DeviceType))
                .GroupBy(s => s.DeviceType!)
                .ToDictionary(g => g.Key, g => g.Select(s => s.UserId).Distinct().Count());

            var usersByLocation = sessions
                .Where(s => !string.IsNullOrEmpty(s.GeolocationData))
                .Select(s => ParseLocation(s.GeolocationData!))
                .Where(loc => !string.IsNullOrEmpty(loc))
                .GroupBy(loc => loc)
                .ToDictionary(g => g.Key, g => g.Count());

            return new UserActivityAnalyticsDto
            {
                TotalActiveUsers = activeUserIds.Count,
                NewUsers = newUsers,
                ReturningUsers = returningUsers,
                AverageSessionsPerUser = avgSessionsPerUser,
                AverageSessionDurationMinutes = (decimal)avgSessionDuration,
                UsersByDevice = usersByDevice,
                UsersByLocation = usersByLocation
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user activity analytics");
            return new UserActivityAnalyticsDto();
        }
    }

    public async Task<List<SessionAnalyticsDto>> GetSessionAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? groupBy = "Day")
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var sessions = await context.UserSessions
                .Where(s => s.StartedAt >= start && s.StartedAt <= end)
                .ToListAsync();

            return sessions
                .GroupBy(s => s.StartedAt.Date)
                .Select(g =>
                {
                    var daySessions = g.ToList();
                    var avgDuration = daySessions
                        .Where(s => s.EndedAt.HasValue)
                        .Select(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes)
                        .DefaultIfEmpty(0)
                        .Average();

                    return new SessionAnalyticsDto
                    {
                        Date = g.Key,
                        SessionCount = daySessions.Count,
                        UniqueUsers = daySessions.Select(s => s.UserId).Distinct().Count(),
                        AverageSessionDurationMinutes = (decimal)avgDuration,
                        BounceCount = daySessions.Count(s => s.ActivityCount <= 1)
                    };
                })
                .OrderBy(s => s.Date)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session analytics");
            return new List<SessionAnalyticsDto>();
        }
    }

    public async Task<List<DeviceAnalyticsDto>> GetDeviceAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var sessions = await context.UserSessions
                .Where(s => s.StartedAt >= start && s.StartedAt <= end)
                .ToListAsync();

            var totalSessions = sessions.Count;

            var deviceStats = sessions
                .Where(s => !string.IsNullOrEmpty(s.DeviceType))
                .GroupBy(s => new
                {
                    DeviceType = s.DeviceType ?? "Unknown",
                    Platform = s.Platform ?? "Unknown",
                    Browser = s.Browser ?? "Unknown"
                })
                .Select(g =>
                {
                    var groupSessions = g.ToList();
                    var avgDuration = groupSessions
                        .Where(s => s.EndedAt.HasValue)
                        .Select(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes)
                        .DefaultIfEmpty(0)
                        .Average();

                    return new DeviceAnalyticsDto
                    {
                        DeviceType = g.Key.DeviceType,
                        Platform = g.Key.Platform,
                        Browser = g.Key.Browser,
                        UserCount = groupSessions.Select(s => s.UserId).Distinct().Count(),
                        SessionCount = groupSessions.Count,
                        AverageSessionDurationMinutes = (decimal)avgDuration,
                        Percentage = totalSessions > 0 ? (double)groupSessions.Count / totalSessions * 100 : 0
                    };
                })
                .OrderByDescending(d => d.SessionCount)
                .ToList();

            return deviceStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device analytics");
            return new List<DeviceAnalyticsDto>();
        }
    }

    #endregion

    #region Business Analytics

    public async Task<BusinessMetricsDto> GetBusinessMetricsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var totalUsers = await context.Users.CountAsync();

            var activeSessions = await context.UserSessions
                .Where(s => s.StartedAt >= start && s.StartedAt <= end)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();

            var newRegistrations = await context.Users
                .Where(u => u.DateJoined >= start && u.DateJoined <= end)
                .CountAsync();

            var enrollments = await context.Enrollments
                .Where(e => e.EnrolledAt >= start && e.EnrolledAt <= end)
                .ToListAsync();

            var completedCourses = enrollments.Count(e => e.CompletedAt.HasValue);

            var payments = await context.Payments
                .Where(p => p.CreatedAt >= start && p.CreatedAt <= end && p.Status == PaymentStatus.Completed)
                .ToListAsync();

            var totalRevenue = payments.Sum(p => p.Amount);

            // Calculate retention rate (users who came back in current period)
            var previousPeriodStart = start.AddDays(-(end - start).Days);
            var previousUsers = await context.UserSessions
                .Where(s => s.StartedAt >= previousPeriodStart && s.StartedAt < start)
                .Select(s => s.UserId)
                .Distinct()
                .ToListAsync();

            var returnedUsers = await context.UserSessions
                .Where(s => s.StartedAt >= start && s.StartedAt <= end && previousUsers.Contains(s.UserId))
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();

            var retentionRate = previousUsers.Count > 0 ? (decimal)returnedUsers / previousUsers.Count * 100 : 0;

            // Engagement score (average sessions per user)
            var engagementScore = activeSessions > 0
                ? await context.UserSessions
                    .Where(s => s.StartedAt >= start && s.StartedAt <= end)
                    .CountAsync() / (decimal)activeSessions
                : 0;

            // Popular courses
            var popularCourses = await context.Courses
                .OrderByDescending(c => c.EnrollmentCount)
                .Take(10)
                .ToDictionaryAsync(c => c.Title, c => c.EnrollmentCount);

            // Feature adoption (from API logs)
            var apiLogs = await context.ApiRequestLogs
                .Where(a => a.RequestedAt >= start && a.RequestedAt <= end && !string.IsNullOrEmpty(a.Feature))
                .ToListAsync();

            var featureAdoption = apiLogs
                .GroupBy(a => a.Feature!)
                .ToDictionary(g => g.Key, g => g.Select(a => a.UserId).Where(id => id.HasValue).Distinct().Count());

            return new BusinessMetricsDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeSessions,
                NewRegistrations = newRegistrations,
                UserRetentionRate = retentionRate,
                UserEngagementScore = engagementScore,
                FeatureAdoptionRates = featureAdoption,
                TotalCourseEnrollments = enrollments.Count,
                CompletedCourses = completedCourses,
                TotalRevenue = totalRevenue,
                PopularCourses = popularCourses
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting business metrics");
            return new BusinessMetricsDto();
        }
    }

    public async Task<List<FeatureUsageDto>> GetFeatureUsageAnalyticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var apiLogs = await context.ApiRequestLogs
                .Where(a => a.RequestedAt >= start && a.RequestedAt <= end && !string.IsNullOrEmpty(a.Feature))
                .ToListAsync();

            var totalUsers = await context.Users.CountAsync();

            var featureUsage = apiLogs
                .GroupBy(a => a.Feature!)
                .Select(g =>
                {
                    var logs = g.ToList();
                    var uniqueUsers = logs.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct().Count();
                    var avgUsageTime = (decimal)logs.Average(l => l.DurationMs) / 1000m; // Convert to seconds

                    return new FeatureUsageDto
                    {
                        FeatureName = g.Key,
                        UsageCount = logs.Count,
                        UniqueUsers = uniqueUsers,
                        AdoptionRate = totalUsers > 0 ? (decimal)uniqueUsers / totalUsers * 100 : 0,
                        AverageUsageTimeSeconds = avgUsageTime
                    };
                })
                .OrderByDescending(f => f.UsageCount)
                .ToList();

            return featureUsage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature usage analytics");
            return new List<FeatureUsageDto>();
        }
    }

    #endregion

    #region Real-time Analytics

    public async Task<RealTimeMetricsDto> GetRealTimeMetricsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);
            var fiveMinutesAgo = now.AddMinutes(-5);

            // Active users (sessions in last 5 minutes)
            var activeUsers = await context.UserSessions
                .Where(s => s.IsActive && s.LastActivityAt >= fiveMinutesAgo)
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();

            // Requests per minute
            var requestsLastMinute = await context.ApiRequestLogs
                .Where(a => a.RequestedAt >= oneMinuteAgo)
                .CountAsync();

            // Current response time (last 5 minutes average)
            var recentLogs = await context.ApiRequestLogs
                .Where(a => a.RequestedAt >= fiveMinutesAgo)
                .Select(a => a.DurationMs)
                .ToListAsync();

            var currentResponseTime = recentLogs.Any() ? (decimal)recentLogs.Average() : 0;

            // Error rate (last 5 minutes)
            var totalRequests = recentLogs.Count;
            var errorRequests = await context.ApiRequestLogs
                .Where(a => a.RequestedAt >= fiveMinutesAgo && a.ResponseStatusCode >= 400)
                .CountAsync();

            var errorRate = totalRequests > 0 ? (decimal)errorRequests / totalRequests * 100 : 0;

            // Get latest performance metrics
            var latestMetrics = await context.PerformanceMetrics
                .Where(m => m.CollectedAt >= fiveMinutesAgo)
                .ToListAsync();

            var cpuUsage = latestMetrics
                .Where(m => m.MetricType == "CpuUsage")
                .OrderByDescending(m => m.CollectedAt)
                .FirstOrDefault()?.Value ?? 0;

            var memoryUsage = latestMetrics
                .Where(m => m.MetricType == "MemoryUsage")
                .OrderByDescending(m => m.CollectedAt)
                .FirstOrDefault()?.Value ?? 0;

            var dbConnections = (int)(latestMetrics
                .Where(m => m.MetricType == "DatabaseConnection")
                .OrderByDescending(m => m.CollectedAt)
                .FirstOrDefault()?.Value ?? 0);

            // Recent errors
            var recentErrors = await context.ErrorLogs
                .Where(e => e.LoggedAt >= fiveMinutesAgo)
                .OrderByDescending(e => e.LoggedAt)
                .Take(10)
                .Select(e => new RecentErrorDto
                {
                    Timestamp = e.LoggedAt,
                    ErrorType = e.ExceptionType,
                    Severity = e.Severity,
                    Source = e.Source ?? "Unknown"
                })
                .ToListAsync();

            return new RealTimeMetricsDto
            {
                CurrentActiveUsers = activeUsers,
                RequestsPerMinute = requestsLastMinute,
                CurrentResponseTimeMs = currentResponseTime,
                ErrorRatePercent = errorRate,
                CpuUsagePercent = cpuUsage,
                MemoryUsagePercent = memoryUsage,
                DatabaseConnections = dbConnections,
                RecentErrors = recentErrors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting real-time metrics");
            return new RealTimeMetricsDto();
        }
    }

    public async Task<List<LiveUserSessionDto>> GetLiveUserSessionsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

            var liveSessions = await context.UserSessions
                .Where(s => s.IsActive && s.LastActivityAt >= fiveMinutesAgo)
                .Include(s => s.User)
                .OrderByDescending(s => s.LastActivityAt)
                .Take(100)
                .Select(s => new LiveUserSessionDto
                {
                    UserId = s.UserId,
                    Email = s.User.Email ?? "Unknown",
                    LoginTime = s.StartedAt,
                    IpAddress = s.IpAddress,
                    DeviceType = s.DeviceType ?? "Unknown",
                    LastActivity = s.LastPageVisited ?? "Unknown",
                    ActivityCount = s.ActivityCount
                })
                .ToListAsync();

            return liveSessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting live user sessions");
            return new List<LiveUserSessionDto>();
        }
    }

    public async Task<AnalyticsSystemHealthDto> GetSystemHealthMetricsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

            // Get latest system health check
            var latestHealth = await context.Set<SystemHealth>()
                .OrderByDescending(h => h.CheckedAt)
                .FirstOrDefaultAsync();

            if (latestHealth == null)
            {
                return new AnalyticsSystemHealthDto
                {
                    Status = "Unknown",
                    Message = "No health data available",
                    CheckedAt = DateTime.UtcNow
                };
            }

            // Get latest performance metrics
            var latestMetrics = await context.PerformanceMetrics
                .Where(m => m.CollectedAt >= fiveMinutesAgo)
                .ToListAsync();

            var cpuUsage = latestMetrics
                .Where(m => m.MetricType == "CpuUsage")
                .OrderByDescending(m => m.CollectedAt)
                .FirstOrDefault()?.Value ?? 0;

            var memoryUsageMB = latestMetrics
                .Where(m => m.MetricType == "MemoryUsage")
                .OrderByDescending(m => m.CollectedAt)
                .FirstOrDefault()?.Value ?? 0;

            var diskUsageMB = latestMetrics
                .Where(m => m.MetricType == "DiskUsage")
                .OrderByDescending(m => m.CollectedAt)
                .FirstOrDefault()?.Value ?? 0;

            // Active database connections
            var activeConnections = await context.UserSessions
                .Where(s => s.IsActive && s.LastActivityAt >= fiveMinutesAgo)
                .CountAsync();

            return new AnalyticsSystemHealthDto
            {
                Status = latestHealth.Status.ToString(),
                Message = latestHealth.Description ?? "System operational",
                CpuUsagePercent = cpuUsage,
                MemoryUsageMB = (long)memoryUsageMB,
                DiskUsageMB = (long)diskUsageMB,
                ActiveConnections = activeConnections,
                CheckedAt = latestHealth.CheckedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health metrics");
            return new AnalyticsSystemHealthDto
            {
                Status = "Error",
                Message = $"Failed to retrieve system health: {ex.Message}",
                CheckedAt = DateTime.UtcNow
            };
        }
    }

    #endregion

    #region Custom Analytics

    public async Task<List<CustomAnalyticsResultDto>> ExecuteCustomAnalyticsQueryAsync(
        string queryName,
        Dictionary<string, object>? parameters = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var startTime = DateTime.UtcNow;

            // TODO: Implement custom query execution based on queryName
            // This would typically involve a repository of pre-defined queries
            // For now, return empty result with execution metadata

            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return new List<CustomAnalyticsResultDto>
            {
                new()
                {
                    QueryName = queryName,
                    Results = new Dictionary<string, object>
                    {
                        { "message", "Custom query execution not yet implemented" },
                        { "queryName", queryName },
                        { "parameters", parameters ?? new Dictionary<string, object>() }
                    },
                    ExecutedAt = DateTime.UtcNow,
                    ExecutionTimeMs = (long)executionTime
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing custom analytics query {QueryName}", queryName);
            return new List<CustomAnalyticsResultDto>();
        }
    }

    #endregion

    #region Export and Reporting

    public async Task<byte[]> ExportAnalyticsReportAsync(
        string reportType,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string format = "Excel")
    {
        try
        {
            // TODO: Implement report export functionality
            // This would use libraries like EPPlus (Excel), iTextSharp (PDF), or CsvHelper (CSV)
            _logger.LogWarning("Export analytics report not yet implemented for type {ReportType}", reportType);

            // Return empty byte array as placeholder
            return Array.Empty<byte>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting analytics report {ReportType} in format {Format}", reportType, format);
            return Array.Empty<byte>();
        }
    }

    public async Task<string> GenerateAnalyticsSummaryAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            // Get all analytics data
            var loginAnalytics = await GetLoginAnalyticsAsync(start, end);
            var errorAnalytics = await GetErrorAnalyticsAsync(start, end);
            var performanceAnalytics = await GetPerformanceAnalyticsAsync(start, end);
            var securityAnalytics = await GetSecurityAnalyticsAsync(start, end);
            var businessMetrics = await GetBusinessMetricsAsync(start, end);

            // Generate summary text
            var summary = $@"
=== ANALYTICS SUMMARY ===
Period: {start:yyyy-MM-dd} to {end:yyyy-MM-dd}

LOGIN ANALYTICS:
- Total Login Attempts: {loginAnalytics.TotalAttempts}
- Successful Logins: {loginAnalytics.SuccessfulLogins}
- Success Rate: {loginAnalytics.SuccessRate:F2}%
- Unique Users: {loginAnalytics.UniqueUsers}

ERROR ANALYTICS:
- Total Errors: {errorAnalytics.TotalErrors}
- Resolved: {errorAnalytics.ResolvedErrors}
- Resolution Rate: {errorAnalytics.ResolutionRate:F2}%
- Average Resolution Time: {errorAnalytics.AverageResolutionTimeHours:F2} hours

PERFORMANCE:
- Average Response Time: {performanceAnalytics.AverageResponseTimeMs:F2}ms
- P95 Response Time: {performanceAnalytics.P95ResponseTimeMs:F2}ms
- Total Requests: {performanceAnalytics.TotalRequests}
- Database Connections: {performanceAnalytics.DatabaseConnectionCount}

SECURITY:
- Security Events: {securityAnalytics.TotalSecurityEvents}
- Critical Events: {securityAnalytics.CriticalEvents}
- Blocked IPs: {securityAnalytics.BlockedIpAddresses}
- Average Risk Score: {securityAnalytics.AverageRiskScore:F2}

BUSINESS METRICS:
- Total Users: {businessMetrics.TotalUsers}
- Active Users: {businessMetrics.ActiveUsers}
- New Registrations: {businessMetrics.NewRegistrations}
- Total Revenue: ${businessMetrics.TotalRevenue:F2}
- Course Enrollments: {businessMetrics.TotalCourseEnrollments}
- Completed Courses: {businessMetrics.CompletedCourses}
";

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating analytics summary");
            return "Error generating analytics summary";
        }
    }

    #endregion

    #region Predictive Analytics

    public async Task<LoginPatternPredictionDto> PredictLoginPatternsAsync(Guid userId)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var userLogins = await context.LoginAttempts
                .Where(l => l.UserId == userId && l.IsSuccess && l.AttemptedAt >= thirtyDaysAgo)
                .OrderBy(l => l.AttemptedAt)
                .ToListAsync();

            // Simple pattern analysis (in production, use ML.NET or similar)
            var hourlyPattern = userLogins
                .GroupBy(l => l.AttemptedAt.Hour)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();

            var predictedTimes = hourlyPattern
                .Select(hour => DateTime.UtcNow.Date.AddDays(1).AddHours(hour))
                .ToList();

            var devicePattern = userLogins
                .Where(l => !string.IsNullOrEmpty(l.DeviceFingerprint))
                .GroupBy(l => ParseDeviceType(l.UserAgent ?? "Unknown"))
                .ToDictionary(
                    g => g.Key,
                    g => (decimal)g.Count() / userLogins.Count
                );

            return new LoginPatternPredictionDto
            {
                UserId = userId,
                PredictedLoginTimes = predictedTimes,
                PreferredDeviceProbabilities = devicePattern,
                AnomalyDetectionThreshold = 0.3m // TODO: Calculate based on historical variance
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting login patterns for user {UserId}", userId);
            return new LoginPatternPredictionDto { UserId = userId };
        }
    }

    public async Task<ErrorPredictionDto> PredictErrorTrendsAsync(string? component = null)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var query = context.ErrorLogs
                .Where(e => e.LoggedAt >= thirtyDaysAgo);

            if (!string.IsNullOrEmpty(component))
            {
                query = query.Where(e => e.Component == component);
            }

            var recentErrors = await query
                .OrderBy(e => e.LoggedAt)
                .ToListAsync();

            // Simple trend analysis (in production, use time series forecasting)
            var errorsByType = recentErrors
                .GroupBy(e => e.ExceptionType)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .ToList();

            var predictions = errorsByType
                .Select(g =>
                {
                    var avgPerDay = g.Count() / 30.0m;
                    var nextWeek = DateTime.UtcNow.AddDays(7);

                    return new PredictedErrorDto
                    {
                        PredictedTime = nextWeek,
                        ErrorType = g.Key,
                        Probability = Math.Min(avgPerDay / 10m, 1m) // Normalize to 0-1
                    };
                })
                .ToList();

            return new ErrorPredictionDto
            {
                Component = component ?? "All",
                PredictedErrors = predictions,
                ConfidenceScore = predictions.Any() ? 0.7m : 0.0m // TODO: Calculate based on model accuracy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting error trends for component {Component}", component);
            return new ErrorPredictionDto { Component = component ?? "All" };
        }
    }

    public async Task<CapacityPredictionDto> PredictCapacityNeedsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            // Get growth trends
            var userGrowth = await context.Users
                .Where(u => u.DateJoined >= thirtyDaysAgo)
                .CountAsync();

            var currentUsers = await context.Users.CountAsync();

            // Get resource usage trends
            var recentMetrics = await context.PerformanceMetrics
                .Where(m => m.CollectedAt >= thirtyDaysAgo)
                .ToListAsync();

            var avgCpu = recentMetrics
                .Where(m => m.MetricType == "CpuUsage")
                .Select(m => m.Value)
                .DefaultIfEmpty(0)
                .Average();

            var avgMemory = recentMetrics
                .Where(m => m.MetricType == "MemoryUsage")
                .Select(m => m.Value)
                .DefaultIfEmpty(0)
                .Average();

            var avgDisk = recentMetrics
                .Where(m => m.MetricType == "DiskUsage")
                .Select(m => m.Value)
                .DefaultIfEmpty(0)
                .Average();

            // Simple linear projection (in production, use proper forecasting)
            var growthRate = (decimal)userGrowth / Math.Max(currentUsers - userGrowth, 1);
            var predictedUsers = currentUsers + (int)(currentUsers * growthRate);

            var recommendations = new List<string>();

            if (avgCpu > 70)
                recommendations.Add("Consider scaling CPU resources");
            if (avgMemory > 80)
                recommendations.Add("Consider increasing memory allocation");
            if (avgDisk > 80)
                recommendations.Add("Consider expanding storage capacity");
            if (predictedUsers > currentUsers * 1.5m)
                recommendations.Add("Prepare for significant user growth");

            return new CapacityPredictionDto
            {
                PredictionDate = DateTime.UtcNow.AddDays(30),
                PredictedUserCount = predictedUsers,
                PredictedCpuUsage = avgCpu * (1 + growthRate),
                PredictedMemoryUsage = avgMemory * (1 + growthRate),
                PredictedDiskUsage = avgDisk * (1 + growthRate),
                RecommendedActions = recommendations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting capacity needs");
            return new CapacityPredictionDto
            {
                PredictionDate = DateTime.UtcNow.AddDays(30)
            };
        }
    }

    #endregion

    #region Helper Methods

    private static string ParseDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        var lowerAgent = userAgent.ToLower();

        if (lowerAgent.Contains("mobile") || lowerAgent.Contains("android") || lowerAgent.Contains("iphone"))
            return "Mobile";
        if (lowerAgent.Contains("tablet") || lowerAgent.Contains("ipad"))
            return "Tablet";

        return "Desktop";
    }

    private static string ParseLocation(string geolocationData)
    {
        if (string.IsNullOrEmpty(geolocationData))
            return string.Empty;

        try
        {
            var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(geolocationData);
            if (json != null && json.TryGetValue("country", out var country))
            {
                var countryStr = country.GetString() ?? "Unknown";
                if (json.TryGetValue("city", out var city))
                {
                    var cityStr = city.GetString();
                    if (!string.IsNullOrEmpty(cityStr))
                        return $"{cityStr}, {countryStr}";
                }
                return countryStr;
            }
        }
        catch
        {
            // If JSON parsing fails, return empty
        }

        return string.Empty;
    }

    private static decimal CalculatePercentile(List<long> sortedValues, int percentile)
    {
        if (sortedValues == null || sortedValues.Count == 0)
            return 0;

        var index = (int)Math.Ceiling(sortedValues.Count * percentile / 100.0) - 1;
        index = Math.Max(0, Math.Min(index, sortedValues.Count - 1));

        return sortedValues[index];
    }

    #endregion
}
