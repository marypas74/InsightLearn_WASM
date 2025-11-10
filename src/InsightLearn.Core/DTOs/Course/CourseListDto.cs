namespace InsightLearn.Core.DTOs.Course;

/// <summary>
/// Lightweight DTO for course listings with pagination
/// </summary>
public class CourseListDto
{
    public List<CourseCardDto> Courses { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}
