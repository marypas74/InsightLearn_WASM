using InsightLearn.Core.DTOs.Enrollment;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service interface for enrollment management and student progress tracking
/// </summary>
public interface IEnrollmentService
{
    // Query methods

    /// <summary>
    /// Gets enrollment details by ID
    /// </summary>
    Task<EnrollmentDto?> GetEnrollmentByIdAsync(Guid id);

    /// <summary>
    /// Gets all enrollments for a specific user
    /// </summary>
    Task<List<EnrollmentDto>> GetUserEnrollmentsAsync(Guid userId);

    /// <summary>
    /// Gets all enrollments for a specific course
    /// </summary>
    Task<List<EnrollmentDto>> GetCourseEnrollmentsAsync(Guid courseId);

    /// <summary>
    /// Checks if a user is currently enrolled in a course
    /// </summary>
    Task<bool> IsUserEnrolledAsync(Guid userId, Guid courseId);

    /// <summary>
    /// Gets comprehensive student dashboard data
    /// </summary>
    Task<StudentDashboardDto> GetStudentDashboardAsync(Guid userId);

    // Command methods

    /// <summary>
    /// Enrolls a user in a course
    /// </summary>
    Task<EnrollmentDto> EnrollUserAsync(CreateEnrollmentDto dto);

    /// <summary>
    /// Updates lesson progress for an enrollment
    /// </summary>
    Task<bool> UpdateProgressAsync(UpdateProgressDto dto);

    /// <summary>
    /// Marks an enrollment as completed and issues certificate
    /// </summary>
    Task<bool> CompleteEnrollmentAsync(Guid enrollmentId);

    /// <summary>
    /// Cancels an enrollment (if eligible)
    /// </summary>
    Task<bool> CancelEnrollmentAsync(Guid enrollmentId);

    // Progress tracking

    /// <summary>
    /// Gets detailed progress information for an enrollment
    /// </summary>
    Task<EnrollmentProgressDto> GetEnrollmentProgressAsync(Guid enrollmentId);

    /// <summary>
    /// Gets progress information for a specific lesson
    /// </summary>
    Task<LessonProgressDto?> GetLessonProgressAsync(Guid enrollmentId, Guid lessonId);
}