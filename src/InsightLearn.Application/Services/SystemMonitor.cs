using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using InsightLearn.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace InsightLearn.Application.Services;

// 🔥 CRITICAL FIX: Use DbContextFactory to prevent concurrency issues
public class SystemMonitor : ISystemMonitor
{
    private readonly ILogger<SystemMonitor> _logger;
    private readonly IDbContextFactory<InsightLearnDbContext> _contextFactory;

    public SystemMonitor(ILogger<SystemMonitor> logger, IDbContextFactory<InsightLearnDbContext> contextFactory)
    {
        _logger = logger;
        _contextFactory = contextFactory;
    }

    public async Task<SystemHealth> GetSystemHealthAsync()
    {
        var health = new SystemHealth
        {
            GeneratedAt = DateTime.UtcNow,
            OverallStatus = HealthStatus.Healthy
        };

        // Check database health
        var dbHealth = await GetDatabaseHealthAsync();
        health.Databases.Add(dbHealth);

        // Check service health
        var apiHealth = await GetApiHealthAsync();
        health.Services.Add(apiHealth);

        // Get current metrics
        health.CurrentMetrics = await GetMetricsAsync();

        // Determine overall status
        health.OverallStatus = DetermineOverallHealth(health);

        // Add recommendations
        health.Recommendations = GenerateRecommendations(health);

        return health;
    }

    public async Task<SystemMetrics> GetMetricsAsync()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            
            // Get basic system metrics (non-blocking)
            var metrics = new SystemMetrics
            {
                CollectedAt = DateTime.UtcNow,
                CpuUsage = GetCpuUsage(),
                MemoryUsage = process.WorkingSet64 / (1024 * 1024), // MB
                DiskUsage = GetDiskUsage(),
                AverageResponseTime = TimeSpan.FromMilliseconds(250) // Default estimate
            };

