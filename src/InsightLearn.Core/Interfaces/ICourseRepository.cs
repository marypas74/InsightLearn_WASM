using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Repository interface for Course entity operations
/// </summary>
public interface ICourseRepository
{
    /// <summary>
    /// Gets all courses with pagination
    /// </summary>
    Task<IEnumerable<Course>> GetAllAsync(int page = 1, int pageSize = 10);

    /// <summary>
    /// Gets a course by its unique identifier
    /// </summary>
    Task<Course?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a course by its URL slug
    /// </summary>
    Task<Course?> GetBySlugAsync(string slug);

    /// <summary>
    /// Gets courses by category with pagination
    /// </summary>
    Task<IEnumerable<Course>> GetByCategoryIdAsync(Guid categoryId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Gets courses by instructor
    /// </summary>
    Task<IEnumerable<Course>> GetByInstructorIdAsync(Guid instructorId);

    /// <summary>
    /// Gets only published courses with pagination
    /// </summary>
    Task<IEnumerable<Course>> GetPublishedCoursesAsync(int page = 1, int pageSize = 10);

    /// <summary>
    /// Searches courses by title, description, or keywords
    /// </summary>
    Task<IEnumerable<Course>> SearchAsync(string query, int page = 1, int pageSize = 10);

    /// <summary>
    /// Creates a new course
    /// </summary>
    Task<Course> CreateAsync(Course course);

    /// <summary>
    /// Updates an existing course
    /// </summary>
    Task<Course> UpdateAsync(Course course);

    /// <summary>
    /// Deletes a course by ID
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Updates course statistics (views, ratings, enrollments)
    /// </summary>
    Task UpdateStatisticsAsync(Guid id, int viewCount, double averageRating, int reviewCount, int enrollmentCount);

    /// <summary>
    /// Gets total count of courses (for pagination)
    /// </summary>
    Task<int> GetTotalCountAsync();

    /// <summary>
    /// Gets total count of published courses
    /// </summary>
    Task<int> GetPublishedCountAsync();

    /// <summary>
    /// Gets count of courses by category (for pagination)
    /// </summary>
    Task<int> GetByCategoryCountAsync(Guid categoryId);

    /// <summary>
    /// Gets count of courses by instructor (for pagination)
    /// </summary>
    Task<int> GetByInstructorCountAsync(Guid instructorId);

    /// <summary>
    /// Gets count of search results (for pagination)
    /// </summary>
    Task<int> SearchCountAsync(string query);
}
