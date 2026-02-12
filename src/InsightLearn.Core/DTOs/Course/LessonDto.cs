namespace InsightLearn.Core.DTOs.Course;

/// <summary>
/// Lesson DTO with video information
/// </summary>
public class LessonDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// URL-safe encoded ID for public URLs (v2.3.113-dev)
    /// </summary>
    public string EncodedId { get; set; } = string.Empty;

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

    // Subtitle tracks for the video
    public List<SubtitleTrackDto>? SubtitleTracks { get; set; }
}

/// <summary>
/// Subtitle track information for video lessons
/// </summary>
public class SubtitleTrackDto
{
    /// <summary>
    /// Unique identifier for the subtitle track
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// URL to the WebVTT subtitle file
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// ISO 639-1 language code (e.g., "en", "it", "es")
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable label (e.g., "English", "Italiano")
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Track kind: "subtitles", "captions", or "descriptions"
    /// </summary>
    public string Kind { get; set; } = "subtitles";

    /// <summary>
    /// Whether this should be the default track
    /// </summary>
    public bool IsDefault { get; set; }
}
