using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Repository interface for Certificate entity
/// </summary>
public interface ICertificateRepository
{
    /// <summary>
    /// Creates a new certificate
    /// </summary>
    Task<Certificate> CreateAsync(Certificate certificate);

    /// <summary>
    /// Gets a certificate by ID
    /// </summary>
    Task<Certificate?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a certificate by enrollment ID
    /// </summary>
    Task<Certificate?> GetByEnrollmentIdAsync(Guid enrollmentId);

    /// <summary>
    /// Gets a certificate by certificate number
    /// </summary>
    Task<Certificate?> GetByCertificateNumberAsync(string certificateNumber);

    /// <summary>
    /// Gets all certificates for a user
    /// </summary>
    Task<IEnumerable<Certificate>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Updates a certificate
    /// </summary>
    Task<Certificate> UpdateAsync(Certificate certificate);

    /// <summary>
    /// Gets total count of certificates issued
    /// </summary>
    Task<int> GetTotalCountAsync();
}
