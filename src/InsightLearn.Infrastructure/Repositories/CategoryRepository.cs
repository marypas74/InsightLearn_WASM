using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Category entity
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly InsightLearnDbContext _context;

    public CategoryRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    // PERFORMANCE FIX (PERF-4): Added pagination with default pageSize=100 (reasonable for categories)
    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query optimization
    public async Task<IEnumerable<Category>> GetAllAsync(int page = 1, int pageSize = 100)
    {
        // Enforce max page size to prevent memory exhaustion
        pageSize = Math.Min(pageSize, 100);

        return await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.OrderIndex)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query
    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query
    public async Task<Category?> GetBySlugAsync(string slug)
    {
        return await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug);
    }

    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query
    public async Task<Category?> GetWithCoursesAsync(Guid id)
    {
        return await _context.Categories
            .AsNoTracking()
            .Include(c => c.Courses.Where(course => course.Status == CourseStatus.Published && course.IsActive))
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Category> CreateAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<Category> UpdateAsync(Category category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}
