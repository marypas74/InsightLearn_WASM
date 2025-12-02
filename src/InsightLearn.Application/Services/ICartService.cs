using InsightLearn.Core.DTOs.Cart;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service interface for shopping cart business logic
/// </summary>
public interface ICartService
{
    /// <summary>
    /// Gets the complete cart for a user with all items, totals, and warnings
    /// </summary>
    Task<CartDto> GetCartAsync(Guid userId);

    /// <summary>
    /// Adds a course to the user's cart
    /// </summary>
    /// <returns>Updated cart</returns>
    Task<CartDto> AddToCartAsync(Guid userId, AddToCartDto dto);

    /// <summary>
    /// Removes a course from the user's cart
    /// </summary>
    /// <returns>Updated cart</returns>
    Task<CartDto> RemoveFromCartAsync(Guid userId, Guid courseId);

    /// <summary>
    /// Clears all items from the user's cart
    /// </summary>
    Task<int> ClearCartAsync(Guid userId);

    /// <summary>
    /// Gets the cart item count for header badge
    /// </summary>
    Task<CartCountDto> GetCartCountAsync(Guid userId);

    /// <summary>
    /// Applies a coupon code to the entire cart
    /// </summary>
    Task<CartCouponResultDto> ApplyCouponAsync(Guid userId, ApplyCartCouponDto dto);

    /// <summary>
    /// Removes the applied cart-wide coupon
    /// </summary>
    Task<CartDto> RemoveCouponAsync(Guid userId);

    /// <summary>
    /// Validates and refreshes cart before checkout
    /// Removes enrolled courses, updates prices, validates items
    /// </summary>
    Task<CartDto> ValidateCartForCheckoutAsync(Guid userId);

    /// <summary>
    /// Checks if a specific course is in user's cart
    /// </summary>
    Task<bool> IsCourseInCartAsync(Guid userId, Guid courseId);
}
