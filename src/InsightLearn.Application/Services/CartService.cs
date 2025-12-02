using InsightLearn.Core.DTOs.Cart;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service implementation for shopping cart business logic
/// Production-ready with comprehensive error handling and validation
/// </summary>
public class CartService : ICartService
{
    private readonly ICartRepository _cartRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly ILogger<CartService> _logger;

    public CartService(
        ICartRepository cartRepository,
        ICourseRepository courseRepository,
        IEnrollmentRepository enrollmentRepository,
        ICouponRepository couponRepository,
        ILogger<CartService> logger)
    {
        _cartRepository = cartRepository;
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
        _couponRepository = couponRepository;
        _logger = logger;
    }

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        _logger.LogDebug("Getting cart for user {UserId}", userId);

        var cartItems = await _cartRepository.GetCartItemsByUserIdAsync(userId);
        var warnings = new List<string>();
        var hasChanges = false;

        var itemDtos = new List<CartItemDto>();
        foreach (var item in cartItems)
        {
            if (item.Course == null)
            {
                warnings.Add($"Course not found for cart item {item.Id}");
                continue;
            }

            var priceChanged = item.PriceAtAddition != item.Course.CurrentPrice;
            if (priceChanged)
            {
                hasChanges = true;
                warnings.Add($"Price for '{item.Course.Title}' has changed from €{item.PriceAtAddition:F2} to €{item.Course.CurrentPrice:F2}");
            }

            itemDtos.Add(MapToCartItemDto(item, priceChanged));
        }

        var subtotal = itemDtos.Sum(i => i.CurrentPrice);
        var totalDiscount = itemDtos.Sum(i => i.DiscountAmount);
        var total = Math.Max(0, subtotal - totalDiscount);

