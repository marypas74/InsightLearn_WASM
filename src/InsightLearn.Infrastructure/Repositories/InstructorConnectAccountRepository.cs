using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InsightLearn.Infrastructure.Repositories;

public class InstructorConnectAccountRepository : IInstructorConnectAccountRepository
{
    private readonly InsightLearnDbContext _context;

    public InstructorConnectAccountRepository(InsightLearnDbContext context)
    {
        _context = context;
    }

    public async Task<InstructorConnectAccount?> GetByIdAsync(Guid id)
    {
        return await _context.InstructorConnectAccounts
            .Include(a => a.Instructor)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<InstructorConnectAccount?> GetByInstructorIdAsync(Guid instructorId)
    {
        return await _context.InstructorConnectAccounts
            .Include(a => a.Instructor)
            .FirstOrDefaultAsync(a => a.InstructorId == instructorId);
    }

    public async Task<InstructorConnectAccount?> GetByStripeAccountIdAsync(string stripeAccountId)
    {
        return await _context.InstructorConnectAccounts
            .Include(a => a.Instructor)
            .FirstOrDefaultAsync(a => a.StripeAccountId == stripeAccountId);
    }

    public async Task<InstructorConnectAccount> CreateAsync(InstructorConnectAccount account)
    {
        account.CreatedAt = DateTime.UtcNow;
        account.UpdatedAt = DateTime.UtcNow;

        _context.InstructorConnectAccounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task<InstructorConnectAccount> UpdateAsync(InstructorConnectAccount account)
    {
        account.UpdatedAt = DateTime.UtcNow;

        _context.InstructorConnectAccounts.Update(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task<bool> UpdateOnboardingStatusAsync(Guid id, string status, bool payoutsEnabled, bool chargesEnabled)
    {
        var account = await GetByIdAsync(id);
        if (account == null)
            return false;

        account.OnboardingStatus = status;
        account.PayoutsEnabled = payoutsEnabled;
        account.ChargesEnabled = chargesEnabled;
        account.UpdatedAt = DateTime.UtcNow;

        if (status == "complete" && payoutsEnabled && chargesEnabled)
        {
            account.OnboardingCompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<InstructorConnectAccount>> GetPendingOnboardingAsync()
    {
        return await _context.InstructorConnectAccounts
            .Include(a => a.Instructor)
            .Where(a => a.OnboardingStatus != "complete" || !a.PayoutsEnabled)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> UpdatePayoutTotalAsync(Guid id, decimal amount)
    {
        var account = await GetByIdAsync(id);
        if (account == null)
            return false;

        account.TotalPaidOut += amount;
        account.LastPayoutAt = DateTime.UtcNow;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<InstructorConnectAccount>> GetActiveAccountsAsync()
    {
        return await _context.InstructorConnectAccounts
            .Include(a => a.Instructor)
            .Where(a => a.OnboardingStatus == "complete" &&
                       a.PayoutsEnabled &&
                       a.ChargesEnabled &&
                       a.DisabledAt == null)
            .OrderByDescending(a => a.TotalPaidOut)
            .ToListAsync();
    }

    public async Task<bool> DisableAccountAsync(Guid id, string reason)
    {
        var account = await GetByIdAsync(id);
        if (account == null)
            return false;

        account.PayoutsEnabled = false;
        account.ChargesEnabled = false;
        account.DisabledAt = DateTime.UtcNow;
        account.DisabledReason = reason;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}
