using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Category;

public class CreateCategoryDto
{
    [Required][StringLength(100)] public string Name { get; set; } = string.Empty;
    [StringLength(200)] public string? IconUrl { get; set; }
    [StringLength(50)][RegularExpression(@"^[a-z0-9-]+$")] public string? Slug { get; set; }
    [StringLength(7)][RegularExpression(@"^#[0-9A-Fa-f]{6}$")] public string? ColorCode { get; set; }
    public int OrderIndex { get; set; } = 0;
}
