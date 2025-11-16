using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Repository interface for Coupon entity operations
/// </summary>
public interface ICouponRepository
{
    /// <summary>
    /// Gets all coupons with pagination (PERF-4 fix)
    /// </summary>
    Task<IEnumerable<Coupon>> GetAllAsync(int page = 1, int pageSize = 50);

    /// <summary>
    /// Gets a coupon by its unique identifier
    /// </summary>
    Task<Coupon?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a coupon by its code
    /// </summary>
    Task<Coupon?> GetByCodeAsync(string code);

    /// <summary>
    /// Gets all active (valid) coupons with pagination (PERF-4 fix)
    /// </summary>
    Task<IEnumerable<Coupon>> GetActiveCouponsAsync(int page = 1, int pageSize = 50);

    /// <summary>
    /// Gets coupons for a specific course with pagination (PERF-4 fix)
    /// </summary>
    Task<IEnumerable<Coupon>> GetByCourseIdAsync(Guid courseId, int page = 1, int pageSize = 20);

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
