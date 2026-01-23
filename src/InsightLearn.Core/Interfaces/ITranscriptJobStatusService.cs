using System;
using System.Threading;
using System.Threading.Tasks;
using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service for tracking transcript job status with real chunk-by-chunk progress.
/// v2.3.97-dev: Provides real-time progress updates for chunked transcription.
/// </summary>
public interface ITranscriptJobStatusService
{
    /// <summary>
    /// Create a new job status entry when transcription starts.
    /// </summary>
    Task<TranscriptJobStatus> CreateJobAsync(
        Guid lessonId,
        string? hangfireJobId,
        string language,
        int chunkCount,
        double? videoDurationSeconds = null,
        CancellationToken ct = default);

    /// <summary>
    /// Update job progress after completing a chunk.
    /// </summary>
    Task UpdateChunkProgressAsync(
        Guid lessonId,
        int completedChunks,
        int currentChunk,
        string phase,
        string statusMessage,
        CancellationToken ct = default);

    /// <summary>
    /// Mark job as completed.
    /// </summary>
    Task CompleteJobAsync(
        Guid lessonId,
        int segmentCount,
        long processingTimeMs,
        CancellationToken ct = default);

    /// <summary>
    /// Mark job as failed.
    /// </summary>
    Task FailJobAsync(
        Guid lessonId,
        string errorMessage,
        CancellationToken ct = default);

    /// <summary>
    /// Get current job status for a lesson.
    /// </summary>
    Task<TranscriptJobStatus?> GetJobStatusAsync(Guid lessonId, CancellationToken ct = default);

    /// <summary>
    /// Get all active jobs for monitoring dashboard.
    /// </summary>
    Task<IList<TranscriptJobStatus>> GetActiveJobsAsync(CancellationToken ct = default);

    /// <summary>
    /// Get recent jobs (completed and failed) for monitoring dashboard.
    /// </summary>
    Task<IList<TranscriptJobStatus>> GetRecentJobsAsync(int limit = 50, CancellationToken ct = default);

    /// <summary>
    /// Delete all job statuses for cleanup.
    /// </summary>
    Task DeleteAllJobsAsync(CancellationToken ct = default);
}
