using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InsightLearn.Core.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;
    
    public string? IconUrl { get; set; }
    
    public string? ColorCode { get; set; } = "#007bff";
    
    public Guid? ParentCategoryId { get; set; }
    
    public int OrderIndex { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsFeatured { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties (with [JsonIgnore] to prevent circular reference)
    [JsonIgnore]
    public virtual Category? ParentCategory { get; set; }

    [JsonIgnore]
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();

    [JsonIgnore]
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    
    public int CourseCount => Courses.Count(c => c.IsActive && c.Status == CourseStatus.Published);
    
    public bool HasSubCategories => SubCategories.Any(sc => sc.IsActive);
}