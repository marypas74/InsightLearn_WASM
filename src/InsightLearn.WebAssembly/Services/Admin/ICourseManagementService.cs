using InsightLearn.WebAssembly.Models.Admin;
using InsightLearn.WebAssembly.Models.Courses;

namespace InsightLearn.WebAssembly.Services.Admin;

public interface ICourseManagementService
{
    Task<PagedResult<CourseListItem>?> GetCoursesAsync(int page, int pageSize, string? search = null);
    Task<CourseDetail?> GetCourseByIdAsync(Guid id);
    Task<Guid?> CreateCourseAsync(CourseCreateRequest request);
    Task<bool> UpdateCourseAsync(Guid id, CourseUpdateRequest request);
    Task<bool> DeleteCourseAsync(Guid id);
}
