using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class ValidationErrorLog
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string ValidationSource { get; set; } = string.Empty; // ModelValidation, FluentValidation, CustomValidation
    
    [Required]
    [StringLength(100)]
    public string FieldName { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? FieldValue { get; set; } // Sanitized field value
    
    [Required]
    [StringLength(200)]
    public string ValidationRule { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string ErrorMessage { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string ModelType { get; set; } = string.Empty; // C# class name
    
    [StringLength(500)]
    public string? RequestPath { get; set; }
    
    [StringLength(10)]
    public string? HttpMethod { get; set; }
    
    public Guid? UserId { get; set; }
    
    [StringLength(100)]
    public string? SessionId { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(1000)]
    public string? UserAgent { get; set; }
    
    public string? RequestData { get; set; } // Sanitized request data JSON
    
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    [StringLength(20)]
    public string Severity { get; set; } = "Warning";
    
    [StringLength(100)]
    public string? CorrelationId { get; set; }
    
    public bool IsClientSideValidated { get; set; } = false;
    
    [StringLength(100)]
    public string? ValidationCategory { get; set; } // Security, Business, Format, etc.
    
    [StringLength(100)]
    public string? RequestId { get; set; }
    
    // Navigation properties
    public virtual User? User { get; set; }
}