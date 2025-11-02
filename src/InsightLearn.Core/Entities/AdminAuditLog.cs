using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class AdminAuditLog
{
    public Guid Id { get; set; }
    
    [Required]
    public Guid AdminUserId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? EntityType { get; set; }
    
    public Guid? EntityId { get; set; }
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public string? OldValues { get; set; }
    
    public string? NewValues { get; set; }
    
    [StringLength(15)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    [StringLength(20)]
    public string Severity { get; set; } = "Information";
    
    // Navigation property
    public virtual User AdminUser { get; set; } = null!;
}