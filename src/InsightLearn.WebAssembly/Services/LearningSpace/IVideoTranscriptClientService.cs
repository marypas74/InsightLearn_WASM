using InsightLearn.WebAssembly.Models;
using InsightLearn.Core.DTOs.VideoTranscript;
using InsightLearn.Core.DTOs.VideoTranscripts;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Frontend service for Video Transcripts API.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public interface IVideoTranscriptClientService
{
    /// <summary>
    /// Get complete transcript for a lesson.
    /// </summary>
    Task<ApiResponse<VideoTranscriptDto>> GetTranscriptAsync(Guid lessonId);

    /// <summary>
    /// Queue transcript generation (background job).
    /// </summary>
    Task<ApiResponse<object>> QueueGenerationAsync(QueueTranscriptDto dto);

    /// <summary>
    /// Get transcript processing status.
    /// </summary>
    Task<ApiResponse<TranscriptStatusDto>> GetStatusAsync(Guid lessonId);

    /// <summary>
    /// Search transcript for a keyword.
    /// </summary>
    Task<ApiResponse<List<TranscriptSearchResultDto>>> SearchTranscriptAsync(Guid lessonId, string searchText);

    /// <summary>
    /// Delete transcript.
    /// </summary>
    Task<ApiResponse<object>> DeleteTranscriptAsync(Guid lessonId);

    /// <summary>
    /// Auto-generate transcript using Ollama LLM.
    /// Creates demo transcript when viewing lesson.
    /// </summary>
    Task<ApiResponse<VideoTranscriptDto>> AutoGenerateTranscriptAsync(AutoGenerateTranscriptRequest request);

    /// <summary>
    /// Get translated transcript for a specific language.
    /// Phase 8.5: Multi-Language Subtitle Support.
    /// </summary>
    /// <param name="lessonId">Lesson ID</param>
    /// <param name="targetLanguage">Target language (ISO 639-1 code: es, fr, de, pt, it)</param>
    Task<ApiResponse<TranslationResponseDto>> GetTranslationAsync(Guid lessonId, string targetLanguage);
}

/// <summary>
/// Request for auto-generating transcripts.
/// </summary>
public class AutoGenerateTranscriptRequest
{
    public Guid LessonId { get; set; }
    public string? LessonTitle { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Language { get; set; }
}

/// <summary>
/// Transcript processing status DTO.
/// </summary>
public class TranscriptStatusDto
{
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public double? Progress { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Transcript search result DTO.
/// </summary>
public class TranscriptSearchResultDto
{
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? Speaker { get; set; }
    public int MatchIndex { get; set; }
}

/// <summary>
/// Translation response DTO.
/// Phase 8.5: Multi-Language Subtitle Support.
/// </summary>
public class TranslationResponseDto
{
    /// <summary>
    /// Translation status: NotFound, Processing, Failed, Completed
    /// </summary>
    public string Status { get; set; } = "Unknown";

    /// <summary>
    /// Optional message (for Processing/NotFound status)
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error message (for Failed status)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Lesson ID
    /// </summary>
    public Guid? LessonId { get; set; }

    /// <summary>
    /// Source language (e.g., "en")
    /// </summary>
    public string? SourceLanguage { get; set; }

    /// <summary>
    /// Target language (e.g., "es", "fr", "de")
    /// </summary>
    public string? TargetLanguage { get; set; }

    /// <summary>
    /// Translator used (e.g., "azure", "ollama")
    /// </summary>
    public string? Translator { get; set; }

    /// <summary>
    /// Quality tier (e.g., "Auto/Azure", "Auto/Ollama")
    /// </summary>
    public string? QualityTier { get; set; }

    /// <summary>
    /// Number of translated segments
    /// </summary>
    public int? SegmentCount { get; set; }

    /// <summary>
    /// Translated transcript segments
    /// </summary>
    public List<TranslatedSegmentDto>? Segments { get; set; }
}

/// <summary>
/// Translated transcript segment.
/// Phase 8.5: Multi-Language Subtitle Support.
/// </summary>
public class TranslatedSegmentDto
{
    public int Index { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public double? Confidence { get; set; }
}
