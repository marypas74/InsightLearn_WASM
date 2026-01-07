using InsightLearn.Shared.DTOs;
using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services;

public class ReviewService : IReviewService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(IApiClient apiClient, EndpointsConfig endpoints, ILogger<ReviewService> logger)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ReviewDto>>> GetCourseReviewsAsync(Guid courseId)
    {
        _logger.LogDebug("Fetching reviews for course: {CourseId}", courseId);
        var response = await _apiClient.GetAsync<List<ReviewDto>>(string.Format(_endpoints.Reviews.GetByCourse, courseId));

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved {ReviewCount} reviews for course {CourseId}",
                response.Data.Count, courseId);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve reviews for course {CourseId}: {ErrorMessage}",
                courseId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<ReviewDto>> CreateReviewAsync(Guid courseId, ReviewDto review)
    {
        _logger.LogInformation("Creating review for course {CourseId} with rating {Rating}",
            courseId, review.Rating);
        var response = await _apiClient.PostAsync<ReviewDto>(string.Format("reviews/course/{0}", courseId), review);

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Review created successfully for course {CourseId} (ReviewId: {ReviewId})",
                courseId, response.Data.Id);
        }
        else
        {
            _logger.LogError("Failed to create review for course {CourseId}: {ErrorMessage}",
                courseId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<ReviewDto>> UpdateReviewAsync(Guid reviewId, ReviewDto review)
    {
        _logger.LogInformation("Updating review {ReviewId} with new rating {Rating}",
            reviewId, review.Rating);
        var response = await _apiClient.PutAsync<ReviewDto>(string.Format(_endpoints.Reviews.GetById, reviewId), review);

        if (response.Success)
        {
            _logger.LogInformation("Review updated successfully: {ReviewId}", reviewId);
        }
        else
        {
            _logger.LogError("Failed to update review {ReviewId}: {ErrorMessage}",
                reviewId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse> DeleteReviewAsync(Guid reviewId)
    {
        _logger.LogWarning("Deleting review: {ReviewId}", reviewId);
        var response = await _apiClient.DeleteAsync(string.Format(_endpoints.Reviews.GetById, reviewId));

        if (response.Success)
        {
            _logger.LogInformation("Review deleted successfully: {ReviewId}", reviewId);
        }
        else
        {
            _logger.LogError("Failed to delete review {ReviewId}: {ErrorMessage}",
                reviewId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse> VoteReviewAsync(Guid reviewId, bool isUpvote)
    {
        _logger.LogInformation("Voting on review {ReviewId}: {VoteType}",
            reviewId, isUpvote ? "upvote" : "downvote");
        var response = await _apiClient.PostAsync(string.Format("reviews/{0}/vote", reviewId), new { IsUpvote = isUpvote });

        if (response.Success)
        {
            _logger.LogInformation("Vote recorded successfully for review {ReviewId}", reviewId);
        }
        else
        {
            _logger.LogWarning("Failed to vote on review {ReviewId}: {ErrorMessage}",
                reviewId, response.Message ?? "Unknown error");
        }

        return response;
    }
}
