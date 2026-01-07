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
        _logger.LogDebug("Fetching payout history (page: {Page}, pageSize: {PageSize})", page, pageSize);
        try
        {
            var result = await _httpClient.GetAsync<PayoutHistoryResponse>($"api/payouts/my-history?page={page}&pageSize={pageSize}");

            if (result != null)
            {
                _logger.LogInformation("Retrieved payout history (page {Page}, {PayoutCount} payouts)",
                    page, result.Payouts?.Count ?? 0);
            }
            else
            {
                _logger.LogWarning("No payout history found (page {Page})", page);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payout history (page {Page}): {ErrorMessage}",
                page, ex.Message);
            return null;
        }
    }

    public async Task<InstructorAnalyticsDto?> GetMyAnalyticsAsync()
    {
        _logger.LogDebug("Fetching instructor analytics");
        try
        {
            var analytics = await _httpClient.GetAsync<InstructorAnalyticsDto>("api/payouts/my-analytics");

            if (analytics != null)
            {
                _logger.LogInformation("Retrieved instructor analytics successfully");
            }
            else
            {
                _logger.LogWarning("No analytics data available");
            }

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting instructor analytics: {ErrorMessage}", ex.Message);
            return null;
        }
    }

    public async Task<decimal> GetLifetimeEarningsAsync()
    {
        _logger.LogDebug("Fetching lifetime earnings");
        try
        {
            var response = await _httpClient.GetAsync<LifetimeEarningsResponse>("api/payouts/lifetime-earnings");
            var earnings = response?.TotalEarnings ?? 0m;

            _logger.LogInformation("Retrieved lifetime earnings: {Earnings:C}", earnings);
            return earnings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lifetime earnings: {ErrorMessage}", ex.Message);
            return 0m;
        }
    }

    public async Task<decimal> GetPendingPayoutAsync()
    {
        _logger.LogDebug("Fetching pending payout amount");
        try
        {
            var response = await _httpClient.GetAsync<PendingPayoutResponse>("api/payouts/pending");
            var amount = response?.PendingAmount ?? 0m;

            _logger.LogInformation("Retrieved pending payout amount: {Amount:C}", amount);
            return amount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending payout: {ErrorMessage}", ex.Message);
            return 0m;
        }
    }

    public async Task<PayoutHistoryResponse?> GetAllPayoutsAsync(string? status = null, int page = 1, int pageSize = 12)
    {
        _logger.LogDebug("Fetching all payouts (status: {Status}, page: {Page}, pageSize: {PageSize})",
            status ?? "All", page, pageSize);
        try
        {
            var url = $"api/payouts/all?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(status))
            {
                url += $"&status={status}";
            }

            var result = await _httpClient.GetAsync<PayoutHistoryResponse>(url);

            if (result != null)
            {
                _logger.LogInformation("Retrieved {PayoutCount} payouts (status: {Status}, page {Page})",
                    result.Payouts?.Count ?? 0, status ?? "All", page);
            }
            else
            {
                _logger.LogWarning("No payouts found (status: {Status}, page {Page})", status ?? "All", page);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all payouts (status: {Status}, page {Page}): {ErrorMessage}",
                status ?? "All", page, ex.Message);
            return null;
        }
    }

    public async Task<PayoutSummaryDto?> GetPayoutSummaryAsync(DateTime periodStart, DateTime periodEnd)
    {
        _logger.LogDebug("Fetching payout summary (period: {PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd})",
            periodStart, periodEnd);
        try
        {
            var url = $"api/payouts/summary?periodStart={periodStart:yyyy-MM-dd}&periodEnd={periodEnd:yyyy-MM-dd}";
            var summary = await _httpClient.GetAsync<PayoutSummaryDto>(url);

            if (summary != null)
            {
                _logger.LogInformation("Retrieved payout summary for period {PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}",
                    periodStart, periodEnd);
            }
            else
            {
                _logger.LogWarning("No payout summary available for period {PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}",
                    periodStart, periodEnd);
            }

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payout summary (period {PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}): {ErrorMessage}",
                periodStart, periodEnd, ex.Message);
            return null;
        }
    }

    public async Task<bool> CalculatePayoutsAsync(DateTime periodStart, DateTime periodEnd)
    {
        _logger.LogInformation("Calculating payouts for period {PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}",
            periodStart, periodEnd);
        try
        {
            var request = new { periodStart, periodEnd };
            var result = await _httpClient.PostAsync<object>("api/payouts/calculate", request);
            var success = result != null;

            if (success)
            {
                _logger.LogInformation("Payout calculation completed successfully for period {PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}",
                    periodStart, periodEnd);
            }
            else
            {
                _logger.LogWarning("Payout calculation failed for period {PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}",
                    periodStart, periodEnd);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating payouts for period {PeriodStart:yyyy-MM-dd} to {PeriodEnd:yyyy-MM-dd}: {ErrorMessage}",
                periodStart, periodEnd, ex.Message);
            return false;
        }
    }

    public async Task<bool> ProcessPendingPayoutsAsync()
    {
        _logger.LogInformation("Processing pending payouts");
        try
        {
            var result = await _httpClient.PostAsync<object>("api/payouts/process-pending", null);
            var success = result != null;

            if (success)
            {
                _logger.LogInformation("Pending payouts processed successfully");
            }
            else
            {
                _logger.LogWarning("Failed to process pending payouts");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending payouts: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    public async Task<bool> ExecutePayoutAsync(Guid payoutId)
    {
        _logger.LogInformation("Executing payout {PayoutId}", payoutId);
        try
        {
            var result = await _httpClient.PostAsync<object>($"api/payouts/{payoutId}/execute", null);
            var success = result != null;

            if (success)
            {
                _logger.LogInformation("Payout {PayoutId} executed successfully", payoutId);
            }
            else
            {
                _logger.LogWarning("Failed to execute payout {PayoutId}", payoutId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing payout {PayoutId}: {ErrorMessage}",
                payoutId, ex.Message);
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
