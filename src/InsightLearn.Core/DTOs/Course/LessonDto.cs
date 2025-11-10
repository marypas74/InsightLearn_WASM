namespace InsightLearn.Core.DTOs.Course;

/// <summary>
/// Lesson DTO with video information
/// </summary>
public class LessonDto
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty; // Video, Text, Quiz, Assignment, Download, LiveSession
    public int OrderIndex { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsFree { get; set; }
    public bool IsActive { get; set; }

    // Content properties
    public string? VideoUrl { get; set; }
    public string? VideoThumbnailUrl { get; set; }
    public string? ContentText { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }

    // Video-specific
    public string? VideoQuality { get; set; }
    public long? VideoFileSize { get; set; }
    public string? VideoFormat { get; set; }
}
