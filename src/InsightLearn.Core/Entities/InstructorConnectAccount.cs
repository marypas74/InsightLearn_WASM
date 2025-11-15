using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InsightLearn.Core.Validation;

namespace InsightLearn.Core.Entities;

/// <summary>
/// Stripe Connect account information for instructors to receive payouts
/// </summary>
public class InstructorConnectAccount
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid InstructorId { get; set; }

    /// <summary>
    /// Stripe Connect account ID
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string StripeAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Onboarding status: "pending", "incomplete", "complete", "restricted", "disabled"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string OnboardingStatus { get; set; } = "pending";

    /// <summary>
    /// Whether the account can receive payouts
    /// </summary>
    public bool PayoutsEnabled { get; set; } = false;

    /// <summary>
    /// Whether charges are enabled on this account
    /// </summary>
    public bool ChargesEnabled { get; set; } = false;

    /// <summary>
    /// Country code (ISO 3166-1 alpha-2)
    /// </summary>
    [MaxLength(2)]
    public string? Country { get; set; }

    /// <summary>
    /// Account currency
    /// </summary>
    [MaxLength(3)]
    [ValidCurrency]  // ISO 4217 validation (nullable)
    public string? Currency { get; set; }

    /// <summary>
    /// Default payout method (e.g., "bank_account", "debit_card")
    /// </summary>
    [MaxLength(50)]
    public string? DefaultPayoutMethod { get; set; }

    /// <summary>
    /// Stripe onboarding URL (for incomplete onboarding)
    /// </summary>
    [MaxLength(500)]
    public string? OnboardingUrl { get; set; }

    /// <summary>
    /// Date when onboarding was completed
    /// </summary>
    public DateTime? OnboardingCompletedAt { get; set; }

    /// <summary>
    /// Total amount paid out to this instructor (lifetime)
    /// </summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPaidOut { get; set; } = 0;

    /// <summary>
    /// Last payout date
    /// </summary>
    public DateTime? LastPayoutAt { get; set; }

    /// <summary>
    /// Requirements needing attention (stored as JSON array)
    /// </summary>
    public string? Requirements { get; set; }

    /// <summary>
    /// Verification status: "unverified", "pending", "verified"
    /// </summary>
    [MaxLength(50)]
    public string VerificationStatus { get; set; } = "unverified";

    /// <summary>
    /// Date when account was disabled (if applicable)
    /// </summary>
    public DateTime? DisabledAt { get; set; }

    /// <summary>
    /// Reason for account restriction/disable
    /// </summary>
    [MaxLength(500)]
    public string? DisabledReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(InstructorId))]
    public virtual User Instructor { get; set; } = null!;

    // Computed properties
    [NotMapped]
    public bool IsFullyOnboarded => OnboardingStatus == "complete" && PayoutsEnabled;
}
