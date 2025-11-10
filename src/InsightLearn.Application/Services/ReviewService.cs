using InsightLearn.Core.DTOs.Review;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service implementation for course reviews and ratings management
/// </summary>
public class ReviewService : IReviewService
{
    private readonly IReviewRepository _reviewRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(
        IReviewRepository reviewRepository,
        IEnrollmentRepository enrollmentRepository,
        ICourseRepository courseRepository,
        ILogger<ReviewService> logger)
    {
        _reviewRepository = reviewRepository;
        _enrollmentRepository = enrollmentRepository;
        _courseRepository = courseRepository;
        _logger = logger;
    }

    #region Query Methods

    public async Task<ReviewListDto> GetCourseReviewsAsync(Guid courseId, int page = 1, int pageSize = 10)
    {
        _logger.LogInformation("[ReviewService] Getting reviews for course {CourseId}, page {Page}",
            courseId, page);

        try
        {
            var reviews = await _reviewRepository.GetByCourseIdAsync(courseId, page, pageSize);
            var totalCount = await _reviewRepository.GetReviewCountAsync(courseId);
            var averageRating = await _reviewRepository.GetAverageRatingAsync(courseId);

            var reviewDtos = reviews.Select(MapToDto).ToList();

            // Get statistics if there are reviews
            ReviewStatisticsDto? statistics = null;
            if (totalCount > 0)
            {
                statistics = await GetReviewStatisticsAsync(courseId);
            }

            return new ReviewListDto
            {
                Reviews = reviewDtos,
                TotalCount = totalCount,
                AverageRating = averageRating,
                Statistics = statistics,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReviewService] Error getting reviews for course {CourseId}", courseId);
            throw;
        }
    }

    public async Task<ReviewDto?> GetReviewByIdAsync(Guid id)
    {
        _logger.LogInformation("[ReviewService] Getting review {ReviewId}", id);

        var review = await _reviewRepository.GetByIdAsync(id);
        if (review == null)
        {
            _logger.LogWarning("[ReviewService] Review {ReviewId} not found", id);
            return null;
        }

        return MapToDto(review);
    }

    public async Task<ReviewDto?> GetUserReviewForCourseAsync(Guid userId, Guid courseId)
    {
        _logger.LogInformation("[ReviewService] Getting review for user {UserId} on course {CourseId}",
            userId, courseId);

        var review = await _reviewRepository.GetUserReviewForCourseAsync(userId, courseId);
        if (review == null)
        {
            _logger.LogInformation("[ReviewService] No review found for user {UserId} on course {CourseId}",
                userId, courseId);
            return null;
        }

        return MapToDto(review);
    }

    public async Task<ReviewStatisticsDto> GetReviewStatisticsAsync(Guid courseId)
    {
        _logger.LogInformation("[ReviewService] Calculating review statistics for course {CourseId}", courseId);

        try
        {
            var allReviews = await _reviewRepository.GetByCourseIdAsync(courseId, 1, int.MaxValue);
            var reviewsList = allReviews.ToList();

            if (!reviewsList.Any())
            {
                return new ReviewStatisticsDto
                {
                    TotalReviews = 0,
                    AverageRating = 0,
                    FiveStarCount = 0,
                    FourStarCount = 0,
                    ThreeStarCount = 0,
                    TwoStarCount = 0,
                    OneStarCount = 0,
                    FiveStarPercentage = 0,
                    FourStarPercentage = 0,
                    ThreeStarPercentage = 0,
                    TwoStarPercentage = 0,
                    OneStarPercentage = 0
                };
            }

            var totalCount = reviewsList.Count;
            var averageRating = reviewsList.Average(r => r.Rating);

            var fiveStarCount = reviewsList.Count(r => r.Rating == 5);
            var fourStarCount = reviewsList.Count(r => r.Rating == 4);
            var threeStarCount = reviewsList.Count(r => r.Rating == 3);
            var twoStarCount = reviewsList.Count(r => r.Rating == 2);
            var oneStarCount = reviewsList.Count(r => r.Rating == 1);

            return new ReviewStatisticsDto
            {
                TotalReviews = totalCount,
                AverageRating = Math.Round(averageRating, 1),
                FiveStarCount = fiveStarCount,
                FourStarCount = fourStarCount,
                ThreeStarCount = threeStarCount,
                TwoStarCount = twoStarCount,
                OneStarCount = oneStarCount,
                FiveStarPercentage = Math.Round((double)fiveStarCount / totalCount * 100, 1),
                FourStarPercentage = Math.Round((double)fourStarCount / totalCount * 100, 1),
                ThreeStarPercentage = Math.Round((double)threeStarCount / totalCount * 100, 1),
                TwoStarPercentage = Math.Round((double)twoStarCount / totalCount * 100, 1),
                OneStarPercentage = Math.Round((double)oneStarCount / totalCount * 100, 1)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReviewService] Error calculating statistics for course {CourseId}", courseId);
            throw;
        }
    }

