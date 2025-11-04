using InsightLearn.Shared.DTOs;
using InsightLearn.WebAssembly.Models;

namespace InsightLearn.WebAssembly.Services;

public interface IEnrollmentService
{
    Task<ApiResponse<List<EnrollmentDto>>> GetUserEnrollmentsAsync();
    Task<ApiResponse<EnrollmentDto>> EnrollInCourseAsync(Guid courseId);
    Task<ApiResponse> UnenrollFromCourseAsync(Guid courseId);
    Task<ApiResponse<EnrollmentDto>> GetEnrollmentAsync(Guid courseId);
    Task<ApiResponse> UpdateProgressAsync(Guid courseId, int progress);
}
