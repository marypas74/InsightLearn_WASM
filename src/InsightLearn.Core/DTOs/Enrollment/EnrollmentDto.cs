namespace InsightLearn.Core.DTOs.Enrollment;

/// <summary>
/// Enrollment data transfer object
/// </summary>
public class EnrollmentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserEmail { get; set; }

    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string? CourseThumbnailUrl { get; set; }

    public DateTime EnrolledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }

    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = string.Empty; // Active, Completed, Suspended, Cancelled, Refunded

    public int CurrentLessonIndex { get; set; }
    public Guid? CurrentLessonId { get; set; }
    public string? CurrentLessonTitle { get; set; }

    public bool HasCertificate { get; set; }

    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
    public double ProgressPercentage { get; set; }

    public int TotalWatchedMinutes { get; set; }

    public bool IsCompleted => CompletedAt.HasValue;
}
