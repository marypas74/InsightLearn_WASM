namespace InsightLearn.Core.DTOs.Engagement;

/// <summary>
/// Top engaged courses leaderboard item
/// </summary>
public class TopEngagedCoursesDto
{
    public Guid CourseId { get; set; }

    public string CourseName { get; set; } = string.Empty;

    public Guid InstructorId { get; set; }

    public string InstructorName { get; set; } = string.Empty;

    public int TotalTimeSeconds { get; set; }

    public int UniqueUsers { get; set; }

    public int Rank { get; set; }

    public decimal ShareOfTotalEngagement { get; set; }
}
