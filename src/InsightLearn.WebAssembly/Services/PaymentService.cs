using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services;

public class PaymentService : IPaymentService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(IApiClient apiClient, EndpointsConfig endpoints, ILogger<PaymentService> logger)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
        _logger = logger;
    }

    public async Task<ApiResponse<PaymentIntent>> CreatePaymentIntentAsync(Guid courseId, decimal amount)
    {
        _logger.LogInformation("Creating payment intent for course {CourseId} with amount {Amount:C}",
            courseId, amount);
        var response = await _apiClient.PostAsync<PaymentIntent>("payments/create-intent", new
        {
            CourseId = courseId,
            Amount = amount
        });

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Payment intent created successfully for course {CourseId} (Intent ID: {IntentId})",
                courseId, response.Data.PaymentId);
        }
        else
        {
            _logger.LogError("Failed to create payment intent for course {CourseId}: {ErrorMessage}",
                courseId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse> ConfirmPaymentAsync(string paymentIntentId)
    {
        _logger.LogInformation("Confirming payment intent: {PaymentIntentId}", paymentIntentId);
        var response = await _apiClient.PostAsync("payments/confirm", new { PaymentIntentId = paymentIntentId });

        if (response.Success)
        {
            _logger.LogInformation("Payment confirmed successfully: {PaymentIntentId}", paymentIntentId);
        }
        else
        {
            _logger.LogError("Failed to confirm payment {PaymentIntentId}: {ErrorMessage}",
                paymentIntentId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<List<PaymentHistory>>> GetPaymentHistoryAsync()
    {
        _logger.LogDebug("Fetching payment history");
        var response = await _apiClient.GetAsync<List<PaymentHistory>>("payments/history");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved {PaymentCount} payment records", response.Data.Count);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve payment history: {ErrorMessage}",
                response.Message ?? "Unknown error");
        }

        return response;
    }
}
