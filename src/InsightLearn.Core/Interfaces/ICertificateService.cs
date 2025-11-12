using InsightLearn.Core.DTOs.Certificate;
using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service interface for certificate generation and management
/// </summary>
public interface ICertificateService
{
    /// <summary>
    /// Generates a certificate for a completed enrollment
    /// </summary>
    /// <param name="enrollmentId">The enrollment ID</param>
    /// <returns>The generated certificate</returns>
    Task<Certificate> GenerateCertificateAsync(Guid enrollmentId);

    /// <summary>
    /// Gets a certificate by ID
    /// </summary>
    /// <param name="certificateId">The certificate ID</param>
    /// <returns>The certificate or null if not found</returns>
    Task<Certificate?> GetCertificateByIdAsync(Guid certificateId);

    /// <summary>
    /// Gets a certificate by enrollment ID
    /// </summary>
    /// <param name="enrollmentId">The enrollment ID</param>
    /// <returns>The certificate or null if not found</returns>
    Task<Certificate?> GetCertificateByEnrollmentIdAsync(Guid enrollmentId);

    /// <summary>
    /// Gets all certificates for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>List of certificates</returns>
    Task<IEnumerable<Certificate>> GetUserCertificatesAsync(Guid userId);

    /// <summary>
    /// Validates a certificate by its unique code
    /// </summary>
    /// <param name="certificateCode">The certificate verification code</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<bool> ValidateCertificateAsync(string certificateCode);

    /// <summary>
    /// Revokes a certificate
    /// </summary>
    /// <param name="certificateId">The certificate ID</param>
    /// <param name="reason">Reason for revocation</param>
    /// <returns>True if revoked successfully</returns>
    Task<bool> RevokeCertificateAsync(Guid certificateId, string reason);
}
