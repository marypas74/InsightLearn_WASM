using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Course entity
/// </summary>
public class CourseRepository : ICourseRepository
{
    private readonly InsightLearnDbContext _context;

    public CourseRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    // PERFORMANCE FIX (PERF-1): Added Include(Reviews) to prevent N+1 query problem
    // Without this, accessing course.Reviews for average rating causes 1 query per course
    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query optimization
    public async Task<IEnumerable<Course>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        return await _context.Courses
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .Include(c => c.Reviews)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query optimization
    public async Task<Course?> GetByIdAsync(Guid id)
    {
        return await _context.Courses
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .Include(c => c.Sections.OrderBy(s => s.OrderIndex))
                .ThenInclude(s => s.Lessons.OrderBy(l => l.OrderIndex))
                    .ThenInclude(l => l.SubtitleTracks.Where(st => st.IsActive))
            .Include(c => c.Reviews)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    // PERFORMANCE FIX (PERF-1): Added Include(Reviews) for course detail page
    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query optimization
    public async Task<Course?> GetBySlugAsync(string slug)
    {
        return await _context.Courses
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .Include(c => c.Sections.OrderBy(s => s.OrderIndex))
                .ThenInclude(s => s.Lessons.OrderBy(l => l.OrderIndex))
                    .ThenInclude(l => l.SubtitleTracks.Where(st => st.IsActive))
            .Include(c => c.Reviews)
            .FirstOrDefaultAsync(c => c.Slug == slug);
    }

    // PERFORMANCE FIX (PERF-1): Added Include(Reviews) to prevent N+1 query problem
    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query optimization
    public async Task<IEnumerable<Course>> GetByCategoryIdAsync(Guid categoryId, int page = 1, int pageSize = 10)
    {
        return await _context.Courses
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .Include(c => c.Reviews)
            .Where(c => c.CategoryId == categoryId && c.IsActive)
            .OrderByDescending(c => c.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    // PERFORMANCE FIX (PERF-1): Added Include(Reviews) to prevent N+1 query problem
    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query optimization
    public async Task<IEnumerable<Course>> GetByInstructorIdAsync(Guid instructorId)
    {
        return await _context.Courses
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Reviews)
            .Where(c => c.InstructorId == instructorId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    // PERFORMANCE FIX (PERF-1): Added Include(Reviews) to prevent N+1 query problem
    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query optimization
    public async Task<IEnumerable<Course>> GetPublishedCoursesAsync(int page = 1, int pageSize = 10)
    {
        return await _context.Courses
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .Include(c => c.Reviews)
            .Where(c => c.Status == CourseStatus.Published && c.IsActive)
            .OrderByDescending(c => c.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    // PERFORMANCE FIX (PERF-1): Added Include(Reviews) to prevent N+1 query problem
    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query optimization
    public async Task<IEnumerable<Course>> SearchAsync(string query, int page = 1, int pageSize = 10)
    {
        var lowerQuery = query.ToLower();

        return await _context.Courses
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Instructor)
            .Include(c => c.Reviews)
            .Where(c => c.IsActive && (
                c.Title.ToLower().Contains(lowerQuery) ||
                c.Description.ToLower().Contains(lowerQuery) ||
                c.ShortDescription != null && c.ShortDescription.ToLower().Contains(lowerQuery)
            ))
            .OrderByDescending(c => c.AverageRating)
            .ThenByDescending(c => c.EnrollmentCount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Course> CreateAsync(Course course)
    {
        course.CreatedAt = DateTime.UtcNow;
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
        return course;
    }

    public async Task<Course> UpdateAsync(Course course)
    {
        course.UpdatedAt = DateTime.UtcNow;
        _context.Courses.Update(course);
        await _context.SaveChangesAsync();
        return course;
    }

    public async Task DeleteAsync(Guid id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course != null)
        {
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateStatisticsAsync(Guid id, int viewCount, double averageRating, int reviewCount, int enrollmentCount)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course != null)
        {
            course.ViewCount = viewCount;
            course.AverageRating = averageRating;
            course.ReviewCount = reviewCount;
            course.EnrollmentCount = enrollmentCount;
            course.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Courses.CountAsync();
    }

    public async Task<int> GetPublishedCountAsync()
    {
        return await _context.Courses
            .CountAsync(c => c.Status == CourseStatus.Published && c.IsActive);
    }

    public async Task<int> GetByCategoryCountAsync(Guid categoryId)
    {
        return await _context.Courses
            .CountAsync(c => c.CategoryId == categoryId && c.IsActive);
    }

    public async Task<int> GetByInstructorCountAsync(Guid instructorId)
    {
        return await _context.Courses
            .CountAsync(c => c.InstructorId == instructorId && c.IsActive);
    }

    public async Task<int> SearchCountAsync(string query)
    {
        var searchTerm = query.ToLower();
        return await _context.Courses
            .CountAsync(c => c.IsActive &&
                (c.Title.ToLower().Contains(searchTerm) ||
                 c.Description.ToLower().Contains(searchTerm)));
    }
}
