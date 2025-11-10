using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Repository interface for Coupon entity operations
/// </summary>
public interface ICouponRepository
{
    /// <summary>
    /// Gets all coupons
    /// </summary>
    Task<IEnumerable<Coupon>> GetAllAsync();

    /// <summary>
    /// Gets a coupon by its unique identifier
    /// </summary>
    Task<Coupon?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a coupon by its code
    /// </summary>
    Task<Coupon?> GetByCodeAsync(string code);

    /// <summary>
    /// Gets all active (valid) coupons
    /// </summary>
    Task<IEnumerable<Coupon>> GetActiveCouponsAsync();

    /// <summary>
    /// Gets coupons for a specific course
    /// </summary>
    Task<IEnumerable<Coupon>> GetByCourseIdAsync(Guid courseId);

    /// <summary>
    /// Creates a new coupon
    /// </summary>
    Task<Coupon> CreateAsync(Coupon coupon);

    /// <summary>
    /// Updates an existing coupon
    /// </summary>
    Task<Coupon> UpdateAsync(Coupon coupon);

    /// <summary>
    /// Deletes a coupon by ID
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Increments the usage count for a coupon
    /// </summary>
    Task IncrementUsageAsync(Guid couponId);

    /// <summary>
    /// Validates if a coupon is valid for a given course
    /// </summary>
    Task<bool> IsValidAsync(string code, Guid? courseId = null);
}
