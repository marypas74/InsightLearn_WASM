using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

public interface IUserSubscriptionRepository
{
    Task<UserSubscription?> GetByIdAsync(Guid id);
    Task<UserSubscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId);
    Task<UserSubscription?> GetActiveByUserIdAsync(Guid userId);
    Task<List<UserSubscription>> GetByUserIdAsync(Guid userId, bool includeInactive = false);
    Task<UserSubscription> CreateAsync(UserSubscription subscription);
    Task<UserSubscription> UpdateAsync(UserSubscription subscription);
    Task<bool> CancelAsync(Guid id, string? reason = null, string? feedback = null);
    Task<int> GetActiveSubscriptionCountAsync();
    Task<List<UserSubscription>> GetExpiringSubscriptionsAsync(int daysBeforeExpiry);
    Task<List<UserSubscription>> GetSubscriptionsByStatusAsync(string status, int page = 1, int pageSize = 50);
    Task<decimal> GetMonthlyRecurringRevenueAsync();
    Task<int> GetChurnCountAsync(int month, int year);
}
