using InsightLearn.Core.DTOs.Certificate;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service for certificate generation and management
/// Phase 3 Stub Implementation - Full PDF generation to be implemented in Phase 4
/// </summary>
public class CertificateService : ICertificateService
{
    private readonly ICertificateRepository _certificateRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(
        ICertificateRepository certificateRepository,
        IEnrollmentRepository enrollmentRepository,
        ICourseRepository courseRepository,
        ILogger<CertificateService> logger)
    {
        _certificateRepository = certificateRepository;
        _enrollmentRepository = enrollmentRepository;
        _courseRepository = courseRepository;
        _logger = logger;
    }

    public async Task<Certificate> GenerateCertificateAsync(Guid enrollmentId)
    {
        try
        {
            _logger.LogInformation("[CertificateService] Generating certificate for enrollment {EnrollmentId}", enrollmentId);

            // 1. Get enrollment with related data
            var enrollment = await _enrollmentRepository.GetByIdAsync(enrollmentId);
            if (enrollment == null)
            {
                _logger.LogWarning("[CertificateService] Enrollment {EnrollmentId} not found", enrollmentId);
                throw new ArgumentException($"Enrollment {enrollmentId} not found");
            }

            // 2. Verify enrollment is completed
            if (!enrollment.IsCompleted)
            {
                _logger.LogWarning("[CertificateService] Enrollment {EnrollmentId} is not completed", enrollmentId);
                throw new InvalidOperationException("Cannot generate certificate for incomplete enrollment");
            }

            // 3. Check if certificate already exists
            var existingCertificate = await _certificateRepository.GetByEnrollmentIdAsync(enrollmentId);
            if (existingCertificate != null)
            {
                _logger.LogInformation("[CertificateService] Certificate already exists for enrollment {EnrollmentId}", enrollmentId);
                return existingCertificate;
            }

            // 4. Get course details
            var course = await _courseRepository.GetByIdAsync(enrollment.CourseId);
            if (course == null)
            {
                throw new ArgumentException($"Course {enrollment.CourseId} not found");
            }

            // 5. Generate unique certificate number
            var certificateNumber = GenerateCertificateNumber(enrollment.UserId, enrollment.CourseId);

            // 6. Create certificate entity
            var certificate = new Certificate
            {
                Id = Guid.NewGuid(),
                UserId = enrollment.UserId,
                CourseId = enrollment.CourseId,
                EnrollmentId = enrollmentId,
                CertificateNumber = certificateNumber,
                IssuedAt = DateTime.UtcNow,
                Status = CertificateStatus.Active,
                CourseHours = (int)Math.Ceiling(course.EstimatedDurationMinutes / 60.0),
                CourseRating = course.AverageRating,
                IsVerified = true,
                // Phase 4: Implement PDF generation with QuestPDF or iText7
                PdfUrl = null,
                TemplateUrl = "/templates/certificate-default.html"
            };

            // 7. Save certificate
            var createdCertificate = await _certificateRepository.CreateAsync(certificate);

            _logger.LogInformation("[CertificateService] Certificate {CertificateNumber} generated successfully for enrollment {EnrollmentId}",
                certificateNumber, enrollmentId);

            // TODO Phase 4: Generate PDF certificate file
            // - Use QuestPDF or iText7 library
            // - Upload to cloud storage (Azure Blob / AWS S3)
            // - Update PdfUrl with public URL

            return createdCertificate;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CertificateService] Failed to generate certificate for enrollment {EnrollmentId}", enrollmentId);
            throw;
        }
    }

    public async Task<Certificate?> GetCertificateByIdAsync(Guid certificateId)
    {
        return await _certificateRepository.GetByIdAsync(certificateId);
    }

    public async Task<Certificate?> GetCertificateByEnrollmentIdAsync(Guid enrollmentId)
    {
        return await _certificateRepository.GetByEnrollmentIdAsync(enrollmentId);
    }

    public async Task<IEnumerable<Certificate>> GetUserCertificatesAsync(Guid userId)
    {
        return await _certificateRepository.GetByUserIdAsync(userId);
    }

    public async Task<bool> ValidateCertificateAsync(string certificateCode)
    {
        try
        {
            var certificate = await _certificateRepository.GetByCertificateNumberAsync(certificateCode);

            if (certificate == null)
            {
                _logger.LogWarning("[CertificateService] Certificate {CertificateCode} not found", certificateCode);
                return false;
            }

            // Check if certificate is active and not revoked
            var isValid = certificate.IsActive && certificate.Status == CertificateStatus.Active;

            _logger.LogInformation("[CertificateService] Certificate {CertificateCode} validation result: {IsValid}",
                certificateCode, isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CertificateService] Failed to validate certificate {CertificateCode}", certificateCode);
            return false;
        }
    }

    public async Task<bool> RevokeCertificateAsync(Guid certificateId, string reason)
    {
        try
        {
            var certificate = await _certificateRepository.GetByIdAsync(certificateId);
            if (certificate == null)
            {
                _logger.LogWarning("[CertificateService] Certificate {CertificateId} not found", certificateId);
                return false;
            }

            certificate.Status = CertificateStatus.Revoked;
            certificate.RevokedAt = DateTime.UtcNow;
            certificate.RevokeReason = reason;

            await _certificateRepository.UpdateAsync(certificate);

            _logger.LogInformation("[CertificateService] Certificate {CertificateId} revoked. Reason: {Reason}",
                certificateId, reason);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CertificateService] Failed to revoke certificate {CertificateId}", certificateId);
            return false;
        }
    }

    /// <summary>
    /// Generates a unique certificate number
    /// Format: CERT-{YEAR}-{USER_ID_SHORT}-{COURSE_ID_SHORT}-{RANDOM}
    /// Example: CERT-2025-A1B2C3-D4E5F6-9K7M
    /// </summary>
    private string GenerateCertificateNumber(Guid userId, Guid courseId)
    {
        var year = DateTime.UtcNow.Year;
        var userIdShort = userId.ToString("N")[..6].ToUpper();
        var courseIdShort = courseId.ToString("N")[..6].ToUpper();
        var random = Guid.NewGuid().ToString("N")[..4].ToUpper();

        return $"CERT-{year}-{userIdShort}-{courseIdShort}-{random}";
    }
}
