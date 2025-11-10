using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Enrollment entity
/// </summary>
public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly InsightLearnDbContext _context;

    public EnrollmentRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Enrollment>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        return await _context.Enrollments
            .Include(e => e.User)
            .Include(e => e.Course)
            .OrderByDescending(e => e.EnrolledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Enrollment?> GetByIdAsync(Guid id)
    {
        return await _context.Enrollments
            .Include(e => e.User)
            .Include(e => e.Course)
                .ThenInclude(c => c.Sections)
                    .ThenInclude(s => s.Lessons)
            .Include(e => e.CurrentLesson)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Enrollment>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Enrollments
            .Include(e => e.Course)
                .ThenInclude(c => c.Instructor)
            .Include(e => e.Course.Category)
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.LastAccessedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Enrollment>> GetByCourseIdAsync(Guid courseId)
    {
        return await _context.Enrollments
            .Include(e => e.User)
            .Where(e => e.CourseId == courseId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();
    }

    public async Task<Enrollment?> GetActiveEnrollmentAsync(Guid userId, Guid courseId)
    {
        return await _context.Enrollments
            .Include(e => e.Course)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.UserId == userId
                && e.CourseId == courseId
                && e.Status == EnrollmentStatus.Active);
    }

    public async Task<IEnumerable<Enrollment>> GetActiveEnrollmentsAsync(Guid userId)
    {
        return await _context.Enrollments
            .Include(e => e.Course)
                .ThenInclude(c => c.Instructor)
            .Include(e => e.Course.Category)
            .Where(e => e.UserId == userId && e.Status == EnrollmentStatus.Active)
            .OrderByDescending(e => e.LastAccessedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Enrollment>> GetCompletedEnrollmentsAsync(Guid userId)
    {
        return await _context.Enrollments
            .Include(e => e.Course)
            .Include(e => e.Certificate)
            .Where(e => e.UserId == userId && e.Status == EnrollmentStatus.Completed)
            .OrderByDescending(e => e.CompletedAt)
            .ToListAsync();
    }

    public async Task<Enrollment> CreateAsync(Enrollment enrollment)
    {
        enrollment.EnrolledAt = DateTime.UtcNow;
        enrollment.LastAccessedAt = DateTime.UtcNow;
        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();
        return enrollment;
    }

    public async Task<Enrollment> UpdateAsync(Enrollment enrollment)
    {
        enrollment.LastAccessedAt = DateTime.UtcNow;
        _context.Enrollments.Update(enrollment);
        await _context.SaveChangesAsync();
        return enrollment;
    }

    public async Task UpdateProgressAsync(Guid enrollmentId, int completedLessons, int totalWatchedMinutes)
    {
        var enrollment = await _context.Enrollments.FindAsync(enrollmentId);
        if (enrollment != null)
        {
            enrollment.CompletedLessons = completedLessons;
            enrollment.TotalWatchedMinutes = totalWatchedMinutes;
            enrollment.LastAccessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task CompleteAsync(Guid enrollmentId)
    {
        var enrollment = await _context.Enrollments.FindAsync(enrollmentId);
        if (enrollment != null)
        {
            enrollment.Status = EnrollmentStatus.Completed;
            enrollment.CompletedAt = DateTime.UtcNow;
            enrollment.HasCertificate = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _context.Enrollments.CountAsync();
    }

    public async Task<bool> IsUserEnrolledAsync(Guid userId, Guid courseId)
    {
        return await _context.Enrollments
            .AnyAsync(e => e.UserId == userId
                && e.CourseId == courseId
                && (e.Status == EnrollmentStatus.Active || e.Status == EnrollmentStatus.Completed));
    }
}
