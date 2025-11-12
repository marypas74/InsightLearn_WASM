using InsightLearn.Core.DTOs.Payment;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service interface for Coupon business logic
/// </summary>
public interface ICouponService
{
    /// <summary>
    /// Gets all coupons
    /// </summary>
    Task<List<CouponDto>> GetAllCouponsAsync();

    /// <summary>
    /// Gets only active (valid) coupons
    /// </summary>
    Task<List<CouponDto>> GetActiveCouponsAsync();

    /// <summary>
    /// Gets a coupon by its code
    /// </summary>
    Task<CouponDto?> GetCouponByCodeAsync(string code);

    /// <summary>
    /// Validates a coupon for a specific course and amount
    /// </summary>
    Task<CouponValidationDto> ValidateCouponAsync(string code, Guid courseId, decimal amount);

    /// <summary>
    /// Creates a new coupon
    /// </summary>
    Task<CouponDto> CreateCouponAsync(CouponDto dto);

    /// <summary>
    /// Updates an existing coupon
    /// </summary>
    Task<CouponDto?> UpdateCouponAsync(Guid id, CouponDto dto);

    /// <summary>
    /// Deletes a coupon
    /// </summary>
    Task<bool> DeleteCouponAsync(Guid id);

    /// <summary>
    /// Increments the usage count for a coupon
    /// </summary>
    Task<bool> IncrementUsageAsync(Guid id);
}