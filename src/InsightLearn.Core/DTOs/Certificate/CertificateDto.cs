namespace InsightLearn.Core.DTOs.Certificate;

public class CertificateDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid EnrollmentId { get; set; }
    public Guid CourseId { get; set; }

    public string UserName { get; set; } = string.Empty;
    public string CourseTitle { get; set; } = string.Empty;

    public string CertificateCode { get; set; } = string.Empty;
    public DateTime IssuedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public string? PdfUrl { get; set; }
    public bool IsRevoked { get; set; }
    public string? RevocationReason { get; set; }
}
