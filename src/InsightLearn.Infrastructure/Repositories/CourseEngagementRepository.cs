using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

public class CourseEngagementRepository : ICourseEngagementRepository
{
    private readonly InsightLearnDbContext _context;

    public CourseEngagementRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    public async Task<CourseEngagement> CreateAsync(CourseEngagement engagement)
    {
        engagement.CreatedAt = DateTime.UtcNow;

        _context.CourseEngagements.Add(engagement);
        await _context.SaveChangesAsync();
        return engagement;
    }

    public async Task<CourseEngagement> UpdateAsync(CourseEngagement engagement)
    {
        _context.CourseEngagements.Update(engagement);
        await _context.SaveChangesAsync();
        return engagement;
    }

    public async Task<CourseEngagement?> GetByIdAsync(Guid id)
    {
        return await _context.CourseEngagements
            .Include(e => e.User)
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<CourseEngagement>> GetByUserIdAsync(Guid userId, int page = 1, int pageSize = 50)
    {
        return await _context.CourseEngagements
            .Include(e => e.Course)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<CourseEngagement>> GetByUserIdAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        return await _context.CourseEngagements
            .Include(e => e.Course)
            .Where(e => e.UserId == userId &&
                       e.StartedAt >= startDate &&
                       e.StartedAt < endDate)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();
    }

    public async Task<List<CourseEngagement>> GetByCourseIdAsync(Guid courseId, int page = 1, int pageSize = 50)
    {
        return await _context.CourseEngagements
            .Include(e => e.User)
            .Where(e => e.CourseId == courseId)
            .OrderByDescending(e => e.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<CourseEngagement>> GetByCourseIdAsync(Guid courseId, DateTime startDate, DateTime endDate)
    {
        return await _context.CourseEngagements
            .Include(e => e.User)
            .Where(e => e.CourseId == courseId &&
                       e.StartedAt >= startDate &&
                       e.StartedAt < endDate)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();
    }

    public async Task<List<CourseEngagement>> GetByUserAndCourseAsync(Guid userId, Guid courseId, DateTime startDate, DateTime endDate)
    {
        return await _context.CourseEngagements
            .Where(e => e.UserId == userId &&
                       e.CourseId == courseId &&
                       e.StartedAt >= startDate &&
                       e.StartedAt < endDate)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();
    }

    public async Task<long> GetTotalEngagementMinutesAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.CourseEngagements
            .Where(e => e.CountsForPayout &&
                       e.StartedAt >= startDate &&
                       e.StartedAt < endDate)
            .SumAsync(e => (long)e.DurationMinutes);
    }

    public async Task<long> GetInstructorEngagementMinutesAsync(Guid instructorId, DateTime startDate, DateTime endDate)
    {
        return await _context.CourseEngagements
            .Include(e => e.Course)
            .Where(e => e.CountsForPayout &&
                       e.Course.InstructorId == instructorId &&
                       e.StartedAt >= startDate &&
                       e.StartedAt < endDate)
            .SumAsync(e => (long)e.DurationMinutes);
    }

    public async Task<long> GetCourseEngagementMinutesAsync(Guid courseId, DateTime startDate, DateTime endDate)
    {
        return await _context.CourseEngagements
            .Where(e => e.CountsForPayout &&
                       e.CourseId == courseId &&
                       e.StartedAt >= startDate &&
                       e.StartedAt < endDate)
            .SumAsync(e => (long)e.DurationMinutes);
    }

    public async Task<int> GetUniqueStudentCountAsync(Guid instructorId, DateTime startDate, DateTime endDate)
    {
        return await _context.CourseEngagements
            .Include(e => e.Course)
            .Where(e => e.CountsForPayout &&
                       e.Course.InstructorId == instructorId &&
                       e.StartedAt >= startDate &&
                       e.StartedAt < endDate)
            .Select(e => e.UserId)
            .Distinct()
            .CountAsync();
    }

    public async Task<Dictionary<Guid, long>> GetCourseEngagementBreakdownAsync(Guid instructorId, DateTime startDate, DateTime endDate)
    {
        return await _context.CourseEngagements
            .Include(e => e.Course)
            .Where(e => e.CountsForPayout &&
                       e.Course.InstructorId == instructorId &&
                       e.StartedAt >= startDate &&
                       e.StartedAt < endDate)
            .GroupBy(e => e.CourseId)
            .Select(g => new
            {
                CourseId = g.Key,
                TotalMinutes = g.Sum(e => (long)e.DurationMinutes)
            })
            .ToDictionaryAsync(x => x.CourseId, x => x.TotalMinutes);
    }

    public async Task<List<CourseEngagement>> GetPendingValidationAsync(DateTime? since = null, int batchSize = 100)
    {
        var query = _context.CourseEngagements
            .Include(e => e.User)
            .Include(e => e.Course)
            .Where(e => e.ValidationScore == 0 && e.CompletedAt != null);

        if (since.HasValue)
        {
            query = query.Where(e => e.CompletedAt >= since.Value);
        }

        return await query
            .OrderBy(e => e.CompletedAt)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task<bool> ValidateEngagementAsync(Guid id, decimal validationScore, bool countsForPayout)
    {
        var engagement = await GetByIdAsync(id);
        if (engagement == null)
            return false;

        engagement.ValidationScore = validationScore;
        engagement.CountsForPayout = countsForPayout;

        await _context.SaveChangesAsync();
        return true;
    }
}
