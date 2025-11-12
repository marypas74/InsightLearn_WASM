using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

public class SubscriptionPlanRepository : ISubscriptionPlanRepository
{
    private readonly InsightLearnDbContext _context;

    public SubscriptionPlanRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionPlan?> GetByIdAsync(Guid id)
    {
        return await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<SubscriptionPlan?> GetByNameAsync(string name)
    {
        return await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<List<SubscriptionPlan>> GetAllActiveAsync()
    {
        return await _context.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();
    }

    public async Task<List<SubscriptionPlan>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.SubscriptionPlans.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.DisplayOrder)
            .ToListAsync();
    }

    public async Task<SubscriptionPlan> CreateAsync(SubscriptionPlan plan)
    {
        plan.CreatedAt = DateTime.UtcNow;
        plan.UpdatedAt = DateTime.UtcNow;

        _context.SubscriptionPlans.Add(plan);
        await _context.SaveChangesAsync();
        return plan;
    }

    public async Task<SubscriptionPlan> UpdateAsync(SubscriptionPlan plan)
    {
        plan.UpdatedAt = DateTime.UtcNow;

        _context.SubscriptionPlans.Update(plan);
        await _context.SaveChangesAsync();
        return plan;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var plan = await GetByIdAsync(id);
        if (plan == null)
            return false;

        // Soft delete - just mark as inactive
        plan.IsActive = false;
        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<SubscriptionPlan?> GetByStripePriceIdAsync(string stripePriceId)
    {
        return await _context.SubscriptionPlans
            .FirstOrDefaultAsync(p =>
                p.StripePriceMonthlyId == stripePriceId ||
                p.StripePriceYearlyId == stripePriceId);
    }
}
