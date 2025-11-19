using System;
using System.Threading;
using System.Threading.Tasks;
using InsightLearn.Core.Entities;
using InsightLearn.Core.DTOs.AITakeaways;

namespace InsightLearn.Core.Interfaces
{
    /// <summary>
    /// Hybrid repository interface for AI Takeaways (SQL Server metadata + MongoDB data).
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public interface IAITakeawayRepository
    {
        /// <summary>
        /// Get complete takeaways for a lesson (SQL metadata + MongoDB data).
        /// </summary>
        Task<VideoKeyTakeawaysDto?> GetTakeawaysAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Get only metadata from SQL Server.
        /// </summary>
        Task<AIKeyTakeawaysMetadata?> GetMetadataAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Create new takeaways metadata and save takeaways data to MongoDB.
        /// </summary>
        Task<AIKeyTakeawaysMetadata> CreateAsync(Guid lessonId, VideoKeyTakeawaysDto takeawaysDto, CancellationToken ct = default);

        /// <summary>
        /// Update takeaway processing status.
        /// </summary>
        Task UpdateProcessingStatusAsync(Guid lessonId, string status, string? errorMessage = null, CancellationToken ct = default);

        /// <summary>
        /// Submit user feedback for a takeaway (thumbs up/down).
        /// </summary>
        Task SubmitFeedbackAsync(Guid lessonId, string takeawayId, int feedback, CancellationToken ct = default);

        /// <summary>
        /// Delete takeaways (SQL metadata + MongoDB data).
        /// </summary>
        Task DeleteAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Check if takeaways exist for a lesson.
        /// </summary>
        Task<bool> ExistsAsync(Guid lessonId, CancellationToken ct = default);
    }
}
