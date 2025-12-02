using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InsightLearn.Core.Entities;

/// <summary>
/// Represents an item in the user's shopping cart
/// Cart items are persisted to database for logged-in users
/// Guest users use LocalStorage (frontend only)
/// </summary>
public class CartItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User who owns this cart item (null for guest carts stored in LocalStorage)
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Course being purchased
    /// </summary>
    [Required]
    public Guid CourseId { get; set; }

    /// <summary>
    /// Price at time of adding to cart (for price change detection)
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal PriceAtAddition { get; set; }

    /// <summary>
    /// Coupon code applied to this specific item (optional)
    /// </summary>
    [StringLength(50)]
    public string? CouponCode { get; set; }

    /// <summary>
    /// Discount amount from coupon
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountAmount { get; set; } = 0;

    /// <summary>
    /// When the item was added to cart
    /// </summary>
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the item was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    [JsonIgnore]
    public virtual User? User { get; set; }

    [JsonIgnore]
    public virtual Course? Course { get; set; }

    // Computed Properties
    [NotMapped]
    public decimal FinalPrice => Math.Max(0, PriceAtAddition - DiscountAmount);

    [NotMapped]
    public bool HasDiscount => DiscountAmount > 0;
}
