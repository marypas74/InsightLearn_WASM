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
        try
        {
            var plans = await _httpClient.GetAsync<List<SubscriptionPlan>>("api/subscriptions/plans");
            return plans ?? new List<SubscriptionPlan>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription plans");
            return new List<SubscriptionPlan>();
        }
    }

    public async Task<SubscriptionPlan?> GetSubscriptionPlanByIdAsync(Guid planId)
    {
        try
        {
            return await _httpClient.GetAsync<SubscriptionPlan>($"api/subscriptions/plans/{planId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting subscription plan {planId}");
            return null;
        }
    }

    public async Task<UserSubscription?> GetMyActiveSubscriptionAsync()
    {
        try
        {
            return await _httpClient.GetAsync<UserSubscription>("api/subscriptions/my-subscription");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active subscription");
            return null;
        }
    }

    public async Task<SubscribeResponse> SubscribeAsync(Guid planId, string billingInterval)
    {
        try
        {
            var request = new { planId, billingInterval };
            var result = await _httpClient.PostAsync<SubscribeResponse>("api/subscriptions/subscribe", request);

            return result ?? new SubscribeResponse
            {
                Success = false,
                Message = "Failed to subscribe"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to plan");
            return new SubscribeResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string? reason = null, string? feedback = null)
    {
        try
        {
            var request = new { reason, feedback };
            var result = await _httpClient.PostAsync<object>($"api/subscriptions/{subscriptionId}/cancel", request);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error cancelling subscription {subscriptionId}");
            return false;
        }
    }

    public async Task<bool> ReactivateSubscriptionAsync(Guid subscriptionId)
    {
        try
        {
            var result = await _httpClient.PostAsync<object>($"api/subscriptions/{subscriptionId}/reactivate", null);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reactivating subscription {subscriptionId}");
            return false;
        }
    }

    public async Task<decimal> GetMonthlyRecurringRevenueAsync()
    {
        try
        {
            var result = await _httpClient.GetAsync<MrrResponse>("api/subscriptions/analytics/mrr");
            return result?.Mrr ?? 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting MRR");
            return 0m;
        }
    }

    public async Task<int> GetActiveSubscriptionCountAsync()
    {
        try
        {
            var result = await _httpClient.GetAsync<ActiveCountResponse>("api/subscriptions/analytics/active-count");
            return result?.ActiveCount ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active subscription count");
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
