using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Payout;

/// <summary>
/// Instructor analytics and earnings data
/// </summary>
public class InstructorAnalyticsDto
{
    /// <summary>
    /// Instructor user ID
    /// </summary>
    [Required(ErrorMessage = "Instructor ID is required")]
    public Guid InstructorId { get; set; }

    /// <summary>
    /// Total revenue generated
    /// </summary>
    [Range(0, 10000000.00, ErrorMessage = "Total revenue must be between $0 and $10,000,000")]
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Total earnings after fees
    /// </summary>
    [Range(0, 10000000.00, ErrorMessage = "Total earnings must be between $0 and $10,000,000")]
    public decimal TotalEarnings { get; set; }

    /// <summary>
    /// Pending payout amount
    /// </summary>
    [Range(0, 100000.00, ErrorMessage = "Pending amount must be between $0 and $100,000")]
    public decimal PendingAmount { get; set; }

    /// <summary>
    /// Available balance for payout
    /// </summary>
    [Range(0, 100000.00, ErrorMessage = "Available balance must be between $0 and $100,000")]
    public decimal AvailableBalance { get; set; }

    /// <summary>
    /// Total number of courses
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total courses must be a non-negative number")]
    public int TotalCourses { get; set; }

    /// <summary>
    /// Total number of students
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total students must be a non-negative number")]
    public int TotalStudents { get; set; }

    /// <summary>
    /// Total number of enrollments
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total enrollments must be a non-negative number")]
    public int TotalEnrollments { get; set; }

    /// <summary>
    /// Average course rating
    /// </summary>
    [Range(0, 5, ErrorMessage = "Average rating must be between 0 and 5")]
    public double AverageRating { get; set; }

    /// <summary>
    /// Total number of reviews
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Total reviews must be a non-negative number")]
    public int TotalReviews { get; set; }

    /// <summary>
    /// Revenue this month
    /// </summary>
    [Range(0, 1000000.00, ErrorMessage = "Monthly revenue must be between $0 and $1,000,000")]
    public decimal RevenueThisMonth { get; set; }

    /// <summary>
    /// Revenue last month
    /// </summary>
    [Range(0, 1000000.00, ErrorMessage = "Last month revenue must be between $0 and $1,000,000")]
    public decimal RevenueLastMonth { get; set; }

    /// <summary>
    /// Revenue growth percentage
    /// </summary>
    [Range(-100, 1000, ErrorMessage = "Growth percentage must be between -100% and 1000%")]
    public double RevenueGrowthPercentage { get; set; }

    /// <summary>
    /// Best selling course ID
    /// </summary>
    public Guid? BestSellingCourseId { get; set; }

    /// <summary>
    /// Best selling course title
    /// </summary>
    [StringLength(200, ErrorMessage = "Course title cannot exceed 200 characters")]
    public string? BestSellingCourseTitle { get; set; }

    /// <summary>
    /// Last payout date
    /// </summary>
    public DateTime? LastPayoutDate { get; set; }

    /// <summary>
    /// Next payout date
    /// </summary>
    public DateTime? NextPayoutDate { get; set; }
}
