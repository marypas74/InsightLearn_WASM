using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class ApiRequestLog
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string RequestId { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? CorrelationId { get; set; }
    
    public Guid? UserId { get; set; }
    
    [StringLength(100)]
    public string? SessionId { get; set; }
    
    [Required]
    [StringLength(10)]
    public string Method { get; set; } = string.Empty; // GET, POST, PUT, DELETE, etc.
    
    [Required]
    [StringLength(2000)]
    public string Path { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string? QueryString { get; set; }
    
    public string? RequestHeaders { get; set; } // JSON
    
    public string? RequestBody { get; set; }
    
    public int ResponseStatusCode { get; set; }
    
    public string? ResponseHeaders { get; set; } // JSON
    
    public string? ResponseBody { get; set; }
    
    public long RequestSize { get; set; } = 0;
    
    public long ResponseSize { get; set; } = 0;
    
    public long DurationMs { get; set; }
    
    [StringLength(45)]
    public string? IpAddress { get; set; }
    
    [StringLength(1000)]
    public string? UserAgent { get; set; }
    
    [StringLength(1000)]
    public string? Referer { get; set; }
    
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    
    public string? Exception { get; set; } // JSON with exception details if any
    
    public bool? CacheHit { get; set; }
    
    public int DatabaseQueries { get; set; } = 0;
    
    public long DatabaseDurationMs { get; set; } = 0;
    
    public decimal? MemoryUsageMB { get; set; }
    
    public long? CpuUsageMs { get; set; }
    
    [StringLength(20)]
    public string? ApiVersion { get; set; }
    
    [StringLength(100)]
    public string? ClientApp { get; set; } // Web, Mobile, API
    
    [StringLength(100)]
    public string? Feature { get; set; } // Auth, Course, Payment, etc.
    
    // Navigation properties
    public virtual User? User { get; set; }
}