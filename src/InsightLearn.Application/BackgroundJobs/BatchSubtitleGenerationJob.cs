using Hangfire;
using InsightLearn.Application.Services;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.BackgroundJobs;

/// <summary>
/// Hangfire recurring job to generate subtitles for ALL videos that don't have subtitles yet.
/// Runs daily to ensure all new videos get auto-generated subtitles.
/// Part of Student Learning Space v2.1.0 batch processing system.
/// </summary>
public class BatchSubtitleGenerationJob
{
    private readonly InsightLearnDbContext _context;
    private readonly ISubtitleGenerationService _subtitleService;
    private readonly ILogger<BatchSubtitleGenerationJob> _logger;
    private readonly IBackgroundJobClient _jobClient;

    public BatchSubtitleGenerationJob(
        InsightLearnDbContext context,
        ISubtitleGenerationService subtitleService,
        ILogger<BatchSubtitleGenerationJob> logger,
        IBackgroundJobClient jobClient)
    {
        _context = context;
        _subtitleService = subtitleService;
        _logger = logger;
        _jobClient = jobClient;
    }

    /// <summary>
    /// Register this job as a recurring job in Hangfire.
    /// Call this method during application startup (Program.cs).
    /// </summary>
    public static void RegisterRecurringJob()
    {
        RecurringJob.AddOrUpdate<BatchSubtitleGenerationJob>(
            "batch-subtitle-generation",
            job => job.ProcessAllLessonsAsync(CancellationToken.None),
            Cron.Daily(3), // 3:00 AM UTC every day (same as transcript batch)
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            }
        );
    }

    /// <summary>
    /// Process all lessons that have videos but no subtitles.
    /// Queues individual subtitle generation jobs for each lesson.
    /// </summary>
    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 300, 900 })] // Retry after 5 min, 15 min
    public async Task ProcessAllLessonsAsync(CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("========================================");
        _logger.LogInformation("[BATCH SUBTITLE] Starting batch subtitle generation job at {Time}", startTime);
        _logger.LogInformation("========================================");

        try
        {
            // Find all lessons with videos but no active subtitles
            var lessonsNeedingSubtitles = await _context.Lessons
                .Where(l => l.VideoUrl != null && l.VideoUrl != string.Empty)
                .Where(l => !_context.SubtitleTracks.Any(st => st.LessonId == l.Id && st.IsActive))
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.VideoUrl,
                    l.CreatedAt
                })
                .OrderBy(l => l.CreatedAt) // Process oldest lessons first (FIFO)
                .ToListAsync(ct);

            var totalLessons = lessonsNeedingSubtitles.Count;

            if (totalLessons == 0)
            {
                _logger.LogInformation("[BATCH SUBTITLE] No lessons need subtitle generation. All videos already have subtitles.");
                return;
            }

            _logger.LogInformation("[BATCH SUBTITLE] Found {Count} lessons needing subtitle generation", totalLessons);

            // Track job IDs for reporting
            var jobIds = new List<string>();
            var successCount = 0;
            var errorCount = 0;

            // Process lessons in batches to avoid overwhelming Hangfire queue
            const int batchSize = 50; // Process 50 at a time
            const int pauseBetweenBatches = 30; // 30 second pause between batches

            for (int i = 0; i < totalLessons; i += batchSize)
            {
                var batch = lessonsNeedingSubtitles.Skip(i).Take(batchSize).ToList();
                var batchNumber = (i / batchSize) + 1;
                var totalBatches = (int)Math.Ceiling(totalLessons / (double)batchSize);

                _logger.LogInformation("[BATCH SUBTITLE] Processing batch {BatchNum}/{TotalBatches} ({Count} lessons)",
                    batchNumber, totalBatches, batch.Count);

                foreach (var lesson in batch)
                {
                    try
                    {
                        // Queue subtitle generation job (default language: Italian)
                        var jobId = await _subtitleService.QueueSubtitleGenerationAsync(
                            lesson.Id,
                            lesson.VideoUrl!,
                            language: "it-IT",
                            ct: ct);

                        jobIds.Add(jobId);
                        successCount++;

                        _logger.LogInformation(
                            "[BATCH SUBTITLE] Queued job {JobId} for lesson '{Title}' ({LessonId})",
                            jobId, lesson.Title, lesson.Id);
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("already exist"))
                    {
                        // Subtitles already exist (race condition with manual generation)
                        _logger.LogInformation(
                            "[BATCH SUBTITLE] Skipping lesson '{Title}' ({LessonId}) - subtitles already exist",
                            lesson.Title, lesson.Id);
                        successCount++; // Count as success
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        _logger.LogError(ex,
                            "[BATCH SUBTITLE] Error queuing subtitle generation for lesson '{Title}' ({LessonId})",
                            lesson.Title, lesson.Id);
                    }
                }

                // Pause between batches (except for last batch)
                if (i + batchSize < totalLessons)
                {
                    _logger.LogInformation("[BATCH SUBTITLE] Pausing {Seconds}s before next batch...", pauseBetweenBatches);
                    await Task.Delay(TimeSpan.FromSeconds(pauseBetweenBatches), ct);
                }
            }

            var elapsed = DateTime.UtcNow - startTime;

            _logger.LogInformation("========================================");
            _logger.LogInformation("[BATCH SUBTITLE] Batch job completed in {Elapsed}",
                elapsed.ToString(@"hh\:mm\:ss"));
            _logger.LogInformation("[BATCH SUBTITLE] Results: {Success} queued, {Errors} errors, {Total} total jobs",
                successCount, errorCount, jobIds.Count);
            _logger.LogInformation("========================================");

            // Schedule a report job to run 6 hours later to check completion status
            _jobClient.Schedule<BatchSubtitleReportJob>(
                job => job.GenerateCompletionReportAsync(jobIds, CancellationToken.None),
                TimeSpan.FromHours(6));

            _logger.LogInformation("[BATCH SUBTITLE] Scheduled completion report job to run in 6 hours");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BATCH SUBTITLE] Fatal error in batch subtitle generation job");
            throw;
        }
    }
}

/// <summary>
/// Generates a completion report for batch subtitle generation.
/// Checks Hangfire job states to determine success/failure rates.
/// </summary>
public class BatchSubtitleReportJob
{
    private readonly ILogger<BatchSubtitleReportJob> _logger;

    public BatchSubtitleReportJob(ILogger<BatchSubtitleReportJob> logger)
    {
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public Task GenerateCompletionReportAsync(List<string> jobIds, CancellationToken ct = default)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("[BATCH SUBTITLE REPORT] Completion Report");
        _logger.LogInformation("========================================");
        _logger.LogInformation("[BATCH SUBTITLE REPORT] Total jobs tracked: {Count}", jobIds.Count);

        // TODO: Query Hangfire storage to check job states
        // For now, just log that the report is available
        _logger.LogInformation("[BATCH SUBTITLE REPORT] Check Hangfire dashboard for detailed job status");
        _logger.LogInformation("[BATCH SUBTITLE REPORT] Dashboard: http://localhost:31081/hangfire");
        _logger.LogInformation("========================================");

        return Task.CompletedTask;
    }
}
