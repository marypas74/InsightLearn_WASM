using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service for tracking transcript job status with real chunk-by-chunk progress.
/// v2.3.97-dev: Provides real-time progress updates for chunked transcription.
/// </summary>
public class TranscriptJobStatusService : ITranscriptJobStatusService
{
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<TranscriptJobStatusService> _logger;

    public TranscriptJobStatusService(
        InsightLearnDbContext context,
        ILogger<TranscriptJobStatusService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TranscriptJobStatus> CreateJobAsync(
        Guid lessonId,
        string? hangfireJobId,
        string language,
        int chunkCount,
        double? videoDurationSeconds = null,
        CancellationToken ct = default)
    {
        // Check if a job already exists for this lesson (delete if exists to allow re-processing)
        var existing = await _context.TranscriptJobStatuses
            .FirstOrDefaultAsync(j => j.LessonId == lessonId, ct);

        if (existing != null)
        {
            _context.TranscriptJobStatuses.Remove(existing);
            _logger.LogInformation("[JOB_STATUS] Removed existing job for lesson {LessonId}", lessonId);
        }

        var job = new TranscriptJobStatus
        {
            LessonId = lessonId,
            HangfireJobId = hangfireJobId,
            Language = language,
            Phase = "Queued",
            Status = "Queued",
            ProgressPercentage = 0,
            StatusMessage = "Job queued for processing",
            ChunkCount = chunkCount,
            CompletedChunks = 0,
            CurrentChunk = 0,
            VideoDurationSeconds = videoDurationSeconds,
            ServiceUsed = "FasterWhisper",
            QueuedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };

        _context.TranscriptJobStatuses.Add(job);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[JOB_STATUS] Created job for lesson {LessonId}, {ChunkCount} chunks, language: {Language}",
            lessonId, chunkCount, language);

        return job;
    }

    public async Task UpdateChunkProgressAsync(
        Guid lessonId,
        int completedChunks,
        int currentChunk,
        string phase,
        string statusMessage,
        CancellationToken ct = default)
    {
        var job = await _context.TranscriptJobStatuses
            .FirstOrDefaultAsync(j => j.LessonId == lessonId, ct);

        if (job == null)
        {
            _logger.LogWarning("[JOB_STATUS] Job not found for lesson {LessonId}", lessonId);
            return;
        }

        // Calculate progress percentage based on chunks
        var totalChunks = job.ChunkCount ?? 10;
        var progressPercent = totalChunks > 0
            ? Math.Min(95, (completedChunks * 90 / totalChunks) + 5) // 5-95% for transcription, reserve 5% for saving
            : 50;

        job.CompletedChunks = completedChunks;
        job.CurrentChunk = currentChunk;
        job.Phase = phase;
        job.Status = "Processing";
        job.ProgressPercentage = progressPercent;
        job.StatusMessage = statusMessage;
        job.LastUpdatedAt = DateTime.UtcNow;

        if (job.StartedAt == null)
        {
            job.StartedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogDebug("[JOB_STATUS] Lesson {LessonId}: Chunk {Current}/{Total} ({Progress}%) - {Message}",
            lessonId, currentChunk, totalChunks, progressPercent, statusMessage);
    }

    public async Task CompleteJobAsync(
        Guid lessonId,
        int segmentCount,
        long processingTimeMs,
        CancellationToken ct = default)
    {
        var job = await _context.TranscriptJobStatuses
            .FirstOrDefaultAsync(j => j.LessonId == lessonId, ct);

        if (job == null)
        {
            _logger.LogWarning("[JOB_STATUS] Job not found for lesson {LessonId}", lessonId);
            return;
        }

        job.Phase = "Completed";
        job.Status = "Completed";
        job.ProgressPercentage = 100;
        job.StatusMessage = $"Transcription completed: {segmentCount} segments";
        job.CompletedChunks = job.ChunkCount;
        job.CurrentChunk = job.ChunkCount;
        job.SegmentCount = segmentCount;
        job.ProcessingTimeMs = processingTimeMs;
        job.CompletedAt = DateTime.UtcNow;
        job.LastUpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[JOB_STATUS] Job completed for lesson {LessonId}: {SegmentCount} segments in {ProcessingTime}ms",
            lessonId, segmentCount, processingTimeMs);
    }

    public async Task FailJobAsync(
        Guid lessonId,
        string errorMessage,
        CancellationToken ct = default)
    {
        var job = await _context.TranscriptJobStatuses
            .FirstOrDefaultAsync(j => j.LessonId == lessonId, ct);

        if (job == null)
        {
            _logger.LogWarning("[JOB_STATUS] Job not found for lesson {LessonId}", lessonId);
            return;
        }

        job.Phase = "Failed";
        job.Status = "Failed";
        job.StatusMessage = "Transcription failed";
        job.ErrorMessage = errorMessage;
        job.CompletedAt = DateTime.UtcNow;
        job.LastUpdatedAt = DateTime.UtcNow;
        job.RetryCount++;

        await _context.SaveChangesAsync(ct);

        _logger.LogError("[JOB_STATUS] Job failed for lesson {LessonId}: {Error}",
            lessonId, errorMessage);
    }

    public async Task<TranscriptJobStatus?> GetJobStatusAsync(Guid lessonId, CancellationToken ct = default)
    {
        return await _context.TranscriptJobStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.LessonId == lessonId, ct);
    }

    public async Task<IList<TranscriptJobStatus>> GetActiveJobsAsync(CancellationToken ct = default)
    {
        return await _context.TranscriptJobStatuses
            .AsNoTracking()
            .Where(j => j.Status != "Completed" && j.Status != "Failed" && j.Status != "Timeout")
            .OrderByDescending(j => j.QueuedAt)
            .ToListAsync(ct);
    }

    public async Task<IList<TranscriptJobStatus>> GetRecentJobsAsync(int limit = 50, CancellationToken ct = default)
    {
        return await _context.TranscriptJobStatuses
            .AsNoTracking()
            .OrderByDescending(j => j.QueuedAt)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task DeleteAllJobsAsync(CancellationToken ct = default)
    {
        var jobs = await _context.TranscriptJobStatuses.ToListAsync(ct);
        _context.TranscriptJobStatuses.RemoveRange(jobs);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[JOB_STATUS] Deleted {Count} job statuses", jobs.Count);
    }
}
