using InsightLearn.WebAssembly.Models.Admin;
using InsightLearn.WebAssembly.Models.Courses;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services.Admin;

/// <summary>
/// Implementation of course management service for admin operations
/// </summary>
public class CourseManagementService : ICourseManagementService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<CourseManagementService> _logger;

    public CourseManagementService(IApiClient apiClient, ILogger<CourseManagementService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<PagedResult<CourseListItem>?> GetCoursesAsync(
        int page,
        int pageSize,
        string? search = null,
        string? status = null,
        Guid? categoryId = null,
        string? sortBy = null,
        bool sortDescending = true)
    {
        try
        {
            _logger.LogInformation("Fetching courses (page: {Page}, pageSize: {PageSize}, search: {Search}, status: {Status}, categoryId: {CategoryId})",
                page, pageSize, search ?? "none", status ?? "all", categoryId?.ToString() ?? "all");

            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrEmpty(search))
            {
                queryParams.Add($"search={Uri.EscapeDataString(search)}");
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                queryParams.Add($"status={Uri.EscapeDataString(status)}");
            }

            if (categoryId.HasValue && categoryId.Value != Guid.Empty)
            {
                queryParams.Add($"categoryId={categoryId.Value}");
            }

            if (!string.IsNullOrEmpty(sortBy))
            {
                queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
                queryParams.Add($"sortDescending={sortDescending.ToString().ToLower()}");
            }

            var url = $"/api/courses?{string.Join("&", queryParams)}";

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
            var response = await _apiClient.GetAsync<CourseDetail>($"/api/courses/{id}");

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
            var response = await _apiClient.PostAsync<CreateCourseResponse>("/api/courses", request);

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
            var response = await _apiClient.PutAsync($"/api/courses/{id}", request);

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
            var response = await _apiClient.DeleteAsync($"/api/courses/{id}");

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

    public async Task<CourseStatsDto?> GetCourseStatsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching course statistics");

            // Try to get stats from dedicated endpoint first
            var response = await _apiClient.GetAsync<CourseStatsDto>("/api/admin/courses/stats");

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Course stats fetched successfully");
                return response.Data;
            }

            // Fallback: Calculate stats from course list
            _logger.LogInformation("Stats endpoint not available, calculating from course list");
            var coursesResponse = await _apiClient.GetAsync<PagedResult<CourseListItem>>("/api/courses?page=1&pageSize=1000");

            if (coursesResponse.Success && coursesResponse.Data != null)
            {
                var courses = coursesResponse.Data.Items;
                var stats = new CourseStatsDto
                {
                    TotalCourses = coursesResponse.Data.TotalCount,
                    PublishedCourses = courses.Count(c => c.Status?.Equals("Published", StringComparison.OrdinalIgnoreCase) == true),
                    DraftCourses = courses.Count(c => c.Status?.Equals("Draft", StringComparison.OrdinalIgnoreCase) == true),
                    ArchivedCourses = courses.Count(c => c.Status?.Equals("Archived", StringComparison.OrdinalIgnoreCase) == true),
                    TotalRevenue = courses.Sum(c => c.Price * c.EnrollmentCount),
                    TotalEnrollments = courses.Sum(c => c.EnrollmentCount),
                    AverageRating = courses.Where(c => c.AverageRating > 0).DefaultIfEmpty().Average(c => c?.AverageRating ?? 0)
                };

                _logger.LogInformation("Course stats calculated: Total={Total}, Published={Published}, Draft={Draft}",
                    stats.TotalCourses, stats.PublishedCourses, stats.DraftCourses);
                return stats;
            }

            _logger.LogWarning("Failed to fetch course stats");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching course statistics");
            return null;
        }
    }

    public async Task<List<CategoryFilterItem>?> GetCategoriesAsync()
    {
        try
        {
            _logger.LogInformation("Fetching categories for filter");
            var response = await _apiClient.GetAsync<List<CategoryFilterItem>>("/api/categories");

            if (response.Success && response.Data != null)
            {
                _logger.LogInformation("Categories fetched successfully: {Count} items", response.Data.Count);
                return response.Data;
            }

            _logger.LogWarning("Failed to fetch categories: {Message}", response.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching categories");
            return null;
        }
    }

    public async Task<bool> UpdateCourseStatusAsync(Guid id, string status)
    {
        try
        {
            _logger.LogInformation("Updating course {CourseId} status to {Status}", id, status);

            var request = new { Status = status };
            var response = await _apiClient.PutAsync($"/api/courses/{id}/status", request);

            if (response.Success)
            {
                _logger.LogInformation("Course {CourseId} status updated to {Status}", id, status);
                return true;
            }

            _logger.LogWarning("Failed to update course {CourseId} status: {Message}", id, response.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course {CourseId} status", id);
            return false;
        }
    }
}

/// <summary>
/// Response model for course creation
/// </summary>
public class CreateCourseResponse
{
    public Guid Id { get; set; }
    public bool Success { get; set; }
}
