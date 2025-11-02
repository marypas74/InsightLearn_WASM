using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid CourseId { get; set; }
    
    public Guid? CouponId { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal OriginalAmount { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; }
    
    [StringLength(3)]
    public string Currency { get; set; } = "USD";
    
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    public PaymentMethodType PaymentMethod { get; set; }
    
    [StringLength(100)]
    public string? TransactionId { get; set; }
    
    [StringLength(100)]
    public string? PaymentGatewayId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? ProcessedAt { get; set; }
    
    public DateTime? RefundedAt { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? RefundAmount { get; set; }
    
    public string? FailureReason { get; set; }
    
    public string? Notes { get; set; }
    
    // Invoice details
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;
    
    public string? BillingAddress { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Course Course { get; set; } = null!;
    public virtual Coupon? Coupon { get; set; }
    
    public bool IsSuccessful => Status == PaymentStatus.Completed;
    
    public bool IsRefunded => Status == PaymentStatus.Refunded;
    
    public decimal NetAmount => Amount - (RefundAmount ?? 0);
}

public class Coupon
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? Description { get; set; }
    
    public CouponType Type { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Value { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinimumAmount { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal? MaximumDiscount { get; set; }
    
    public int? UsageLimit { get; set; }
    
    public int UsedCount { get; set; }
    
    public DateTime ValidFrom { get; set; }
    
    public DateTime ValidUntil { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public Guid? CourseId { get; set; }
    
    public Guid? InstructorId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Course? Course { get; set; }
    public virtual User? Instructor { get; set; }
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    
    public bool IsValid => IsActive 
        && DateTime.UtcNow >= ValidFrom 
        && DateTime.UtcNow <= ValidUntil 
        && (UsageLimit == null || UsedCount < UsageLimit);
    
    public decimal CalculateDiscount(decimal amount)
    {
        if (!IsValid || (MinimumAmount.HasValue && amount < MinimumAmount))
            return 0;
            
        var discount = Type == CouponType.Percentage 
            ? amount * (Value / 100) 
            : Value;
            
        if (MaximumDiscount.HasValue && discount > MaximumDiscount)
            discount = MaximumDiscount.Value;
            
        return Math.Min(discount, amount);
    }
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled,
    Refunded,
    PartiallyRefunded
}

public enum PaymentMethodType
{
    CreditCard,
    PayPal,
    BankTransfer,
    Crypto,
    Wallet
}

public enum CouponType
{
    Percentage,
    FixedAmount
}