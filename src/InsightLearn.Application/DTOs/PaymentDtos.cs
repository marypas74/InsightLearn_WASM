using System.ComponentModel.DataAnnotations;
using InsightLearn.Core.Entities;

namespace InsightLearn.Application.DTOs;

public class CreatePaymentDto
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid CourseId { get; set; }
    
    [Required]
    [EmailAddress]
    public string UserEmail { get; set; } = string.Empty;
    
    public string? CouponCode { get; set; }
}

public class PaymentIntentDto
{
    public string ClientSecret { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public Guid PaymentId { get; set; }
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserEmail { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public PaymentMethodType PaymentMethod { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? CouponCode { get; set; }
}

public class RefundResultDto
{
    public bool Success { get; set; }
    public string? RefundId { get; set; }
    public decimal RefundAmount { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CouponValidationDto
{
    public bool IsValid { get; set; }
    public Guid? CouponId { get; set; }
    public CouponType? DiscountType { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MinimumAmount { get; set; }
    public decimal? MaximumDiscount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CreateCouponDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? Description { get; set; }
    
    [Required]
    public CouponType Type { get; set; }
    
    [Required]
    [Range(0.01, 9999.99)]
    public decimal Value { get; set; }
    
    [Range(0.01, 9999.99)]
    public decimal? MinimumAmount { get; set; }
    
    [Range(0.01, 9999.99)]
    public decimal? MaximumDiscount { get; set; }
    
    [Range(1, int.MaxValue)]
    public int? UsageLimit { get; set; }
    
    [Required]
    public DateTime ValidFrom { get; set; }
    
    [Required]
    public DateTime ValidUntil { get; set; }
    
    public Guid? CourseId { get; set; }
}