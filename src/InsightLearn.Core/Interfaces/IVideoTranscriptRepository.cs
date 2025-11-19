using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLearn.Core.Entities;
using InsightLearn.Core.DTOs.VideoTranscript;

namespace InsightLearn.Core.Interfaces
{
    /// <summary>
    /// Hybrid repository interface for VideoTranscript (SQL Server metadata + MongoDB data).
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public interface IVideoTranscriptRepository
    {
        /// <summary>
        /// Get complete transcript for a lesson (SQL metadata + MongoDB data).
        /// </summary>
        Task<VideoTranscriptDto?> GetTranscriptAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Get only metadata from SQL Server.
        /// </summary>
        Task<VideoTranscriptMetadata?> GetMetadataAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Search within transcript text (MongoDB full-text search).
        /// </summary>
        Task<TranscriptSearchResultDto> SearchTranscriptAsync(Guid lessonId, string query, int limit = 10, CancellationToken ct = default);

        /// <summary>
        /// Create new transcript metadata and save transcript data to MongoDB.
        /// </summary>
        Task<VideoTranscriptMetadata> CreateAsync(Guid lessonId, VideoTranscriptDto transcriptDto, CancellationToken ct = default);

        /// <summary>
        /// Update transcript processing status.
        /// </summary>
        Task UpdateProcessingStatusAsync(Guid lessonId, string status, string? errorMessage = null, CancellationToken ct = default);

        /// <summary>
        /// Delete transcript (SQL metadata + MongoDB data).
        /// </summary>
        Task DeleteAsync(Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Check if transcript exists for a lesson.
        /// </summary>
        Task<bool> ExistsAsync(Guid lessonId, CancellationToken ct = default);
    }
}
