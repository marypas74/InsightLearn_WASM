using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class PerformanceMetric
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string MetricType { get; set; } = string.Empty; // ResponseTime, MemoryUsage, CpuUsage, DatabaseConnection
    
    [Required]
    [StringLength(200)]
    public string MetricName { get; set; } = string.Empty;
    
    public decimal Value { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty; // ms, MB, %, count
    
    [Required]
    [StringLength(100)]
    public string Source { get; set; } = string.Empty; // API, Database, Cache, External
    
    [StringLength(100)]
    public string? Component { get; set; } // Specific component or service name
    
    [StringLength(100)]
    public string? RequestId { get; set; }
    
    public Guid? UserId { get; set; }
    
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(1000)]
    public string? Tags { get; set; } // JSON with additional metadata
    
    public decimal? Threshold { get; set; } // Alert threshold if exceeded
    
    public bool IsAlert { get; set; } = false;
    
    [Required]
    [StringLength(50)]
    public string Environment { get; set; } = "Production"; // Development, Staging, Production
    
    // Navigation properties
    public virtual User? User { get; set; }
}