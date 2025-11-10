using InsightLearn.Core.DTOs.Enrollment;

namespace InsightLearn.Core.DTOs.User;

public class UserProfileDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public DateTime DateJoined { get; set; }
    public bool IsInstructor { get; set; }
    public UserStatisticsDto? Statistics { get; set; }
    public List<EnrollmentDto> RecentEnrollments { get; set; } = new();
}
