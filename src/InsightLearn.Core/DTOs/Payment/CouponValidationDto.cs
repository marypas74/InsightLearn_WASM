namespace InsightLearn.Core.DTOs.Payment;

public class CouponValidationDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public CouponDto? Coupon { get; set; }
}
