using InsightLearn.Shared.DTOs;
using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;
using System.Text;

namespace InsightLearn.WebAssembly.Services;

public class CourseService : ICourseService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;

    public CourseService(IApiClient apiClient, EndpointsConfig endpoints)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
    }

    public async Task<ApiResponse<List<CourseDto>>> GetAllCoursesAsync()
    {
        return await _apiClient.GetAsync<List<CourseDto>>(_endpoints.Courses.GetAll);
    }

    public async Task<ApiResponse<CourseDto>> GetCourseByIdAsync(Guid id)
    {
        return await _apiClient.GetAsync<CourseDto>(string.Format(_endpoints.Courses.GetById, id));
    }

    public async Task<ApiResponse<List<CourseDto>>> GetCoursesByCategoryAsync(Guid categoryId)
    {
        return await _apiClient.GetAsync<List<CourseDto>>(string.Format(_endpoints.Courses.GetByCategory, categoryId));
    }

    public async Task<ApiResponse<List<CourseDto>>> SearchCoursesAsync(string searchTerm)
    {
        return await _apiClient.GetAsync<List<CourseDto>>($"{_endpoints.Courses.Search}?q={Uri.EscapeDataString(searchTerm)}");
    }

    public async Task<ApiResponse<CourseDto>> CreateCourseAsync(CourseDto course)
    {
        return await _apiClient.PostAsync<CourseDto>(_endpoints.Courses.Create, course);
    }

    public async Task<ApiResponse<CourseDto>> UpdateCourseAsync(Guid id, CourseDto course)
    {
        return await _apiClient.PutAsync<CourseDto>(string.Format(_endpoints.Courses.Update, id), course);
    }

    public async Task<ApiResponse> DeleteCourseAsync(Guid id)
    {
        return await _apiClient.DeleteAsync(string.Format(_endpoints.Courses.Delete, id));
    }

    public async Task<ApiResponse<CourseSearchResultDto>> SearchCoursesAsync(CourseSearchDto searchDto)
    {
        var queryString = BuildSearchQueryString(searchDto);
        var endpoint = $"{_endpoints.Courses.Search}?{queryString}";
        return await _apiClient.GetAsync<CourseSearchResultDto>(endpoint);
    }

    public Task<ApiResponse<bool>> IsEnrolledAsync(Guid courseId)
    {
        // This would check if the current user is enrolled in the course
        // For now, return false as placeholder
        return Task.FromResult(new ApiResponse<bool> { Success = true, Data = false });
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
