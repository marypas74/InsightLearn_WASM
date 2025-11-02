using InsightLearn.WebAssembly.Models;

namespace InsightLearn.WebAssembly.Services;

public class DashboardData
{
    public int TotalCourses { get; set; }
    public int EnrolledCourses { get; set; }
    public int CompletedCourses { get; set; }
    public double AverageProgress { get; set; }
    public List<CourseProgressItem> RecentCourses { get; set; } = new();
}

public class CourseProgressItem
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int Progress { get; set; }
    public DateTime LastAccessed { get; set; }
}

public interface IDashboardService
{
    Task<ApiResponse<DashboardData>> GetStudentDashboardAsync();
    Task<ApiResponse<DashboardData>> GetInstructorDashboardAsync();
    Task<ApiResponse<DashboardData>> GetAdminDashboardAsync();
}
