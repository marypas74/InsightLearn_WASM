using InsightLearn.Application.DTOs;
using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;

    public EnrollmentService(IApiClient apiClient, EndpointsConfig endpoints)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
    }

    public async Task<ApiResponse<List<EnrollmentDto>>> GetUserEnrollmentsAsync()
    {
        return await _apiClient.GetAsync<List<EnrollmentDto>>("enrollments/my-enrollments");
    }

    public async Task<ApiResponse<EnrollmentDto>> EnrollInCourseAsync(Guid courseId)
    {
        return await _apiClient.PostAsync<EnrollmentDto>(string.Format("enrollments/enroll/{0}", courseId));
    }

    public async Task<ApiResponse> UnenrollFromCourseAsync(Guid courseId)
    {
        return await _apiClient.DeleteAsync(string.Format(_endpoints.Enrollments.GetById, courseId));
    }

    public async Task<ApiResponse<EnrollmentDto>> GetEnrollmentAsync(Guid courseId)
    {
        return await _apiClient.GetAsync<EnrollmentDto>(string.Format(_endpoints.Enrollments.GetById, courseId));
    }

    public async Task<ApiResponse> UpdateProgressAsync(Guid courseId, int progress)
    {
        return await _apiClient.PutAsync(string.Format("enrollments/{0}/progress", courseId), new { Progress = progress });
    }
}
