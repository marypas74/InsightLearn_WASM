using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class LogEntry
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Level { get; set; } = string.Empty; // Debug, Information, Warning, Error, Fatal
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    public string? Exception { get; set; }
    
    [StringLength(200)]
    public string? Logger { get; set; }
    
    [StringLength(100)]
    public string? Application { get; set; }
    
    [StringLength(100)]
    public string? MachineName { get; set; }
    
    [StringLength(500)]
    public string? RequestPath { get; set; }
    
    [StringLength(20)]
    public string? HttpMethod { get; set; }
    
    [StringLength(15)]
    public string? IpAddress { get; set; }
    
    [StringLength(500)]
    public string? UserAgent { get; set; }
    
    public Guid? UserId { get; set; }
    
    [StringLength(100)]
    public string? UserName { get; set; }
    
    [StringLength(50)]
    public string? SessionId { get; set; }
    
    [StringLength(100)]
    public string? CorrelationId { get; set; }
    
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public string? Properties { get; set; } // JSON serialized additional properties
    
    [StringLength(100)]
    public string? ThreadId { get; set; }
    
    [StringLength(50)]
    public string? ProcessId { get; set; }
    
    // Navigation property
    public virtual User? User { get; set; }
}