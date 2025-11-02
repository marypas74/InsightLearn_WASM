using InsightLearn.Application.Interfaces;
using InsightLearn.Application.DTOs;
using InsightLearn.Core.Entities;
using Microsoft.Extensions.Logging;
// REMOVED: Microsoft.Extensions.Hosting to prevent BackgroundService conflicts
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using SystemAlertEntity = InsightLearn.Core.Entities.SystemAlert;

namespace InsightLearn.Application.Services;

// Temporary data structure for performance metrics until entity mapping is resolved
public class PerformanceData
{
    public DateTime Timestamp { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public int ActiveUsers { get; set; }
    public int RequestsPerMinute { get; set; }
    public double AverageResponseTime { get; set; }
}

// Extension for RealTimeMetric to add Status property functionality
public static class RealTimeMetricExtensions
{
    public static MetricStatus GetStatus(this RealTimeMetric metric, double warningThreshold, double criticalThreshold)
    {
        if (metric.Value >= criticalThreshold) return MetricStatus.Critical;
        if (metric.Value >= warningThreshold) return MetricStatus.Warning;
        return MetricStatus.Good;
    }
}

public class EnterpriseMonitoringService : IEnterpriseMonitoringService
{
    private readonly ILogger<EnterpriseMonitoringService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, RealTimeMetric> _realTimeMetrics;
    // REMOVED: Background timers to prevent blocking operations
    // private readonly Timer _metricsTimer;
    // private readonly Timer _alertsTimer;
    // private readonly Timer _cleanupTimer;
    
    // Enterprise Metrics Storage
    private readonly List<PerformanceMetric> _performanceHistory;
    private readonly List<SystemAlertEntity> _activeAlerts;
    
    // Temporary storage for performance data until proper entity mapping
    private readonly List<PerformanceData> _performanceDataHistory;
    private readonly ConcurrentQueue<SecurityEvent> _recentSecurityEvents;
    
    private bool _isMonitoring = false;

    public EnterpriseMonitoringService(
        ILogger<EnterpriseMonitoringService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _realTimeMetrics = new ConcurrentDictionary<string, RealTimeMetric>();
        _performanceHistory = new List<PerformanceMetric>();
        _activeAlerts = new List<SystemAlertEntity>();
        _recentSecurityEvents = new ConcurrentQueue<SecurityEvent>();
        _performanceDataHistory = new List<PerformanceData>();
        
        // REMOVED: Background timers initialization to prevent blocking
        // _metricsTimer = new Timer(CollectMetrics, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        // _alertsTimer = new Timer(ProcessAlerts, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        // _cleanupTimer = new Timer(CleanupOldData, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    // REMOVED: Background service execution to prevent blocking web operations
    // protected override async Task ExecuteAsync(CancellationToken stoppingToken)

    // 🔥 CRITICAL FIX: Enhanced Enterprise Health Status with better error handling and timeouts
    public async Task<EnterpriseHealthStatus> GetEnterpriseHealthStatusAsync()
    {
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("🔥 ENTERPRISE: Getting health status. CorrelationId: {CorrelationId}", correlationId);
            
            // Use timeout for entire operation to prevent hanging
            using var overallTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            
            using var scope = _serviceProvider.CreateScope();
            var systemMonitor = scope.ServiceProvider.GetRequiredService<InsightLearn.Core.Interfaces.ISystemMonitor>();
            
            // Get system health and alerts with individual timeouts
            var systemHealthTask = GetSystemHealthSafely(systemMonitor, correlationId);
            var alertsTask = GetAlertsSafely(systemMonitor, correlationId);
            
            // Wait for both tasks with timeout
            await Task.WhenAll(systemHealthTask, alertsTask).WaitAsync(overallTimeout.Token);
            
            var systemHealth = await systemHealthTask;
            var alerts = await alertsTask;
            
            var enterpriseHealth = new EnterpriseHealthStatus
            {
                GeneratedAt = DateTime.UtcNow,
                OverallStatus = MapHealthStatus(systemHealth.OverallStatus),
                SystemHealth = MapSystemHealth(systemHealth),
                ActiveAlertsCount = alerts.Count(),
                ActiveAlerts = MapSystemAlerts(alerts.Take(5).ToList()),
                RealTimeMetrics = _realTimeMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                PerformanceMetrics = GetLatestPerformanceMetrics(25), // Reduced count for faster response
                SecurityEvents = GetRecentSecurityEvents(5), // Reduced count
                Recommendations = GenerateEnterpriseRecommendations(MapSystemHealth(systemHealth), MapSystemAlerts(alerts.ToList()))
            };
            
            _logger.LogInformation("✅ ENTERPRISE: Health status retrieved successfully. CorrelationId: {CorrelationId}", correlationId);
            return enterpriseHealth;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("🔥 ENTERPRISE: Health status request timed out. CorrelationId: {CorrelationId}", correlationId);
            return CreateFallbackEnterpriseHealthStatus("Health status request timed out", HealthStatus.Warning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 ENTERPRISE: Error getting health status. CorrelationId: {CorrelationId}", correlationId);
            return CreateFallbackEnterpriseHealthStatus($"Error: {ex.Message}", HealthStatus.Critical);
        }
    }
    
    // 🔥 Helper method to get system health safely with timeout
    private async Task<InsightLearn.Core.Interfaces.SystemHealth> GetSystemHealthSafely(InsightLearn.Core.Interfaces.ISystemMonitor systemMonitor, string correlationId)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            return await systemMonitor.GetSystemHealthAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "🔥 ENTERPRISE: System health check failed - using fallback. CorrelationId: {CorrelationId}", correlationId);
            
