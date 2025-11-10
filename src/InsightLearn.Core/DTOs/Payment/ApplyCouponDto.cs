using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Payment;

public class ApplyCouponDto
{
    [Required] public string CouponCode { get; set; } = string.Empty;
    [Required] public Guid CourseId { get; set; }
    [Required][Range(0, double.MaxValue)] public decimal OriginalAmount { get; set; }
}
