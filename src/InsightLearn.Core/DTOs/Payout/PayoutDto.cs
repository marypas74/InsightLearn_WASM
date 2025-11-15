using System.ComponentModel.DataAnnotations;
using InsightLearn.Core.Validation;

namespace InsightLearn.Core.DTOs.Payout;

/// <summary>
/// Instructor payout data transfer object
/// </summary>
public class PayoutDto
{
    /// <summary>
    /// Unique payout identifier
    /// </summary>
    [Required(ErrorMessage = "Payout ID is required")]
    public Guid Id { get; set; }

    /// <summary>
    /// Instructor user ID
    /// </summary>
    [Required(ErrorMessage = "Instructor ID is required")]
    public Guid InstructorId { get; set; }

    /// <summary>
    /// Instructor name
    /// </summary>
    [Required(ErrorMessage = "Instructor name is required")]
    [StringLength(200, ErrorMessage = "Instructor name cannot exceed 200 characters")]
    public string InstructorName { get; set; } = string.Empty;

    /// <summary>
    /// Payout amount
    /// </summary>
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 100000.00, ErrorMessage = "Amount must be between $0.01 and $100,000")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code
    /// </summary>
    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter code")]
    [ValidCurrency]  // Use custom validator for ISO 4217 compliance
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Payout status
    /// </summary>
    [Required(ErrorMessage = "Status is required")]
    [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
    [RegularExpression(@"^(Pending|Processing|Completed|Failed|Cancelled)$", ErrorMessage = "Invalid payout status")]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Payout method
    /// </summary>
    [Required(ErrorMessage = "Payout method is required")]
    [StringLength(50, ErrorMessage = "Payout method cannot exceed 50 characters")]
    [RegularExpression(@"^(bank_transfer|paypal|stripe|check|wire)$", ErrorMessage = "Invalid payout method")]
    public string PayoutMethod { get; set; } = string.Empty;

    /// <summary>
    /// Period start date
    /// </summary>
    [Required(ErrorMessage = "Period start date is required")]
    public DateTime PeriodStartDate { get; set; }

    /// <summary>
    /// Period end date
    /// </summary>
    [Required(ErrorMessage = "Period end date is required")]
    public DateTime PeriodEndDate { get; set; }

    /// <summary>
    /// Number of transactions included
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Transaction count must be a non-negative number")]
    public int TransactionCount { get; set; }

    /// <summary>
    /// Total earnings before fees
    /// </summary>
    [Range(0, 100000.00, ErrorMessage = "Gross amount must be between $0 and $100,000")]
    public decimal GrossAmount { get; set; }

    /// <summary>
    /// Platform fee amount
    /// </summary>
    [Range(0, 100000.00, ErrorMessage = "Platform fee must be between $0 and $100,000")]
    public decimal PlatformFee { get; set; }

    /// <summary>
    /// Processing fee amount
    /// </summary>
    [Range(0, 1000.00, ErrorMessage = "Processing fee must be between $0 and $1,000")]
    public decimal ProcessingFee { get; set; }

    /// <summary>
    /// Tax amount withheld
    /// </summary>
    [Range(0, 100000.00, ErrorMessage = "Tax amount must be between $0 and $100,000")]
    public decimal TaxAmount { get; set; }

    /// <summary>
    /// Request date
    /// </summary>
    [Required(ErrorMessage = "Request date is required")]
    public DateTime RequestDate { get; set; }

    /// <summary>
    /// Processing date
    /// </summary>
    public DateTime? ProcessingDate { get; set; }

    /// <summary>
    /// Completion date
    /// </summary>
    public DateTime? CompletionDate { get; set; }

    /// <summary>
    /// Transaction reference
    /// </summary>
    [StringLength(200, ErrorMessage = "Transaction reference cannot exceed 200 characters")]
    public string? TransactionReference { get; set; }

    /// <summary>
    /// Notes or comments
    /// </summary>
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
}
