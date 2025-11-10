using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Enrollment;

/// <summary>
/// DTO for updating lesson progress
/// </summary>
public class UpdateProgressDto
{
    [Required]
    public Guid EnrollmentId { get; set; }

    [Required]
    public Guid LessonId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Watched minutes must be a positive number")]
    public int WatchedMinutes { get; set; }

    public bool IsCompleted { get; set; }

    public Guid? NextLessonId { get; set; }
}
