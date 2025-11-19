using System;
using System.Threading;
using System.Threading.Tasks;
using InsightLearn.Core.DTOs.AITakeaways;

namespace InsightLearn.Core.Interfaces
{
    /// <summary>
    /// Service interface for AI-powered content analysis.
    /// Integrates with Ollama (qwen2:0.5b) for key takeaway extraction.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public interface IAIAnalysisService
    {
        /// <summary>
        /// Get AI-generated key takeaways for a lesson with caching.
        /// Checks Redis cache first, then database.
        /// </summary>
        Task<VideoKeyTakeawaysDto?> GetTakeawaysAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Queue takeaway generation job for a lesson.
        /// Returns immediately, processing happens in background via Hangfire.
        /// </summary>
        /// <param name="lessonId">Lesson ID</param>
        /// <param name="transcriptText">Full transcript text or null to fetch from database</param>
        Task<string> QueueTakeawayGenerationAsync(Guid lessonId, string? transcriptText = null, CancellationToken ct = default);

        /// <summary>
        /// Generate takeaways synchronously using Ollama AI.
        /// Analyzes transcript to extract key concepts, best practices, examples.
        /// </summary>
        Task<VideoKeyTakeawaysDto> GenerateTakeawaysAsync(Guid lessonId, string transcriptText, CancellationToken ct = default);

        /// <summary>
        /// Submit user feedback for a takeaway (thumbs up/down).
        /// Updates MongoDB document.
        /// </summary>
        Task SubmitFeedbackAsync(Guid lessonId, string takeawayId, int feedback, CancellationToken ct = default);

        /// <summary>
        /// Get takeaway processing status.
        /// </summary>
        Task<TakeawayProcessingStatusDto> GetProcessingStatusAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Delete takeaways (cache + database).
        /// </summary>
        Task DeleteTakeawaysAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Invalidate cache for lesson takeaways.
        /// </summary>
        Task InvalidateCacheAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Update processing status (used by background job).
        /// </summary>
        Task UpdateProcessingStatusAsync(Guid lessonId, string status, string? errorMessage = null, CancellationToken ct = default);

        /// <summary>
        /// Calculate relevance score for a takeaway text.
        /// Uses heuristic analysis (keyword density, position in transcript, etc.).
        /// </summary>
        double CalculateRelevanceScore(string takeawayText, string fullTranscript);

        /// <summary>
        /// Classify takeaway category based on content.
        /// Returns: CoreConcept, BestPractice, Example, Warning, Summary, KeyPoint.
        /// </summary>
        string ClassifyTakeawayCategory(string takeawayText);
    }
}
