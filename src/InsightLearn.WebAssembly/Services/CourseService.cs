using InsightLearn.Application.DTOs;
using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;

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
}
