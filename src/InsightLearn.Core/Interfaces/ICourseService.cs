using InsightLearn.Core.DTOs.Course;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service interface for course business logic operations
/// </summary>
public interface ICourseService
{
    // Query methods
    Task<CourseListDto> GetCoursesAsync(int page = 1, int pageSize = 10, Guid? categoryId = null);
    Task<CourseDto?> GetCourseByIdAsync(Guid id);
    Task<CourseDto?> GetCourseBySlugAsync(string slug);
    Task<CourseListDto> GetCoursesByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 10);
    Task<List<CourseCardDto>> GetCoursesByInstructorAsync(Guid instructorId);
    Task<CourseListDto> SearchCoursesAsync(CourseSearchDto searchDto);

    // Command methods
    Task<CourseDto> CreateCourseAsync(CreateCourseDto dto);
    Task<CourseDto?> UpdateCourseAsync(Guid id, UpdateCourseDto dto);
    Task<bool> DeleteCourseAsync(Guid id);
    Task<bool> PublishCourseAsync(Guid id);
    Task<bool> UnpublishCourseAsync(Guid id);

    // Statistics
    Task<CourseSummaryDto> GetCourseSummaryAsync(Guid id);
    Task IncrementViewCountAsync(Guid id);
    Task UpdateCourseStatisticsAsync(Guid id);
}