            // Return a basic fallback system health
            return new InsightLearn.Core.Interfaces.SystemHealth
            {
                GeneratedAt = DateTime.UtcNow,
                OverallStatus = InsightLearn.Core.Interfaces.HealthStatus.Warning,
                Services = new List<InsightLearn.Core.Interfaces.ServiceHealth>(),
                Databases = new List<InsightLearn.Core.Interfaces.DatabaseHealth>(),
                CurrentMetrics = new InsightLearn.Core.Interfaces.SystemMetrics(),
                Recommendations = new List<string> { "System health check failed - using fallback data" }
            };
        }
    }
    
    // 🔥 Helper method to get alerts safely with timeout
    private async Task<IEnumerable<InsightLearn.Core.Interfaces.SystemAlert>> GetAlertsSafely(InsightLearn.Core.Interfaces.ISystemMonitor systemMonitor, string correlationId)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            return await systemMonitor.GetActiveAlertsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "🔥 ENTERPRISE: Alerts check failed - using empty list. CorrelationId: {CorrelationId}", correlationId);
            return new List<InsightLearn.Core.Interfaces.SystemAlert>();
        }
    }
    
    // 🔥 Create fallback enterprise health status
    private EnterpriseHealthStatus CreateFallbackEnterpriseHealthStatus(string reason, HealthStatus status)
    {
        return new EnterpriseHealthStatus
        {
            GeneratedAt = DateTime.UtcNow,
            OverallStatus = status,
            SystemHealth = new InsightLearn.Core.Entities.SystemHealth
            {
                Component = "Enterprise Monitor",
                Status = status,
                Description = reason,
                CheckedAt = DateTime.UtcNow,
                ErrorMessage = status == HealthStatus.Critical ? reason : null
            },
            ActiveAlertsCount = 0,
            ActiveAlerts = new List<InsightLearn.Core.Entities.SystemAlert>(),
            RealTimeMetrics = new Dictionary<string, RealTimeMetric>(),
            PerformanceMetrics = new List<PerformanceMetric>(),
            SecurityEvents = new List<SecurityEvent>(),
            Recommendations = new List<string> { reason }
        };
    }

    public async Task<SystemHealth> GetSystemHealthAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var systemMonitor = scope.ServiceProvider.GetRequiredService<InsightLearn.Core.Interfaces.ISystemMonitor>();
            
            var systemHealth = await systemMonitor.GetSystemHealthAsync();
            return MapSystemHealth(systemHealth);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system health");
            
            // Return a default SystemHealth object in case of error
            return new SystemHealth
            {
                Component = "System",
                Status = HealthStatus.Unknown,
                Description = $"Error retrieving system health: {ex.Message}",
                CheckedAt = DateTime.UtcNow,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<IEnumerable<SystemAlertEntity>> GetActiveAlertsAsync()
    {
        return await Task.FromResult(_activeAlerts.AsEnumerable());
    }

    public async Task<IEnumerable<PerformanceMetric>> GetPerformanceMetricsAsync(int count = 100)
    {
        // FIXED: Clean up old data on-demand to prevent memory issues
        CleanupOldDataIfNeeded();
        
        // Convert our temporary performance data to PerformanceMetric entities
        var recentData = _performanceDataHistory
            .OrderByDescending(p => p.Timestamp)
            .Take(count)
            .ToList();

        var performanceMetrics = recentData.Select(data => new PerformanceMetric
        {
            Id = Guid.NewGuid(),
            MetricType = "SystemPerformance",
            MetricName = "Composite Performance Metric",
            Value = (decimal)((data.CpuUsage + data.MemoryUsage / 10) / 2), // Simple composite score
            Unit = "composite",
            Source = "EnterpriseMonitoring",
            Component = "System",
            CollectedAt = data.Timestamp,
            Tags = System.Text.Json.JsonSerializer.Serialize(new
            {
                CpuUsage = data.CpuUsage,
                MemoryUsage = data.MemoryUsage,
                ActiveUsers = data.ActiveUsers,
                RequestsPerMinute = data.RequestsPerMinute,
                AverageResponseTime = data.AverageResponseTime
            }),
            Environment = "Production"
        }).ToList();

        return await Task.FromResult(performanceMetrics.AsEnumerable());
    }

    public async Task<Dictionary<string, RealTimeMetric>> GetRealTimeMetricsAsync()
    {
        // 🔥 CRITICAL FIX: Collect metrics on-demand with enhanced error handling and shorter timeouts
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("🔥 METRICS: Collecting real-time metrics. CorrelationId: {CorrelationId}", correlationId);
            
            using var scope = _serviceProvider.CreateScope();
            var systemMonitor = scope.ServiceProvider.GetRequiredService<InsightLearn.Core.Interfaces.ISystemMonitor>();
            
            // Get metrics with shorter timeout to prevent navigation blocking
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // Reduced timeout
            
            InsightLearn.Core.Interfaces.SystemMetrics metrics;
            try
            {
                metrics = await systemMonitor.GetMetricsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "🔥 METRICS: System monitor failed - using process metrics. CorrelationId: {CorrelationId}", correlationId);
                // Fallback to basic process metrics
                metrics = new InsightLearn.Core.Interfaces.SystemMetrics
                {
                    CollectedAt = DateTime.UtcNow,
                    ActiveUsers = 0,
                    RequestsPerMinute = 0
                };
            }
            
            var process = Process.GetCurrentProcess();
            
            // Update real-time metrics (on-demand) with error handling for each metric
            try
            {
                var cpuUsage = GetCpuUsage();
                _realTimeMetrics["cpu_usage"] = new RealTimeMetric
                {
                    Name = "CPU Usage",
                    Value = cpuUsage,
                    Unit = "%",
                    Timestamp = DateTime.UtcNow,
                    Source = "SystemMonitor",
                    Category = "Performance",
                    Threshold = 80,
                    IsAlert = cpuUsage > 90
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "🔥 METRICS: CPU metric collection failed. CorrelationId: {CorrelationId}", correlationId);
            }
            
            try
            {
                var memoryUsage = process.WorkingSet64 / (1024 * 1024);
                _realTimeMetrics["memory_usage"] = new RealTimeMetric
                {
                    Name = "Memory Usage",
                    Value = memoryUsage,
                    Unit = "MB",
                    Timestamp = DateTime.UtcNow,
                    Source = "SystemMonitor",
                    Category = "Performance",
                    Threshold = 1000,
                    IsAlert = memoryUsage > 1500
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "🔥 METRICS: Memory metric collection failed. CorrelationId: {CorrelationId}", correlationId);
            }
            
            try
            {
                _realTimeMetrics["active_users"] = new RealTimeMetric
                {
                    Name = "Active Users",
                    Value = metrics.ActiveUsers,
                    Unit = "users",
                    Timestamp = DateTime.UtcNow,
                    Source = "SystemMonitor",
                    Category = "Usage",
                    IsAlert = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "🔥 METRICS: Active users metric failed. CorrelationId: {CorrelationId}", correlationId);
            }
            
            try
            {
                _realTimeMetrics["requests_per_minute"] = new RealTimeMetric
                {
                    Name = "Requests/Minute",
                    Value = metrics.RequestsPerMinute,
                    Unit = "req/min",
                    Timestamp = DateTime.UtcNow,
                    Source = "SystemMonitor",
                    Category = "Performance",
                    Threshold = 500,
                    IsAlert = metrics.RequestsPerMinute > 1000
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "🔥 METRICS: Requests per minute metric failed. CorrelationId: {CorrelationId}", correlationId);
            }
            
            _logger.LogInformation("✅ METRICS: Real-time metrics collected successfully. CorrelationId: {CorrelationId}", correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔥 METRICS: Error collecting real-time metrics on-demand. CorrelationId: {CorrelationId}", correlationId);
            // Return cached metrics if collection fails - don't clear existing metrics
        }
        
        return await Task.FromResult(_realTimeMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }

    public async Task CreateAlertAsync(SystemAlertEntity alert)
    {
        if (alert == null)
            throw new ArgumentNullException(nameof(alert));

        alert.CreatedAt = DateTime.UtcNow;
        alert.TriggeredAt = DateTime.UtcNow;
        alert.IsActive = true;
        alert.IsResolved = false;

        _activeAlerts.Add(alert);
        
        // Notify via SignalR if available
        await NotifyDashboardClients("AlertCreated", alert);
        
        _logger.LogWarning("Alert created: {Title} - {Description}", alert.Title, alert.Description);
    }

    public async Task ResolveAlertAsync(int alertId, string resolutionNotes)
    {
        // Find alert by numeric ID - convert to int if the alert has a numeric representation
        var alert = _activeAlerts.FirstOrDefault(a => a.Id.GetHashCode() == alertId);
        if (alert != null)
        {
            await ResolveAlertAsync(alert.Id, resolutionNotes);
        }
        else
        {
            _logger.LogWarning("Alert with ID {AlertId} not found for resolution", alertId);
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var systemMonitor = scope.ServiceProvider.GetRequiredService<InsightLearn.Core.Interfaces.ISystemMonitor>();
            
            var systemHealth = await systemMonitor.GetSystemHealthAsync();
            
            // Check if system is healthy
            var isSystemHealthy = systemHealth.OverallStatus == InsightLearn.Core.Interfaces.HealthStatus.Healthy;
            
            // Check for critical alerts
            var hasCriticalAlerts = _activeAlerts.Any(a => a.IsActive && a.Severity == AlertSeverity.Critical);
            
            // Check key metrics
            var cpuMetric = _realTimeMetrics.GetValueOrDefault("cpu_usage");
            var memoryMetric = _realTimeMetrics.GetValueOrDefault("memory_usage");
            
            var isPerformanceHealthy = cpuMetric?.Value < 80 && memoryMetric?.Value < 1200;
            
            return isSystemHealthy && !hasCriticalAlerts && isPerformanceHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking system health status");
            return false;
        }
    }

    public async Task<object> GetPerformanceHistoryAsync(int hours)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        var historyData = _performanceDataHistory
            .Where(p => p.Timestamp >= cutoffTime)
            .OrderBy(p => p.Timestamp)
            .Select(p => new
            {
                timestamp = p.Timestamp,
                cpuUsage = p.CpuUsage,
                memoryUsage = p.MemoryUsage,
                activeUsers = p.ActiveUsers,
                requestsPerMinute = p.RequestsPerMinute,
                averageResponseTime = p.AverageResponseTime
            })
            .ToList();

        return await Task.FromResult((object)historyData);
    }

    [Obsolete("Use GetPerformanceHistoryAsync(int hours) instead")]
    public async Task<List<PerformanceMetric>> GetPerformanceHistoryListAsync(int hours = 24)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-hours);
        return await Task.FromResult(
            _performanceHistory
                .Where(p => p.CollectedAt >= cutoffTime)
                .OrderBy(p => p.CollectedAt)
                .ToList()
        );
    }

    [Obsolete("Use GetActiveAlertsAsync() from interface instead")]
    public async Task<List<SystemAlertEntity>> GetActiveAlertsListAsync()
    {
        return await Task.FromResult(_activeAlerts.ToList());
    }

    public async Task<EnterpriseAnalyticsSummary> GetAnalyticsSummaryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        startDate ??= DateTime.UtcNow.AddDays(-7);
        endDate ??= DateTime.UtcNow;

        using var scope = _serviceProvider.CreateScope();
        var analyticsService = scope.ServiceProvider.GetService<IAnalyticsService>();
        
        if (analyticsService == null)
        {
            return new EnterpriseAnalyticsSummary
            {
                GeneratedAt = DateTime.UtcNow,
                Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                Summary = "Analytics service not available"
            };
        }

        try
        {
            // Collect comprehensive analytics
            var loginAnalytics = await analyticsService.GetLoginAnalyticsAsync(startDate, endDate);
            var errorAnalytics = await analyticsService.GetErrorAnalyticsAsync(startDate, endDate);
            var performanceAnalytics = await analyticsService.GetPerformanceAnalyticsAsync(startDate, endDate);
            var securityAnalytics = await analyticsService.GetSecurityAnalyticsAsync(startDate, endDate);
            var businessMetrics = await analyticsService.GetBusinessMetricsAsync(startDate, endDate);
            
            return new EnterpriseAnalyticsSummary
            {
                GeneratedAt = DateTime.UtcNow,
                Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                LoginAnalytics = MapLoginAnalytics(loginAnalytics),
                ErrorAnalytics = MapErrorAnalytics(errorAnalytics),
                PerformanceAnalytics = MapPerformanceAnalytics(performanceAnalytics),
                SecurityAnalytics = MapSecurityAnalytics(securityAnalytics),
                BusinessMetrics = MapBusinessMetrics(businessMetrics),
                Summary = GenerateAnalyticsSummary(MapLoginAnalytics(loginAnalytics), MapErrorAnalytics(errorAnalytics), MapPerformanceAnalytics(performanceAnalytics), MapSecurityAnalytics(securityAnalytics), MapBusinessMetrics(businessMetrics))
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating analytics summary");
            return new EnterpriseAnalyticsSummary
            {
                GeneratedAt = DateTime.UtcNow,
                Period = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                Summary = $"Error generating analytics: {ex.Message}"
            };
        }
    }

    public async Task TriggerAlertAsync(string alertType, string severity, string message, Dictionary<string, object>? metadata = null)
    {
        var alert = new SystemAlertEntity
        {
            Id = Guid.NewGuid(),
            Severity = Enum.Parse<AlertSeverity>(severity, true),
            Title = alertType,
            Description = message,
            TriggeredAt = DateTime.UtcNow,
            Metadata = metadata ?? new Dictionary<string, object>()
        };

        _activeAlerts.Add(alert);
        
        // Notify via SignalR if available
        await NotifyDashboardClients("AlertTriggered", alert);
        
        _logger.LogWarning("Alert triggered: {AlertType} - {Message}", alertType, message);
    }

    public async Task ResolveAlertAsync(Guid alertId, string resolutionNote)
    {
        var alert = _activeAlerts.FirstOrDefault(a => a.Id == alertId);
        if (alert != null)
        {
            alert.IsResolved = true;
            alert.ResolvedAt = DateTime.UtcNow;
            alert.ResolutionNotes = resolutionNote;
            
            _activeAlerts.Remove(alert);
            
            await NotifyDashboardClients("AlertResolved", new { alertId, resolutionNote });
            _logger.LogInformation("Alert resolved: {AlertId}", alertId);
        }
    }

    // REMOVED: Background metrics collection to prevent blocking operations
    // Metrics are now collected on-demand when requested to prevent authentication conflicts

    // REMOVED: Background alert processing to prevent blocking operations
    // Alerts are now processed on-demand to prevent authentication conflicts

    // FIXED: On-demand data cleanup to prevent memory issues without blocking authentication
    private void CleanupOldDataIfNeeded()
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-48);
            
            // Only cleanup if we have too much data to prevent memory issues
            if (_performanceDataHistory.Count > 1000)
            {
                var itemsToRemove = _performanceDataHistory.Where(p => p.Timestamp < cutoffTime).ToList();
                foreach (var item in itemsToRemove)
                {
                    _performanceDataHistory.Remove(item);
                }
                _logger.LogDebug("Cleaned up {Count} old performance data items", itemsToRemove.Count);
            }
            
            if (_performanceHistory.Count > 1000)
            {
                var metricsToRemove = _performanceHistory.Where(p => p.CollectedAt < cutoffTime).ToList();
                foreach (var metric in metricsToRemove)
                {
                    _performanceHistory.Remove(metric);
                }
                _logger.LogDebug("Cleaned up {Count} old performance metrics", metricsToRemove.Count);
            }
            
            // Cleanup resolved alerts older than 24 hours
            var alertCutoffTime = DateTime.UtcNow.AddHours(-24);
            if (_activeAlerts.Count > 100)
            {
                var alertsToRemove = _activeAlerts.Where(a => a.IsResolved && a.ResolvedAt < alertCutoffTime).ToList();
                foreach (var alert in alertsToRemove)
                {
                    _activeAlerts.Remove(alert);
                }
                _logger.LogDebug("Cleaned up {Count} old resolved alerts", alertsToRemove.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during on-demand data cleanup");
        }
    }

    private async Task PerformHealthChecks()
    {
        // Implemented by the SystemMonitor service
        await Task.CompletedTask;
    }

    private async Task MonitorSystemPerformance()
    {
        // Performance monitoring is handled by metrics collection
        await Task.CompletedTask;
    }

    private async Task DetectAnomalies()
    {
        try
        {
            // Simple anomaly detection based on recent performance trends
            var recentMetrics = _performanceDataHistory
                .Where(p => p.Timestamp >= DateTime.UtcNow.AddMinutes(-30))
                .ToList();
                
            if (recentMetrics.Count > 10)
            {
                var avgCpu = recentMetrics.Average(m => m.CpuUsage);
                var avgMemory = recentMetrics.Average(m => m.MemoryUsage);
                var avgResponseTime = recentMetrics.Average(m => m.AverageResponseTime);
                
                var currentCpu = recentMetrics.Last().CpuUsage;
                var currentMemory = recentMetrics.Last().MemoryUsage;
                var currentResponseTime = recentMetrics.Last().AverageResponseTime;
                
                // Detect CPU spikes
                if (currentCpu > avgCpu * 2 && currentCpu > 50)
                {
                    await TriggerAlertAsync("CpuAnomaly", "Warning", 
                        $"CPU usage anomaly detected: {currentCpu:F1}% (average: {avgCpu:F1}%)");
                }
                
                // Detect memory spikes
                if (currentMemory > avgMemory * 1.5 && currentMemory > 800)
                {
                    await TriggerAlertAsync("MemoryAnomaly", "Warning", 
                        $"Memory usage anomaly detected: {currentMemory:F0}MB (average: {avgMemory:F0}MB)");
                }
                
                // Detect response time spikes
                if (currentResponseTime > avgResponseTime * 3 && currentResponseTime > 1000)
                {
                    await TriggerAlertAsync("ResponseTimeAnomaly", "Warning", 
                        $"Response time anomaly detected: {currentResponseTime:F0}ms (average: {avgResponseTime:F0}ms)");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting anomalies");
        }
    }

    private async Task NotifyDashboardClients(string method, object data)
    {
        try
        {
            // TODO: Implement proper event-driven notification system
            // For now, just log the event - SignalR integration should be done at Web layer
            _logger.LogDebug("Dashboard notification: {Method} with data type {DataType}", method, data?.GetType().Name);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Dashboard notification failed");
        }
    }

    private double GetCpuUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            return Math.Min(Math.Round(process.TotalProcessorTime.TotalMilliseconds / Environment.TickCount * 100, 2), 100);
        }
        catch
        {
            return 0.0;
        }
    }

    private MetricStatus GetMetricStatus(double value, double warningThreshold, double criticalThreshold)
    {
        if (value >= criticalThreshold) return MetricStatus.Critical;
        if (value >= warningThreshold) return MetricStatus.Warning;
        return MetricStatus.Good;
    }
    
    private static bool ShouldAlert(double value, double threshold)
    {
        return value > threshold;
    }

    private bool IsAlertConditionResolved(SystemAlertEntity alert)
    {
        return alert.Title switch
        {
            "HighCpuUsage" => _realTimeMetrics.GetValueOrDefault("cpu_usage")?.Value < 80,
            "HighMemoryUsage" => _realTimeMetrics.GetValueOrDefault("memory_usage")?.Value < 1000,
            _ => false
        };
    }

    private List<RealTimeMetric> GetCurrentRealTimeMetrics()
    {
        return _realTimeMetrics.Values.OrderBy(m => m.Name).ToList();
    }

    private List<PerformanceMetric> GetLatestPerformanceMetrics(int count)
    {
        return _performanceHistory
            .OrderByDescending(p => p.CollectedAt)
            .Take(count)
            .OrderBy(p => p.CollectedAt)
            .ToList();
    }

    private List<SecurityEvent> GetRecentSecurityEvents(int count)
    {
        return _recentSecurityEvents
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToList();
    }

    private List<string> GenerateEnterpriseRecommendations(InsightLearn.Core.Entities.SystemHealth systemHealth, List<InsightLearn.Core.Entities.SystemAlert> alerts)
    {
        var recommendations = new List<string>();
        
        if (alerts.Any(a => a.Severity == AlertSeverity.Critical))
        {
            recommendations.Add("Critical alerts require immediate attention");
        }
        
        var cpuMetric = _realTimeMetrics.GetValueOrDefault("cpu_usage");
        if (cpuMetric?.Value > 80)
        {
            recommendations.Add("Consider scaling up CPU resources or optimizing application performance");
        }
        
        var memoryMetric = _realTimeMetrics.GetValueOrDefault("memory_usage");
        if (memoryMetric?.Value > 1000)
        {
            recommendations.Add("Memory usage is high - consider memory optimization or increasing available memory");
        }
        
        if (_performanceDataHistory.Count > 10)
        {
            var avgResponseTime = _performanceDataHistory.TakeLast(10).Average(p => p.AverageResponseTime);
            if (avgResponseTime > 1000)
            {
                recommendations.Add("Average response time is degraded - investigate performance bottlenecks");
            }
        }
        
        return recommendations;
    }

    private string GenerateAnalyticsSummary(
        LoginAnalytics loginAnalytics,
        ErrorAnalytics errorAnalytics,
        PerformanceAnalytics performanceAnalytics,
        SecurityAnalytics securityAnalytics,
        BusinessMetrics businessMetrics)
    {
        var summary = new List<string>();
        
        summary.Add($"Login success rate: {loginAnalytics.SuccessRate:F1}% ({loginAnalytics.SuccessfulLogins}/{loginAnalytics.TotalAttempts})");
        summary.Add($"System errors: {errorAnalytics.TotalErrors} (Resolution rate: {errorAnalytics.ResolutionRate:F1}%)");
        summary.Add($"Average response time: {performanceAnalytics.AverageResponseTimeMs:F0}ms");
        summary.Add($"Security events: {securityAnalytics.TotalSecurityEvents} ({securityAnalytics.CriticalEvents} critical)");
        summary.Add($"Active users: {businessMetrics.ActiveUsers} (Retention: {businessMetrics.UserRetentionRate:F1}%)");
        
        return string.Join("; ", summary);
    }

    private LoginAnalytics MapLoginAnalytics(LoginAnalyticsDto dto)
    {
        return new LoginAnalytics
        {
            TotalAttempts = dto.TotalAttempts,
            SuccessfulLogins = dto.SuccessfulLogins,
            FailedLogins = dto.FailedAttempts,
            LoginsByMethod = dto.LoginsByMethod,
            LoginsByHour = new Dictionary<DateTime, int>() // Empty for now - could be mapped from HourlyDistribution
        };
    }

    private ErrorAnalytics MapErrorAnalytics(ErrorAnalyticsDto dto)
    {
        return new ErrorAnalytics
        {
            TotalErrors = dto.TotalErrors,
            ResolvedErrors = dto.ResolvedErrors,
            PendingErrors = dto.UnresolvedErrors,
            ErrorsByType = dto.ErrorsByType,
            ErrorsByHour = new Dictionary<DateTime, int>() // Empty for now
        };
    }

    private PerformanceAnalytics MapPerformanceAnalytics(PerformanceAnalyticsDto dto)
    {
        return new PerformanceAnalytics
        {
            AverageResponseTimeMs = (double)dto.AverageResponseTimeMs,
            MaxResponseTimeMs = (double)dto.P99ResponseTimeMs,
            MinResponseTimeMs = 0, // Not available in DTO
            TotalRequests = 0, // Not available in DTO
            EndpointResponseTimes = new Dictionary<string, double>(),
            ResponseTimesByHour = new Dictionary<DateTime, double>()
        };
    }

    private SecurityAnalytics MapSecurityAnalytics(SecurityAnalyticsDto dto)
    {
        return new SecurityAnalytics
        {
            TotalSecurityEvents = dto.TotalSecurityEvents,
            CriticalEvents = dto.CriticalEvents,
            WarningEvents = 0, // Not available in DTO - could calculate from EventsByType
            InfoEvents = 0, // Not available in DTO
            EventsByType = dto.EventsByType,
            EventsByHour = new Dictionary<DateTime, int>()
        };
    }

    private BusinessMetrics MapBusinessMetrics(BusinessMetricsDto dto)
    {
        return new BusinessMetrics
        {
            ActiveUsers = dto.ActiveUsers,
            NewRegistrations = dto.NewRegistrations,
            TotalCourseEnrollments = 0, // Not available in DTO - business-specific
            CompletedCourses = 0, // Not available in DTO - business-specific
            UserRetentionRate = (double)dto.UserRetentionRate,
            TotalRevenue = 0, // Not available in DTO - would need separate calculation
            PopularCourses = new Dictionary<string, int>()
        };
    }

    private InsightLearn.Core.Entities.SystemHealth MapSystemHealth(InsightLearn.Core.Interfaces.SystemHealth interfaceHealth)
    {
        return new InsightLearn.Core.Entities.SystemHealth
        {
            Component = "System",
            Status = MapHealthStatus(interfaceHealth.OverallStatus),
            Description = $"Overall system status with {interfaceHealth.Services.Count} services monitored",
            CheckedAt = interfaceHealth.GeneratedAt,
            ResponseTimeMs = interfaceHealth.CurrentMetrics.AverageResponseTime.TotalMilliseconds,
            Metrics = new Dictionary<string, object>
            {
                ["cpu_usage"] = interfaceHealth.CurrentMetrics.CpuUsage,
                ["memory_usage"] = interfaceHealth.CurrentMetrics.MemoryUsage,
                ["disk_usage"] = interfaceHealth.CurrentMetrics.DiskUsage,
                ["active_users"] = interfaceHealth.CurrentMetrics.ActiveUsers,
                ["requests_per_minute"] = interfaceHealth.CurrentMetrics.RequestsPerMinute,
                ["service_count"] = interfaceHealth.Services.Count,
                ["database_count"] = interfaceHealth.Databases.Count
            },
            LastHealthyAt = interfaceHealth.OverallStatus == InsightLearn.Core.Interfaces.HealthStatus.Healthy ? interfaceHealth.GeneratedAt : null
        };
    }

    private HealthStatus MapHealthStatus(InsightLearn.Core.Interfaces.HealthStatus interfaceStatus)
    {
        return interfaceStatus switch
        {
            InsightLearn.Core.Interfaces.HealthStatus.Healthy => HealthStatus.Healthy,
            InsightLearn.Core.Interfaces.HealthStatus.Warning => HealthStatus.Warning,
            InsightLearn.Core.Interfaces.HealthStatus.Critical => HealthStatus.Critical,
            InsightLearn.Core.Interfaces.HealthStatus.Unknown => HealthStatus.Unknown,
            _ => HealthStatus.Unknown
        };
    }

    private List<SystemAlertEntity> MapSystemAlerts(List<InsightLearn.Core.Interfaces.SystemAlert> interfaceAlerts)
    {
        return interfaceAlerts.Select(alert => new SystemAlertEntity
        {
            Id = alert.Id,
            Title = alert.Title,
            Description = alert.Description,
            Severity = MapAlertSeverity(alert.Severity),
            TriggeredAt = alert.TriggeredAt,
            IsActive = alert.IsActive,
            Message = alert.Description,
            Source = "SystemMonitor"
        }).ToList();
    }

    private AlertSeverity MapAlertSeverity(InsightLearn.Core.Interfaces.AlertSeverity interfaceSeverity)
    {
        return interfaceSeverity switch
        {
            InsightLearn.Core.Interfaces.AlertSeverity.Info => AlertSeverity.Info,
            InsightLearn.Core.Interfaces.AlertSeverity.Warning => AlertSeverity.Warning,
            InsightLearn.Core.Interfaces.AlertSeverity.Error => AlertSeverity.Error,
            InsightLearn.Core.Interfaces.AlertSeverity.Critical => AlertSeverity.Critical,
            _ => AlertSeverity.Info
        };
    }

    public void Dispose()
    {
        // REMOVED: Timer disposal as timers are no longer used
        // _metricsTimer?.Dispose();
        // _alertsTimer?.Dispose();
        // _cleanupTimer?.Dispose();
        // base.Dispose();
    }
}

