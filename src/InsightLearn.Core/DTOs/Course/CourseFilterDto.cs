namespace InsightLearn.Core.DTOs.Course;

/// <summary>
/// Filter options for course browsing
/// </summary>
public class CourseFilterDto
{
    public List<Guid>? CategoryIds { get; set; }
    public List<string>? Levels { get; set; }
    public List<string>? Languages { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? MinRating { get; set; }
    public bool? IsFree { get; set; }
    public bool? HasCertificate { get; set; }
    public int? MinDurationMinutes { get; set; }
    public int? MaxDurationMinutes { get; set; }
}
