using InsightLearn.Shared.DTOs;
using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;
using Microsoft.Extensions.Logging;
using System.Text;

namespace InsightLearn.WebAssembly.Services;

public class CourseService : ICourseService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;
    private readonly ILogger<CourseService> _logger;

    public CourseService(IApiClient apiClient, EndpointsConfig endpoints, ILogger<CourseService> logger)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CourseDto>>> GetAllCoursesAsync()
    {
        _logger.LogDebug("Fetching all courses");
        var response = await _apiClient.GetAsync<List<CourseDto>>(_endpoints.Courses.GetAll);

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved {CourseCount} courses", response.Data.Count);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve courses: {ErrorMessage}", response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<CourseDto>> GetCourseByIdAsync(Guid id)
    {
        _logger.LogInformation("Fetching course: {CourseId}", id);
        var response = await _apiClient.GetAsync<CourseDto>(string.Format(_endpoints.Courses.GetById, id));

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Course retrieved: {CourseTitle} (ID: {CourseId})",
                response.Data.Title, id);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve course {CourseId}: {ErrorMessage}",
                id, response.Message ?? "Not found");
        }

        return response;
    }

    public async Task<ApiResponse<List<CourseDto>>> GetCoursesByCategoryAsync(Guid categoryId)
    {
        _logger.LogDebug("Fetching courses for category: {CategoryId}", categoryId);
        var response = await _apiClient.GetAsync<List<CourseDto>>(string.Format(_endpoints.Courses.GetByCategory, categoryId));

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved {CourseCount} courses for category {CategoryId}",
                response.Data.Count, categoryId);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve courses for category {CategoryId}: {ErrorMessage}",
                categoryId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<List<CourseDto>>> SearchCoursesAsync(string searchTerm)
    {
        _logger.LogInformation("Searching courses with term: {SearchTerm}", searchTerm);
        var response = await _apiClient.GetAsync<List<CourseDto>>($"{_endpoints.Courses.Search}?q={Uri.EscapeDataString(searchTerm)}");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Search returned {ResultCount} courses for term: {SearchTerm}",
                response.Data.Count, searchTerm);
        }

        return response;
    }

    public async Task<ApiResponse<CourseDto>> CreateCourseAsync(CourseDto course)
    {
        _logger.LogInformation("Creating course: {CourseTitle}", course.Title);
        var response = await _apiClient.PostAsync<CourseDto>(_endpoints.Courses.Create, course);

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Course created successfully: {CourseTitle} (ID: {CourseId})",
                response.Data.Title, response.Data.Id);
        }
        else
        {
            _logger.LogError("Failed to create course {CourseTitle}: {ErrorMessage}",
                course.Title, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<CourseDto>> UpdateCourseAsync(Guid id, CourseDto course)
    {
        _logger.LogInformation("Updating course: {CourseId} - {CourseTitle}", id, course.Title);
        var response = await _apiClient.PutAsync<CourseDto>(string.Format(_endpoints.Courses.Update, id), course);

        if (response.Success)
        {
            _logger.LogInformation("Course updated successfully: {CourseId}", id);
        }
        else
        {
            _logger.LogError("Failed to update course {CourseId}: {ErrorMessage}",
                id, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse> DeleteCourseAsync(Guid id)
    {
        _logger.LogWarning("Deleting course: {CourseId}", id);
        var response = await _apiClient.DeleteAsync(string.Format(_endpoints.Courses.Delete, id));

        if (response.Success)
        {
            _logger.LogInformation("Course deleted successfully: {CourseId}", id);
        }
        else
        {
            _logger.LogError("Failed to delete course {CourseId}: {ErrorMessage}",
                id, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<CourseSearchResultDto>> SearchCoursesAsync(CourseSearchDto searchDto)
    {
        _logger.LogInformation("Advanced search: Query={Query}, CategoryId={CategoryId}, Level={Level}, Page={Page}",
            searchDto.Query ?? "(all)", searchDto.CategoryId, searchDto.Level, searchDto.Page);

        var queryString = BuildSearchQueryString(searchDto);
        var endpoint = $"{_endpoints.Courses.Search}?{queryString}";
        var response = await _apiClient.GetAsync<CourseSearchResultDto>(endpoint);

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Advanced search returned {TotalResults} total results ({PageResults} on page {Page})",
                response.Data.TotalCount, response.Data.Courses?.Count ?? 0, searchDto.Page);
        }

        return response;
    }

    public async Task<ApiResponse<bool>> IsEnrolledAsync(Guid courseId)
    {
        _logger.LogDebug("Checking enrollment status for course: {CourseId}", courseId);
        var response = await _apiClient.GetAsync<bool>($"api/courses/{courseId}/enrolled");

        if (response.Success)
        {
            _logger.LogDebug("Enrollment check for {CourseId}: {IsEnrolled}", courseId, response.Data);
        }

        return response;
    }

    private string BuildSearchQueryString(CourseSearchDto searchDto)
    {
        var query = new StringBuilder();
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(searchDto.Query))
            parameters.Add($"query={Uri.EscapeDataString(searchDto.Query)}");

        if (searchDto.CategoryId.HasValue)
            parameters.Add($"categoryId={searchDto.CategoryId.Value}");

        if (searchDto.Level.HasValue)
            parameters.Add($"level={searchDto.Level.Value}");

        if (searchDto.MinPrice.HasValue)
            parameters.Add($"minPrice={searchDto.MinPrice.Value}");

        if (searchDto.MaxPrice.HasValue)
            parameters.Add($"maxPrice={searchDto.MaxPrice.Value}");

        if (searchDto.HasCertificate.HasValue)
            parameters.Add($"hasCertificate={searchDto.HasCertificate.Value}");

        if (!string.IsNullOrWhiteSpace(searchDto.Language))
            parameters.Add($"language={Uri.EscapeDataString(searchDto.Language)}");

        if (searchDto.MinRating.HasValue)
            parameters.Add($"minRating={searchDto.MinRating.Value}");

        if (!string.IsNullOrWhiteSpace(searchDto.SortBy))
            parameters.Add($"sortBy={Uri.EscapeDataString(searchDto.SortBy)}");

        parameters.Add($"page={searchDto.Page}");
        parameters.Add($"pageSize={searchDto.PageSize}");

        return string.Join("&", parameters);
    }
}
