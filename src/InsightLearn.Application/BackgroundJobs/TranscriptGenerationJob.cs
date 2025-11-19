using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using InsightLearn.Core.Interfaces;

namespace InsightLearn.Application.BackgroundJobs
{
    /// <summary>
    /// Hangfire background job for asynchronous video transcript generation.
    /// Calls Whisper API and stores result in database + Redis cache.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class TranscriptGenerationJob
    {
        private readonly IVideoTranscriptService _transcriptService;
        private readonly ILogger<TranscriptGenerationJob> _logger;

        public TranscriptGenerationJob(
            IVideoTranscriptService transcriptService,
            ILogger<TranscriptGenerationJob> logger)
        {
            _transcriptService = transcriptService ?? throw new ArgumentNullException(nameof(transcriptService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Execute transcript generation job.
        /// Enqueued by VideoTranscriptService.QueueTranscriptGenerationAsync().
        /// </summary>
        /// <param name="lessonId">Lesson ID to generate transcript for</param>
        /// <param name="videoUrl">Video URL (MongoDB GridFS or external)</param>
        /// <param name="language">Language code (e.g., "en-US", "it-IT")</param>
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })] // Retry: 1 min, 5 min, 15 min
        public async Task ExecuteAsync(Guid lessonId, string videoUrl, string language = "en-US")
        {
            _logger.LogInformation("[HANGFIRE] Starting transcript generation job for lesson {LessonId}, language {Language}",
                lessonId, language);

            try
            {
                // Call service to generate transcript (synchronous Whisper API call)
                var transcript = await _transcriptService.GenerateTranscriptAsync(
                    lessonId,
                    videoUrl,
                    language,
                    CancellationToken.None);

                _logger.LogInformation("[HANGFIRE] Transcript generation completed for lesson {LessonId}, {SegmentCount} segments",
                    lessonId, transcript.Segments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HANGFIRE] Transcript generation failed for lesson {LessonId}", lessonId);
                throw; // Re-throw to trigger Hangfire retry
            }
        }

        /// <summary>
        /// Schedule transcript generation job to run immediately.
        /// </summary>
        public static string Enqueue(Guid lessonId, string videoUrl, string language = "en-US")
        {
            return BackgroundJob.Enqueue<TranscriptGenerationJob>(
                job => job.ExecuteAsync(lessonId, videoUrl, language));
        }

        /// <summary>
        /// Schedule transcript generation job to run after a delay.
        /// </summary>
        public static string Schedule(Guid lessonId, string videoUrl, string language, TimeSpan delay)
        {
            return BackgroundJob.Schedule<TranscriptGenerationJob>(
                job => job.ExecuteAsync(lessonId, videoUrl, language),
                delay);
        }
    }
}
