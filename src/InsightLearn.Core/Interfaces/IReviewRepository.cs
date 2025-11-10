using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Repository interface for Review entity operations
/// </summary>
public interface IReviewRepository
{
    /// <summary>
    /// Gets all reviews with pagination
    /// </summary>
    Task<IEnumerable<Review>> GetAllAsync(int page = 1, int pageSize = 10);

    /// <summary>
    /// Gets a review by its unique identifier
    /// </summary>
    Task<Review?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all reviews for a specific course with pagination
    /// </summary>
    Task<IEnumerable<Review>> GetByCourseIdAsync(Guid courseId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Gets all reviews by a specific user
    /// </summary>
    Task<IEnumerable<Review>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Gets a user's review for a specific course
    /// </summary>
    Task<Review?> GetUserReviewForCourseAsync(Guid userId, Guid courseId);

    /// <summary>
    /// Creates a new review
    /// </summary>
    Task<Review> CreateAsync(Review review);

    /// <summary>
    /// Updates an existing review
    /// </summary>
    Task<Review> UpdateAsync(Review review);

    /// <summary>
    /// Deletes a review by ID
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Gets average rating for a course
    /// </summary>
    Task<double> GetAverageRatingAsync(Guid courseId);

    /// <summary>
    /// Gets review count for a course
    /// </summary>
    Task<int> GetReviewCountAsync(Guid courseId);
}
