using System;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using InsightLearn.Core.Interfaces;

namespace InsightLearn.Application.BackgroundJobs
{
    /// <summary>
    /// Hangfire background job for asynchronous AI takeaway generation.
    /// Calls Ollama API (qwen2:0.5b) to extract key takeaways from video transcripts.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class AITakeawayGenerationJob
    {
        private readonly IAIAnalysisService _analysisService;
        private readonly IVideoTranscriptRepository _transcriptRepository;
        private readonly ILogger<AITakeawayGenerationJob> _logger;

        public AITakeawayGenerationJob(
            IAIAnalysisService analysisService,
            IVideoTranscriptRepository transcriptRepository,
            ILogger<AITakeawayGenerationJob> logger)
        {
            _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
            _transcriptRepository = transcriptRepository ?? throw new ArgumentNullException(nameof(transcriptRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Execute AI takeaway generation job.
        /// Enqueued by AIAnalysisService.QueueTakeawayGenerationAsync().
        /// </summary>
        /// <param name="lessonId">Lesson ID to generate takeaways for</param>
        /// <param name="transcriptText">Optional transcript text (if null, fetches from database)</param>
        [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 120, 300 })] // Retry: 30s, 2min, 5min
        public async Task ExecuteAsync(Guid lessonId, string? transcriptText = null)
        {
            _logger.LogInformation("[HANGFIRE] Starting AI takeaway generation job for lesson {LessonId}", lessonId);

            try
            {
                // Get transcript text if not provided
                if (string.IsNullOrEmpty(transcriptText))
                {
                    _logger.LogDebug("Fetching transcript from database for lesson {LessonId}", lessonId);
                    var transcript = await _transcriptRepository.GetTranscriptAsync(lessonId, CancellationToken.None);

                    if (transcript == null)
                    {
                        _logger.LogWarning("Transcript not found for lesson {LessonId}, cannot generate takeaways", lessonId);
                        return;
                    }

                    transcriptText = transcript.FullTranscript;
                }

                // Call AI analysis service to generate takeaways
                var takeaways = await _analysisService.GenerateTakeawaysAsync(
                    lessonId,
                    transcriptText,
                    CancellationToken.None);

                _logger.LogInformation("[HANGFIRE] AI takeaway generation completed for lesson {LessonId}, {TakeawayCount} takeaways",
                    lessonId, takeaways.Takeaways.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HANGFIRE] AI takeaway generation failed for lesson {LessonId}", lessonId);
                throw; // Re-throw to trigger Hangfire retry
            }
        }

        /// <summary>
        /// Schedule AI takeaway generation job to run immediately.
        /// </summary>
        public static string Enqueue(Guid lessonId, string? transcriptText = null)
        {
            return BackgroundJob.Enqueue<AITakeawayGenerationJob>(
                job => job.ExecuteAsync(lessonId, transcriptText));
        }

        /// <summary>
        /// Schedule AI takeaway generation job to run after a delay.
        /// Useful for chaining after transcript generation completes.
        /// </summary>
        public static string Schedule(Guid lessonId, string? transcriptText, TimeSpan delay)
        {
            return BackgroundJob.Schedule<AITakeawayGenerationJob>(
                job => job.ExecuteAsync(lessonId, transcriptText),
                delay);
        }

        /// <summary>
        /// Schedule AI takeaway generation job to run after transcript generation completes.
        /// Uses Hangfire continuation feature.
        /// </summary>
        public static string ContinueWith(string transcriptJobId, Guid lessonId)
        {
            return BackgroundJob.ContinueJobWith<AITakeawayGenerationJob>(
                transcriptJobId,
                job => job.ExecuteAsync(lessonId, null));
        }
    }
}
