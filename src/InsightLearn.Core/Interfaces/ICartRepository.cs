using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Repository interface for CartItem entity operations
/// Shopping cart persistence for logged-in users
/// </summary>
public interface ICartRepository
{
    /// <summary>
    /// Gets all cart items for a specific user with course details
    /// </summary>
    Task<IEnumerable<CartItem>> GetCartItemsByUserIdAsync(Guid userId);

    /// <summary>
    /// Gets a specific cart item by its unique identifier
    /// </summary>
    Task<CartItem?> GetByIdAsync(Guid cartItemId);

    /// <summary>
    /// Gets a cart item by user and course (to check if already in cart)
    /// </summary>
    Task<CartItem?> GetByUserAndCourseAsync(Guid userId, Guid courseId);

    /// <summary>
    /// Adds a new item to the cart
    /// </summary>
    Task<CartItem> AddAsync(CartItem cartItem);

    /// <summary>
    /// Updates an existing cart item (e.g., coupon code, discount)
    /// </summary>
    Task<CartItem> UpdateAsync(CartItem cartItem);

    /// <summary>
    /// Removes a specific item from the cart
    /// </summary>
    Task<bool> RemoveAsync(Guid cartItemId);

    /// <summary>
    /// Removes a course from user's cart
    /// </summary>
    Task<bool> RemoveByUserAndCourseAsync(Guid userId, Guid courseId);

    /// <summary>
    /// Clears all items from user's cart (after successful checkout)
    /// </summary>
    Task<int> ClearCartAsync(Guid userId);

    /// <summary>
    /// Gets the count of items in user's cart (for header badge)
    /// </summary>
    Task<int> GetCartItemCountAsync(Guid userId);

    /// <summary>
    /// Checks if a course is already in user's cart
    /// </summary>
    Task<bool> IsCourseInCartAsync(Guid userId, Guid courseId);

    /// <summary>
    /// Gets cart items with price changes detected
    /// </summary>
    Task<IEnumerable<CartItem>> GetItemsWithPriceChangesAsync(Guid userId);

    /// <summary>
    /// Updates prices for all cart items to current course prices
    /// </summary>
    Task<int> RefreshCartPricesAsync(Guid userId);

    /// <summary>
    /// Removes items from cart for courses user is already enrolled in
    /// </summary>
    Task<int> RemoveEnrolledCoursesAsync(Guid userId);
}