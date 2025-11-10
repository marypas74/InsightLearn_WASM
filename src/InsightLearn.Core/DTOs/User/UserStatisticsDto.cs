namespace InsightLearn.Core.DTOs.User;

public class UserStatisticsDto
{
    public Guid UserId { get; set; }

    // Enrollment Statistics
    public int TotalEnrollments { get; set; }
    public int CompletedCourses { get; set; }
    public int InProgressCourses { get; set; }
    public int TotalCertificates { get; set; }

    // Learning Statistics
    public int TotalMinutesLearned { get; set; }
    public TimeSpan TotalTimeLearned => TimeSpan.FromMinutes(TotalMinutesLearned);
    public string FormattedTimeLearned => FormatTimeSpan(TotalTimeLearned);

    // Financial Statistics
    public decimal TotalSpent { get; set; }
    public decimal AverageCoursePrice => TotalEnrollments > 0 ? TotalSpent / TotalEnrollments : 0;

    // Review Statistics
    public double AverageRatingGiven { get; set; }
    public int TotalReviewsWritten { get; set; }

    // Activity
    public DateTime? LastActivityDate { get; set; }
    public int DaysSinceLastActivity
    {
        get
        {
            if (!LastActivityDate.HasValue) return int.MaxValue;
            return (DateTime.UtcNow - LastActivityDate.Value).Days;
        }
    }

    // Instructor Statistics (if applicable)
    public int? CoursesCreated { get; set; }
    public int? TotalStudents { get; set; }
    public decimal? TotalEarnings { get; set; }
    public double? AverageInstructorRating { get; set; }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours < 1)
            return $"{timeSpan.Minutes}m";
        if (timeSpan.TotalDays < 1)
            return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
        return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h";
    }
}
