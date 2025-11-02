using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class DatabaseErrorLog
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string ErrorType { get; set; } = string.Empty; // SqlException, Timeout, Deadlock, etc.
    
    public string? SqlCommand { get; set; }
    
    public string? Parameters { get; set; } // JSON
    
    public int? SqlErrorNumber { get; set; }
    
    public int? SqlErrorSeverity { get; set; }
    
    public int? SqlErrorState { get; set; }
    
    [StringLength(128)]
    public string? DatabaseName { get; set; }
    
    [StringLength(128)]
    public string? TableName { get; set; }
    
    [StringLength(128)]
    public string? ProcedureName { get; set; }
    
    public int? LineNumber { get; set; }
    
    public long? ExecutionTimeMs { get; set; }
    
    public int? RowsAffected { get; set; }
    
    [StringLength(500)]
    public string? ConnectionString { get; set; } // Masked connection string
    
    [StringLength(100)]
    public string? TransactionId { get; set; }
    
    [StringLength(50)]
    public string? IsolationLevel { get; set; }
    
    public Guid? UserId { get; set; }
    
    [StringLength(100)]
    public string? RequestId { get; set; }
    
    [StringLength(100)]
    public string? CorrelationId { get; set; }
    
    [Required]
    public string Exception { get; set; } = string.Empty; // Full exception details
    
    public string? StackTrace { get; set; }
    
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    [StringLength(20)]
    public string Severity { get; set; } = "Error";
    
    public bool IsResolved { get; set; } = false;
    
    public DateTime? ResolvedAt { get; set; }
    
    [StringLength(1000)]
    public string? ResolutionNotes { get; set; }
    
    // Navigation properties
    public virtual User? User { get; set; }
}