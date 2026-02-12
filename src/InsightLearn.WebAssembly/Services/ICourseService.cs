using InsightLearn.Shared.DTOs;
using InsightLearn.WebAssembly.Models;

namespace InsightLearn.WebAssembly.Services;

public interface ICourseService
{
    Task<ApiResponse<List<CourseDto>>> GetAllCoursesAsync();
    Task<ApiResponse<CourseDto>> GetCourseByIdAsync(Guid id);

    /// <summary>
    /// v2.3.113-dev: Get course by encoded ID (URL obfuscation)
    /// </summary>
    Task<ApiResponse<CourseDto>> GetCourseByEncodedIdAsync(string encodedId);
    Task<ApiResponse<List<CourseDto>>> GetCoursesByCategoryAsync(Guid categoryId);
    Task<ApiResponse<List<CourseDto>>> SearchCoursesAsync(string searchTerm);
    Task<ApiResponse<CourseDto>> CreateCourseAsync(CourseDto course);
    Task<ApiResponse<CourseDto>> UpdateCourseAsync(Guid id, CourseDto course);
    Task<ApiResponse> DeleteCourseAsync(Guid id);

    // New methods for Browse Courses page
    Task<ApiResponse<CourseSearchResultDto>> SearchCoursesAsync(CourseSearchDto searchDto);
    Task<ApiResponse<bool>> IsEnrolledAsync(Guid courseId);
}