        return new CartDto
        {
            Items = itemDtos,
            ItemCount = itemDtos.Count,
            Subtotal = subtotal,
            TotalDiscount = totalDiscount,
            Total = total,
            Currency = "EUR",
            Warnings = warnings,
            HasPriceChanges = hasChanges
        };
    }

    public async Task<CartDto> AddToCartAsync(Guid userId, AddToCartDto dto)
    {
        _logger.LogInformation("Adding course {CourseId} to cart for user {UserId}", dto.CourseId, userId);

        // Validate course exists
        var course = await _courseRepository.GetByIdAsync(dto.CourseId);
        if (course == null)
        {
            _logger.LogWarning("Course {CourseId} not found", dto.CourseId);
            throw new InvalidOperationException($"Course not found: {dto.CourseId}");
        }

        // Check if user is already enrolled
        var isEnrolled = await _enrollmentRepository.IsUserEnrolledAsync(userId, dto.CourseId);
        if (isEnrolled)
        {
            _logger.LogWarning("User {UserId} is already enrolled in course {CourseId}", userId, dto.CourseId);
            throw new InvalidOperationException("You are already enrolled in this course");
        }

        // Check if already in cart
        var existingItem = await _cartRepository.GetByUserAndCourseAsync(userId, dto.CourseId);
        if (existingItem != null)
        {
            _logger.LogWarning("Course {CourseId} is already in cart for user {UserId}", dto.CourseId, userId);
            throw new InvalidOperationException("This course is already in your cart");
        }

        // Calculate discount if coupon provided
        decimal discountAmount = 0;
        if (!string.IsNullOrWhiteSpace(dto.CouponCode))
        {
            var coupon = await _couponRepository.GetByCodeAsync(dto.CouponCode);
            if (coupon != null && await _couponRepository.IsValidAsync(dto.CouponCode, dto.CourseId))
            {
                discountAmount = CalculateDiscount(course.CurrentPrice, coupon);
                _logger.LogInformation("Applied coupon {CouponCode} with discount {Discount}", dto.CouponCode, discountAmount);
            }
        }

        var cartItem = new CartItem
        {
            UserId = userId,
            CourseId = dto.CourseId,
            PriceAtAddition = course.CurrentPrice,
            CouponCode = dto.CouponCode,
            DiscountAmount = discountAmount
        };

        await _cartRepository.AddAsync(cartItem);
        _logger.LogInformation("Course {CourseId} added to cart for user {UserId}", dto.CourseId, userId);

        return await GetCartAsync(userId);
    }

    public async Task<CartDto> RemoveFromCartAsync(Guid userId, Guid courseId)
    {
        _logger.LogInformation("Removing course {CourseId} from cart for user {UserId}", courseId, userId);

        var removed = await _cartRepository.RemoveByUserAndCourseAsync(userId, courseId);
        if (!removed)
        {
            _logger.LogWarning("Course {CourseId} was not in cart for user {UserId}", courseId, userId);
        }

        return await GetCartAsync(userId);
    }

    public async Task<int> ClearCartAsync(Guid userId)
    {
        _logger.LogInformation("Clearing cart for user {UserId}", userId);
        var count = await _cartRepository.ClearCartAsync(userId);
        _logger.LogInformation("Cleared {Count} items from cart for user {UserId}", count, userId);
        return count;
    }

    public async Task<CartCountDto> GetCartCountAsync(Guid userId)
    {
        var count = await _cartRepository.GetCartItemCountAsync(userId);
        return new CartCountDto { Count = count };
    }

    public async Task<CartCouponResultDto> ApplyCouponAsync(Guid userId, ApplyCartCouponDto dto)
    {
        _logger.LogInformation("Applying coupon {CouponCode} to cart for user {UserId}", dto.CouponCode, userId);

        var coupon = await _couponRepository.GetByCodeAsync(dto.CouponCode);
        if (coupon == null)
        {
            return new CartCouponResultDto
            {
                IsValid = false,
                ErrorMessage = "Invalid coupon code"
            };
        }

        var isValid = await _couponRepository.IsValidAsync(dto.CouponCode);
        if (!isValid)
        {
            return new CartCouponResultDto
            {
                IsValid = false,
                ErrorMessage = "This coupon is expired or has reached its usage limit"
            };
        }

        // Apply coupon to all cart items
        var cartItems = await _cartRepository.GetCartItemsByUserIdAsync(userId);
        decimal totalDiscount = 0;

        foreach (var item in cartItems)
        {
            if (item.Course == null) continue;

            var discount = CalculateDiscount(item.Course.CurrentPrice, coupon);
            item.CouponCode = dto.CouponCode;
            item.DiscountAmount = discount;
            await _cartRepository.UpdateAsync(item);
            totalDiscount += discount;
        }

        var cart = await GetCartAsync(userId);

        return new CartCouponResultDto
        {
            IsValid = true,
            CouponCode = dto.CouponCode,
            Description = coupon.Description,
            DiscountAmount = totalDiscount,
            NewTotal = cart.Total
        };
    }

    public async Task<CartDto> RemoveCouponAsync(Guid userId)
    {
        _logger.LogInformation("Removing coupon from cart for user {UserId}", userId);

        var cartItems = await _cartRepository.GetCartItemsByUserIdAsync(userId);
        foreach (var item in cartItems)
        {
            item.CouponCode = null;
            item.DiscountAmount = 0;
            await _cartRepository.UpdateAsync(item);
        }

        return await GetCartAsync(userId);
    }

    public async Task<CartDto> ValidateCartForCheckoutAsync(Guid userId)
    {
        _logger.LogInformation("Validating cart for checkout for user {UserId}", userId);

        // Remove enrolled courses
        var removedCount = await _cartRepository.RemoveEnrolledCoursesAsync(userId);
        if (removedCount > 0)
        {
            _logger.LogInformation("Removed {Count} already-enrolled courses from cart", removedCount);
        }

        // Refresh prices
        var priceUpdates = await _cartRepository.RefreshCartPricesAsync(userId);
        if (priceUpdates > 0)
        {
            _logger.LogInformation("Updated prices for {Count} cart items", priceUpdates);
        }

        return await GetCartAsync(userId);
    }

    public async Task<bool> IsCourseInCartAsync(Guid userId, Guid courseId)
    {
        return await _cartRepository.IsCourseInCartAsync(userId, courseId);
    }

    #region Private Helper Methods

    private static CartItemDto MapToCartItemDto(CartItem item, bool hasPriceChanged)
    {
        var course = item.Course!;
        return new CartItemDto
        {
            Id = item.Id,
            CourseId = item.CourseId,
            CourseTitle = course.Title,
            CourseThumbnailUrl = course.ThumbnailUrl,
            InstructorName = course.Instructor?.FirstName + " " + course.Instructor?.LastName ?? "Unknown",
            OriginalPrice = item.PriceAtAddition,
            CurrentPrice = course.CurrentPrice,
            DiscountAmount = item.DiscountAmount,
            FinalPrice = Math.Max(0, course.CurrentPrice - item.DiscountAmount),
            CouponCode = item.CouponCode,
            HasPriceChanged = hasPriceChanged,
            AddedAt = item.AddedAt,
            AverageRating = course.Reviews?.Any() == true
                ? course.Reviews.Average(r => r.Rating)
                : 0,
            ReviewCount = course.Reviews?.Count ?? 0,
            EstimatedDurationMinutes = course.EstimatedDurationMinutes,
            Language = course.Language
        };
    }

    private static decimal CalculateDiscount(decimal price, Coupon coupon)
    {
        if (coupon.Type == CouponType.Percentage)
        {
            return Math.Round(price * coupon.Value / 100, 2);
        }
        else // FixedAmount
        {
            return Math.Min(coupon.Value, price); // Don't discount more than price
        }
    }

    #endregion
}
