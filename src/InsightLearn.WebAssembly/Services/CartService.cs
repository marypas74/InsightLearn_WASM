using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services;

/// <summary>
/// Frontend service implementation for shopping cart operations
/// Communicates with backend Cart API endpoints
/// </summary>
public class CartService : ICartService
{
    private readonly IApiClient _apiClient;

    public event Action? OnCartUpdated;

    public CartService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponse<CartModel>> GetCartAsync()
    {
        return await _apiClient.GetAsync<CartModel>("cart");
    }

    public async Task<ApiResponse<CartCountModel>> GetCartCountAsync()
    {
        return await _apiClient.GetAsync<CartCountModel>("cart/count");
    }

    public async Task<ApiResponse<CartModel>> AddToCartAsync(Guid courseId, string? couponCode = null)
    {
        var request = new AddToCartRequest
        {
            CourseId = courseId,
            CouponCode = couponCode
        };

        var result = await _apiClient.PostAsync<CartModel>("cart/add", request);

        if (result.Success)
        {
            NotifyCartUpdated();
        }

        return result;
    }

    public async Task<ApiResponse<CartModel>> RemoveFromCartAsync(Guid courseId)
    {
        var result = await _apiClient.DeleteAsync<CartModel>($"cart/{courseId}");

        if (result.Success)
        {
            NotifyCartUpdated();
        }

        return result;
    }

    public async Task<ApiResponse> ClearCartAsync()
    {
        var result = await _apiClient.DeleteAsync("cart");

        if (result.Success)
        {
            NotifyCartUpdated();
        }

        return result;
    }

    public async Task<ApiResponse<CouponResultModel>> ApplyCouponAsync(string couponCode)
    {
        var request = new ApplyCouponRequest { CouponCode = couponCode };
        var result = await _apiClient.PostAsync<CouponResultModel>("cart/coupon", request);

        if (result.Success)
        {
            NotifyCartUpdated();
        }

        return result;
    }

    public async Task<ApiResponse<CartModel>> RemoveCouponAsync()
    {
        var result = await _apiClient.DeleteAsync<CartModel>("cart/coupon");

        if (result.Success)
        {
            NotifyCartUpdated();
        }

        return result;
    }

    public async Task<ApiResponse<CartModel>> ValidateCartForCheckoutAsync()
    {
        var result = await _apiClient.PostAsync<CartModel>("cart/validate", new { });

        if (result.Success)
        {
            NotifyCartUpdated();
        }

        return result;
    }

    public async Task<ApiResponse<bool>> IsCourseInCartAsync(Guid courseId)
    {
        try
        {
            var response = await _apiClient.GetAsync<IsInCartResponse>($"cart/check/{courseId}");

            if (response.Success && response.Data != null)
            {
                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = response.Data.IsInCart
                };
            }

            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = response.Message
            };
        }
        catch
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Data = false
            };
        }
    }

    public void NotifyCartUpdated()
    {
        OnCartUpdated?.Invoke();
    }

    // Helper class for deserializing the check response
    private class IsInCartResponse
    {
        public bool IsInCart { get; set; }
    }
}
