using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace InsightLearn.Core.Entities;

public class Enrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid CourseId { get; set; }
    
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? CompletedAt { get; set; }
    
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal AmountPaid { get; set; }
    
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    
    public int CurrentLessonIndex { get; set; }
    
    public Guid? CurrentLessonId { get; set; }
    
    public bool HasCertificate { get; set; }
    
    // Progress tracking
    public int CompletedLessons { get; set; }
    
    public int TotalWatchedMinutes { get; set; }
    
    // Navigation properties (with [JsonIgnore] to prevent circular reference)
    [JsonIgnore]
    public virtual User User { get; set; } = null!;

    [JsonIgnore]
    public virtual Course Course { get; set; } = null!;

    [JsonIgnore]
    public virtual Lesson? CurrentLesson { get; set; }

    [JsonIgnore]
    public virtual Certificate? Certificate { get; set; }

    // Subscription support
    public Guid? SubscriptionId { get; set; }

    [JsonIgnore]
    public virtual UserSubscription? Subscription { get; set; }
    
    public double ProgressPercentage
    {
        get
        {
            var totalLessons = Course?.Sections.SelectMany(s => s.Lessons).Count(l => l.IsActive) ?? 0;
            return totalLessons > 0 ? (double)CompletedLessons / totalLessons * 100 : 0;
        }
    }
    
    public double Progress => ProgressPercentage;
    
    public bool IsCompleted => CompletedAt.HasValue;
    
    public TimeSpan TotalWatchedTime => TimeSpan.FromMinutes(TotalWatchedMinutes);
}

public enum EnrollmentStatus
{
    Active,
    Completed,
    Suspended,
    Cancelled,
    Refunded
}