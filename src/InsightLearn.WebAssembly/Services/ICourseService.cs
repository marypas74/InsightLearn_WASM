using InsightLearn.Shared.DTOs;
using InsightLearn.WebAssembly.Models;

namespace InsightLearn.WebAssembly.Services;

public interface ICourseService
{
    Task<ApiResponse<List<CourseDto>>> GetAllCoursesAsync();
    Task<ApiResponse<CourseDto>> GetCourseByIdAsync(Guid id);
    Task<ApiResponse<List<CourseDto>>> GetCoursesByCategoryAsync(Guid categoryId);
    Task<ApiResponse<List<CourseDto>>> SearchCoursesAsync(string searchTerm);
    Task<ApiResponse<CourseDto>> CreateCourseAsync(CourseDto course);
    Task<ApiResponse<CourseDto>> UpdateCourseAsync(Guid id, CourseDto course);
    Task<ApiResponse> DeleteCourseAsync(Guid id);
}
