using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

public interface ISubscriptionPlanRepository
{
    Task<SubscriptionPlan?> GetByIdAsync(Guid id);
    Task<SubscriptionPlan?> GetByNameAsync(string name);
    Task<List<SubscriptionPlan>> GetAllActiveAsync();
    Task<List<SubscriptionPlan>> GetAllAsync(bool includeInactive = false);
    Task<SubscriptionPlan> CreateAsync(SubscriptionPlan plan);
    Task<SubscriptionPlan> UpdateAsync(SubscriptionPlan plan);
    Task<bool> DeleteAsync(Guid id);
    Task<SubscriptionPlan?> GetByStripePriceIdAsync(string stripePriceId);
}
