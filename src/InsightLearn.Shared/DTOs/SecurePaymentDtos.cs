using System.ComponentModel.DataAnnotations;
using InsightLearn.Core.Entities;

namespace InsightLearn.Shared.DTOs;

/// <summary>
/// DTO for creating a new secure payment method
/// </summary>
public class CreatePaymentMethodDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public PaymentMethodType Type { get; set; }

    [StringLength(100)]
    public string? DisplayName { get; set; }

    // Credit Card Fields
    [StringLength(19, MinimumLength = 13)]
    public string? CardNumber { get; set; }

    [StringLength(100)]
    public string? CardholderName { get; set; }

    [Range(1, 12)]
    public int? ExpirationMonth { get; set; }

    [Range(2024, 2099)]
    public int? ExpirationYear { get; set; }

    [StringLength(4, MinimumLength = 3)]
    public string? Cvv { get; set; }

    // Billing Address
    public string? BillingAddress { get; set; }

    [StringLength(100)]
    public string? BillingCity { get; set; }

    [StringLength(100)]
    public string? BillingState { get; set; }

    [StringLength(20)]
    public string? BillingPostalCode { get; set; }

    [StringLength(2)]
    public string? BillingCountry { get; set; }

    // PayPal Fields
    [EmailAddress]
    [StringLength(255)]
    public string? PayPalEmail { get; set; }

    // Bank Transfer Fields
    [StringLength(50)]
    public string? BankAccountNumber { get; set; }

    [StringLength(20)]
    public string? BankRoutingNumber { get; set; }

    [StringLength(200)]
    public string? BankName { get; set; }

    public bool IsDefault { get; set; }

    // Security Context
    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }
}

/// <summary>
/// DTO for updating an existing payment method
/// </summary>
public class UpdatePaymentMethodDto
{
    [StringLength(100)]
    public string? DisplayName { get; set; }

    // Only allow updating non-sensitive fields
    public string? BillingAddress { get; set; }

    [StringLength(100)]
    public string? BillingCity { get; set; }

    [StringLength(100)]
    public string? BillingState { get; set; }

    [StringLength(20)]
    public string? BillingPostalCode { get; set; }

    [StringLength(2)]
    public string? BillingCountry { get; set; }

    public bool? IsDefault { get; set; }

    // Security Context
    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }
}

/// <summary>
/// DTO for secure payment method information (never exposes sensitive data)
/// </summary>
public class SecurePaymentMethodDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public PaymentMethodType Type { get; set; }
    public string? DisplayName { get; set; }
    public string? LastFourDigits { get; set; }
    public CardType? CardType { get; set; }
    public int? ExpirationMonth { get; set; }
    public int? ExpirationYear { get; set; }
    public string? BillingAddress { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? BillingCountry { get; set; }
    public string? PayPalEmail { get; set; }
    public string? BankName { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public bool IsVerified { get; set; }
    public bool IsExpired { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int RiskScore { get; set; }
    public string DisplayText { get; set; } = string.Empty;
}

/// <summary>
/// DTO for processing secure payments with transaction security
/// </summary>
public class ProcessSecurePaymentDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid PaymentMethodId { get; set; }

    [Required]
    public Guid CourseId { get; set; }

    [Required]
    [Range(0.01, 999999.99)]
    public decimal Amount { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "USD";

    public string? CouponCode { get; set; }

    // Transaction Security
    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public string? DeviceFingerprint { get; set; }

    public bool RequireTwoFactorAuth { get; set; }

    public string? TwoFactorToken { get; set; }

    // Additional Security Context
    public Dictionary<string, string>? SecurityContext { get; set; }
}

/// <summary>
/// DTO for secure payment transaction results
/// </summary>
public class SecurePaymentTransactionDto
{
    public Guid TransactionId { get; set; }
    public Guid PaymentId { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
    public FraudAnalysisDto? FraudAnalysis { get; set; }
    public bool RequiresAdditionalVerification { get; set; }
    public string? VerificationMethod { get; set; }
    public Dictionary<string, string>? AdditionalData { get; set; }
}

/// <summary>
/// DTO for payment method validation results
/// </summary>
public class PaymentMethodValidationDto
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> ValidationWarnings { get; set; } = new();
    public int RiskScore { get; set; }
    public string? RiskReason { get; set; }
    public bool RequiresVerification { get; set; }
    public string? VerificationMethod { get; set; }
}

/// <summary>
/// DTO for fraud analysis and risk assessment
/// </summary>
public class FraudAnalysisDto
{
    public int RiskScore { get; set; } // 0-100
    public string RiskLevel { get; set; } = string.Empty; // Low, Medium, High
    public List<string> RiskFactors { get; set; } = new();
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public bool RequiresManualReview { get; set; }
    public Dictionary<string, object>? AnalysisDetails { get; set; }
}

/// <summary>
/// DTO for transaction risk analysis input
/// </summary>
public class AnalyzeTransactionRiskDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid PaymentMethodId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public string Currency { get; set; } = string.Empty;

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public string? DeviceFingerprint { get; set; }

    public DateTime TransactionTime { get; set; } = DateTime.UtcNow;

    public Dictionary<string, string>? AdditionalContext { get; set; }
}

/// <summary>
/// DTO for security events related to payments
/// </summary>
public class SecurityEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Severity { get; set; } // 1-5
    public DateTime OccurredAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object>? EventData { get; set; }
}

/// <summary>
/// DTO for reporting suspicious activity
/// </summary>
public class ReportSuspiciousActivityDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string ActivityType { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public Guid? RelatedPaymentMethodId { get; set; }

    public Guid? RelatedTransactionId { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public Dictionary<string, string>? Evidence { get; set; }
}

/// <summary>
/// DTO for payment method audit log entries
/// </summary>
public class PaymentMethodAuditDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// DTO for secure transaction history
/// </summary>
public class SecureTransactionHistoryDto
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public PaymentMethodType PaymentMethodType { get; set; }
    public string PaymentMethodDisplay { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? InvoiceNumber { get; set; }
}

/// <summary>
/// DTO for detailed secure transaction information
/// </summary>
public class SecureTransactionDetailsDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public PaymentMethodType PaymentMethodType { get; set; }
    public string PaymentMethodDisplay { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? TransactionId { get; set; }
    public string? FailureReason { get; set; }
    public string? Notes { get; set; }
    public FraudAnalysisDto? FraudAnalysis { get; set; }
}

/// <summary>
/// DTO for JWT payment transaction tokens
/// </summary>
public class PaymentTransactionTokenDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Guid TransactionId { get; set; }
    public string TransactionSignature { get; set; } = string.Empty;
}