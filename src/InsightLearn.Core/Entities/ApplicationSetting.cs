using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class ApplicationSetting
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;
    
    [Required]
    public string Value { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [StringLength(50)]
    public string Category { get; set; } = "General";
    
    [StringLength(20)]
    public string ValueType { get; set; } = "String"; // String, Integer, Boolean, JSON
    
    public bool IsPublic { get; set; } = false;
    
    public bool IsReadOnly { get; set; } = false;
    
    public bool IsEncrypted { get; set; } = false;
    
    public string DisplayName { get; set; } = string.Empty;
    
    public string DataType { get; set; } = "String";
    
    public bool IsRequired { get; set; } = false;
    
    public string? DefaultValue { get; set; }
    
    public string? ValidationRules { get; set; }
    
    public int SortOrder { get; set; } = 0;
    
    public Guid? CreatedBy { get; set; }
    
    public Guid? UpdatedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}