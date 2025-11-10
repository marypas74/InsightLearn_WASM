using InsightLearn.Core.DTOs.Payment;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service implementation for Coupon business logic
/// </summary>
public class CouponService : ICouponService
{
    private readonly ICouponRepository _couponRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ILogger<CouponService> _logger;

    public CouponService(
        ICouponRepository couponRepository,
        ICourseRepository courseRepository,
        ILogger<CouponService> logger)
    {
        _couponRepository = couponRepository;
        _courseRepository = courseRepository;
        _logger = logger;
    }

    public async Task<List<CouponDto>> GetAllCouponsAsync()
    {
        try
        {
            _logger.LogInformation("[CouponService] Getting all coupons");

            var coupons = await _couponRepository.GetAllAsync();
            var dtos = coupons.Select(MapToDto).ToList();

            _logger.LogInformation("[CouponService] Retrieved {Count} coupons", dtos.Count);

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CouponService] Error getting all coupons");
            throw;
        }
    }

    public async Task<List<CouponDto>> GetActiveCouponsAsync()
    {
        try
        {
            _logger.LogInformation("[CouponService] Getting active coupons");

            var coupons = await _couponRepository.GetActiveCouponsAsync();
            var dtos = coupons.Select(MapToDto).ToList();

            _logger.LogInformation("[CouponService] Retrieved {Count} active coupons", dtos.Count);

            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CouponService] Error getting active coupons");
            throw;
        }
    }

    public async Task<CouponDto?> GetCouponByCodeAsync(string code)
    {
        try
        {
            _logger.LogInformation("[CouponService] Getting coupon by code {Code}", code);

            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogWarning("[CouponService] Empty coupon code provided");
                return null;
            }

            var coupon = await _couponRepository.GetByCodeAsync(code.ToUpperInvariant());
            if (coupon == null)
            {
                _logger.LogWarning("[CouponService] Coupon with code {Code} not found", code);
                return null;
            }

            var dto = MapToDto(coupon);
            _logger.LogInformation("[CouponService] Retrieved coupon {CouponId} with code {Code}",
                coupon.Id, code);

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CouponService] Error getting coupon by code {Code}", code);
            throw;
        }
    }

    public async Task<CouponValidationDto> ValidateCouponAsync(string code, Guid courseId, decimal amount)
    {
        try
        {
            _logger.LogInformation("[CouponService] Validating coupon {Code} for course {CourseId} with amount {Amount}",
                code, courseId, amount);

            var result = new CouponValidationDto
            {
                IsValid = false,
                DiscountAmount = 0,
                FinalAmount = amount
            };

            // Get the coupon
            var coupon = await _couponRepository.GetByCodeAsync(code.ToUpperInvariant());
            if (coupon == null)
            {
                result.ErrorMessage = "Invalid coupon code";
                _logger.LogWarning("[CouponService] Coupon {Code} not found", code);
                return result;
            }

            result.Coupon = MapToDto(coupon);

            // Check if active
            if (!coupon.IsActive)
            {
                result.ErrorMessage = "This coupon is no longer active";
                _logger.LogWarning("[CouponService] Coupon {Code} is not active", code);
                return result;
            }

            // Check expiry dates
            var now = DateTime.UtcNow;
            if (now < coupon.ValidFrom)
            {
                result.ErrorMessage = $"This coupon is not valid until {coupon.ValidFrom:yyyy-MM-dd}";
                _logger.LogWarning("[CouponService] Coupon {Code} not yet valid", code);
                return result;
            }

            if (now > coupon.ValidUntil)
            {
                result.ErrorMessage = "This coupon has expired";
                _logger.LogWarning("[CouponService] Coupon {Code} has expired", code);
                return result;
            }

            // Check usage limit
            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            {
                result.ErrorMessage = "This coupon has reached its usage limit";
                _logger.LogWarning("[CouponService] Coupon {Code} usage limit reached", code);
                return result;
            }

            // Check minimum amount
            if (coupon.MinimumAmount.HasValue && amount < coupon.MinimumAmount.Value)
            {
                result.ErrorMessage = $"Minimum purchase amount of ${coupon.MinimumAmount.Value:F2} required";
                _logger.LogWarning("[CouponService] Amount {Amount} below minimum {MinAmount} for coupon {Code}",
                    amount, coupon.MinimumAmount.Value, code);
                return result;
            }

            // Check course-specific coupon
            if (coupon.CourseId.HasValue && coupon.CourseId.Value != courseId)
            {
                result.ErrorMessage = "This coupon is not valid for the selected course";
                _logger.LogWarning("[CouponService] Coupon {Code} not valid for course {CourseId}", code, courseId);
                return result;
            }

            // If course-specific, verify course exists and get price
            if (coupon.CourseId.HasValue)
            {
                var course = await _courseRepository.GetByIdAsync(courseId);
                if (course == null)
                {
                    result.ErrorMessage = "Course not found";
                    _logger.LogError("[CouponService] Course {CourseId} not found", courseId);
                    return result;
                }

                // Verify minimum amount is less than course price
                if (coupon.MinimumAmount.HasValue && coupon.MinimumAmount.Value >= course.Price)
                {
                    result.ErrorMessage = "Coupon minimum amount exceeds course price";
                    _logger.LogWarning("[CouponService] Coupon minimum {MinAmount} exceeds course price {Price}",
                        coupon.MinimumAmount.Value, course.Price);
                    return result;
                }
            }

            // Calculate discount
            decimal discount = 0;
            if (coupon.Type == CouponType.Percentage)
            {
                if (coupon.Value > 100)
                {
                    _logger.LogError("[CouponService] Invalid percentage value {Value} for coupon {Code}",
                        coupon.Value, code);
                    result.ErrorMessage = "Invalid coupon configuration";
                    return result;
                }

                discount = amount * (coupon.Value / 100);
                _logger.LogInformation("[CouponService] Calculated {Percent}% discount: {Discount}",
                    coupon.Value, discount);
            }
            else if (coupon.Type == CouponType.FixedAmount)
            {
                discount = coupon.Value;
                _logger.LogInformation("[CouponService] Fixed discount amount: {Discount}", discount);
            }

            // Apply maximum discount cap
            if (coupon.MaximumDiscount.HasValue && discount > coupon.MaximumDiscount.Value)
            {
                discount = coupon.MaximumDiscount.Value;
                _logger.LogInformation("[CouponService] Discount capped at maximum: {MaxDiscount}",
                    coupon.MaximumDiscount.Value);
            }

            // Ensure discount doesn't exceed the amount
            discount = Math.Min(discount, amount);

            result.IsValid = true;
            result.DiscountAmount = Math.Round(discount, 2);
            result.FinalAmount = Math.Round(amount - discount, 2);
            result.ErrorMessage = null;

            _logger.LogInformation("[CouponService] Coupon {Code} validated successfully. Discount: {Discount}, Final: {Final}",
                code, result.DiscountAmount, result.FinalAmount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CouponService] Error validating coupon {Code}", code);

            return new CouponValidationDto
            {
                IsValid = false,
                ErrorMessage = "An error occurred while validating the coupon",
                DiscountAmount = 0,
                FinalAmount = amount
            };
        }
    }

    public async Task<CouponDto> CreateCouponAsync(CouponDto dto)
    {
        try
        {
            _logger.LogInformation("[CouponService] Creating new coupon with code {Code}", dto.Code);

            // Validate coupon type
            if (!Enum.TryParse<CouponType>(dto.Type, out var couponType))
            {
                _logger.LogError("[CouponService] Invalid coupon type: {Type}", dto.Type);
                throw new ArgumentException($"Invalid coupon type: {dto.Type}");
            }

            // Generate code if not provided
            if (string.IsNullOrWhiteSpace(dto.Code))
            {
                dto.Code = GenerateCouponCode(couponType, dto.Value);
                _logger.LogInformation("[CouponService] Generated coupon code: {Code}", dto.Code);
            }
            else
            {
                // Validate code format (alphanumeric only)
                if (!Regex.IsMatch(dto.Code, @"^[A-Z0-9]+$", RegexOptions.IgnoreCase))
                {
                    _logger.LogError("[CouponService] Invalid coupon code format: {Code}", dto.Code);
                    throw new ArgumentException("Coupon code must contain only alphanumeric characters");
                }

                dto.Code = dto.Code.ToUpperInvariant();
            }

            // Check if code already exists
            var existing = await _couponRepository.GetByCodeAsync(dto.Code);
            if (existing != null)
            {
                _logger.LogError("[CouponService] Coupon with code {Code} already exists", dto.Code);
                throw new InvalidOperationException($"Coupon with code {dto.Code} already exists");
            }

            // Validate value
            if (dto.Value <= 0)
            {
                _logger.LogError("[CouponService] Invalid coupon value: {Value}", dto.Value);
                throw new ArgumentException("Coupon value must be greater than 0");
            }

            if (couponType == CouponType.Percentage && dto.Value > 100)
            {
                _logger.LogError("[CouponService] Percentage value {Value} exceeds 100", dto.Value);
                throw new ArgumentException("Percentage discount cannot exceed 100%");
            }

            // Validate dates
            if (dto.ValidFrom >= dto.ValidUntil)
            {
                _logger.LogError("[CouponService] Invalid date range: ValidFrom {From} >= ValidUntil {Until}",
                    dto.ValidFrom, dto.ValidUntil);
                throw new ArgumentException("ValidFrom must be before ValidUntil");
            }

            // Validate minimum amount
            if (dto.MinimumAmount.HasValue && dto.MinimumAmount.Value <= 0)
            {
                _logger.LogError("[CouponService] Invalid minimum amount: {Amount}", dto.MinimumAmount.Value);
                throw new ArgumentException("Minimum amount must be greater than 0");
            }

            // For fixed amount coupons, ensure minimum amount is greater than discount
            if (couponType == CouponType.FixedAmount && dto.MinimumAmount.HasValue)
            {
                if (dto.MinimumAmount.Value <= dto.Value)
                {
                    _logger.LogError("[CouponService] Minimum amount {MinAmount} must be greater than discount {Value}",
                        dto.MinimumAmount.Value, dto.Value);
                    throw new ArgumentException("Minimum amount must be greater than the discount value");
                }
            }

            var newCoupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = dto.Code,
                Description = dto.Description,
                Type = couponType,
                Value = dto.Value,
                MinimumAmount = dto.MinimumAmount,
                MaximumDiscount = dto.MaximumDiscount,
                UsageLimit = dto.UsageLimit,
                UsedCount = 0,
                ValidFrom = dto.ValidFrom,
                ValidUntil = dto.ValidUntil,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _couponRepository.CreateAsync(newCoupon);
            var resultDto = MapToDto(created);

            _logger.LogInformation("[CouponService] Created coupon {CouponId} with code {Code}",
                created.Id, created.Code);

            return resultDto;
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "[CouponService] Error creating coupon");
            throw;
        }
    }

    public async Task<CouponDto?> UpdateCouponAsync(Guid id, CouponDto dto)
    {
        try
        {
            _logger.LogInformation("[CouponService] Updating coupon {CouponId}", id);

            var coupon = await _couponRepository.GetByIdAsync(id);
            if (coupon == null)
            {
                _logger.LogWarning("[CouponService] Coupon {CouponId} not found for update", id);
                return null;
            }

            // Cannot modify coupon after first usage
            if (coupon.UsedCount > 0)
            {
                _logger.LogError("[CouponService] Cannot modify coupon {CouponId} after it has been used", id);
                throw new InvalidOperationException("Cannot modify a coupon that has already been used");
            }

            // Apply updates
            var hasChanges = false;

            // Description can always be updated
            if (dto.Description != coupon.Description)
            {
                coupon.Description = dto.Description;
                hasChanges = true;
            }

            // Update type if provided and valid
            if (!string.IsNullOrEmpty(dto.Type) && Enum.TryParse<CouponType>(dto.Type, out var couponType))
            {
                if (coupon.Type != couponType)
                {
                    coupon.Type = couponType;
                    hasChanges = true;
                }
            }

            // Update value
            if (dto.Value > 0 && dto.Value != coupon.Value)
            {
                if (coupon.Type == CouponType.Percentage && dto.Value > 100)
                {
                    _logger.LogError("[CouponService] Percentage value {Value} exceeds 100", dto.Value);
                    throw new ArgumentException("Percentage discount cannot exceed 100%");
                }
                coupon.Value = dto.Value;
                hasChanges = true;
            }

            // Update minimum amount
            if (dto.MinimumAmount != coupon.MinimumAmount)
            {
                if (dto.MinimumAmount.HasValue && dto.MinimumAmount.Value <= 0)
                {
                    _logger.LogError("[CouponService] Invalid minimum amount: {Amount}", dto.MinimumAmount.Value);
                    throw new ArgumentException("Minimum amount must be greater than 0");
                }

                // For fixed amount coupons, ensure minimum amount is greater than discount
                if (coupon.Type == CouponType.FixedAmount && dto.MinimumAmount.HasValue)
                {
                    if (dto.MinimumAmount.Value <= coupon.Value)
                    {
                        _logger.LogError("[CouponService] Minimum amount {MinAmount} must be greater than discount {Value}",
                            dto.MinimumAmount.Value, coupon.Value);
                        throw new ArgumentException("Minimum amount must be greater than the discount value");
                    }
                }

                coupon.MinimumAmount = dto.MinimumAmount;
                hasChanges = true;
            }

            // Update maximum discount
            if (dto.MaximumDiscount != coupon.MaximumDiscount)
            {
                coupon.MaximumDiscount = dto.MaximumDiscount;
                hasChanges = true;
            }

            // Update usage limit (can only increase, not decrease)
            if (dto.UsageLimit.HasValue && dto.UsageLimit.Value > (coupon.UsageLimit ?? 0))
            {
                coupon.UsageLimit = dto.UsageLimit;
                hasChanges = true;
            }

            // Update validity dates
            if (dto.ValidFrom != coupon.ValidFrom || dto.ValidUntil != coupon.ValidUntil)
            {
                if (dto.ValidFrom >= dto.ValidUntil)
                {
                    _logger.LogError("[CouponService] Invalid date range: ValidFrom {From} >= ValidUntil {Until}",
                        dto.ValidFrom, dto.ValidUntil);
                    throw new ArgumentException("ValidFrom must be before ValidUntil");
                }

                coupon.ValidFrom = dto.ValidFrom;
                coupon.ValidUntil = dto.ValidUntil;
                hasChanges = true;
            }

            // Update active status
            if (dto.IsActive != coupon.IsActive)
            {
                coupon.IsActive = dto.IsActive;
                hasChanges = true;
            }

            if (!hasChanges)
            {
                _logger.LogInformation("[CouponService] No changes to apply for coupon {CouponId}", id);
                return MapToDto(coupon);
            }

            var updated = await _couponRepository.UpdateAsync(coupon);
            var resultDto = MapToDto(updated);

            _logger.LogInformation("[CouponService] Updated coupon {CouponId}", id);

            return resultDto;
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "[CouponService] Error updating coupon {CouponId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteCouponAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[CouponService] Attempting to delete coupon {CouponId}", id);

            var coupon = await _couponRepository.GetByIdAsync(id);
            if (coupon == null)
            {
                _logger.LogWarning("[CouponService] Coupon {CouponId} not found for deletion", id);
                return false;
            }

            // Check if coupon has been used
            if (coupon.UsedCount > 0)
            {
                _logger.LogWarning("[CouponService] Cannot delete coupon {CouponId} as it has been used {Count} times",
                    id, coupon.UsedCount);

                // Soft delete - mark as inactive instead
                coupon.IsActive = false;
                await _couponRepository.UpdateAsync(coupon);

                _logger.LogInformation("[CouponService] Soft deleted (deactivated) coupon {CouponId}", id);
                return true;
            }

            await _couponRepository.DeleteAsync(id);

            _logger.LogInformation("[CouponService] Successfully deleted coupon {CouponId}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CouponService] Error deleting coupon {CouponId}", id);
            return false;
        }
    }

    public async Task<bool> IncrementUsageAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("[CouponService] Incrementing usage for coupon {CouponId}", id);

            var coupon = await _couponRepository.GetByIdAsync(id);
            if (coupon == null)
            {
                _logger.LogWarning("[CouponService] Coupon {CouponId} not found", id);
                return false;
            }

            // Check usage limit before incrementing
            if (coupon.UsageLimit.HasValue && coupon.UsedCount >= coupon.UsageLimit.Value)
            {
                _logger.LogError("[CouponService] Coupon {CouponId} has reached its usage limit", id);
                return false;
            }

            await _couponRepository.IncrementUsageAsync(id);

            _logger.LogInformation("[CouponService] Successfully incremented usage for coupon {CouponId}. New count: {Count}",
                id, coupon.UsedCount + 1);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CouponService] Error incrementing usage for coupon {CouponId}", id);
            return false;
        }
    }

    private string GenerateCouponCode(CouponType type, decimal value)
    {
        var prefix = type == CouponType.Percentage ? "SAVE" : "DISCOUNT";
        var valueStr = type == CouponType.Percentage ? $"{(int)value}" : $"{(int)value}";
        var suffix = type == CouponType.Percentage ? "OFF" : "";
        var random = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant();

        return $"{prefix}{valueStr}{suffix}{random}";
    }

    private CouponDto MapToDto(Coupon coupon)
    {
        return new CouponDto
        {
            Id = coupon.Id,
            Code = coupon.Code,
            Description = coupon.Description,
            Type = coupon.Type.ToString(),
            Value = coupon.Value,
            MinimumAmount = coupon.MinimumAmount,
            MaximumDiscount = coupon.MaximumDiscount,
            UsageLimit = coupon.UsageLimit,
            UsedCount = coupon.UsedCount,
            ValidFrom = coupon.ValidFrom,
            ValidUntil = coupon.ValidUntil,
            IsActive = coupon.IsActive,
            IsValid = coupon.IsValid
        };
    }
}