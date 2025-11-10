using InsightLearn.Core.DTOs.Review;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service interface for course reviews and ratings management
/// </summary>
public interface IReviewService
{
    // Query methods

    /// <summary>
    /// Gets paginated reviews for a specific course
    /// </summary>
    Task<ReviewListDto> GetCourseReviewsAsync(Guid courseId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Gets a review by its unique identifier
    /// </summary>
    Task<ReviewDto?> GetReviewByIdAsync(Guid id);

    /// <summary>
    /// Gets a specific user's review for a course (if exists)
    /// </summary>
    Task<ReviewDto?> GetUserReviewForCourseAsync(Guid userId, Guid courseId);

    /// <summary>
    /// Gets statistical information about reviews for a course
    /// </summary>
    Task<ReviewStatisticsDto> GetReviewStatisticsAsync(Guid courseId);

    // Command methods

    /// <summary>
    /// Creates a new review for a course
    /// </summary>
    Task<ReviewDto> CreateReviewAsync(CreateReviewDto dto);

    /// <summary>
    /// Updates an existing review
    /// </summary>
    Task<ReviewDto?> UpdateReviewAsync(Guid id, UpdateReviewDto dto);

    /// <summary>
    /// Deletes a review
    /// </summary>
    Task<bool> DeleteReviewAsync(Guid id);

    /// <summary>
    /// Marks a review as helpful by a user
    /// </summary>
    Task<bool> MarkReviewHelpfulAsync(Guid reviewId, Guid userId);
}