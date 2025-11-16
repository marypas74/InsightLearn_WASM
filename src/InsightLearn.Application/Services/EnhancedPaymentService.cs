using InsightLearn.Core.DTOs.Payment;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace InsightLearn.Application.Services;

/// <summary>
/// Enhanced payment processing service with Stripe and PayPal integration
/// </summary>
public class EnhancedPaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<EnhancedPaymentService> _logger;
    private readonly IConfiguration _configuration;
    private readonly MetricsService _metricsService;

    // Configuration keys
    private readonly string _stripePublicKey;
    private readonly string _stripeSecretKey;
    private readonly string _paypalClientId;
    private readonly string _paypalClientSecret;
    private readonly string _currency;

    public EnhancedPaymentService(
        IPaymentRepository paymentRepository,
        ICouponRepository couponRepository,
        ICourseRepository courseRepository,
        IEnrollmentRepository enrollmentRepository,
        InsightLearnDbContext context,
        ILogger<EnhancedPaymentService> logger,
        IConfiguration configuration,
        MetricsService metricsService)
    {
        _paymentRepository = paymentRepository;
        _couponRepository = couponRepository;
        _courseRepository = courseRepository;
        _enrollmentRepository = enrollmentRepository;
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _metricsService = metricsService;

        // SECURITY FIX (CRIT-5): Enforce payment credentials from environment variables
        // NO fallback to mock values - fail fast if not configured
        _stripePublicKey = Environment.GetEnvironmentVariable("STRIPE_PUBLIC_KEY")
            ?? configuration["Stripe:PublicKey"]
            ?? throw new InvalidOperationException("STRIPE_PUBLIC_KEY environment variable not configured");

        _stripeSecretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
            ?? configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("STRIPE_SECRET_KEY environment variable not configured");

        _paypalClientId = Environment.GetEnvironmentVariable("PAYPAL_CLIENT_ID")
            ?? configuration["PayPal:ClientId"]
            ?? throw new InvalidOperationException("PAYPAL_CLIENT_ID environment variable not configured");

        _paypalClientSecret = Environment.GetEnvironmentVariable("PAYPAL_CLIENT_SECRET")
            ?? configuration["PayPal:ClientSecret"]
            ?? throw new InvalidOperationException("PAYPAL_CLIENT_SECRET environment variable not configured");

        _currency = configuration["Payment:Currency"] ?? "USD";

        // Validate no mock/insecure values
        if (_stripePublicKey.Contains("mock", StringComparison.OrdinalIgnoreCase) ||
            _stripeSecretKey.Contains("mock", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Stripe credentials contain mock values");

        if (_paypalClientId.Contains("mock", StringComparison.OrdinalIgnoreCase) ||
            _paypalClientSecret.Contains("mock", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("PayPal credentials contain mock values");

        logger.LogInformation("[SECURITY] Payment credentials loaded (CRIT-5 fix)");
    }

    // ===== Checkout Methods =====

    public async Task<StripeCheckoutDto> CreateStripeCheckoutAsync(CreatePaymentDto dto)
    {
        // Begin transaction (ALL OR NOTHING)
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Validate course exists
            var course = await _courseRepository.GetByIdAsync(dto.CourseId);
            if (course == null)
            {
                _logger.LogError("[Payment] Course not found: {CourseId}", dto.CourseId);
                throw new ArgumentException($"Course with ID {dto.CourseId} not found");
            }

            // 2. Check if user is already enrolled (prevent double payment)
            var isEnrolled = await _enrollmentRepository.IsUserEnrolledAsync(dto.UserId, dto.CourseId);
            if (isEnrolled)
            {
                _logger.LogWarning("[Payment] User {UserId} already enrolled in course {CourseId}",
                    dto.UserId, dto.CourseId);
                throw new InvalidOperationException("User is already enrolled in this course");
            }

            // 3. Calculate final amount with coupon if provided
            decimal originalAmount = dto.Amount;
            decimal finalAmount = originalAmount;
            Guid? couponId = null;
            decimal discountAmount = 0;

            if (!string.IsNullOrEmpty(dto.CouponCode))
            {
                var couponValidation = await ValidateCouponAsync(new ApplyCouponDto
                {
                    CouponCode = dto.CouponCode,
                    CourseId = dto.CourseId,
                    OriginalAmount = originalAmount
                });

                if (couponValidation.IsValid && couponValidation.Coupon != null)
                {
                    finalAmount = couponValidation.FinalAmount;
                    discountAmount = couponValidation.DiscountAmount;
                    couponId = couponValidation.Coupon.Id;

                    _logger.LogInformation("[Payment] Applied coupon {CouponCode}, discount: {Discount}",
                        dto.CouponCode, discountAmount);
                }
            }

            // 4. Create payment record with Pending status (UNCOMMITTED)
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                CourseId = dto.CourseId,
                Amount = finalAmount,
                OriginalAmount = originalAmount,
                DiscountAmount = discountAmount,
                Currency = dto.Currency ?? _currency,
                Status = PaymentStatus.Pending,
                PaymentMethod = PaymentMethodType.CreditCard,
                PaymentGatewayId = "Stripe",
                CouponId = couponId,
                InvoiceNumber = GenerateInvoiceNumber(),
                CreatedAt = DateTime.UtcNow
            };

            payment = await _paymentRepository.CreateAsync(payment);
            await _context.SaveChangesAsync(); // Save to get Payment ID

            // 5. Call Stripe API (ROLLBACK if fails)
            try
            {
                // MOCK Stripe checkout session (replace with real Stripe SDK in production)
                var sessionId = $"cs_test_{Guid.NewGuid():N}";
                var checkoutUrl = $"https://checkout.stripe.com/pay/{sessionId}?payment_id={payment.Id}";

                // In production: var session = await _stripeService.CreateCheckoutSessionAsync(payment);

                // 6. Update payment with Stripe session ID (UNCOMMITTED)
                payment.TransactionId = sessionId;
                payment.Status = PaymentStatus.Processing;
                await _context.SaveChangesAsync();

                // COMMIT ALL OR NOTHING
                await transaction.CommitAsync();

                _logger.LogInformation("[Payment] Created Stripe checkout session {SessionId} for payment {PaymentId}, amount: {Amount} {Currency}",
                    sessionId, payment.Id, finalAmount, payment.Currency);

                return new StripeCheckoutDto
                {
                    SessionId = sessionId,
                    CheckoutUrl = checkoutUrl,
                    PaymentId = payment.Id,
                    PublicKey = _stripePublicKey
                };
            }
            catch (Exception stripeEx)
            {
                // Stripe API failed - ROLLBACK TRANSACTION
                await transaction.RollbackAsync();
                _logger.LogError(stripeEx, "[Payment] Stripe API failed - rolling back payment {PaymentId}", payment.Id);
                throw new InvalidOperationException("Payment gateway error. Please try again.", stripeEx);
            }
        }
        catch (Exception ex)
        {
            // Any other error - ROLLBACK TRANSACTION
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[Payment] Failed to create Stripe checkout for user {UserId}, course {CourseId}",
                dto.UserId, dto.CourseId);
            throw;
        }
    }

    public async Task<PayPalCheckoutDto> CreatePayPalCheckoutAsync(CreatePaymentDto dto)
    {
        // Begin transaction (ALL OR NOTHING)
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Validate course exists
            var course = await _courseRepository.GetByIdAsync(dto.CourseId);
            if (course == null)
            {
                _logger.LogError("[Payment] Course not found: {CourseId}", dto.CourseId);
                throw new ArgumentException($"Course with ID {dto.CourseId} not found");
            }

            // 2. Check enrollment status (prevent double payment)
            var isEnrolled = await _enrollmentRepository.IsUserEnrolledAsync(dto.UserId, dto.CourseId);
            if (isEnrolled)
            {
                throw new InvalidOperationException("User is already enrolled in this course");
            }

            // 3. Calculate amount with coupon
            decimal originalAmount = dto.Amount;
            decimal finalAmount = originalAmount;
            Guid? couponId = null;
            decimal discountAmount = 0;

            if (!string.IsNullOrEmpty(dto.CouponCode))
            {
                var couponValidation = await ValidateCouponAsync(new ApplyCouponDto
                {
                    CouponCode = dto.CouponCode,
                    CourseId = dto.CourseId,
                    OriginalAmount = originalAmount
                });

                if (couponValidation.IsValid && couponValidation.Coupon != null)
                {
                    finalAmount = couponValidation.FinalAmount;
                    discountAmount = couponValidation.DiscountAmount;
                    couponId = couponValidation.Coupon.Id;

                    _logger.LogInformation("[Payment] Applied coupon {CouponCode}, discount: {Discount}",
                        dto.CouponCode, discountAmount);
                }
            }

            // 4. Create payment record (UNCOMMITTED)
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                CourseId = dto.CourseId,
                Amount = finalAmount,
                OriginalAmount = originalAmount,
                DiscountAmount = discountAmount,
                Currency = dto.Currency ?? _currency,
                Status = PaymentStatus.Pending,
                PaymentMethod = PaymentMethodType.PayPal,
                PaymentGatewayId = "PayPal",
                CouponId = couponId,
                InvoiceNumber = GenerateInvoiceNumber(),
                CreatedAt = DateTime.UtcNow
            };

            payment = await _paymentRepository.CreateAsync(payment);
            await _context.SaveChangesAsync(); // Save to get Payment ID

            // 5. Call PayPal API (ROLLBACK if fails)
            try
            {
                // MOCK PayPal checkout (replace with real PayPal SDK in production)
                var orderId = $"PAYPAL_ORDER_{Guid.NewGuid():N}";
                var approvalUrl = $"https://www.paypal.com/checkoutnow?token={orderId}&payment_id={payment.Id}";

                // In production: var order = await _paypalService.CreateOrderAsync(payment);

                // 6. Update payment with PayPal order ID (UNCOMMITTED)
                payment.TransactionId = orderId;
                payment.Status = PaymentStatus.Processing;
                await _context.SaveChangesAsync();

                // COMMIT ALL OR NOTHING
                await transaction.CommitAsync();

                _logger.LogInformation("[Payment] Created PayPal checkout order {OrderId} for payment {PaymentId}, amount: {Amount} {Currency}",
                    orderId, payment.Id, finalAmount, payment.Currency);

                return new PayPalCheckoutDto
                {
                    OrderId = orderId,
                    ApprovalUrl = approvalUrl,
                    PaymentId = payment.Id
                };
            }
            catch (Exception paypalEx)
            {
                // PayPal API failed - ROLLBACK TRANSACTION
                await transaction.RollbackAsync();
                _logger.LogError(paypalEx, "[Payment] PayPal API failed - rolling back payment {PaymentId}", payment.Id);
                throw new InvalidOperationException("Payment gateway error. Please try again.", paypalEx);
            }
        }
        catch (Exception ex)
        {
            // Any other error - ROLLBACK TRANSACTION
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[Payment] Failed to create PayPal checkout for user {UserId}, course {CourseId}",
                dto.UserId, dto.CourseId);
            throw;
        }
    }

    public async Task<PaymentIntentDto> CreatePaymentIntentAsync(CreatePaymentDto dto)
    {
        // Begin transaction (ALL OR NOTHING)
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Validate course exists
            var course = await _courseRepository.GetByIdAsync(dto.CourseId);
            if (course == null)
            {
                _logger.LogError("[Payment] Course not found: {CourseId}", dto.CourseId);
                throw new ArgumentException($"Course with ID {dto.CourseId} not found");
            }

            // 2. Check enrollment status (prevent double payment)
            var isEnrolled = await _enrollmentRepository.IsUserEnrolledAsync(dto.UserId, dto.CourseId);
            if (isEnrolled)
            {
                throw new InvalidOperationException("User is already enrolled in this course");
            }

            // 3. Calculate amount with coupon
            decimal originalAmount = dto.Amount;
            decimal finalAmount = originalAmount;
            Guid? couponId = null;
            decimal discountAmount = 0;

            if (!string.IsNullOrEmpty(dto.CouponCode))
            {
                var couponValidation = await ValidateCouponAsync(new ApplyCouponDto
                {
                    CouponCode = dto.CouponCode,
                    CourseId = dto.CourseId,
                    OriginalAmount = originalAmount
                });

                if (couponValidation.IsValid && couponValidation.Coupon != null)
                {
                    finalAmount = couponValidation.FinalAmount;
                    discountAmount = couponValidation.DiscountAmount;
                    couponId = couponValidation.Coupon.Id;

                    _logger.LogInformation("[Payment] Applied coupon {CouponCode}, discount: {Discount}",
                        dto.CouponCode, discountAmount);
                }
            }

            // 4. Create payment record (UNCOMMITTED)
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                CourseId = dto.CourseId,
                Amount = finalAmount,
                OriginalAmount = originalAmount,
                DiscountAmount = discountAmount,
                Currency = dto.Currency ?? _currency,
                Status = PaymentStatus.Pending,
                PaymentMethod = PaymentMethodType.CreditCard,
                PaymentGatewayId = "Stripe",
                CouponId = couponId,
                InvoiceNumber = GenerateInvoiceNumber(),
                CreatedAt = DateTime.UtcNow
            };

            payment = await _paymentRepository.CreateAsync(payment);
            await _context.SaveChangesAsync(); // Save to get Payment ID

            // 5. Create Stripe Payment Intent (ROLLBACK if fails)
            try
            {
                // MOCK payment intent (replace with real Stripe SDK in production)
                var paymentIntentId = $"pi_test_{Guid.NewGuid():N}";
                var clientSecret = $"pi_test_{Guid.NewGuid():N}_secret_{Guid.NewGuid():N}";

                // In production: var intent = await _stripeService.CreatePaymentIntentAsync(payment);

                // 6. Update payment with payment intent ID (UNCOMMITTED)
                payment.TransactionId = paymentIntentId;
                payment.Status = PaymentStatus.Processing;
                await _context.SaveChangesAsync();

                // COMMIT ALL OR NOTHING
                await transaction.CommitAsync();

                _logger.LogInformation("[Payment] Created payment intent {IntentId} for payment {PaymentId}, amount: {Amount} {Currency}",
                    paymentIntentId, payment.Id, finalAmount, payment.Currency);

                return new PaymentIntentDto
                {
                    PaymentId = payment.Id,
                    ClientSecret = clientSecret,
                    Amount = finalAmount,
                    Currency = payment.Currency
                };
            }
            catch (Exception stripeEx)
            {
                // Stripe API failed - ROLLBACK TRANSACTION
                await transaction.RollbackAsync();
                _logger.LogError(stripeEx, "[Payment] Stripe Payment Intent API failed - rolling back payment {PaymentId}", payment.Id);
                throw new InvalidOperationException("Payment gateway error. Please try again.", stripeEx);
            }
        }
        catch (Exception ex)
        {
            // Any other error - ROLLBACK TRANSACTION
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[Payment] Failed to create payment intent for user {UserId}, course {CourseId}",
                dto.UserId, dto.CourseId);
            throw;
        }
    }

    // ===== Payment Processing =====

    public async Task<PaymentDto?> ProcessStripeWebhookAsync(string payload, string signature)
    {
        try
        {
            // MOCK implementation - real implementation would verify Stripe signature and parse webhook
            _logger.LogInformation("[Payment] Processing Stripe webhook");

            // In production, parse the webhook payload and extract payment info
            // For now, return null as placeholder
            await Task.Delay(100); // Simulate processing

            _logger.LogWarning("[Payment] Stripe webhook processing is in MOCK mode");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payment] Failed to process Stripe webhook");
            throw;
        }
    }

    public async Task<PaymentDto?> ProcessPayPalWebhookAsync(string payload)
    {
        try
        {
            // MOCK implementation - real implementation would verify PayPal signature
            _logger.LogInformation("[Payment] Processing PayPal webhook");

            await Task.Delay(100); // Simulate processing

            _logger.LogWarning("[Payment] PayPal webhook processing is in MOCK mode");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payment] Failed to process PayPal webhook");
            throw;
        }
    }

    public async Task<PaymentDto?> CompletePaymentAsync(Guid paymentId, string transactionId)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                _logger.LogWarning("[Payment] Payment not found: {PaymentId}", paymentId);
                return null;
            }

            if (payment.Status == PaymentStatus.Completed)
            {
                _logger.LogWarning("[Payment] Payment already completed: {PaymentId}", paymentId);
                return MapToDto(payment);
            }

            // Update payment status to Completed
            payment.Status = PaymentStatus.Completed;
            payment.TransactionId = transactionId;
            payment.PaymentGatewayId = transactionId; // Store gateway transaction ID
            payment.ProcessedAt = DateTime.UtcNow;

            // Use transaction to ensure payment completion, enrollment creation, and coupon update are atomic
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _paymentRepository.UpdateAsync(payment);

                // Create enrollment for the user
                var enrollment = new Enrollment
                {
                    Id = Guid.NewGuid(),
                    UserId = payment.UserId,
                    CourseId = payment.CourseId,
                    AmountPaid = payment.Amount,
                    Status = EnrollmentStatus.Active,
                    EnrolledAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow,
                    CompletedLessons = 0,
                    TotalWatchedMinutes = 0
                };

                await _enrollmentRepository.CreateAsync(enrollment);

                // Increment coupon usage if applicable
                if (payment.CouponId.HasValue)
                {
                    await _couponRepository.IncrementUsageAsync(payment.CouponId.Value);
                    _logger.LogInformation("[Payment] Incremented usage for coupon {CouponId}", payment.CouponId.Value);
                }

                await transaction.CommitAsync();

                _logger.LogInformation("[Payment] Completed payment {PaymentId} with transaction {TransactionId}, created enrollment {EnrollmentId}",
                    paymentId, transactionId, enrollment.Id);

                // Record payment metrics (Phase 4.2)
                var paymentMethod = payment.PaymentMethod.ToString().ToLower();
                _metricsService.RecordPayment("completed", paymentMethod, payment.Amount);
                _metricsService.RecordPaymentAmount(paymentMethod, payment.Amount);

                return MapToDto(payment);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payment] Failed to complete payment {PaymentId}", paymentId);
            throw;
        }
    }

    public async Task<bool> CancelPaymentAsync(Guid paymentId)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                _logger.LogWarning("[Payment] Payment not found for cancellation: {PaymentId}", paymentId);
                return false;
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                _logger.LogWarning("[Payment] Cannot cancel payment {PaymentId} with status {Status}",
                    paymentId, payment.Status);
                return false;
            }

            await _paymentRepository.UpdateStatusAsync(paymentId, PaymentStatus.Cancelled);

            _logger.LogInformation("[Payment] Cancelled payment {PaymentId}", paymentId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payment] Failed to cancel payment {PaymentId}", paymentId);
            return false;
        }
    }

    // ===== Refunds =====

    public async Task<bool> ProcessRefundAsync(Guid paymentId, decimal refundAmount, string reason)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
            {
                _logger.LogWarning("[Payment] Payment not found for refund: {PaymentId}", paymentId);
                return false;
            }

            if (payment.Status != PaymentStatus.Completed)
            {
                _logger.LogWarning("[Payment] Cannot refund payment {PaymentId} with status {Status}",
                    paymentId, payment.Status);
                return false;
            }

            if (refundAmount > payment.Amount)
            {
                _logger.LogWarning("[Payment] Refund amount {RefundAmount} exceeds payment amount {PaymentAmount}",
                    refundAmount, payment.Amount);
                return false;
            }

            // Process refund through repository (uses transaction for data consistency)
            await _paymentRepository.ProcessRefundAsync(paymentId, refundAmount);

            // Update or cancel associated enrollment
            var enrollment = await _enrollmentRepository.GetActiveEnrollmentAsync(payment.UserId, payment.CourseId);
            if (enrollment != null)
            {
                // Full refund - set enrollment to Refunded
                if (refundAmount >= payment.Amount)
                {
                    enrollment.Status = EnrollmentStatus.Refunded;
                    await _enrollmentRepository.UpdateAsync(enrollment);

                    _logger.LogInformation("[Payment] Set enrollment {EnrollmentId} to Refunded status", enrollment.Id);
                }
                // Partial refund - enrollment remains active but log it
                else
                {
                    _logger.LogInformation("[Payment] Partial refund processed, enrollment {EnrollmentId} remains active",
                        enrollment.Id);
                }
            }

            _logger.LogInformation("[Payment] Processed refund of {Amount} for payment {PaymentId}, reason: {Reason}",
                refundAmount, paymentId, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payment] Failed to process refund for payment {PaymentId}", paymentId);
            return false;
        }
    }

    // ===== Coupon Validation =====

    public async Task<CouponValidationDto> ValidateCouponAsync(ApplyCouponDto dto)
    {
        try
        {
            var coupon = await _couponRepository.GetByCodeAsync(dto.CouponCode);

            if (coupon == null)
            {
                _logger.LogWarning("[Payment] Coupon not found: {CouponCode}", dto.CouponCode);
                return new CouponValidationDto
                {
                    IsValid = false,
                    ErrorMessage = "Coupon code not found",
                    FinalAmount = dto.OriginalAmount
                };
            }

            // Check if coupon is active
            if (!coupon.IsActive)
            {
                return new CouponValidationDto
                {
                    IsValid = false,
                    ErrorMessage = "Coupon is not active",
                    FinalAmount = dto.OriginalAmount
                };
            }

            // Check validity period
            if (!coupon.IsValid)
            {
                return new CouponValidationDto
                {
                    IsValid = false,
                    ErrorMessage = "Coupon has expired or reached usage limit",
                    FinalAmount = dto.OriginalAmount
                };
            }

            // Check if coupon is course-specific
            if (coupon.CourseId.HasValue && coupon.CourseId.Value != dto.CourseId)
            {
                return new CouponValidationDto
                {
                    IsValid = false,
                    ErrorMessage = "Coupon is not valid for this course",
                    FinalAmount = dto.OriginalAmount
                };
            }

            // Check minimum amount requirement
            if (coupon.MinimumAmount.HasValue && dto.OriginalAmount < coupon.MinimumAmount.Value)
            {
                return new CouponValidationDto
                {
                    IsValid = false,
                    ErrorMessage = $"Minimum purchase amount of {coupon.MinimumAmount.Value:C} required",
                    FinalAmount = dto.OriginalAmount
                };
            }

            // Calculate discount
            var discountAmount = coupon.CalculateDiscount(dto.OriginalAmount);
            var finalAmount = Math.Max(0, dto.OriginalAmount - discountAmount);

            _logger.LogInformation("[Payment] Validated coupon {CouponCode}, discount: {Discount}, final: {Final}",
                dto.CouponCode, discountAmount, finalAmount);

            return new CouponValidationDto
            {
                IsValid = true,
                DiscountAmount = discountAmount,
                FinalAmount = finalAmount,
                Coupon = new CouponDto
                {
                    Id = coupon.Id,
                    Code = coupon.Code,
                    Description = coupon.Description,
                    Type = coupon.Type.ToString(),
                    Value = coupon.Value,
                    MinimumAmount = coupon.MinimumAmount,
                    MaximumDiscount = coupon.MaximumDiscount,
                    ValidFrom = coupon.ValidFrom,
                    ValidUntil = coupon.ValidUntil,
                    UsageLimit = coupon.UsageLimit,
                    UsedCount = coupon.UsedCount,
                    IsActive = coupon.IsActive,
                    IsValid = coupon.IsValid
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payment] Failed to validate coupon {CouponCode}", dto.CouponCode);
            return new CouponValidationDto
            {
                IsValid = false,
                ErrorMessage = "Error validating coupon",
                FinalAmount = dto.OriginalAmount
            };
        }
    }

    public async Task<bool> ApplyCouponToPaymentAsync(Guid paymentId, string couponCode)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId);
            if (payment == null || payment.Status != PaymentStatus.Pending)
            {
                _logger.LogWarning("[Payment] Cannot apply coupon to payment {PaymentId}", paymentId);
                return false;
            }

            var validation = await ValidateCouponAsync(new ApplyCouponDto
            {
                CouponCode = couponCode,
                CourseId = payment.CourseId,
                OriginalAmount = payment.OriginalAmount
            });

            if (!validation.IsValid || validation.Coupon == null)
            {
                return false;
            }

            // Update payment with coupon
            payment.CouponId = validation.Coupon.Id;
            payment.DiscountAmount = validation.DiscountAmount;
            payment.Amount = validation.FinalAmount;

            await _paymentRepository.UpdateAsync(payment);

            _logger.LogInformation("[Payment] Applied coupon {CouponCode} to payment {PaymentId}",
                couponCode, paymentId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payment] Failed to apply coupon to payment {PaymentId}", paymentId);
            return false;
        }
    }

    // ===== Query Methods =====

    public async Task<PaymentDto?> GetPaymentByIdAsync(Guid id)
    {
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(id);
            return payment != null ? MapToDto(payment) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payment] Failed to get payment by ID: {PaymentId}", id);
            throw;
        }
    }

    public async Task<PaymentDto?> GetPaymentByTransactionIdAsync(string transactionId)
    {
        try
        {
            var payment = await _paymentRepository.GetByTransactionIdAsync(transactionId);
            return payment != null ? MapToDto(payment) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payment] Failed to get payment by transaction ID: {TransactionId}", transactionId);
            throw;
        }
    }

    public async Task<List<TransactionDto>> GetUserTransactionsAsync(Guid userId)
    {
        try
        {
            var payments = await _paymentRepository.GetByUserIdAsync(userId);

            return payments.Select(p => new TransactionDto
            {
                Id = p.Id,
                UserId = p.UserId,
                CourseId = p.CourseId,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status.ToString(),
                PaymentMethod = p.PaymentMethod.ToString(),
                TransactionId = p.TransactionId,
                ProcessedAt = p.ProcessedAt,
                CreatedAt = p.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payment] Failed to get user transactions for user: {UserId}", userId);
            throw;
        }
    }

    public async Task<TransactionListDto> GetAllTransactionsAsync(int page = 1, int pageSize = 10, PaymentStatus? status = null)
    {
        try
        {
            var payments = await _paymentRepository.GetTransactionsAsync(null, null, status);
            var totalCount = await _paymentRepository.GetTotalCountAsync();

            // Apply pagination
            var paginatedPayments = payments
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var transactions = paginatedPayments.Select(p => new TransactionDto
            {
                Id = p.Id,
                UserId = p.UserId,
                CourseId = p.CourseId,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status.ToString(),
                PaymentMethod = p.PaymentMethod.ToString(),
                TransactionId = p.TransactionId,
                ProcessedAt = p.ProcessedAt,
                CreatedAt = p.CreatedAt
            }).ToList();

            return new TransactionListDto
            {
                Transactions = transactions,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payment] Failed to get all transactions");
            throw;
        }
    }

    // ===== Revenue Reporting =====

    public async Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var revenue = await _paymentRepository.GetTotalRevenueAsync(startDate, endDate);

            _logger.LogInformation("[Payment] Total revenue from {StartDate} to {EndDate}: {Revenue}",
                startDate?.ToString("yyyy-MM-dd") ?? "beginning",
                endDate?.ToString("yyyy-MM-dd") ?? "now",
                revenue);

            return revenue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payment] Failed to get total revenue");
            throw;
        }
    }

    public async Task<Dictionary<string, decimal>> GetRevenueByMonthAsync(int year)
    {
        try
        {
            var monthlyRevenue = new Dictionary<string, decimal>();

            for (int month = 1; month <= 12; month++)
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var revenue = await _paymentRepository.GetTotalRevenueAsync(startDate, endDate);
                var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

                monthlyRevenue[monthName] = revenue;
            }

            _logger.LogInformation("[Payment] Retrieved monthly revenue for year {Year}", year);

            return monthlyRevenue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Payment] Failed to get revenue by month for year {Year}", year);
            throw;
        }
    }

    // ===== Helper Methods =====

    private string GenerateInvoiceNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
        var random = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        return $"INV-{timestamp}-{random}";
    }

    private PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            UserId = payment.UserId,
            CourseId = payment.CourseId,
            Amount = payment.Amount,
            OriginalAmount = payment.OriginalAmount,
            DiscountAmount = payment.DiscountAmount,
            Currency = payment.Currency,
            Status = payment.Status,
            PaymentMethod = payment.PaymentMethod,
            PaymentGatewayId = payment.PaymentGatewayId,
            TransactionId = payment.TransactionId,
            InvoiceNumber = payment.InvoiceNumber,
            CreatedAt = payment.CreatedAt,
            ProcessedAt = payment.ProcessedAt,
            Metadata = payment.Notes
        };
    }
}