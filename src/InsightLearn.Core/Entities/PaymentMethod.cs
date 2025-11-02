using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

/// <summary>
/// Represents a secure payment method with encrypted card storage for PCI DSS compliance
/// All sensitive card data is encrypted using AES-256-GCM encryption
/// </summary>
public class PaymentMethod
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of payment method (CreditCard, PayPal, BankTransfer)
    /// </summary>
    [Required]
    public PaymentMethodType Type { get; set; }

    /// <summary>
    /// User-friendly name for this payment method (e.g., "My Visa Card", "Work Credit Card")
    /// </summary>
    [StringLength(100)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Encrypted credit card number using AES-256-GCM
    /// NEVER store plain text card numbers for PCI DSS compliance
    /// </summary>
    public string? EncryptedCardNumber { get; set; }

    /// <summary>
    /// Last 4 digits of card number for display purposes (unencrypted)
    /// Used for user identification of payment method
    /// </summary>
    [StringLength(4)]
    public string? LastFourDigits { get; set; }

    /// <summary>
    /// Card type detected from card number (Visa, MasterCard, Amex, etc.)
    /// </summary>
    public CardType? CardType { get; set; }

    /// <summary>
    /// Encrypted cardholder name using AES-256-GCM
    /// </summary>
    public string? EncryptedCardholderName { get; set; }

    /// <summary>
    /// Card expiration month (1-12)
    /// Stored as integer for validation purposes
    /// </summary>
    public int? ExpirationMonth { get; set; }

    /// <summary>
    /// Card expiration year (full 4-digit year)
    /// </summary>
    public int? ExpirationYear { get; set; }

    /// <summary>
    /// Encrypted CVV/CVC code using AES-256-GCM
    /// Should be cleared after successful transaction for security
    /// </summary>
    public string? EncryptedCvv { get; set; }

    /// <summary>
    /// Billing address associated with this payment method
    /// </summary>
    public string? BillingAddress { get; set; }

    /// <summary>
    /// Billing city
    /// </summary>
    [StringLength(100)]
    public string? BillingCity { get; set; }

    /// <summary>
    /// Billing state/province
    /// </summary>
    [StringLength(100)]
    public string? BillingState { get; set; }

    /// <summary>
    /// Billing postal code
    /// </summary>
    [StringLength(20)]
    public string? BillingPostalCode { get; set; }

    /// <summary>
    /// Billing country code (ISO 3166-1 alpha-2)
    /// </summary>
    [StringLength(2)]
    public string? BillingCountry { get; set; }

    /// <summary>
    /// PayPal email address for PayPal payment methods
    /// </summary>
    [StringLength(255)]
    public string? PayPalEmail { get; set; }

    /// <summary>
    /// Bank account number for bank transfer (encrypted)
    /// </summary>
    public string? EncryptedBankAccountNumber { get; set; }

    /// <summary>
    /// Bank routing number for bank transfer
    /// </summary>
    [StringLength(20)]
    public string? BankRoutingNumber { get; set; }

    /// <summary>
    /// Bank name for bank transfer
    /// </summary>
    [StringLength(200)]
    public string? BankName { get; set; }

    /// <summary>
    /// Whether this is the default payment method for the user
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Whether this payment method is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this payment method was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this payment method was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this payment method was last used for a transaction
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// External payment gateway token/ID (for Stripe, PayPal, etc.)
    /// </summary>
    [StringLength(255)]
    public string? ExternalPaymentMethodId { get; set; }

    /// <summary>
    /// Whether this payment method has been verified by the payment gateway
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Verification status details
    /// </summary>
    [StringLength(500)]
    public string? VerificationNotes { get; set; }

    /// <summary>
    /// Security fingerprint for fraud detection
    /// Generated from encrypted card data hash for duplicate detection
    /// </summary>
    [StringLength(128)]
    public string? SecurityFingerprint { get; set; }

    /// <summary>
    /// Risk score for fraud detection (0-100, higher = more risky)
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Encryption key ID used for this payment method
    /// Supports key rotation for enhanced security
    /// </summary>
    [StringLength(50)]
    public string? EncryptionKeyId { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<PaymentMethodAuditLog> AuditLogs { get; set; } = new List<PaymentMethodAuditLog>();

    // Computed properties
    public bool IsExpired => ExpirationYear.HasValue && ExpirationMonth.HasValue &&
                            (ExpirationYear < DateTime.UtcNow.Year ||
                             (ExpirationYear == DateTime.UtcNow.Year && ExpirationMonth < DateTime.UtcNow.Month));

    public string DisplayText => Type switch
    {
        PaymentMethodType.CreditCard => $"{CardType?.ToString() ?? "Card"} ****{LastFourDigits}",
        PaymentMethodType.PayPal => $"PayPal ({PayPalEmail})",
        PaymentMethodType.BankTransfer => $"Bank Transfer ({BankName})",
        _ => DisplayName ?? Type.ToString()
    };
}

/// <summary>
/// Audit log for payment method operations to track all changes for security compliance
/// </summary>
public class PaymentMethodAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PaymentMethodId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of operation performed (Created, Updated, Deleted, Used, Verified, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the action performed
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// IP address from which the action was performed
    /// </summary>
    [StringLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string from the client
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// When this action was performed
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata in JSON format
    /// </summary>
    public string? Metadata { get; set; }

    // Navigation properties
    public virtual PaymentMethod PaymentMethod { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

/// <summary>
/// Supported credit card types for validation and display
/// </summary>
public enum CardType
{
    Visa,
    MasterCard,
    AmericanExpress,
    Discover,
    JCB,
    DinersClub,
    UnionPay,
    Maestro,
    Unknown
}