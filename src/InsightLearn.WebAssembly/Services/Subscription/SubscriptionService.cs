using InsightLearn.Core.Entities;
using InsightLearn.WebAssembly.Services.Http;
using System.Net.Http.Json;

namespace InsightLearn.WebAssembly.Services.Subscription;

public class SubscriptionService : ISubscriptionService
{
    private readonly IAuthHttpClient _httpClient;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(IAuthHttpClient httpClient, ILogger<SubscriptionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<SubscriptionPlan>> GetSubscriptionPlansAsync()
    {
        _logger.LogDebug("Fetching subscription plans");
        try
        {
            var plans = await _httpClient.GetAsync<List<SubscriptionPlan>>("api/subscriptions/plans");
            var result = plans ?? new List<SubscriptionPlan>();

            _logger.LogInformation("Retrieved {PlanCount} subscription plans", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans: {ErrorMessage}", ex.Message);
            return new List<SubscriptionPlan>();
        }
    }

    public async Task<SubscriptionPlan?> GetSubscriptionPlanByIdAsync(Guid planId)
    {
        _logger.LogDebug("Fetching subscription plan: {PlanId}", planId);
        try
        {
            var plan = await _httpClient.GetAsync<SubscriptionPlan>($"api/subscriptions/plans/{planId}");

            if (plan != null)
            {
                _logger.LogInformation("Retrieved subscription plan {PlanId} ({PlanName})",
                    planId, plan.Name ?? "Unknown");
            }
            else
            {
                _logger.LogWarning("Subscription plan {PlanId} not found", planId);
            }

            return plan;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plan {PlanId}: {ErrorMessage}",
                planId, ex.Message);
            return null;
        }
    }

    public async Task<UserSubscription?> GetMyActiveSubscriptionAsync()
    {
        _logger.LogDebug("Fetching user's active subscription");
        try
        {
            var subscription = await _httpClient.GetAsync<UserSubscription>("api/subscriptions/my-subscription");

            if (subscription != null)
            {
                _logger.LogInformation("Retrieved active subscription (plan: {PlanId}, status: {Status})",
                    subscription.PlanId, subscription.Status);
            }
            else
            {
                _logger.LogDebug("No active subscription found for user");
            }

            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active subscription: {ErrorMessage}", ex.Message);
            return null;
        }
    }

    public async Task<SubscribeResponse> SubscribeAsync(Guid planId, string billingInterval)
    {
        _logger.LogInformation("User subscribing to plan {PlanId} with {BillingInterval} billing",
            planId, billingInterval);
        try
        {
            var request = new { planId, billingInterval };
            var result = await _httpClient.PostAsync<SubscribeResponse>("api/subscriptions/subscribe", request);

            if (result?.Success == true)
            {
                _logger.LogInformation("User successfully subscribed to plan {PlanId} (subscription: {SubscriptionId})",
                    planId, result.SubscriptionId);
            }
            else
            {
                _logger.LogWarning("Failed to subscribe to plan {PlanId}: {ErrorMessage}",
                    planId, result?.Message ?? "Unknown error");
            }

            return result ?? new SubscribeResponse
            {
                Success = false,
                Message = "Failed to subscribe"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to plan {PlanId}: {ErrorMessage}",
                planId, ex.Message);
            return new SubscribeResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string? reason = null, string? feedback = null)
    {
        _logger.LogWarning("Cancelling subscription {SubscriptionId} (reason: {Reason})",
            subscriptionId, reason ?? "Not provided");
        try
        {
            var request = new { reason, feedback };
            var result = await _httpClient.PostAsync<object>($"api/subscriptions/{subscriptionId}/cancel", request);
            var success = result != null;

            if (success)
            {
                _logger.LogInformation("Subscription {SubscriptionId} cancelled successfully", subscriptionId);
            }
            else
            {
                _logger.LogError("Failed to cancel subscription {SubscriptionId}", subscriptionId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription {SubscriptionId}: {ErrorMessage}",
                subscriptionId, ex.Message);
            return false;
        }
    }

    public async Task<bool> ReactivateSubscriptionAsync(Guid subscriptionId)
    {
        _logger.LogInformation("Reactivating subscription {SubscriptionId}", subscriptionId);
        try
        {
            var result = await _httpClient.PostAsync<object>($"api/subscriptions/{subscriptionId}/reactivate", null);
            var success = result != null;

            if (success)
            {
                _logger.LogInformation("Subscription {SubscriptionId} reactivated successfully", subscriptionId);
            }
            else
            {
                _logger.LogError("Failed to reactivate subscription {SubscriptionId}", subscriptionId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating subscription {SubscriptionId}: {ErrorMessage}",
                subscriptionId, ex.Message);
            return false;
        }
    }

    public async Task<decimal> GetMonthlyRecurringRevenueAsync()
    {
        _logger.LogDebug("Fetching Monthly Recurring Revenue (MRR)");
        try
        {
            var result = await _httpClient.GetAsync<MrrResponse>("api/subscriptions/analytics/mrr");
            var mrr = result?.Mrr ?? 0m;

            _logger.LogInformation("Retrieved MRR: {Mrr:C}", mrr);
            return mrr;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MRR: {ErrorMessage}", ex.Message);
            return 0m;
        }
    }

    public async Task<int> GetActiveSubscriptionCountAsync()
    {
        _logger.LogDebug("Fetching active subscription count");
        try
        {
            var result = await _httpClient.GetAsync<ActiveCountResponse>("api/subscriptions/analytics/active-count");
            var count = result?.ActiveCount ?? 0;

            _logger.LogInformation("Retrieved active subscription count: {ActiveCount}", count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active subscription count: {ErrorMessage}", ex.Message);
            return 0;
        }
    }

    // Helper DTOs for API responses
    private class MrrResponse
    {
        public decimal Mrr { get; set; }
    }

    private class ActiveCountResponse
    {
        public int ActiveCount { get; set; }
    }
}
