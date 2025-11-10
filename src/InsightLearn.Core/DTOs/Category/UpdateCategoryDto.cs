using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Category;

public class UpdateCategoryDto
{
    [Required] public Guid Id { get; set; }
    [StringLength(100)] public string? Name { get; set; }
    [StringLength(200)] public string? IconUrl { get; set; }
    [StringLength(7)] public string? ColorCode { get; set; }
    public int? OrderIndex { get; set; }
}
