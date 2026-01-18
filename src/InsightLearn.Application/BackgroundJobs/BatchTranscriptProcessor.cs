using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using InsightLearn.Core.Interfaces;

namespace InsightLearn.Application.BackgroundJobs
{
    /// <summary>
    /// Hangfire recurring job to batch-process ALL lessons without transcripts.
    /// LinkedIn Learning approach: pre-generate transcripts offline, NOT on-demand.
    /// Scheduled: Daily at 3:00 AM UTC via Cron.Daily(3).
    /// Part of Batch Video Transcription System v2.3.23-dev.
    /// </summary>
    public class BatchTranscriptProcessor
    {
        private readonly ILessonRepository _lessonRepository;
        private readonly ILogger<BatchTranscriptProcessor> _logger;

        public BatchTranscriptProcessor(
            ILessonRepository lessonRepository,
            ILogger<BatchTranscriptProcessor> logger)
        {
            _lessonRepository = lessonRepository ?? throw new ArgumentNullException(nameof(lessonRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Main processing method - finds all lessons without transcripts and queues generation jobs.
        /// </summary>
        [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 300, 900 })] // Retry: 5 min, 15 min
        public async Task ProcessAllLessonsAsync(PerformContext context)
        {
            var cancellationToken = context?.CancellationToken.ShutdownToken ?? CancellationToken.None;

            _logger.LogInformation("[BATCH PROCESSOR] Starting batch transcript processing...");

            try
            {
                // Find all lessons WITHOUT transcripts
                var lessonsWithoutTranscripts = await _lessonRepository.GetLessonsWithoutTranscriptsAsync();

                if (lessonsWithoutTranscripts.Count == 0)
                {
                    _logger.LogInformation("[BATCH PROCESSOR] No lessons need transcript generation. All done!");
                    return;
                }

                _logger.LogInformation("[BATCH PROCESSOR] Found {Count} lessons without transcripts",
                    lessonsWithoutTranscripts.Count);

                var jobIds = new List<string>();
                int processed = 0;
                int batchSize = 100;

                foreach (var lesson in lessonsWithoutTranscripts)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("[BATCH PROCESSOR] Cancellation requested - stopping at {Processed}/{Total}",
                            processed, lessonsWithoutTranscripts.Count);
                        break;
                    }

                    try
                    {
                        // Queue TranscriptGenerationJob for this lesson
                        var videoUrl = lesson.VideoUrl;
                        var jobId = TranscriptGenerationJob.Enqueue(
                            lesson.Id,
                            videoUrl,
                            "en-US" // Default language
                        );

                        jobIds.Add(jobId);
                        processed++;

                        _logger.LogDebug("[BATCH PROCESSOR] Queued job {JobId} for lesson {LessonId} ({Processed}/{Total})",
                            jobId, lesson.Id, processed, lessonsWithoutTranscripts.Count);

                        // Throttle: Pause every 100 jobs to avoid overwhelming Hangfire
                        if (processed % batchSize == 0)
                        {
                            _logger.LogInformation("[BATCH PROCESSOR] Processed {Processed}/{Total} - pausing 30 seconds...",
                                processed, lessonsWithoutTranscripts.Count);
                            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[BATCH PROCESSOR] Failed to queue job for lesson {LessonId}",
                            lesson.Id);
                        // Continue processing other lessons
                    }
                }

                _logger.LogInformation("[BATCH PROCESSOR] Batch processing complete - {Processed} jobs queued",
                    processed);

                // Schedule completion report after 6 hours (estimated completion time for all jobs)
                BackgroundJob.Schedule<BatchTranscriptReportJob>(
                    job => job.GenerateReportAsync(jobIds),
                    TimeSpan.FromHours(6)
                );

                _logger.LogInformation("[BATCH PROCESSOR] Scheduled completion report for 6 hours from now");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BATCH PROCESSOR] Batch processing failed");
                throw; // Re-throw to trigger Hangfire retry
            }
        }

        /// <summary>
        /// Register this job as a recurring job in Hangfire.
        /// Call this method during application startup (Program.cs).
        /// </summary>
        public static void RegisterRecurringJob()
        {
            RecurringJob.AddOrUpdate<BatchTranscriptProcessor>(
                "batch-transcript-processor",
                processor => processor.ProcessAllLessonsAsync(null),
                "30 23 * * *", // 23:30 (11:30 PM) UTC every day
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.Utc
                }
            );
        }
    }
}
