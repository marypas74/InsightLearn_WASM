using InsightLearn.Core.DTOs.Course;

namespace InsightLearn.Core.DTOs.Enrollment;

/// <summary>
/// Detailed enrollment information with course details
/// </summary>
public class EnrollmentDetailDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    public CourseDto? Course { get; set; }

    public DateTime EnrolledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastAccessedAt { get; set; }

    public decimal AmountPaid { get; set; }
    public string Status { get; set; } = string.Empty;

    public int CurrentLessonIndex { get; set; }
    public Guid? CurrentLessonId { get; set; }

    public bool HasCertificate { get; set; }
    public Guid? CertificateId { get; set; }

    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
    public double ProgressPercentage { get; set; }

    public int TotalWatchedMinutes { get; set; }
    public int TotalCourseMinutes { get; set; }

    public List<LessonProgressDto> LessonProgress { get; set; } = new();
}