    #endregion

    #region Command Methods

    public async Task<ReviewDto> CreateReviewAsync(CreateReviewDto dto)
    {
        _logger.LogInformation("[ReviewService] User {UserId} creating review for course {CourseId}",
            dto.UserId, dto.CourseId);

        try
        {
            // Validate rating
            if (dto.Rating < 1 || dto.Rating > 5)
            {
                throw new ArgumentException("Rating must be between 1 and 5");
            }

            // Verify course exists
            var course = await _courseRepository.GetByIdAsync(dto.CourseId);
            if (course == null)
            {
                throw new ArgumentException($"Course {dto.CourseId} not found");
            }

            // Check if user is the instructor (cannot review own course)
            if (course.InstructorId == dto.UserId)
            {
                _logger.LogWarning("[ReviewService] Instructor {UserId} cannot review their own course {CourseId}",
                    dto.UserId, dto.CourseId);
                throw new InvalidOperationException("Cannot review your own course");
            }

            // Verify user is enrolled in the course
            var isEnrolled = await _enrollmentRepository.IsUserEnrolledAsync(dto.UserId, dto.CourseId);
            if (!isEnrolled)
            {
                _logger.LogWarning("[ReviewService] User {UserId} not enrolled in course {CourseId}",
                    dto.UserId, dto.CourseId);
                throw new InvalidOperationException("You must be enrolled in the course to leave a review");
            }

            // Check if user has already reviewed this course
            var existingReview = await _reviewRepository.GetUserReviewForCourseAsync(dto.UserId, dto.CourseId);
            if (existingReview != null)
            {
                _logger.LogWarning("[ReviewService] User {UserId} has already reviewed course {CourseId}",
                    dto.UserId, dto.CourseId);
                throw new InvalidOperationException("You have already reviewed this course");
            }

            // Create new review
            var review = new Review
            {
                UserId = dto.UserId,
                CourseId = dto.CourseId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow,
                IsApproved = true, // Auto-approve for enrolled users
                HelpfulVotes = 0,
                UnhelpfulVotes = 0
            };

            var createdReview = await _reviewRepository.CreateAsync(review);

            // Update course rating statistics
            await UpdateCourseRating(dto.CourseId);

            _logger.LogInformation("[ReviewService] Review created successfully for course {CourseId} by user {UserId}",
                dto.CourseId, dto.UserId);

            // Map to DTO with verified purchase flag
            var reviewDto = MapToDto(createdReview);
            reviewDto.IsVerifiedPurchase = isEnrolled;

            return reviewDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReviewService] Error creating review for course {CourseId} by user {UserId}",
                dto.CourseId, dto.UserId);
            throw;
        }
    }

    public async Task<ReviewDto?> UpdateReviewAsync(Guid id, UpdateReviewDto dto)
    {
        _logger.LogInformation("[ReviewService] Updating review {ReviewId}", id);

        try
        {
            // Validate rating if provided
            if (dto.Rating.HasValue && (dto.Rating.Value < 1 || dto.Rating.Value > 5))
            {
                throw new ArgumentException("Rating must be between 1 and 5");
            }

            var review = await _reviewRepository.GetByIdAsync(id);
            if (review == null)
            {
                _logger.LogWarning("[ReviewService] Review {ReviewId} not found", id);
                return null;
            }

            // Only the review author can update their review
            if (review.UserId != dto.Id)
            {
                _logger.LogWarning("[ReviewService] User {UserId} not authorized to update review {ReviewId}",
                    dto.Id, id);
                throw new UnauthorizedAccessException("You can only update your own reviews");
            }

            // Update review properties
            bool ratingChanged = false;
            if (dto.Rating.HasValue && dto.Rating.Value != review.Rating)
            {
                review.Rating = dto.Rating.Value;
                ratingChanged = true;
            }

            if (!string.IsNullOrWhiteSpace(dto.Comment))
            {
                review.Comment = dto.Comment;
            }

            review.UpdatedAt = DateTime.UtcNow;

            var updatedReview = await _reviewRepository.UpdateAsync(review);

            // Recalculate course rating if rating changed
            if (ratingChanged)
            {
                await UpdateCourseRating(review.CourseId);
            }

            _logger.LogInformation("[ReviewService] Review {ReviewId} updated successfully", id);

            // Check if user is enrolled for verified purchase flag
            var isEnrolled = await _enrollmentRepository.IsUserEnrolledAsync(review.UserId, review.CourseId);
            var reviewDto = MapToDto(updatedReview);
            reviewDto.IsVerifiedPurchase = isEnrolled;

            return reviewDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReviewService] Error updating review {ReviewId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteReviewAsync(Guid id)
    {
        _logger.LogInformation("[ReviewService] Deleting review {ReviewId}", id);

        try
        {
            var review = await _reviewRepository.GetByIdAsync(id);
            if (review == null)
            {
                _logger.LogWarning("[ReviewService] Review {ReviewId} not found", id);
                return false;
            }

            var courseId = review.CourseId;

            await _reviewRepository.DeleteAsync(id);

            // Recalculate course rating after deletion
            await UpdateCourseRating(courseId);

            _logger.LogInformation("[ReviewService] Review {ReviewId} deleted successfully", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReviewService] Error deleting review {ReviewId}", id);
            return false;
        }
    }

    public async Task<bool> MarkReviewHelpfulAsync(Guid reviewId, Guid userId)
    {
        _logger.LogInformation("[ReviewService] User {UserId} marking review {ReviewId} as helpful",
            userId, reviewId);

        try
        {
            var review = await _reviewRepository.GetByIdAsync(reviewId);
            if (review == null)
            {
                _logger.LogWarning("[ReviewService] Review {ReviewId} not found", reviewId);
                return false;
            }

            // Check if user is trying to vote on their own review
            if (review.UserId == userId)
            {
                _logger.LogWarning("[ReviewService] User {UserId} cannot vote on their own review", userId);
                return false;
            }

            // For now, we'll just increment the helpful votes count
            // In a complete implementation, you'd track which users have voted
            // to prevent duplicate votes
            review.HelpfulVotes++;
            await _reviewRepository.UpdateAsync(review);

            _logger.LogInformation("[ReviewService] Review {ReviewId} marked as helpful by user {UserId}",
                reviewId, userId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReviewService] Error marking review {ReviewId} as helpful", reviewId);
            return false;
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task UpdateCourseRating(Guid courseId)
    {
        _logger.LogInformation("[ReviewService] Updating rating for course {CourseId}", courseId);

        try
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null)
            {
                _logger.LogWarning("[ReviewService] Course {CourseId} not found for rating update", courseId);
                return;
            }

            var averageRating = await _reviewRepository.GetAverageRatingAsync(courseId);
            var reviewCount = await _reviewRepository.GetReviewCountAsync(courseId);

            await _courseRepository.UpdateStatisticsAsync(
                courseId,
                course.ViewCount,
                averageRating,
                reviewCount,
                course.EnrollmentCount);

            _logger.LogInformation("[ReviewService] Course {CourseId} rating updated: {AverageRating} ({ReviewCount} reviews)",
                courseId, averageRating, reviewCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ReviewService] Error updating course {CourseId} rating", courseId);
            // Don't throw - rating update failure shouldn't fail the main operation
        }
    }

    private ReviewDto MapToDto(Review review)
    {
        return new ReviewDto
        {
            Id = review.Id,
            UserId = review.UserId,
            UserName = review.User?.FullName ?? string.Empty,
            UserProfilePictureUrl = review.User?.ProfilePictureUrl,
            CourseId = review.CourseId,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt,
            HelpfulCount = review.HelpfulVotes,
            IsVerifiedPurchase = false // Will be set by calling method based on enrollment check
        };
    }

    #endregion
}