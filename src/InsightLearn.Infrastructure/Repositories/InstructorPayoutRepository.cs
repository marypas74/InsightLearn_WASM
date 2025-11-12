using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

public class InstructorPayoutRepository : IInstructorPayoutRepository
{
    private readonly InsightLearnDbContext _context;

    public InstructorPayoutRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    public async Task<InstructorPayout?> GetByIdAsync(Guid id)
    {
        return await _context.InstructorPayouts
            .Include(p => p.Instructor)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<InstructorPayout?> GetByInstructorAndPeriodAsync(Guid instructorId, int month, int year)
    {
        return await _context.InstructorPayouts
            .Include(p => p.Instructor)
            .FirstOrDefaultAsync(p => p.InstructorId == instructorId &&
                                     p.Month == month &&
                                     p.Year == year);
    }

    public async Task<List<InstructorPayout>> GetByInstructorIdAsync(Guid instructorId, int page = 1, int pageSize = 50)
    {
        return await _context.InstructorPayouts
            .Where(p => p.InstructorId == instructorId)
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<InstructorPayout>> GetPendingPayoutsAsync()
    {
        return await _context.InstructorPayouts
            .Include(p => p.Instructor)
            .Where(p => p.Status == "pending")
            .OrderBy(p => p.Year)
            .ThenBy(p => p.Month)
            .ToListAsync();
    }

    public async Task<List<InstructorPayout>> GetPayoutsByPeriodAsync(int month, int year)
    {
        return await _context.InstructorPayouts
            .Include(p => p.Instructor)
            .Where(p => p.Month == month && p.Year == year)
            .OrderByDescending(p => p.PayoutAmount)
            .ToListAsync();
    }

    public async Task<InstructorPayout> CreateAsync(InstructorPayout payout)
    {
        payout.CreatedAt = DateTime.UtcNow;
        payout.UpdatedAt = DateTime.UtcNow;

        _context.InstructorPayouts.Add(payout);
        await _context.SaveChangesAsync();
        return payout;
    }

    public async Task<InstructorPayout> UpdateAsync(InstructorPayout payout)
    {
        payout.UpdatedAt = DateTime.UtcNow;

        _context.InstructorPayouts.Update(payout);
        await _context.SaveChangesAsync();
        return payout;
    }

    public async Task<bool> MarkAsPaidAsync(Guid id, string stripeTransferId)
    {
        var payout = await GetByIdAsync(id);
        if (payout == null)
            return false;

        payout.Status = "paid";
        payout.StripeTransferId = stripeTransferId;
        payout.PaidAt = DateTime.UtcNow;
        payout.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAsFailedAsync(Guid id, string errorMessage)
    {
        var payout = await GetByIdAsync(id);
        if (payout == null)
            return false;

        payout.Status = "failed";
        payout.ErrorMessage = errorMessage;
        payout.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> GetTotalPaidOutAsync(Guid instructorId)
    {
        return await _context.InstructorPayouts
            .Where(p => p.InstructorId == instructorId && p.Status == "paid")
            .SumAsync(p => p.PayoutAmount);
    }

    public async Task<decimal> GetMonthlyPlatformRevenueAsync(int month, int year)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        // Get all subscription revenues for the period
        var revenue = await _context.SubscriptionRevenues
            .Where(r => r.Status == "paid" &&
                       r.PaidAt >= startDate &&
                       r.PaidAt < endDate)
            .SumAsync(r => r.Amount);

        return revenue;
    }
}
