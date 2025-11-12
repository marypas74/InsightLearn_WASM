using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

public interface ICourseEngagementRepository
{
    Task<CourseEngagement> CreateAsync(CourseEngagement engagement);
    Task<CourseEngagement> UpdateAsync(CourseEngagement engagement);
    Task<CourseEngagement?> GetByIdAsync(Guid id);
    Task<List<CourseEngagement>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 50);
    Task<List<CourseEngagement>> GetByUserIdAsync(Guid userId, DateTime startDate, DateTime endDate);
    Task<List<CourseEngagement>> GetByCourseIdAsync(Guid courseId, int page = 1, int pageSize = 50);
    Task<List<CourseEngagement>> GetByCourseIdAsync(Guid courseId, DateTime startDate, DateTime endDate);
    Task<List<CourseEngagement>> GetByUserAndCourseAsync(Guid userId, Guid courseId, DateTime startDate, DateTime endDate);
    Task<long> GetTotalEngagementMinutesAsync(DateTime startDate, DateTime endDate);
    Task<long> GetInstructorEngagementMinutesAsync(Guid instructorId, DateTime startDate, DateTime endDate);
    Task<long> GetCourseEngagementMinutesAsync(Guid courseId, DateTime startDate, DateTime endDate);
    Task<int> GetUniqueStudentCountAsync(Guid instructorId, DateTime startDate, DateTime endDate);
    Task<Dictionary<Guid, long>> GetCourseEngagementBreakdownAsync(Guid instructorId, DateTime startDate, DateTime endDate);
    Task<List<CourseEngagement>> GetPendingValidationAsync(DateTime? since = null, int batchSize = 100);
    Task<bool> ValidateEngagementAsync(Guid id, decimal validationScore, bool countsForPayout);
}
