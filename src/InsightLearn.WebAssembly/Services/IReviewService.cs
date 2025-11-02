using InsightLearn.Application.DTOs;
using InsightLearn.WebAssembly.Models;

namespace InsightLearn.WebAssembly.Services;

public interface IReviewService
{
    Task<ApiResponse<List<ReviewDto>>> GetCourseReviewsAsync(Guid courseId);
    Task<ApiResponse<ReviewDto>> CreateReviewAsync(Guid courseId, ReviewDto review);
    Task<ApiResponse<ReviewDto>> UpdateReviewAsync(Guid reviewId, ReviewDto review);
    Task<ApiResponse> DeleteReviewAsync(Guid reviewId);
    Task<ApiResponse> VoteReviewAsync(Guid reviewId, bool isUpvote);
}
