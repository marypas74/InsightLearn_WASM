using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

public class SubscriptionRevenueRepository : ISubscriptionRevenueRepository
{
    private readonly InsightLearnDbContext _context;

    public SubscriptionRevenueRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionRevenue?> GetByIdAsync(Guid id)
    {
        return await _context.SubscriptionRevenues
            .Include(r => r.Subscription)
                .ThenInclude(s => s.User)
            .Include(r => r.Subscription)
                .ThenInclude(s => s.Plan)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<SubscriptionRevenue?> GetByStripeInvoiceIdAsync(string stripeInvoiceId)
    {
        return await _context.SubscriptionRevenues
            .Include(r => r.Subscription)
            .FirstOrDefaultAsync(r => r.StripeInvoiceId == stripeInvoiceId);
    }

    public async Task<List<SubscriptionRevenue>> GetBySubscriptionIdAsync(Guid subscriptionId)
    {
        return await _context.SubscriptionRevenues
            .Where(r => r.SubscriptionId == subscriptionId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<SubscriptionRevenue> CreateAsync(SubscriptionRevenue revenue)
    {
        revenue.CreatedAt = DateTime.UtcNow;

        _context.SubscriptionRevenues.Add(revenue);
        await _context.SaveChangesAsync();
        return revenue;
    }

    public async Task<SubscriptionRevenue> UpdateAsync(SubscriptionRevenue revenue)
    {
        _context.SubscriptionRevenues.Update(revenue);
        await _context.SaveChangesAsync();
        return revenue;
    }

    public async Task<bool> MarkAsPaidAsync(Guid id)
    {
        var revenue = await GetByIdAsync(id);
        if (revenue == null)
            return false;

        revenue.Status = "paid";
        revenue.PaidAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAsFailedAsync(Guid id, string failureReason)
    {
        var revenue = await GetByIdAsync(id);
        if (revenue == null)
            return false;

        revenue.Status = "failed";
        revenue.FailedAt = DateTime.UtcNow;
        revenue.FailureReason = failureReason;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAsRefundedAsync(Guid id, decimal? refundAmount = null, string? refundReason = null)
    {
        var revenue = await GetByIdAsync(id);
        if (revenue == null)
            return false;

        revenue.Status = "refunded";
        revenue.RefundedAt = DateTime.UtcNow;
        revenue.RefundAmount = refundAmount ?? revenue.Amount;
        revenue.RefundReason = refundReason;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.SubscriptionRevenues
            .Where(r => r.Status == "paid" &&
                       r.PaidAt >= startDate &&
                       r.PaidAt < endDate)
            .SumAsync(r => r.Amount);
    }

    public async Task<decimal> GetRevenueByPlanAsync(Guid planId, DateTime startDate, DateTime endDate)
    {
        return await _context.SubscriptionRevenues
            .Include(r => r.Subscription)
            .Where(r => r.Status == "paid" &&
                       r.Subscription.PlanId == planId &&
                       r.PaidAt >= startDate &&
                       r.PaidAt < endDate)
            .SumAsync(r => r.Amount);
    }

    public async Task<List<SubscriptionRevenue>> GetFailedPaymentsAsync(int days = 7)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);

        return await _context.SubscriptionRevenues
            .Include(r => r.Subscription)
                .ThenInclude(s => s.User)
            .Include(r => r.Subscription)
                .ThenInclude(s => s.Plan)
            .Where(r => r.Status == "failed" &&
                       r.FailedAt >= cutoffDate)
            .OrderByDescending(r => r.FailedAt)
            .ToListAsync();
    }

    public async Task<int> GetSuccessfulPaymentCountAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.SubscriptionRevenues
            .Where(r => r.Status == "paid" &&
                       r.PaidAt >= startDate &&
                       r.PaidAt < endDate)
            .CountAsync();
    }
}
