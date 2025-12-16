using System;
using System.Threading;
using System.Threading.Tasks;
using InsightLearn.Core.DTOs.VideoTranscript;

namespace InsightLearn.Core.Interfaces
{
    /// <summary>
    /// Service interface for video transcript generation and management.
    /// Integrates with Whisper API for ASR (Automatic Speech Recognition).
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public interface IVideoTranscriptService
    {
        /// <summary>
        /// Get transcript for a lesson with caching.
        /// Checks Redis cache first, then database.
        /// </summary>
        Task<VideoTranscriptDto?> GetTranscriptAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Queue transcript generation job for a lesson.
        /// Returns immediately, processing happens in background via Hangfire.
        /// </summary>
        /// <param name="lessonId">Lesson ID</param>
        /// <param name="videoUrl">URL to video file (MongoDB GridFS or external)</param>
        /// <param name="language">Language code (e.g., "en-US", "it-IT")</param>
        Task<string> QueueTranscriptGenerationAsync(Guid lessonId, string videoUrl, string language = "en-US", CancellationToken ct = default);

        /// <summary>
        /// Generate transcript synchronously (for testing or small videos).
        /// Uses Whisper API for ASR.
        /// </summary>
        Task<VideoTranscriptDto> GenerateTranscriptAsync(Guid lessonId, string videoUrl, string language = "en-US", CancellationToken ct = default);

        /// <summary>
        /// Search transcript content using full-text search.
        /// Returns matching segments with timestamps.
        /// </summary>
        Task<TranscriptSearchResultDto> SearchTranscriptAsync(Guid lessonId, string query, int limit = 10, CancellationToken ct = default);

        /// <summary>
        /// Get transcript processing status.
        /// </summary>
        Task<TranscriptProcessingStatusDto> GetProcessingStatusAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Delete transcript (cache + database).
        /// </summary>
        Task DeleteTranscriptAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Invalidate cache for a lesson transcript.
        /// Forces refresh from database on next request.
        /// </summary>
        Task InvalidateCacheAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Get transcript metadata without full content (faster).
        /// </summary>
        Task<TranscriptMetadataDto?> GetMetadataAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Update processing status (used by background job).
        /// </summary>
        Task UpdateProcessingStatusAsync(Guid lessonId, string status, string? errorMessage = null, CancellationToken ct = default);

        /// <summary>
        /// Generate demo transcript using Ollama LLM.
        /// Used when no real ASR is available - creates sample educational content.
        /// </summary>
        /// <param name="lessonId">Lesson ID</param>
        /// <param name="lessonTitle">Lesson title for context</param>
        /// <param name="durationSeconds">Video duration in seconds (for segment timing)</param>
        /// <param name="language">Language code</param>
        Task<VideoTranscriptDto> GenerateDemoTranscriptAsync(Guid lessonId, string lessonTitle, int durationSeconds = 300, string language = "en-US", CancellationToken ct = default);
    }
}