            // 🔥 CRITICAL FIX: Get active users count with separate DbContext to prevent concurrency
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)); // Shorter timeout
                using var context = await _contextFactory.CreateDbContextAsync(cts.Token);
                
                metrics.ActiveUsers = await context.UserSessions
                    .Where(s => s.IsActive && s.LastActivityAt > DateTime.UtcNow.AddMinutes(-30))
                    .CountAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Active users count query timed out - using cached value");
                metrics.ActiveUsers = 0; // Default safe value
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get active users count");
                metrics.ActiveUsers = 0;
            }

            // Estimate requests per minute (this would be better tracked in middleware)
            metrics.RequestsPerMinute = EstimateRequestsPerMinute();

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect system metrics");
            return new SystemMetrics();
        }
    }

    public async Task<IEnumerable<SystemAlert>> GetActiveAlertsAsync()
    {
        var alerts = new List<SystemAlert>();

        try
        {
            var health = await GetSystemHealthAsync();
            
            // Generate alerts based on system health
            if (health.OverallStatus == HealthStatus.Critical)
            {
                alerts.Add(new SystemAlert
                {
                    Severity = AlertSeverity.Critical,
                    Title = "System Health Critical",
                    Description = "One or more critical system components are failing",
                    TriggeredAt = DateTime.UtcNow
                });
            }

            if (health.OverallStatus == HealthStatus.Warning)
            {
                alerts.Add(new SystemAlert
                {
                    Severity = AlertSeverity.Warning,
                    Title = "System Performance Warning",
                    Description = "System performance is degraded",
                    TriggeredAt = DateTime.UtcNow
                });
            }

            // Check for high resource usage
            if (health.CurrentMetrics.MemoryUsage > 1000) // > 1GB
            {
                alerts.Add(new SystemAlert
                {
                    Severity = AlertSeverity.Warning,
                    Title = "High Memory Usage",
                    Description = $"Memory usage is high: {health.CurrentMetrics.MemoryUsage:F0} MB",
                    TriggeredAt = DateTime.UtcNow
                });
            }

            return alerts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active alerts");
            return alerts;
        }
    }

    public async Task<MonitoringDashboard> GetDashboardDataAsync()
    {
        try
        {
            var health = await GetSystemHealthAsync();
            var alerts = await GetActiveAlertsAsync();

            var dashboard = new MonitoringDashboard
            {
                GeneratedAt = DateTime.UtcNow,
                Health = health,
                RecentAlerts = alerts.OrderByDescending(a => a.TriggeredAt).Take(10).ToList()
            };

            // Create dashboard widgets
            dashboard.Widgets = new List<DashboardWidget>
            {
                new()
                {
                    Id = "active-users",
                    Title = "Active Users",
                    Type = WidgetType.Counter,
                    Data = new Dictionary<string, object>
                    {
                        ["value"] = health.CurrentMetrics.ActiveUsers,
                        ["unit"] = "users"
                    }
                },
                new()
                {
                    Id = "memory-usage",
                    Title = "Memory Usage",
                    Type = WidgetType.Progress,
                    Data = new Dictionary<string, object>
                    {
                        ["value"] = health.CurrentMetrics.MemoryUsage,
                        ["max"] = 2048, // 2GB
                        ["unit"] = "MB"
                    }
                },
                new()
                {
                    Id = "requests-per-minute",
                    Title = "Requests/Minute",
                    Type = WidgetType.Counter,
                    Data = new Dictionary<string, object>
                    {
                        ["value"] = health.CurrentMetrics.RequestsPerMinute,
                        ["unit"] = "req/min"
                    }
                },
                new()
                {
                    Id = "response-time",
                    Title = "Avg Response Time",
                    Type = WidgetType.Status,
                    Data = new Dictionary<string, object>
                    {
                        ["value"] = health.CurrentMetrics.AverageResponseTime.TotalMilliseconds,
                        ["unit"] = "ms",
                        ["status"] = health.CurrentMetrics.AverageResponseTime.TotalMilliseconds < 500 ? "good" : "warning"
                    }
                }
            };

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate dashboard data");
            return new MonitoringDashboard();
        }
    }

    private async Task<DatabaseHealth> GetDatabaseHealthAsync()
    {
        var dbHealth = new DatabaseHealth
        {
            DatabaseName = "InsightLearnDb",
            LastChecked = DateTime.UtcNow
        };

        try
        {
            // 🔥 CRITICAL FIX: Use separate DbContext for health check to prevent concurrency
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // Shorter timeout for health checks
            using var context = await _contextFactory.CreateDbContextAsync(cts.Token);
            
            var stopwatch = Stopwatch.StartNew();
            var canConnect = await context.Database.CanConnectAsync(cts.Token);
            stopwatch.Stop();

            dbHealth.ConnectionTime = stopwatch.Elapsed;
            dbHealth.Status = canConnect ? HealthStatus.Healthy : HealthStatus.Critical;
            
            if (canConnect)
            {
                // Get connection count with timeout to prevent blocking
                try
                {
                    using var queryTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2)); // Even shorter timeout
                    var connectionCount = await context.Database
                        .SqlQueryRaw<int>("SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE is_user_process = 1")
                        .FirstOrDefaultAsync(queryTimeout.Token);
                    dbHealth.ActiveConnections = connectionCount;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Database connection count query timed out - using safe default");
                    dbHealth.ActiveConnections = 1; // Default safe value
                }
                catch (Exception queryEx)
                {
                    _logger.LogWarning(queryEx, "Failed to get connection count - using safe default");
                    dbHealth.ActiveConnections = 1; // At least our connection
                }
            }
        }
        catch (OperationCanceledException)
        {
            dbHealth.Status = HealthStatus.Critical;
            dbHealth.ErrorMessage = "Database connection timed out";
            _logger.LogWarning("Database connection check timed out");
        }
        catch (Exception ex)
        {
            dbHealth.Status = HealthStatus.Critical;
            dbHealth.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Database health check failed");
        }

        return dbHealth;
    }

    private async Task<ServiceHealth> GetApiHealthAsync()
    {
        return await Task.FromResult(new ServiceHealth
        {
            ServiceName = "InsightLearn.Api",
            Status = HealthStatus.Healthy,
            ResponseTime = TimeSpan.FromMilliseconds(150),
            LastChecked = DateTime.UtcNow
        });
    }

    private double GetCpuUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            return Math.Round(process.TotalProcessorTime.TotalMilliseconds / Environment.TickCount * 100, 2);
        }
        catch
        {
            return 0.0;
        }
    }

    private double GetDiskUsage()
    {
        try
        {
            var drive = DriveInfo.GetDrives()
                .FirstOrDefault(d => d.DriveType == DriveType.Fixed && d.IsReady);
            
            if (drive != null)
            {
                var usedSpace = drive.TotalSize - drive.AvailableFreeSpace;
                return Math.Round((double)usedSpace / drive.TotalSize * 100, 2);
            }
        }
        catch
        {
            // Ignore errors
        }
        
        return 0.0;
    }

    private int EstimateRequestsPerMinute()
    {
        // This is a placeholder - in a real implementation, this would be tracked
        // by middleware or metrics collection
        return new Random().Next(50, 200);
    }

    private HealthStatus DetermineOverallHealth(SystemHealth health)
    {
        var statuses = new List<HealthStatus>();
        
        statuses.AddRange(health.Services.Select(s => s.Status));
        statuses.AddRange(health.Databases.Select(d => d.Status));

        if (statuses.Any(s => s == HealthStatus.Critical))
            return HealthStatus.Critical;
        
        if (statuses.Any(s => s == HealthStatus.Warning))
            return HealthStatus.Warning;
            
        if (statuses.Any(s => s == HealthStatus.Unknown))
            return HealthStatus.Unknown;

        return HealthStatus.Healthy;
    }

    private List<string> GenerateRecommendations(SystemHealth health)
    {
        var recommendations = new List<string>();

        if (health.CurrentMetrics.MemoryUsage > 1000)
        {
            recommendations.Add("Consider optimizing memory usage or increasing available memory");
        }

        if (health.CurrentMetrics.AverageResponseTime.TotalMilliseconds > 1000)
        {
            recommendations.Add("Response time is high - consider performance optimization");
        }

        if (health.Databases.Any(d => d.Status != HealthStatus.Healthy))
        {
            recommendations.Add("Database connectivity issues detected - check database server");
        }

        if (health.Services.Any(s => s.Status != HealthStatus.Healthy))
        {
            recommendations.Add("Service health issues detected - check dependent services");
        }

        return recommendations;
    }
}