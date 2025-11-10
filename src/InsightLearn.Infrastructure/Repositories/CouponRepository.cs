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

    public async Task<IEnumerable<Coupon>> GetAllAsync()
    {
        return await _context.Coupons
            .Include(c => c.Course)
            .Include(c => c.Instructor)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Coupon?> GetByIdAsync(Guid id)
    {
        return await _context.Coupons
            .Include(c => c.Course)
            .Include(c => c.Instructor)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Coupon?> GetByCodeAsync(string code)
    {
        return await _context.Coupons
            .Include(c => c.Course)
            .FirstOrDefaultAsync(c => c.Code.ToLower() == code.ToLower());
    }

    public async Task<IEnumerable<Coupon>> GetActiveCouponsAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Coupons
            .Include(c => c.Course)
            .Where(c => c.IsActive
                && c.ValidFrom <= now
                && c.ValidUntil >= now
                && (c.UsageLimit == null || c.UsedCount < c.UsageLimit))
            .ToListAsync();
    }

    public async Task<IEnumerable<Coupon>> GetByCourseIdAsync(Guid courseId)
    {
        var now = DateTime.UtcNow;
        return await _context.Coupons
            .Where(c => c.CourseId == courseId
                && c.IsActive
                && c.ValidFrom <= now
                && c.ValidUntil >= now)
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
