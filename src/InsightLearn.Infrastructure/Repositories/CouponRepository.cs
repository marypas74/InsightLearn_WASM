using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Coupon entity
/// </summary>
public class CouponRepository : ICouponRepository
{
    private readonly InsightLearnDbContext _context;

    public CouponRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    // PERFORMANCE FIX (PERF-4): Added pagination with default pageSize=50 (reasonable for coupons)
    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query optimization
    public async Task<IEnumerable<Coupon>> GetAllAsync(int page = 1, int pageSize = 50)
    {
        // Enforce max page size to prevent memory exhaustion
        pageSize = Math.Min(pageSize, 100);

        return await _context.Coupons
            .AsNoTracking()
            .Include(c => c.Course)
            .Include(c => c.Instructor)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query
    public async Task<Coupon?> GetByIdAsync(Guid id)
    {
        return await _context.Coupons
            .AsNoTracking()
            .Include(c => c.Course)
            .Include(c => c.Instructor)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query
    public async Task<Coupon?> GetByCodeAsync(string code)
    {
        return await _context.Coupons
            .AsNoTracking()
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower());
    }

    // PERFORMANCE FIX (PERF-4): Added pagination with default pageSize=50
    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query
    public async Task<IEnumerable<Coupon>> GetActiveCouponsAsync(int page = 1, int pageSize = 50)
    {
        pageSize = Math.Min(pageSize, 100);
        var now = DateTime.UtcNow;

        return await _context.Coupons
            .AsNoTracking()
            .Include(c => c.Course)
            .Where(c => c.IsActive
                && c.ValidFrom <= now
                && c.ValidUntil >= now
                && (c.UsageLimit == null || c.UsedCount < c.UsageLimit))
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    // PERFORMANCE FIX (PERF-4): Added pagination with default pageSize=20
    // PERFORMANCE FIX (PERF-2): Added AsNoTracking() for read-only query
    public async Task<IEnumerable<Coupon>> GetByCourseIdAsync(Guid courseId, int page = 1, int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 50);
        var now = DateTime.UtcNow;

        return await _context.Coupons
            .AsNoTracking()
            .Where(c => c.CourseId == courseId
                && c.IsActive
                && c.ValidFrom <= now
                && c.ValidUntil >= now)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Coupon> CreateAsync(Coupon coupon)
    {
        coupon.CreatedAt = DateTime.UtcNow;
        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync();
        return coupon;
    }

    public async Task<Coupon> UpdateAsync(Coupon coupon)
    {
        _context.Coupons.Update(coupon);
        await _context.SaveChangesAsync();
        return coupon;
    }

    public async Task DeleteAsync(Guid id)
    {
        var coupon = await _context.Coupons.FindAsync(id);
        if (coupon != null)
        {
            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();
        }
    }

    public async Task IncrementUsageAsync(Guid couponId)
    {
        var coupon = await _context.Coupons.FindAsync(couponId);
        if (coupon != null)
        {
            coupon.UsedCount++;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsValidAsync(string code, Guid? courseId = null)
    {
        var now = DateTime.UtcNow;
        var query = _context.Coupons
            .Where(c => c.Code.ToLower() == code.ToLower()
                && c.IsActive
                && c.ValidFrom <= now
                && c.ValidUntil >= now
                && (c.UsageLimit == null || c.UsedCount < c.UsageLimit));

        if (courseId.HasValue)
        {
            query = query.Where(c => c.CourseId == null || c.CourseId == courseId.Value);
        }

        return await query.AnyAsync();
    }
}
