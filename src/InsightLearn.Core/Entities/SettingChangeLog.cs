using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class SettingChangeLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid SettingId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string SettingKey { get; set; } = string.Empty;
    
    public string? OldValue { get; set; }
    
    public string? NewValue { get; set; }
    
    [Required]
    public Guid ChangedBy { get; set; }
    
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(500)]
    public string? ChangeReason { get; set; }
    
    [StringLength(50)]
    public string ChangeType { get; set; } = "Update"; // Create, Update, Delete
    
    // Navigation properties
    public virtual ApplicationSetting Setting { get; set; } = null!;
    public virtual User ChangedByUser { get; set; } = null!;
}