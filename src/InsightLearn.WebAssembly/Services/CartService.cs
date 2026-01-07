using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services;

/// <summary>
/// Frontend service implementation for shopping cart operations
/// Communicates with backend Cart API endpoints
/// </summary>
public class CartService : ICartService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<CartService> _logger;

    public event Action? OnCartUpdated;

    public CartService(IApiClient apiClient, ILogger<CartService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<ApiResponse<CartModel>> GetCartAsync()
    {
        _logger.LogDebug("Fetching shopping cart");
        var response = await _apiClient.GetAsync<CartModel>("cart");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Cart retrieved: {ItemCount} items, Total: {Total:C}",
                response.Data.ItemCount, response.Data.Total);
        }

        return response;
    }

    public async Task<ApiResponse<CartCountModel>> GetCartCountAsync()
    {
        _logger.LogDebug("Fetching cart item count");
        return await _apiClient.GetAsync<CartCountModel>("cart/count");
    }

    public async Task<ApiResponse<CartModel>> AddToCartAsync(Guid courseId, string? couponCode = null)
    {
        _logger.LogInformation("Adding course to cart: {CourseId} (Coupon: {CouponCode})",
            courseId, couponCode ?? "none");

        var request = new AddToCartRequest
        {
            CourseId = courseId,
            CouponCode = couponCode
        };

        var result = await _apiClient.PostAsync<CartModel>("cart/add", request);

        if (result.Success)
        {
            _logger.LogInformation("Course {CourseId} added to cart successfully", courseId);
            NotifyCartUpdated();
        }
        else
        {
            _logger.LogWarning("Failed to add course {CourseId} to cart: {ErrorMessage}",
                courseId, result.Message ?? "Unknown error");
        }

        return result;
    }

    public async Task<ApiResponse<CartModel>> RemoveFromCartAsync(Guid courseId)
    {
        _logger.LogInformation("Removing course from cart: {CourseId}", courseId);
        var result = await _apiClient.DeleteAsync<CartModel>($"cart/{courseId}");

        if (result.Success)
        {
            _logger.LogInformation("Course {CourseId} removed from cart", courseId);
            NotifyCartUpdated();
        }
        else
        {
            _logger.LogWarning("Failed to remove course {CourseId} from cart: {ErrorMessage}",
                courseId, result.Message ?? "Unknown error");
        }

        return result;
    }

    public async Task<ApiResponse> ClearCartAsync()
    {
        _logger.LogWarning("Clearing entire shopping cart");
        var result = await _apiClient.DeleteAsync("cart");

        if (result.Success)
        {
            _logger.LogInformation("Cart cleared successfully");
            NotifyCartUpdated();
        }
        else
        {
            _logger.LogError("Failed to clear cart: {ErrorMessage}", result.Message ?? "Unknown error");
        }

        return result;
    }

    public async Task<ApiResponse<CouponResultModel>> ApplyCouponAsync(string couponCode)
    {
        _logger.LogInformation("Applying coupon code: {CouponCode}", couponCode);
        var request = new ApplyCouponRequest { CouponCode = couponCode };
        var result = await _apiClient.PostAsync<CouponResultModel>("cart/coupon", request);

        if (result.Success && result.Data != null)
        {
            _logger.LogInformation("Coupon {CouponCode} applied: Discount {Discount:C}, New Total: {NewTotal:C}",
                couponCode, result.Data.DiscountAmount, result.Data.NewTotal);
            NotifyCartUpdated();
        }
        else
        {
            _logger.LogWarning("Failed to apply coupon {CouponCode}: {ErrorMessage}",
                couponCode, result.Message ?? "Invalid coupon");
        }

        return result;
    }

    public async Task<ApiResponse<CartModel>> RemoveCouponAsync()
    {
        _logger.LogInformation("Removing applied coupon from cart");
        var result = await _apiClient.DeleteAsync<CartModel>("cart/coupon");

        if (result.Success)
        {
            _logger.LogInformation("Coupon removed successfully");
            NotifyCartUpdated();
        }
        else
        {
            _logger.LogWarning("Failed to remove coupon: {ErrorMessage}",
                result.Message ?? "Unknown error");
        }

        return result;
    }

    public async Task<ApiResponse<CartModel>> ValidateCartForCheckoutAsync()
    {
        _logger.LogInformation("Validating cart for checkout");
        var result = await _apiClient.PostAsync<CartModel>("cart/validate", new { });

        if (result.Success)
        {
            _logger.LogInformation("Cart validated successfully");
            NotifyCartUpdated();
        }
        else
        {
            _logger.LogWarning("Cart validation failed: {ErrorMessage}",
                result.Message ?? "Unknown error");
        }

        return result;
    }

    public async Task<ApiResponse<bool>> IsCourseInCartAsync(Guid courseId)
    {
        try
        {
            _logger.LogDebug("Checking if course {CourseId} is in cart", courseId);
            var response = await _apiClient.GetAsync<IsInCartResponse>($"cart/check/{courseId}");

            if (response.Success && response.Data != null)
            {
                _logger.LogDebug("Course {CourseId} cart status: {IsInCart}",
                    courseId, response.Data.IsInCart);
                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = response.Data.IsInCart
                };
            }

            _logger.LogWarning("Failed to check course {CourseId} in cart: {ErrorMessage}",
                courseId, response.Message ?? "Unknown error");
            return new ApiResponse<bool>
            {
                Success = false,
                Data = false,
                Message = response.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while checking if course {CourseId} is in cart", courseId);
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
