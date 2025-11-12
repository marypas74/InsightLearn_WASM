using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InsightLearn.Core.Entities;

public class Section
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid CourseId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int OrderIndex { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties (with [JsonIgnore] to prevent circular reference)
    [JsonIgnore]
    public virtual Course Course { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    
    public int TotalLessons => Lessons.Count(l => l.IsActive);
    public int TotalDurationMinutes => Lessons.Where(l => l.IsActive).Sum(l => l.DurationMinutes);
}

public class Lesson
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid SectionId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public LessonType Type { get; set; }
    
    public int OrderIndex { get; set; }
    
    public int DurationMinutes { get; set; }
    
    public bool IsFree { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Content properties
    public string? VideoUrl { get; set; }
    public string? VideoThumbnailUrl { get; set; }
    public string? ContentText { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    
    // Video-specific properties
    public string? VideoQuality { get; set; } // 480p, 720p, 1080p
    public long? VideoFileSize { get; set; }
    public string? VideoFormat { get; set; } // mp4, webm
    
    // Navigation properties (with [JsonIgnore] to prevent circular reference)
    [JsonIgnore]
    public virtual Section Section { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<LessonProgress> LessonProgress { get; set; } = new List<LessonProgress>();

    [JsonIgnore]
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
}

public enum LessonType
{
    Video,
    Text,
    Quiz,
    Assignment,
    Download,
    LiveSession
}

public class LessonProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid LessonId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    public bool IsCompleted { get; set; }
    
    public int WatchedMinutes { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties (with [JsonIgnore] to prevent circular reference)
    [JsonIgnore]
    public virtual Lesson Lesson { get; set; } = null!;

    [JsonIgnore]
    public virtual User User { get; set; } = null!;
    
    public double ProgressPercentage => Lesson.DurationMinutes > 0 
        ? Math.Min(100, (double)WatchedMinutes / Lesson.DurationMinutes * 100) 
        : 0;
}

public class Note
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid LessonId { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public int? TimeStamp { get; set; } // For video notes - timestamp in seconds
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties (with [JsonIgnore] to prevent circular reference)
    [JsonIgnore]
    public virtual User User { get; set; } = null!;

    [JsonIgnore]
    public virtual Lesson Lesson { get; set; } = null!;
}