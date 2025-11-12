using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

/// <summary>
/// Tracks user engagement with courses for instructor payout calculation
/// </summary>
public class CourseEngagement
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid CourseId { get; set; }

    public Guid? LessonId { get; set; }

    /// <summary>
    /// Type of engagement: "video_watch", "quiz_attempt", "assignment_submit", "reading", "discussion"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EngagementType { get; set; } = string.Empty;

    /// <summary>
    /// Duration of engagement in minutes (actual time spent, validated)
    /// </summary>
    [Required]
    public int DurationMinutes { get; set; }

    /// <summary>
    /// When the engagement started
    /// </summary>
    [Required]
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// When the engagement completed (null if still in progress)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Validation score (0.0 - 1.0) - higher means more valid engagement
    /// Anti-fraud: checks tab visibility, playback speed, session continuity
    /// </summary>
    [Column(TypeName = "decimal(5,4)")]
    public decimal ValidationScore { get; set; } = 1.0m;

    /// <summary>
    /// Whether this engagement counts toward instructor payout
    /// Only counts if ValidationScore >= 0.7
    /// </summary>
    public bool CountsForPayout { get; set; } = true;

    /// <summary>
    /// Additional metadata as JSON (video progress, quiz score, device info, etc.)
    /// </summary>
    public string? MetaData { get; set; }

    /// <summary>
    /// IP address for fraud detection
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent for device tracking
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Device fingerprint for bot detection
    /// </summary>
    [MaxLength(64)]
    public string? DeviceFingerprint { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    [ForeignKey(nameof(CourseId))]
    public virtual Course Course { get; set; } = null!;

    [ForeignKey(nameof(LessonId))]
    public virtual Lesson? Lesson { get; set; }
}
