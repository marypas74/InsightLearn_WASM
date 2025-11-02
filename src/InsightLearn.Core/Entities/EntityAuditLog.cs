using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class EntityAuditLog
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string EntityType { get; set; } = string.Empty; // Course, User, Category, etc.
    
    [Required]
    [StringLength(100)]
    public string EntityId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty; // Create, Update, Delete, View
    
    [StringLength(100)]
    public string? PropertyName { get; set; } // For updates, which property changed
    
    public string? OldValue { get; set; }
    
    public string? NewValue { get; set; }
    
    public Guid? UserId { get; set; } // Who made the change
    
    [StringLength(256)]
    public string? UserEmail { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(1000)]
    public string? UserAgent { get; set; }
    
    [StringLength(100)]
    public string? RequestId { get; set; }
    
    [StringLength(500)]
    public string? Reason { get; set; } // Why the change was made
    
    [StringLength(100)]
    public string? ChangeSource { get; set; } // UI, API, Import, System
    
    public Guid? BatchId { get; set; } // For bulk operations
    
    public DateTime AuditedAt { get; set; } = DateTime.UtcNow;
    
    public string? AdditionalContext { get; set; } // JSON with extra information
    
    // Navigation properties
    public virtual User? User { get; set; }
}