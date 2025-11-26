using InsightLearn.WebAssembly.Models.Admin;
using InsightLearn.WebAssembly.Models.Courses;

namespace InsightLearn.WebAssembly.Services.Admin;

/// <summary>
/// Service interface for admin course management operations
/// </summary>
public interface ICourseManagementService
{
    /// <summary>
    /// Get paginated list of courses with optional filtering
    /// </summary>
    Task<PagedResult<CourseListItem>?> GetCoursesAsync(
        int page,
        int pageSize,
        string? search = null,
        string? status = null,
        Guid? categoryId = null,
        string? sortBy = null,
        bool sortDescending = true);

    /// <summary>
    /// Get course details by ID
    /// </summary>
    Task<CourseDetail?> GetCourseByIdAsync(Guid id);

    /// <summary>
    /// Create a new course
    /// </summary>
    Task<Guid?> CreateCourseAsync(CourseCreateRequest request);

    /// <summary>
    /// Update an existing course
    /// </summary>
    Task<bool> UpdateCourseAsync(Guid id, CourseUpdateRequest request);

    /// <summary>
    /// Delete a course
    /// </summary>
    Task<bool> DeleteCourseAsync(Guid id);

    /// <summary>
    /// Get course statistics for KPI cards
    /// </summary>
    Task<CourseStatsDto?> GetCourseStatsAsync();

    /// <summary>
    /// Get all categories for filter dropdown
    /// </summary>
    Task<List<CategoryFilterItem>?> GetCategoriesAsync();

    /// <summary>
    /// Update course status (publish, unpublish, archive)
    /// </summary>
    Task<bool> UpdateCourseStatusAsync(Guid id, string status);
}

/// <summary>
/// Course statistics for admin dashboard KPI cards
/// </summary>
public class CourseStatsDto
{
    public int TotalCourses { get; set; }
    public int PublishedCourses { get; set; }
    public int DraftCourses { get; set; }
    public int ArchivedCourses { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalEnrollments { get; set; }
    public double AverageRating { get; set; }
}

/// <summary>
/// Category item for filter dropdown
/// </summary>
public class CategoryFilterItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CourseCount { get; set; }
}
