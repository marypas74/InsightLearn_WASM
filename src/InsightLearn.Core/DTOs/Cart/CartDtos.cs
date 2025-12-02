using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Cart;

/// <summary>
/// DTO for a single cart item with course details
/// </summary>
public class CartItemDto
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

    // Course metadata
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public string? Language { get; set; }
}

/// <summary>
/// DTO for the complete cart with all items and totals
/// </summary>
public class CartDto
{
    public List<CartItemDto> Items { get; set; } = new();
    public int ItemCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "EUR";

    // Applied cart-wide coupon
    public string? AppliedCouponCode { get; set; }
    public decimal CouponDiscount { get; set; }

    // Warnings
    public List<string> Warnings { get; set; } = new();
    public bool HasPriceChanges { get; set; }
}

/// <summary>
/// DTO for adding a course to the cart
/// </summary>
public class AddToCartDto
{
    [Required(ErrorMessage = "Course ID is required")]
    public Guid CourseId { get; set; }

    [StringLength(50, ErrorMessage = "Coupon code cannot exceed 50 characters")]
    [RegularExpression(@"^[A-Z0-9\-]+$", ErrorMessage = "Coupon code must contain only uppercase letters, numbers, and hyphens")]
    public string? CouponCode { get; set; }
}

/// <summary>
/// DTO for applying a coupon to the cart
/// </summary>
public class ApplyCartCouponDto
{
    [Required(ErrorMessage = "Coupon code is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Coupon code must be between 3 and 50 characters")]
    [RegularExpression(@"^[A-Z0-9\-]+$", ErrorMessage = "Coupon code must contain only uppercase letters, numbers, and hyphens")]
    public string CouponCode { get; set; } = string.Empty;
}

/// <summary>
/// DTO for cart coupon validation result
/// </summary>
public class CartCouponResultDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CouponCode { get; set; }
    public string? Description { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NewTotal { get; set; }
}

/// <summary>
/// DTO for removing an item from cart
/// </summary>
public class RemoveFromCartDto
{
    [Required(ErrorMessage = "Course ID is required")]
    public Guid CourseId { get; set; }
}

/// <summary>
/// Simple DTO for cart item count (header badge)
/// </summary>
public class CartCountDto
{
    public int Count { get; set; }
}
