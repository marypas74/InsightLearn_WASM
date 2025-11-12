using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

/// <summary>
/// Monthly payout calculation for instructors based on engagement
/// </summary>
public class InstructorPayout
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid InstructorId { get; set; }

    /// <summary>
    /// Payout month (1-12)
    /// </summary>
    [Required]
    public int Month { get; set; }

    /// <summary>
    /// Payout year (e.g., 2024, 2025)
    /// </summary>
    [Required]
    public int Year { get; set; }

    /// <summary>
    /// Total engagement minutes for this instructor's courses
    /// </summary>
    [Required]
    public long TotalEngagementMinutes { get; set; }

    /// <summary>
    /// Platform-wide total engagement minutes for the month
    /// </summary>
    [Required]
    public long PlatformTotalEngagementMinutes { get; set; }

    /// <summary>
    /// Total platform subscription revenue for the month
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPlatformRevenue { get; set; }

    /// <summary>
    /// Instructor's engagement as percentage of total (0.0000 - 1.0000)
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(5,4)")]
    public decimal EngagementPercentage { get; set; }

    /// <summary>
    /// Platform commission rate (e.g., 0.20 = 20%)
    /// </summary>
    [Column(TypeName = "decimal(5,4)")]
    public decimal PlatformCommissionRate { get; set; } = 0.20m;

    /// <summary>
    /// Final payout amount in EUR
    /// Formula: (TotalPlatformRevenue * (1 - PlatformCommissionRate)) * EngagementPercentage
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PayoutAmount { get; set; }

    /// <summary>
    /// Payout status: "pending", "processing", "paid", "failed", "on_hold"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Stripe transfer ID (when paid via Stripe Connect)
    /// </summary>
    [MaxLength(255)]
    public string? StripeTransferId { get; set; }

    /// <summary>
    /// Date when payout was processed
    /// </summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Error message if payout failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Additional notes (e.g., payment method, bank details)
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Number of unique students engaged with instructor's courses
    /// </summary>
    public int UniqueStudentCount { get; set; }

    /// <summary>
    /// Number of courses that contributed to this payout
    /// </summary>
    public int ActiveCoursesCount { get; set; }

    /// <summary>
    /// Breakdown of engagement by course (stored as JSON)
    /// </summary>
    public string? CourseEngagementBreakdown { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(InstructorId))]
    public virtual User Instructor { get; set; } = null!;
}
