namespace InsightLearn.Core.DTOs.Enrollment;

/// <summary>
/// Student dashboard summary data
/// </summary>
public class StudentDashboardDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;

    public int TotalEnrollments { get; set; }
    public int ActiveEnrollments { get; set; }
    public int CompletedEnrollments { get; set; }

    public int TotalCertificates { get; set; }
    public int TotalMinutesLearned { get; set; }

    public List<EnrollmentDto> RecentEnrollments { get; set; } = new();
    public List<EnrollmentDto> ContinueLearning { get; set; } = new(); // Courses in progress

    public DateTime? LastActivityDate { get; set; }
    public int DaysStreak { get; set; } // Days of consecutive learning
}
