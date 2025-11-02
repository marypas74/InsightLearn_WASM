using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class SystemAlert
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public AlertSeverity Severity { get; set; } = AlertSeverity.Info;
    
    [Required]
    [StringLength(100)]
    public string Source { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ResolvedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsResolved { get; set; } = false;
    
    [StringLength(500)]
    public string? ResolutionNotes { get; set; }
    
    [StringLength(100)]
    public string? Category { get; set; }
    
    public Dictionary<string, object>? Metadata { get; set; }
}

public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}