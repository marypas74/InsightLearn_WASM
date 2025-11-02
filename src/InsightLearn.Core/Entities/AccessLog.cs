using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class AccessLog
{
    public Guid Id { get; set; }
    
    public Guid? UserId { get; set; }
    
    [Required]
    [StringLength(15)]
    public string IpAddress { get; set; } = string.Empty;
    
    [Required]
    [StringLength(500)]
    public string RequestPath { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string HttpMethod { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    [StringLength(500)]
    public string? Referer { get; set; }
    
    public int StatusCode { get; set; }
    
    public long? ResponseTimeMs { get; set; }
    
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    
    [StringLength(50)]
    public string? SessionId { get; set; }
    
    [StringLength(1000)]
    public string? AdditionalData { get; set; }
    
    // Navigation property
    public virtual User? User { get; set; }
}