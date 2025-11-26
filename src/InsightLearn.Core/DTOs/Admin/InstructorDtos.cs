using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Admin;

public class InstructorSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Active, Suspended, Pending
    public int CoursesCount { get; set; }
    public int StudentsCount { get; set; }
    public decimal TotalEarnings { get; set; }
    public DateTime JoinedDate { get; set; }
    public DateTime? LastActive { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class InstructorStatsDto
{
    public Guid InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public int TotalCourses { get; set; }
    public int PublishedCourses { get; set; }
    public int DraftCourses { get; set; }
    public int TotalEnrollments { get; set; }
    public int ActiveStudents { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public List<InstructorTopCourseDto> TopCourses { get; set; } = new();
    public Dictionary<string, int> EnrollmentsByMonth { get; set; } = new();
}

public class InstructorTopCourseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public int EnrollmentCount { get; set; }
    public decimal Revenue { get; set; }
    public decimal AverageRating { get; set; }
    public decimal CompletionRate { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class SuspendInstructorDto
{
    [Required(ErrorMessage = "Reason is required")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Reason must be between 10 and 500 characters")]
    public string Reason { get; set; } = string.Empty;

    public DateTime? SuspendUntil { get; set; }
    public string? Notes { get; set; }
}
