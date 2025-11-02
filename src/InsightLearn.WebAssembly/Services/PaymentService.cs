using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services;

public class PaymentService : IPaymentService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;

    public PaymentService(IApiClient apiClient, EndpointsConfig endpoints)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
    }

    public async Task<ApiResponse<PaymentIntent>> CreatePaymentIntentAsync(Guid courseId, decimal amount)
    {
        return await _apiClient.PostAsync<PaymentIntent>("payments/create-intent", new
        {
            CourseId = courseId,
            Amount = amount
        });
    }

    public async Task<ApiResponse> ConfirmPaymentAsync(string paymentIntentId)
    {
        return await _apiClient.PostAsync("payments/confirm", new { PaymentIntentId = paymentIntentId });
    }

    public async Task<ApiResponse<List<PaymentHistory>>> GetPaymentHistoryAsync()
    {
        return await _apiClient.GetAsync<List<PaymentHistory>>("payments/history");
    }
}
