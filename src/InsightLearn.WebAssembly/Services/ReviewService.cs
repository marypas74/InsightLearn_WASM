using InsightLearn.Shared.DTOs;
using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services;

public class ReviewService : IReviewService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;

    public ReviewService(IApiClient apiClient, EndpointsConfig endpoints)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
    }

    public async Task<ApiResponse<List<ReviewDto>>> GetCourseReviewsAsync(Guid courseId)
    {
        return await _apiClient.GetAsync<List<ReviewDto>>(string.Format(_endpoints.Reviews.GetByCourse, courseId));
    }

    public async Task<ApiResponse<ReviewDto>> CreateReviewAsync(Guid courseId, ReviewDto review)
    {
        return await _apiClient.PostAsync<ReviewDto>(string.Format("reviews/course/{0}", courseId), review);
    }

    public async Task<ApiResponse<ReviewDto>> UpdateReviewAsync(Guid reviewId, ReviewDto review)
    {
        return await _apiClient.PutAsync<ReviewDto>(string.Format(_endpoints.Reviews.GetById, reviewId), review);
    }

    public async Task<ApiResponse> DeleteReviewAsync(Guid reviewId)
    {
        return await _apiClient.DeleteAsync(string.Format(_endpoints.Reviews.GetById, reviewId));
    }

    public async Task<ApiResponse> VoteReviewAsync(Guid reviewId, bool isUpvote)
    {
        return await _apiClient.PostAsync(string.Format("reviews/{0}/vote", reviewId), new { IsUpvote = isUpvote });
    }
}
