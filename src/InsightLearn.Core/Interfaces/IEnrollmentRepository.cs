using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Repository interface for Enrollment entity operations
/// </summary>
public interface IEnrollmentRepository
{
    /// <summary>
    /// Gets all enrollments with pagination
    /// </summary>
    Task<IEnumerable<Enrollment>> GetAllAsync(int page = 1, int pageSize = 10);

    /// <summary>
    /// Gets an enrollment by its unique identifier
    /// </summary>
    Task<Enrollment?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all enrollments for a specific user
    /// </summary>
    Task<IEnumerable<Enrollment>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Gets all enrollments for a specific course
    /// </summary>
    Task<IEnumerable<Enrollment>> GetByCourseIdAsync(Guid courseId);

    /// <summary>
    /// Gets active enrollment for a user in a specific course
    /// </summary>
    Task<Enrollment?> GetActiveEnrollmentAsync(Guid userId, Guid courseId);

    /// <summary>
    /// Gets all active enrollments for a user
    /// </summary>
    Task<IEnumerable<Enrollment>> GetActiveEnrollmentsAsync(Guid userId);

    /// <summary>
    /// Gets completed enrollments for a user
    /// </summary>
    Task<IEnumerable<Enrollment>> GetCompletedEnrollmentsAsync(Guid userId);

    /// <summary>
    /// Creates a new enrollment
    /// </summary>
    Task<Enrollment> CreateAsync(Enrollment enrollment);

    /// <summary>
    /// Updates an existing enrollment
    /// </summary>
    Task<Enrollment> UpdateAsync(Enrollment enrollment);

    /// <summary>
    /// Updates enrollment progress
    /// </summary>
    Task UpdateProgressAsync(Guid enrollmentId, int completedLessons, int totalWatchedMinutes);

    /// <summary>
    /// Marks an enrollment as completed
    /// </summary>
    Task CompleteAsync(Guid enrollmentId);

    /// <summary>
    /// Gets total count of enrollments
    /// </summary>
    Task<int> GetTotalCountAsync();

    /// <summary>
    /// Checks if a user is enrolled in a course
    /// </summary>
    Task<bool> IsUserEnrolledAsync(Guid userId, Guid courseId);
}
