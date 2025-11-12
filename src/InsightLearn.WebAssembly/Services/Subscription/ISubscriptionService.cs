using InsightLearn.Core.Entities;

namespace InsightLearn.WebAssembly.Services.Subscription;

/// <summary>
/// Service for managing user subscriptions (frontend)
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Get all available subscription plans
    /// </summary>
    Task<List<SubscriptionPlan>> GetSubscriptionPlansAsync();

    /// <summary>
    /// Get subscription plan by ID
    /// </summary>
    Task<SubscriptionPlan?> GetSubscriptionPlanByIdAsync(Guid planId);

    /// <summary>
    /// Get current user's active subscription
    /// </summary>
    Task<UserSubscription?> GetMyActiveSubscriptionAsync();

    /// <summary>
    /// Subscribe to a plan
    /// </summary>
    Task<SubscribeResponse> SubscribeAsync(Guid planId, string billingInterval);

    /// <summary>
    /// Cancel active subscription
    /// </summary>
    Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string? reason = null, string? feedback = null);

    /// <summary>
    /// Reactivate cancelled subscription
    /// </summary>
    Task<bool> ReactivateSubscriptionAsync(Guid subscriptionId);

    /// <summary>
    /// Get MRR analytics (Admin only)
    /// </summary>
    Task<decimal> GetMonthlyRecurringRevenueAsync();

    /// <summary>
    /// Get active subscription count (Admin only)
    /// </summary>
    Task<int> GetActiveSubscriptionCountAsync();
}

/// <summary>
/// Response from subscribe endpoint
/// </summary>
public class SubscribeResponse
{
    public bool Success { get; set; }
    public Guid SubscriptionId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? StripeCheckoutUrl { get; set; }
}
