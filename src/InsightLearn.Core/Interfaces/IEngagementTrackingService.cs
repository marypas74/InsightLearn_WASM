using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

public interface IEngagementTrackingService
{
    /// <summary>
    /// Records a new engagement event with anti-fraud validation
    /// </summary>
    Task<CourseEngagement?> RecordEngagementAsync(
        Guid userId,
        Guid courseId,
        string engagementType,
        int durationMinutes,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceFingerprint = null,
        Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Calculates validation score for an engagement based on multiple factors
    /// Score range: 0.0 (definitely fake) to 1.0 (definitely real)
    /// </summary>
    Task<decimal> CalculateValidationScoreAsync(
        Guid userId,
        Guid courseId,
        int durationMinutes,
        string? deviceFingerprint = null,
        Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Validates pending engagements in batch (for background processing)
    /// </summary>
    Task<int> ValidatePendingEngagementsAsync(DateTime? since = null, int batchSize = 100);

    /// <summary>
    /// Get engagement analytics for a specific user
    /// </summary>
    Task<UserEngagementAnalytics> GetUserEngagementAnalyticsAsync(Guid userId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get engagement analytics for a specific course
    /// </summary>
    Task<CourseEngagementAnalytics> GetCourseEngagementAnalyticsAsync(Guid courseId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get engagement breakdown by instructor for payout calculation
    /// </summary>
    Task<InstructorEngagementBreakdown> GetInstructorEngagementBreakdownAsync(Guid instructorId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Mark an engagement as fraudulent (admin override)
    /// </summary>
    Task<bool> MarkEngagementAsFraudulentAsync(Guid engagementId, string reason);

    /// <summary>
    /// Recalculate validation score for an existing engagement
    /// </summary>
    Task<bool> RecalculateValidationScoreAsync(Guid engagementId);
}

/// <summary>
/// User engagement analytics DTO
/// </summary>
public class UserEngagementAnalytics
{
    public Guid UserId { get; set; }
    public long TotalEngagementMinutes { get; set; }
    public long ValidatedEngagementMinutes { get; set; }
    public int TotalEngagements { get; set; }
    public int ValidatedEngagements { get; set; }
    public decimal AverageValidationScore { get; set; }
    public int UniqueCoursesEngaged { get; set; }
    public Dictionary<string, long> EngagementByType { get; set; } = new();
}

/// <summary>
/// Course engagement analytics DTO
/// </summary>
public class CourseEngagementAnalytics
{
    public Guid CourseId { get; set; }
    public long TotalEngagementMinutes { get; set; }
    public long ValidatedEngagementMinutes { get; set; }
    public int TotalEngagements { get; set; }
    public int ValidatedEngagements { get; set; }
    public int UniqueUsers { get; set; }
    public decimal AverageValidationScore { get; set; }
    public Dictionary<string, long> EngagementByType { get; set; } = new();
}

/// <summary>
/// Instructor engagement breakdown for payout calculation
/// </summary>
public class InstructorEngagementBreakdown
{
    public Guid InstructorId { get; set; }
    public long TotalValidatedEngagementMinutes { get; set; }
    public int UniqueStudents { get; set; }
    public int TotalEngagements { get; set; }
    public Dictionary<Guid, CourseEngagementDetail> CourseBreakdown { get; set; } = new();
}

/// <summary>
/// Engagement detail for a specific course
/// </summary>
public class CourseEngagementDetail
{
    public Guid CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public long ValidatedEngagementMinutes { get; set; }
    public int UniqueStudents { get; set; }
    public int TotalEngagements { get; set; }
}
