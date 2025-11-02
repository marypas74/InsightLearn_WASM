using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class RealTimeMetric
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public double Value { get; set; }
    
    [StringLength(50)]
    public string Unit { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [StringLength(100)]
    public string? Source { get; set; }
    
    [StringLength(100)]
    public string? Category { get; set; }
    
    public Dictionary<string, object>? Tags { get; set; }
    
    public double? Threshold { get; set; }
    
    public bool IsAlert { get; set; }
}