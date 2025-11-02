namespace InsightLearn.Core.Interfaces;

public interface ISystemMonitor
{
    Task<SystemHealth> GetSystemHealthAsync();
    Task<SystemMetrics> GetMetricsAsync();
    Task<IEnumerable<SystemAlert>> GetActiveAlertsAsync();
    Task<MonitoringDashboard> GetDashboardDataAsync();
}

public class SystemHealth
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public HealthStatus OverallStatus { get; set; }
    public List<ServiceHealth> Services { get; set; } = new();
    public List<DatabaseHealth> Databases { get; set; } = new();
    public SystemMetrics CurrentMetrics { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class ServiceHealth
{
    public string ServiceName { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
}

public class DatabaseHealth
{
    public string DatabaseName { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public TimeSpan ConnectionTime { get; set; }
    public int ActiveConnections { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
}

public class SystemMetrics
{
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public double DiskUsage { get; set; }
    public int ActiveUsers { get; set; }
    public int RequestsPerMinute { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
}

public class SystemAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public class MonitoringDashboard
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public SystemHealth Health { get; set; } = new();
    public List<DashboardWidget> Widgets { get; set; } = new();
    public List<SystemAlert> RecentAlerts { get; set; } = new();
}

public class DashboardWidget
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public WidgetType Type { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

public enum HealthStatus
{
    Healthy = 0,
    Warning = 1,
    Critical = 2,
    Unknown = 3
}

public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}

public enum WidgetType
{
    Counter,
    Chart,
    Progress,
    Status,
    Table
}