using InsightLearn.Core.DTOs.Payout;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services.Payout;

public class PayoutService : IPayoutService
{
    private readonly IAuthHttpClient _httpClient;
    private readonly ILogger<PayoutService> _logger;

    public PayoutService(IAuthHttpClient httpClient, ILogger<PayoutService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PayoutHistoryResponse?> GetMyPayoutHistoryAsync(int page = 1, int pageSize = 12)
    {
        try
        {
            var result = await _httpClient.GetAsync<PayoutHistoryResponse>($"api/payouts/my-history?page={page}&pageSize={pageSize}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payout history");
            return null;
        }
    }

    public async Task<InstructorAnalyticsDto?> GetMyAnalyticsAsync()
    {
        try
        {
            return await _httpClient.GetAsync<InstructorAnalyticsDto>("api/payouts/my-analytics");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting analytics");
            return null;
        }
    }

    public async Task<decimal> GetLifetimeEarningsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync<LifetimeEarningsResponse>("api/payouts/lifetime-earnings");
            return response?.TotalEarnings ?? 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lifetime earnings");
            return 0m;
        }
    }

    public async Task<decimal> GetPendingPayoutAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync<PendingPayoutResponse>("api/payouts/pending");
            return response?.PendingAmount ?? 0m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending payout");
            return 0m;
        }
    }

    public async Task<PayoutHistoryResponse?> GetAllPayoutsAsync(string? status = null, int page = 1, int pageSize = 12)
    {
        try
        {
            var url = $"api/payouts/all?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(status))
            {
                url += $"&status={status}";
            }

            return await _httpClient.GetAsync<PayoutHistoryResponse>(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all payouts");
            return null;
        }
    }

    public async Task<PayoutSummaryDto?> GetPayoutSummaryAsync(DateTime periodStart, DateTime periodEnd)
    {
        try
        {
            var url = $"api/payouts/summary?periodStart={periodStart:yyyy-MM-dd}&periodEnd={periodEnd:yyyy-MM-dd}";
            return await _httpClient.GetAsync<PayoutSummaryDto>(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payout summary");
            return null;
        }
    }

    public async Task<bool> CalculatePayoutsAsync(DateTime periodStart, DateTime periodEnd)
    {
        try
        {
            var request = new { periodStart, periodEnd };
            var result = await _httpClient.PostAsync<object>("api/payouts/calculate", request);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating payouts");
            return false;
        }
    }

    public async Task<bool> ProcessPendingPayoutsAsync()
    {
        try
        {
            var result = await _httpClient.PostAsync<object>("api/payouts/process-pending", null);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending payouts");
            return false;
        }
    }

    public async Task<bool> ExecutePayoutAsync(Guid payoutId)
    {
        try
        {
            var result = await _httpClient.PostAsync<object>($"api/payouts/{payoutId}/execute", null);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing payout {payoutId}");
            return false;
        }
    }

    // Helper DTOs for API responses
    private class LifetimeEarningsResponse
    {
        public decimal TotalEarnings { get; set; }
    }

    private class PendingPayoutResponse
    {
        public decimal PendingAmount { get; set; }
    }
}
