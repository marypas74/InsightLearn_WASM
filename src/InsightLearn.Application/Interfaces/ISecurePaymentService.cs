using InsightLearn.Application.DTOs;
using InsightLearn.Core.Entities;

namespace InsightLearn.Application.Interfaces;

/// <summary>
/// Secure payment service interface for encrypted payment method management
/// Provides PCI DSS compliant payment operations with comprehensive security features
/// </summary>
public interface ISecurePaymentService
{
    // Payment Method Management
    Task<SecurePaymentMethodDto> CreatePaymentMethodAsync(CreatePaymentMethodDto createDto);
    Task<SecurePaymentMethodDto?> GetPaymentMethodAsync(Guid paymentMethodId, Guid userId);
    Task<List<SecurePaymentMethodDto>> GetUserPaymentMethodsAsync(Guid userId);
    Task<bool> UpdatePaymentMethodAsync(Guid paymentMethodId, UpdatePaymentMethodDto updateDto, Guid userId);
    Task<bool> DeletePaymentMethodAsync(Guid paymentMethodId, Guid userId);
    Task<bool> SetDefaultPaymentMethodAsync(Guid paymentMethodId, Guid userId);

    // Payment Processing
    Task<SecurePaymentTransactionDto> ProcessSecurePaymentAsync(ProcessSecurePaymentDto paymentDto);
    Task<bool> VerifyPaymentMethodAsync(Guid paymentMethodId, Guid userId);
    Task<PaymentMethodValidationDto> ValidatePaymentMethodAsync(Guid paymentMethodId, Guid userId);

    // Security & Fraud Detection
    Task<FraudAnalysisDto> AnalyzeTransactionRiskAsync(AnalyzeTransactionRiskDto riskDto);
    Task<List<SecurityEventDto>> GetPaymentSecurityEventsAsync(Guid userId, DateTime? fromDate = null);
    Task<bool> ReportSuspiciousActivityAsync(ReportSuspiciousActivityDto reportDto);

    // Encryption & Key Management
    Task<bool> RotatePaymentMethodEncryptionAsync(Guid paymentMethodId);
    Task<bool> ValidatePaymentMethodIntegrityAsync(Guid paymentMethodId);
    Task<int> RotateAllPaymentMethodKeysAsync();

    // Audit & Compliance
    Task<List<PaymentMethodAuditDto>> GetPaymentMethodAuditLogAsync(Guid paymentMethodId, Guid userId);
    Task LogPaymentMethodActionAsync(Guid paymentMethodId, Guid userId, string action, string? description = null, string? ipAddress = null, string? userAgent = null);

    // Transaction History
    Task<List<SecureTransactionHistoryDto>> GetSecureTransactionHistoryAsync(Guid userId, int page = 1, int pageSize = 10);
    Task<SecureTransactionDetailsDto?> GetSecureTransactionDetailsAsync(Guid transactionId, Guid userId);
}