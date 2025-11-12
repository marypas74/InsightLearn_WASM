using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

public interface ISubscriptionService
{
    // Subscription Plans
    Task<List<SubscriptionPlan>> GetActiveSubscriptionPlansAsync();
    Task<SubscriptionPlan?> GetSubscriptionPlanByIdAsync(Guid planId);

    // User Subscriptions
    Task<UserSubscription?> GetActiveSubscriptionAsync(Guid userId);
    Task<UserSubscription?> CreateSubscriptionAsync(Guid userId, Guid planId, string billingInterval, string? stripeSubscriptionId = null);
    Task<UserSubscription?> UpdateSubscriptionStatusAsync(string stripeSubscriptionId, string status);
    Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string? reason = null, string? feedback = null);
    Task<bool> ReactivateSubscriptionAsync(Guid subscriptionId);

    // Subscription Access Control
    Task<bool> HasActiveSubscriptionAsync(Guid userId);
    Task<bool> CanAccessCourseAsync(Guid userId, Guid courseId);
    Task AutoEnrollSubscriberAsync(Guid userId, Guid subscriptionId);

    // Analytics
    Task<decimal> GetMonthlyRecurringRevenueAsync();
    Task<int> GetActiveSubscriptionCountAsync();
    Task<int> GetChurnRateAsync(int month, int year);
    Task<List<UserSubscription>> GetExpiringSubscriptionsAsync(int daysBeforeExpiry);

    // Stripe Webhooks Support
    Task HandleSubscriptionCreatedAsync(string stripeSubscriptionId, Guid userId, Guid planId);
    Task HandleSubscriptionUpdatedAsync(string stripeSubscriptionId, DateTime currentPeriodEnd, string status);
    Task HandleSubscriptionCancelledAsync(string stripeSubscriptionId);
    Task HandleInvoicePaidAsync(string stripeInvoiceId, string stripeSubscriptionId, decimal amount);
    Task HandleInvoicePaymentFailedAsync(string stripeInvoiceId, string stripeSubscriptionId, string failureReason);
}
