using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

public interface ISubscriptionRevenueRepository
{
    Task<SubscriptionRevenue?> GetByIdAsync(Guid id);
    Task<SubscriptionRevenue?> GetByStripeInvoiceIdAsync(string stripeInvoiceId);
    Task<List<SubscriptionRevenue>> GetBySubscriptionIdAsync(Guid subscriptionId);
    Task<SubscriptionRevenue> CreateAsync(SubscriptionRevenue revenue);
    Task<SubscriptionRevenue> UpdateAsync(SubscriptionRevenue revenue);
    Task<bool> MarkAsPaidAsync(Guid id);
    Task<bool> MarkAsFailedAsync(Guid id, string failureReason);
    Task<bool> MarkAsRefundedAsync(Guid id, decimal? refundAmount = null, string? refundReason = null);
    Task<decimal> GetTotalRevenueAsync(DateTime startDate, DateTime endDate);
    Task<decimal> GetRevenueByPlanAsync(Guid planId, DateTime startDate, DateTime endDate);
    Task<List<SubscriptionRevenue>> GetFailedPaymentsAsync(int days = 7);
    Task<int> GetSuccessfulPaymentCountAsync(DateTime startDate, DateTime endDate);
}
