using InsightLearn.WebAssembly.Models.Admin;
using InsightLearn.WebAssembly.Models.Courses;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services.Admin;

public class CourseManagementService : ICourseManagementService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<CourseManagementService> _logger;

    public CourseManagementService(IApiClient apiClient, ILogger<CourseManagementService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<PagedResult<CourseListItem>?> GetCoursesAsync(int page, int pageSize, string? search = null)
    {
        try
        {
            _logger.LogInformation("Fetching courses (page: {Page}, pageSize: {PageSize}, search: {Search})",
                page, pageSize, search ?? "none");

            var url = $"/api/admin/courses?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(search))
            {
                url += $"&search={Uri.EscapeDataString(search)}";
            }

            var response = await _apiClient.GetAsync<PagedResult<CourseListItem>>(url);

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Courses fetched successfully: {Count} items", response.Data.Items.Count);
                return response.Data;
            }

            _logger.LogWarning("Failed to fetch courses: {Message}", response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching courses");
            return null;
        }
    }

    public async Task<CourseDetail?> GetCourseByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Fetching course {CourseId}", id);
            var response = await _apiClient.GetAsync<CourseDetail>($"/api/admin/courses/{id}");

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Course fetched successfully: {Title}", response.Data.Title);
                return response.Data;
            }

            _logger.LogWarning("Failed to fetch course {CourseId}: {Message}", id, response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching course {CourseId}", id);
            return null;
        }
    }

    public async Task<Guid?> CreateCourseAsync(CourseCreateRequest request)
    {
        try
        {
            _logger.LogInformation("Creating course: {Title}", request.Title);
            var response = await _apiClient.PostAsync<CreateCourseResponse>("/api/admin/courses", request);

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Course created successfully: {CourseId}", response.Data.Id);
                return response.Data.Id;
            }

            _logger.LogWarning("Failed to create course: {Message}", response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course");
            return null;
        }
    }

    public async Task<bool> UpdateCourseAsync(Guid id, CourseUpdateRequest request)
    {
        try
        {
            _logger.LogInformation("Updating course {CourseId}", id);
            var response = await _apiClient.PutAsync($"/api/admin/courses/{id}", request);

            if (response.Success)
            {
                _logger.LogInformation("Course {CourseId} updated successfully", id);
                return true;
            }

            _logger.LogWarning("Failed to update course {CourseId}: {Message}", id, response.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course {CourseId}", id);
            return false;
        }
    }

    public async Task<bool> DeleteCourseAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting course {CourseId}", id);
            var response = await _apiClient.DeleteAsync($"/api/admin/courses/{id}");

            if (response.Success)
            {
                _logger.LogInformation("Course {CourseId} deleted successfully", id);
                return true;
            }

            _logger.LogWarning("Failed to delete course {CourseId}: {Message}", id, response.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting course {CourseId}", id);
            return false;
        }
    }
}

public class CreateCourseResponse
{
    public Guid Id { get; set; }
    public bool Success { get; set; }
}
