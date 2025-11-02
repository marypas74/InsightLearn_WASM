using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class SystemHealth
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Component { get; set; } = string.Empty;
    
    [Required]
    public HealthStatus Status { get; set; } = HealthStatus.Healthy;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    
    public double ResponseTimeMs { get; set; }
    
    public Dictionary<string, object>? Metrics { get; set; }
    
    [StringLength(100)]
    public string? Version { get; set; }
    
    public DateTime? LastHealthyAt { get; set; }
    
    [StringLength(200)]
    public string? ErrorMessage { get; set; }
}

public enum HealthStatus
{
    Healthy = 0,
    Degraded = 1,
    Warning = 1,
    Unhealthy = 2,
    Critical = 3,
    Unknown = 4
}