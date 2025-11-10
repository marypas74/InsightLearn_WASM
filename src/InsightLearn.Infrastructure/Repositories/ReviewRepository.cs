using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Review entity
/// </summary>
public class ReviewRepository : IReviewRepository
{
    private readonly InsightLearnDbContext _context;

    public ReviewRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Review>> GetAllAsync(int page = 1, int pageSize = 10)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Course)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Review?> GetByIdAsync(Guid id)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Course)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Review>> GetByCourseIdAsync(Guid courseId, int page = 1, int pageSize = 10)
    {
        return await _context.Reviews
            .Include(r => r.User)
            .Where(r => r.CourseId == courseId)
            .OrderByDescending(r => r.HelpfulVotes)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Review>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Reviews
            .Include(r => r.Course)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Review?> GetUserReviewForCourseAsync(Guid userId, Guid courseId)
    {
        return await _context.Reviews
            .Include(r => r.Course)
            .FirstOrDefaultAsync(r => r.UserId == userId && r.CourseId == courseId);
    }

    public async Task<Review> CreateAsync(Review review)
    {
        review.CreatedAt = DateTime.UtcNow;
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        return review;
    }

    public async Task<Review> UpdateAsync(Review review)
    {
        review.UpdatedAt = DateTime.UtcNow;
        _context.Reviews.Update(review);
        await _context.SaveChangesAsync();
        return review;
    }

    public async Task DeleteAsync(Guid id)
    {
        var review = await _context.Reviews.FindAsync(id);
        if (review != null)
        {
            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<double> GetAverageRatingAsync(Guid courseId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.CourseId == courseId)
            .ToListAsync();

        return reviews.Any() ? reviews.Average(r => r.Rating) : 0;
    }

    public async Task<int> GetReviewCountAsync(Guid courseId)
    {
        return await _context.Reviews
            .CountAsync(r => r.CourseId == courseId);
    }
}
