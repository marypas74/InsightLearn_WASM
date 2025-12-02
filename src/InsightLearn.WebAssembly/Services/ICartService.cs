using InsightLearn.WebAssembly.Models;

namespace InsightLearn.WebAssembly.Services;

/// <summary>
/// Frontend models for shopping cart (mirroring backend DTOs)
/// </summary>
public class CartItemModel
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public string? CourseThumbnailUrl { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalPrice { get; set; }
    public string? CouponCode { get; set; }
    public bool HasPriceChanged { get; set; }
    public DateTime AddedAt { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public string? Language { get; set; }
}

public class CartModel
{
    public List<CartItemModel> Items { get; set; } = new();
    public int ItemCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "EUR";
    public string? AppliedCouponCode { get; set; }
    public decimal CouponDiscount { get; set; }
    public List<string> Warnings { get; set; } = new();
    public bool HasPriceChanges { get; set; }
}

public class AddToCartRequest
{
    public Guid CourseId { get; set; }
    public string? CouponCode { get; set; }
}

public class ApplyCouponRequest
{
    public string CouponCode { get; set; } = string.Empty;
}

public class CouponResultModel
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CouponCode { get; set; }
    public string? Description { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NewTotal { get; set; }
}

public class CartCountModel
{
    public int Count { get; set; }
}

/// <summary>
/// Frontend service interface for shopping cart operations
/// </summary>
public interface ICartService
{
    /// <summary>
    /// Gets the current user's shopping cart
    /// </summary>
    Task<ApiResponse<CartModel>> GetCartAsync();

    /// <summary>
    /// Gets the cart item count for header badge
    /// </summary>
    Task<ApiResponse<CartCountModel>> GetCartCountAsync();

    /// <summary>
    /// Adds a course to the cart
    /// </summary>
    Task<ApiResponse<CartModel>> AddToCartAsync(Guid courseId, string? couponCode = null);

    /// <summary>
    /// Removes a course from the cart
    /// </summary>
    Task<ApiResponse<CartModel>> RemoveFromCartAsync(Guid courseId);

    /// <summary>
    /// Clears the entire cart
    /// </summary>
    Task<ApiResponse> ClearCartAsync();

    /// <summary>
    /// Applies a coupon code to the cart
    /// </summary>
    Task<ApiResponse<CouponResultModel>> ApplyCouponAsync(string couponCode);

    /// <summary>
    /// Removes the applied coupon from the cart
    /// </summary>
    Task<ApiResponse<CartModel>> RemoveCouponAsync();

    /// <summary>
    /// Validates the cart before checkout (updates prices, removes enrolled courses)
    /// </summary>
    Task<ApiResponse<CartModel>> ValidateCartForCheckoutAsync();

    /// <summary>
    /// Checks if a course is already in the cart
    /// </summary>
    Task<ApiResponse<bool>> IsCourseInCartAsync(Guid courseId);

    /// <summary>
    /// Event raised when cart is updated (for header badge)
    /// </summary>
    event Action? OnCartUpdated;

    /// <summary>
    /// Notifies listeners that the cart has been updated
    /// </summary>
    void NotifyCartUpdated();
}
