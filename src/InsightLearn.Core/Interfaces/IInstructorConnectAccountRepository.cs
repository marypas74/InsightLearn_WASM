using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

public interface IInstructorConnectAccountRepository
{
    Task<InstructorConnectAccount?> GetByIdAsync(Guid id);
    Task<InstructorConnectAccount?> GetByInstructorIdAsync(Guid instructorId);
    Task<InstructorConnectAccount?> GetByStripeAccountIdAsync(string stripeAccountId);
    Task<InstructorConnectAccount> CreateAsync(InstructorConnectAccount account);
    Task<InstructorConnectAccount> UpdateAsync(InstructorConnectAccount account);
    Task<bool> UpdateOnboardingStatusAsync(Guid id, string status, bool payoutsEnabled, bool chargesEnabled);
    Task<bool> UpdatePayoutTotalAsync(Guid id, decimal amount);
    Task<List<InstructorConnectAccount>> GetPendingOnboardingAsync();
    Task<List<InstructorConnectAccount>> GetActiveAccountsAsync();
    Task<bool> DisableAccountAsync(Guid id, string reason);
}
