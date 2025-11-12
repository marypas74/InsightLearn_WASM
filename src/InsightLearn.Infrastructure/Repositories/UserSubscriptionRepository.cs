using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

public class UserSubscriptionRepository : IUserSubscriptionRepository
{
    private readonly InsightLearnDbContext _context;

    public UserSubscriptionRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    public async Task<UserSubscription?> GetByIdAsync(Guid id)
    {
        return await _context.UserSubscriptions
            .Include(s => s.User)
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<UserSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId)
    {
        return await _context.UserSubscriptions
            .Include(s => s.User)
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);
    }

    public async Task<UserSubscription?> GetActiveByUserIdAsync(Guid userId)
    {
        return await _context.UserSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId && s.Status == "active")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<UserSubscription>> GetByUserIdAsync(Guid userId, bool includeInactive = false)
    {
        var query = _context.UserSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId);

        if (!includeInactive)
        {
            query = query.Where(s => s.Status == "active");
        }

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserSubscription> CreateAsync(UserSubscription subscription)
    {
        subscription.CreatedAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;

        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task<UserSubscription> UpdateAsync(UserSubscription subscription)
    {
        subscription.UpdatedAt = DateTime.UtcNow;

        _context.UserSubscriptions.Update(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task<bool> CancelAsync(Guid id, string? reason = null, string? feedback = null)
    {
        var subscription = await GetByIdAsync(id);
        if (subscription == null)
            return false;

        subscription.Status = "cancelled";
        subscription.CancelledAt = DateTime.UtcNow;
        subscription.CancellationReason = reason;
        subscription.CancellationFeedback = feedback;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetActiveSubscriptionCountAsync()
    {
        return await _context.UserSubscriptions
            .Where(s => s.Status == "active" && s.CurrentPeriodEnd > DateTime.UtcNow)
            .CountAsync();
    }

    public async Task<List<UserSubscription>> GetExpiringSubscriptionsAsync(int daysBeforeExpiry)
    {
        var expiryDate = DateTime.UtcNow.AddDays(daysBeforeExpiry);

        return await _context.UserSubscriptions
            .Include(s => s.User)
            .Include(s => s.Plan)
            .Where(s => s.Status == "active" &&
                       s.AutoRenew == false &&
                       s.CurrentPeriodEnd <= expiryDate &&
                       s.CurrentPeriodEnd > DateTime.UtcNow)
            .OrderBy(s => s.CurrentPeriodEnd)
            .ToListAsync();
    }

    public async Task<List<UserSubscription>> GetSubscriptionsByStatusAsync(string status, int page = 1, int pageSize = 50)
    {
        return await _context.UserSubscriptions
            .Include(s => s.User)
            .Include(s => s.Plan)
            .Where(s => s.Status == status)
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<decimal> GetMonthlyRecurringRevenueAsync()
    {
        // Calculate MRR: sum of all active monthly subscriptions + (yearly/12)
        var monthlyRevenue = await _context.UserSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.Status == "active" &&
                       s.CurrentPeriodEnd > DateTime.UtcNow &&
                       s.BillingInterval == "month")
            .SumAsync(s => s.Plan.PriceMonthly);

        var yearlyRevenue = await _context.UserSubscriptions
            .Include(s => s.Plan)
            .Where(s => s.Status == "active" &&
                       s.CurrentPeriodEnd > DateTime.UtcNow &&
                       s.BillingInterval == "year")
            .SumAsync(s => (s.Plan.PriceYearly ?? s.Plan.PriceMonthly * 12) / 12);

        return monthlyRevenue + yearlyRevenue;
    }

    public async Task<int> GetChurnCountAsync(int month, int year)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        return await _context.UserSubscriptions
            .Where(s => s.Status == "cancelled" &&
                       s.CancelledAt >= startDate &&
                       s.CancelledAt < endDate)
            .CountAsync();
    }
}
