namespace InsightLearn.Core.DTOs.Enrollment;

/// <summary>
/// Overall enrollment progress information
/// </summary>
public class EnrollmentProgressDto
{
    public Guid EnrollmentId { get; set; }
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;

    public int TotalSections { get; set; }
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }

    public double ProgressPercentage { get; set; }

    public int TotalCourseMinutes { get; set; }
    public int TotalWatchedMinutes { get; set; }

    public DateTime LastAccessedAt { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }

    public List<SectionProgressDto> Sections { get; set; } = new();
}

/// <summary>
/// Section-level progress information
/// </summary>
public class SectionProgressDto
{
    public Guid SectionId { get; set; }
    public string SectionTitle { get; set; } = string.Empty;
    public int TotalLessons { get; set; }
    public int CompletedLessons { get; set; }
    public double ProgressPercentage { get; set; }
}
