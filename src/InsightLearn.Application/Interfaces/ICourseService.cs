using InsightLearn.Application.DTOs;

namespace InsightLearn.Application.Interfaces;

public interface ICourseService
{
    // Course Management
    Task<CourseDto?> GetCourseByIdAsync(Guid courseId, Guid? currentUserId = null);
    Task<CourseDto?> GetCourseBySlugAsync(string slug, Guid? currentUserId = null);
    Task<CourseSearchResultDto> SearchCoursesAsync(CourseSearchDto searchDto);
    Task<List<CourseDto>> GetFeaturedCoursesAsync(int count = 8);
    Task<List<CourseDto>> GetLatestCoursesAsync(int count = 8);
    Task<List<CourseDto>> GetPopularCoursesAsync(int count = 8);
    Task<List<CourseDto>> GetCoursesByCategoryAsync(Guid categoryId, int page = 1, int pageSize = 12);
    Task<List<CourseDto>> GetInstructorCoursesAsync(Guid instructorId);
    
    // Course CRUD for Instructors
    Task<CourseDto?> CreateCourseAsync(CreateCourseDto createDto, Guid instructorId);
    Task<CourseDto?> UpdateCourseAsync(Guid courseId, UpdateCourseDto updateDto, Guid instructorId);
    Task<bool> DeleteCourseAsync(Guid courseId, Guid instructorId);
    Task<bool> PublishCourseAsync(Guid courseId, Guid instructorId);
    Task<bool> UnpublishCourseAsync(Guid courseId, Guid instructorId);
    
    // Section Management
    Task<SectionDto?> CreateSectionAsync(Guid courseId, CreateSectionDto createDto, Guid instructorId);
    Task<SectionDto?> UpdateSectionAsync(Guid sectionId, CreateSectionDto updateDto, Guid instructorId);
    Task<bool> DeleteSectionAsync(Guid sectionId, Guid instructorId);
    Task<bool> ReorderSectionsAsync(Guid courseId, List<Guid> sectionIds, Guid instructorId);
    
    // Lesson Management
    Task<LessonDto?> CreateLessonAsync(Guid sectionId, CreateLessonDto createDto, Guid instructorId);
    Task<LessonDto?> UpdateLessonAsync(Guid lessonId, CreateLessonDto updateDto, Guid instructorId);
    Task<bool> DeleteLessonAsync(Guid lessonId, Guid instructorId);
    Task<bool> ReorderLessonsAsync(Guid sectionId, List<Guid> lessonIds, Guid instructorId);
    
    // Analytics
    Task<CourseAnalyticsDto> GetCourseAnalyticsAsync(Guid courseId, Guid instructorId);
    Task<InstructorDashboardDto> GetInstructorDashboardAsync(Guid instructorId);
}


public class DailyEnrollmentDto
{
    public DateTime Date { get; set; }
    public int Enrollments { get; set; }
}

public class MonthlyRevenueDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Revenue { get; set; }
}

public class InstructorDashboardDto
{
    public int TotalCourses { get; set; }
    public int PublishedCourses { get; set; }
    public int TotalStudents { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public List<CourseDto> RecentCourses { get; set; } = new();
    public List<CourseAnalyticsDto> TopPerformingCourses { get; set; } = new();
}