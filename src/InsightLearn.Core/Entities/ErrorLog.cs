using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class ErrorLog
{
    public Guid Id { get; set; }
    
    [StringLength(100)]
    public string RequestId { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? CorrelationId { get; set; }
    
    [StringLength(100)]
    public string? SessionId { get; set; }
    
    public Guid? UserId { get; set; }
    
    [StringLength(256)]
    public string? Email { get; set; }
    
    [Required]
    [StringLength(500)]
    public string ExceptionType { get; set; } = string.Empty;
    
    [Required]
    public string ExceptionMessage { get; set; } = string.Empty;
    
    public string? StackTrace { get; set; }
    
    public string? InnerException { get; set; }
    
    [StringLength(1000)]
    public string? RequestPath { get; set; }
    
    [StringLength(20)]
    public string? HttpMethod { get; set; }
    
    public string? RequestData { get; set; }
    
    public int? ResponseStatusCode { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(1000)]
    public string? UserAgent { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Severity { get; set; } = "Error";
    
    [StringLength(100)]
    public string? Source { get; set; }
    
    [StringLength(100)]
    public string? Component { get; set; }
    
    [StringLength(200)]
    public string? Method { get; set; }
    
    public int? LineNumber { get; set; }
    
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    
    public string? AdditionalData { get; set; }
    
    public bool IsResolved { get; set; } = false;
    
    public DateTime? ResolvedAt { get; set; }
    
    public Guid? ResolvedByUserId { get; set; }
    
    [StringLength(1000)]
    public string? ResolutionNotes { get; set; }
    
    [StringLength(50)]
    public string? Environment { get; set; }
    
    [StringLength(20)]
    public string? Version { get; set; }
    
    public int? RetryCount { get; set; }
    
    public bool NotificationSent { get; set; } = false;
    
    // Navigation properties removed to prevent EF cascade conflicts
    // Use UserId and ResolvedByUserId as simple foreign key fields
}