using Stripe;
using Stripe.Checkout;
using InsightLearn.Application.DTOs;
using InsightLearn.Application.Interfaces;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly InsightLearnDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private readonly string _stripeSecretKey;
    private readonly string _stripePublishableKey;

    public PaymentService(
        InsightLearnDbContext context,
        IConfiguration configuration,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _stripeSecretKey = configuration["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe SecretKey not found");
        _stripePublishableKey = configuration["Stripe:PublishableKey"] ?? throw new InvalidOperationException("Stripe PublishableKey not found");
        
        StripeConfiguration.ApiKey = _stripeSecretKey;
    }

    public async Task<PaymentIntentDto> CreatePaymentIntentAsync(CreatePaymentDto createPaymentDto)
    {
        try
        {
            var course = await _context.Courses
                .Include(c => c.Instructor)
                .FirstOrDefaultAsync(c => c.Id == createPaymentDto.CourseId);

            if (course == null)
            {
                throw new ArgumentException("Course not found");
            }

            // Check if user is already enrolled
            var existingEnrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserId == createPaymentDto.UserId && e.CourseId == createPaymentDto.CourseId);

            if (existingEnrollment != null)
            {
                throw new InvalidOperationException("User is already enrolled in this course");
            }

            var amount = course.CurrentPrice;
            var currency = "usd";

            // Apply coupon if provided
            if (!string.IsNullOrEmpty(createPaymentDto.CouponCode))
            {
                var coupon = await ValidateCouponAsync(createPaymentDto.CouponCode, course.Id, amount);
                if (coupon != null)
                {
                    var discountAmount = coupon.CalculateDiscount(amount);
                    amount -= discountAmount;
                }
            }

            // Create Stripe PaymentIntent
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = currency,
                Metadata = new Dictionary<string, string>
                {
                    { "course_id", course.Id.ToString() },
                    { "user_id", createPaymentDto.UserId.ToString() },
                    { "coupon_code", createPaymentDto.CouponCode ?? "" }
                },
                Description = $"Course: {course.Title}",
                ReceiptEmail = createPaymentDto.UserEmail
            };

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options);

            // Create payment record
            var payment = new Payment
            {
                UserId = createPaymentDto.UserId,
                CourseId = createPaymentDto.CourseId,
                Amount = amount,
                OriginalAmount = course.CurrentPrice,
                DiscountAmount = course.CurrentPrice - amount,
                Currency = currency.ToUpper(),
                PaymentGatewayId = paymentIntent.Id,
                Status = PaymentStatus.Pending,
                PaymentMethod = Core.Entities.PaymentMethodType.CreditCard,
                InvoiceNumber = GenerateInvoiceNumber()
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return new PaymentIntentDto
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                Amount = amount,
                Currency = currency,
                PaymentId = payment.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent for course {CourseId}", createPaymentDto.CourseId);
            throw;
        }
    }

    public async Task<bool> ConfirmPaymentAsync(string paymentIntentId)
    {
        try
        {
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId);

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentGatewayId == paymentIntentId);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for PaymentIntent {PaymentIntentId}", paymentIntentId);
                return false;
            }

            if (paymentIntent.Status == "succeeded")
            {
                payment.Status = PaymentStatus.Completed;
                payment.ProcessedAt = DateTime.UtcNow;
                payment.TransactionId = paymentIntent.Id;

                // Create enrollment
                var enrollment = new Enrollment
                {
                    UserId = payment.UserId,
                    CourseId = payment.CourseId,
                    AmountPaid = payment.Amount,
                    Status = EnrollmentStatus.Active
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Payment confirmed and enrollment created for user {UserId} in course {CourseId}", 
                    payment.UserId, payment.CourseId);

                return true;
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailureReason = $"Stripe status: {paymentIntent.Status}";
                await _context.SaveChangesAsync();

                _logger.LogWarning("Payment failed for PaymentIntent {PaymentIntentId}, status: {Status}", 
                    paymentIntentId, paymentIntent.Status);

                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment for PaymentIntent {PaymentIntentId}", paymentIntentId);
            return false;
        }
    }

    public async Task<PaymentDto?> GetPaymentAsync(Guid paymentId)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Course)
                .Include(p => p.Coupon)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null) return null;

            return new PaymentDto
            {
                Id = payment.Id,
                UserId = payment.UserId,
                UserEmail = payment.User.Email,
                CourseId = payment.CourseId,
                CourseTitle = payment.Course.Title,
                Amount = payment.Amount,
                OriginalAmount = payment.OriginalAmount,
                DiscountAmount = payment.DiscountAmount,
                Currency = payment.Currency,
                Status = payment.Status,
                PaymentMethod = payment.PaymentMethod,
                CreatedAt = payment.CreatedAt,
                ProcessedAt = payment.ProcessedAt,
                InvoiceNumber = payment.InvoiceNumber,
                CouponCode = payment.Coupon?.Code
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment {PaymentId}", paymentId);
            throw;
        }
    }

    public async Task<List<PaymentDto>> GetUserPaymentsAsync(Guid userId, int page = 1, int pageSize = 10)
    {
        try
        {
            var payments = await _context.Payments
                .Include(p => p.Course)
                .Include(p => p.Coupon)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PaymentDto
                {
                    Id = p.Id,
                    UserId = p.UserId,
                    CourseId = p.CourseId,
                    CourseTitle = p.Course.Title,
                    Amount = p.Amount,
                    OriginalAmount = p.OriginalAmount,
                    DiscountAmount = p.DiscountAmount,
                    Currency = p.Currency,
                    Status = p.Status,
                    PaymentMethod = p.PaymentMethod,
                    CreatedAt = p.CreatedAt,
                    ProcessedAt = p.ProcessedAt,
                    InvoiceNumber = p.InvoiceNumber,
                    CouponCode = p.Coupon != null ? p.Coupon.Code : null
                })
                .ToListAsync();

            return payments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for user {UserId}", userId);
            throw;
        }
    }

    public async Task<RefundResultDto> ProcessRefundAsync(Guid paymentId, decimal? amount = null, string? reason = null)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                return new RefundResultDto { Success = false, ErrorMessage = "Payment not found" };
            }

            if (payment.Status != PaymentStatus.Completed)
            {
                return new RefundResultDto { Success = false, ErrorMessage = "Payment is not in a refundable state" };
            }

            var refundAmount = amount ?? payment.Amount;

            // Process refund with Stripe
            var refundService = new RefundService();
            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = payment.PaymentGatewayId,
                Amount = (long)(refundAmount * 100), // Convert to cents
                Reason = reason switch
                {
                    "requested_by_customer" => "requested_by_customer",
                    "duplicate" => "duplicate",
                    "fraudulent" => "fraudulent",
                    _ => "requested_by_customer"
                }
            };

            var refund = await refundService.CreateAsync(refundOptions);

            if (refund.Status == "succeeded")
            {
                payment.Status = refundAmount >= payment.Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
                payment.RefundAmount = (payment.RefundAmount ?? 0) + refundAmount;
                payment.RefundedAt = DateTime.UtcNow;
                payment.Notes = reason;

                // Update enrollment status
                var enrollment = await _context.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == payment.UserId && e.CourseId == payment.CourseId);

                if (enrollment != null)
                {
                    enrollment.Status = EnrollmentStatus.Refunded;
                }

                await _context.SaveChangesAsync();

                return new RefundResultDto
                {
                    Success = true,
                    RefundId = refund.Id,
                    RefundAmount = refundAmount,
                    Status = refund.Status
                };
            }
            else
            {
                return new RefundResultDto 
                { 
                    Success = false, 
                    ErrorMessage = $"Refund failed with status: {refund.Status}" 
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
            return new RefundResultDto 
            { 
                Success = false, 
                ErrorMessage = "An error occurred while processing the refund" 
            };
        }
    }

    public async Task<CouponValidationDto> ValidateCouponAsync(string couponCode, Guid? courseId = null)
    {
        try
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToLower() == couponCode.ToLower() && c.IsActive);

            if (coupon == null)
            {
                return new CouponValidationDto { IsValid = false, ErrorMessage = "Coupon not found" };
            }

            if (!coupon.IsValid)
            {
                return new CouponValidationDto { IsValid = false, ErrorMessage = "Coupon has expired or reached usage limit" };
            }

            if (coupon.CourseId.HasValue && courseId.HasValue && coupon.CourseId != courseId)
            {
                return new CouponValidationDto { IsValid = false, ErrorMessage = "Coupon is not valid for this course" };
            }

            return new CouponValidationDto
            {
                IsValid = true,
                CouponId = coupon.Id,
                DiscountType = coupon.Type,
                DiscountValue = coupon.Value,
                MinimumAmount = coupon.MinimumAmount,
                MaximumDiscount = coupon.MaximumDiscount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating coupon {CouponCode}", couponCode);
            return new CouponValidationDto { IsValid = false, ErrorMessage = "An error occurred while validating the coupon" };
        }
    }

    private async Task<InsightLearn.Core.Entities.Coupon?> ValidateCouponAsync(string couponCode, Guid courseId, decimal amount)
    {
        var coupon = await _context.Coupons
            .FirstOrDefaultAsync(c => c.Code.ToLower() == couponCode.ToLower() && c.IsActive);

        if (coupon?.IsValid == true)
        {
            if (coupon.CourseId.HasValue && coupon.CourseId != courseId)
                return null;

            if (coupon.MinimumAmount.HasValue && amount < coupon.MinimumAmount)
                return null;

            return coupon;
        }

        return null;
    }

    private string GenerateInvoiceNumber()
    {
        return $"INV-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
    }
}