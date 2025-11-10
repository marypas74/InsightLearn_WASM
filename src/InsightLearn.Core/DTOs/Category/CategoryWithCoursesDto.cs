using InsightLearn.Core.DTOs.Course;

namespace InsightLearn.Core.DTOs.Category;

public class CategoryWithCoursesDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? ColorCode { get; set; }
    public List<CourseCardDto> Courses { get; set; } = new();
    public int TotalCourses { get; set; }
}
