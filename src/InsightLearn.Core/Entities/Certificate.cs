using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class Certificate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid CourseId { get; set; }
    
    [Required]
    public Guid EnrollmentId { get; set; }
    
    [Required]
    [StringLength(100)]
    public string CertificateNumber { get; set; } = string.Empty;
    
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? RevokedAt { get; set; }
    
    public string? RevokeReason { get; set; }
    
    [StringLength(500)]
    public string? TemplateUrl { get; set; }
    
    [StringLength(500)]
    public string? PdfUrl { get; set; }
    
    public bool IsVerified { get; set; } = true;
    
    public CertificateStatus Status { get; set; } = CertificateStatus.Active;
    
    // Certificate metadata
    public string? SkillsAcquired { get; set; }
    
    public int CourseHours { get; set; }
    
    public double CourseRating { get; set; }
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Course Course { get; set; } = null!;
    public virtual Enrollment Enrollment { get; set; } = null!;
    
    public bool IsActive => Status == CertificateStatus.Active && !RevokedAt.HasValue;
    
    public string VerificationUrl => $"/verify-certificate/{CertificateNumber}";
}


public enum CertificateStatus
{
    Active,
    Revoked,
    Expired,
    Suspended
}