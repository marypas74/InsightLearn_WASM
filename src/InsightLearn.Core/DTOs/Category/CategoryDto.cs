namespace InsightLearn.Core.DTOs.Category;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? ColorCode { get; set; }
    public int OrderIndex { get; set; }
    public int CourseCount { get; set; }
}
