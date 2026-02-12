namespace InsightLearn.Core.DTOs.Course;

/// <summary>
/// Course section DTO
/// </summary>
public class SectionDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// URL-safe encoded ID for public URLs (v2.3.113-dev)
    /// </summary>
    public string EncodedId { get; set; } = string.Empty;

    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public bool IsActive { get; set; }

    public List<LessonDto> Lessons { get; set; } = new();

    public int TotalLessons => Lessons.Count;
    public int TotalDurationMinutes => Lessons.Sum(l => l.DurationMinutes);
}
