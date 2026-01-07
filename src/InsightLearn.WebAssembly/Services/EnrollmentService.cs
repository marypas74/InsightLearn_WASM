using InsightLearn.Shared.DTOs;
using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;
    private readonly ILogger<EnrollmentService> _logger;

    public EnrollmentService(IApiClient apiClient, EndpointsConfig endpoints, ILogger<EnrollmentService> logger)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
        _logger = logger;
    }

    public async Task<ApiResponse<List<EnrollmentDto>>> GetUserEnrollmentsAsync()
    {
        _logger.LogDebug("Fetching user enrollments");
        var response = await _apiClient.GetAsync<List<EnrollmentDto>>("enrollments/my-enrollments");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved {EnrollmentCount} enrollments for user", response.Data.Count);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve user enrollments: {ErrorMessage}",
                response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<EnrollmentDto>> EnrollInCourseAsync(Guid courseId)
    {
        _logger.LogInformation("Enrolling user in course: {CourseId}", courseId);
        var response = await _apiClient.PostAsync<EnrollmentDto>(string.Format("enrollments/enroll/{0}", courseId));

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("User enrolled successfully in course {CourseId}, EnrollmentId: {EnrollmentId}",
                courseId, response.Data.Id);
        }
        else
        {
            _logger.LogError("Failed to enroll user in course {CourseId}: {ErrorMessage}",
                courseId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse> UnenrollFromCourseAsync(Guid courseId)
    {
        _logger.LogWarning("Unenrolling user from course: {CourseId}", courseId);
        var response = await _apiClient.DeleteAsync(string.Format(_endpoints.Enrollments.GetById, courseId));

        if (response.Success)
        {
            _logger.LogInformation("User unenrolled successfully from course {CourseId}", courseId);
        }
        else
        {
            _logger.LogError("Failed to unenroll from course {CourseId}: {ErrorMessage}",
                courseId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<EnrollmentDto>> GetEnrollmentAsync(Guid courseId)
    {
        _logger.LogDebug("Fetching enrollment for course: {CourseId}", courseId);
        var response = await _apiClient.GetAsync<EnrollmentDto>(string.Format(_endpoints.Enrollments.GetById, courseId));

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Enrollment retrieved for course {CourseId}: EnrollmentId {EnrollmentId}",
                courseId, response.Data.Id);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve enrollment for course {CourseId}: {ErrorMessage}",
                courseId, response.Message ?? "Not found");
        }

        return response;
    }

    public async Task<ApiResponse> UpdateProgressAsync(Guid courseId, int progress)
    {
        _logger.LogInformation("Updating progress for course {CourseId}: {Progress}%", courseId, progress);
        var response = await _apiClient.PutAsync(string.Format("enrollments/{0}/progress", courseId), new { Progress = progress });

        if (response.Success)
        {
            _logger.LogDebug("Progress updated successfully for course {CourseId}", courseId);
        }
        else
        {
            _logger.LogWarning("Failed to update progress for course {CourseId}: {ErrorMessage}",
                courseId, response.Message ?? "Unknown error");
        }

        return response;
    }
}
