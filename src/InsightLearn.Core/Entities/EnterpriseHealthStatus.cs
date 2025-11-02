using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class EnterpriseHealthStatus
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    public HealthStatus OverallStatus { get; set; } = HealthStatus.Unknown;
    
    public object? SystemHealth { get; set; }
    
    public int ActiveAlertsCount { get; set; }
    
    public List<SystemAlert> ActiveAlerts { get; set; } = new();
    
    public Dictionary<string, RealTimeMetric> RealTimeMetrics { get; set; } = new();
    
    public List<PerformanceMetric> PerformanceMetrics { get; set; } = new();
    
    public List<SecurityEvent> SecurityEvents { get; set; } = new();
    
    public List<string> Recommendations { get; set; } = new();
    
    public double SystemScore { get; set; } = 100.0;
    
    public Dictionary<string, object> Metadata { get; set; } = new();
}