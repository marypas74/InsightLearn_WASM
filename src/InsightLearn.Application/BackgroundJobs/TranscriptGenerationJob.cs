using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InsightLearn.Core.Interfaces;
using InsightLearn.Application.Services;

namespace InsightLearn.Application.BackgroundJobs
{
    /// <summary>
    /// Hangfire background job for asynchronous video transcript generation.
    /// Calls Whisper API and stores result in database + Redis cache.
    /// Phase 7.4: Auto-generates WebVTT subtitle files for download (LinkedIn Learning parity).
    /// Phase 8.4: Auto-queues translation jobs for top 5 languages (LinkedIn Learning parity).
    /// Part of Student Learning Space v2.1.0 and Batch Video Transcription System v2.3.23-dev.
    /// </summary>
    public class TranscriptGenerationJob
    {
        private readonly IVideoTranscriptService _transcriptService;
        private readonly ILessonRepository _lessonRepository;
        private readonly ILogger<TranscriptGenerationJob> _logger;
        private readonly MetricsService _metricsService;
        private readonly ISubtitleGenerator _subtitleGenerator;
        private readonly IMongoVideoStorageService _mongoStorage;
        private readonly IConfiguration _configuration;

        public TranscriptGenerationJob(
            IVideoTranscriptService transcriptService,
            ILessonRepository lessonRepository,
            ILogger<TranscriptGenerationJob> logger,
            MetricsService metricsService,
            ISubtitleGenerator subtitleGenerator,
            IMongoVideoStorageService mongoStorage,
            IConfiguration configuration)
        {
            _transcriptService = transcriptService ?? throw new ArgumentNullException(nameof(transcriptService));
            _lessonRepository = lessonRepository ?? throw new ArgumentNullException(nameof(lessonRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
            _subtitleGenerator = subtitleGenerator ?? throw new ArgumentNullException(nameof(subtitleGenerator));
            _mongoStorage = mongoStorage ?? throw new ArgumentNullException(nameof(mongoStorage));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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

            // Get lesson to determine video duration for metrics labeling
            var lesson = await _lessonRepository.GetByIdAsync(lessonId);
            var videoDurationMinutes = lesson?.DurationMinutes ?? 10; // Default to 10 if not set

            try
            {
                // Measure transcript processing duration with Prometheus
                using (_metricsService.MeasureTranscriptProcessing(videoDurationMinutes))
                {
                    // Call service to generate transcript (synchronous Whisper API call)
                    var transcript = await _transcriptService.GenerateTranscriptAsync(
                        lessonId,
                        videoUrl,
                        language,
                        CancellationToken.None);

                    _logger.LogInformation("[HANGFIRE] Transcript generation completed for lesson {LessonId}, {SegmentCount} segments",
                        lessonId, transcript.Segments.Count);

                    // Phase 7.4: Auto-generate WebVTT and SRT subtitle files
                    // LinkedIn Learning approach: Pre-generate subtitle files for instant download
                    try
                    {
                        _logger.LogInformation("[HANGFIRE] Auto-generating WebVTT subtitle file for lesson {LessonId}", lessonId);

                        // Generate WebVTT content
                        var webVttContent = _subtitleGenerator.TranscriptToWebVTT(transcript);
                        var webVttBytes = System.Text.Encoding.UTF8.GetBytes(webVttContent);

                        // Store WebVTT file in MongoDB GridFS
                        var webVttFileName = $"lesson-{lessonId}-{language}.vtt";
                        var webVttFileId = await _mongoStorage.StoreSubtitleFileAsync(
                            webVttFileName,
                            new System.IO.MemoryStream(webVttBytes),
                            "text/vtt",
                            lessonId,
                            Guid.Empty); // System-generated, no specific user

                        _logger.LogInformation("[HANGFIRE] WebVTT file generated and stored: {FileName}, FileId: {FileId}, Size: {Size} bytes",
                            webVttFileName, webVttFileId, webVttBytes.Length);

                        // Generate SRT content (alternative format)
                        _logger.LogInformation("[HANGFIRE] Auto-generating SRT subtitle file for lesson {LessonId}", lessonId);
                        var srtContent = _subtitleGenerator.TranscriptToSRT(transcript);
                        var srtBytes = System.Text.Encoding.UTF8.GetBytes(srtContent);

                        // Store SRT file in MongoDB GridFS
                        var srtFileName = $"lesson-{lessonId}-{language}.srt";
                        var srtFileId = await _mongoStorage.StoreSubtitleFileAsync(
                            srtFileName,
                            new System.IO.MemoryStream(srtBytes),
                            "application/x-subrip",
                            lessonId,
                            Guid.Empty); // System-generated

                        _logger.LogInformation("[HANGFIRE] SRT file generated and stored: {FileName}, FileId: {FileId}, Size: {Size} bytes",
                            srtFileName, srtFileId, srtBytes.Length);
                    }
                    catch (Exception subtitleEx)
                    {
                        // Log error but don't fail the entire job (subtitle generation is secondary)
                        _logger.LogError(subtitleEx, "[HANGFIRE] Failed to auto-generate subtitle files for lesson {LessonId}", lessonId);
                    }

                    // Record successful transcript generation
                    _metricsService.RecordTranscriptJob("success");

                    // Phase 8.4: Auto-queue translation jobs for top 5 languages (LinkedIn Learning parity)
                    // Only translate if source is English and auto-translation is enabled
                    if (language.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                    {
                        var enableAutoTranslation = _configuration.GetValue<bool>("Translation:EnableAutoTranslation", false);

                        if (enableAutoTranslation)
                        {
                            _logger.LogInformation("[HANGFIRE] Auto-translation enabled. Queueing translation jobs for lesson {LessonId}", lessonId);

                            // Read target languages from configuration (default: top 5 European languages)
                            var targetLanguages = _configuration.GetSection("Translation:TargetLanguages").Get<string[]>()
                                ?? new[] { "es", "fr", "de", "pt", "it" };

                            var defaultTranslator = _configuration.GetValue<string>("Translation:DefaultTranslator", "ollama");
                            var translationDelaySeconds = _configuration.GetValue<int>("Translation:TranslationDelaySeconds", 30);

                            _logger.LogInformation("[HANGFIRE] Queueing translation jobs for {LanguageCount} languages: {Languages}, translator: {Translator}",
                                targetLanguages.Length, string.Join(", ", targetLanguages), defaultTranslator);

                            // Queue translation jobs with staggered delays to prevent system overload
                            for (int i = 0; i < targetLanguages.Length; i++)
                            {
                                var targetLanguage = targetLanguages[i];
                                var delay = TimeSpan.FromSeconds(translationDelaySeconds * (i + 1)); // Stagger: 30s, 60s, 90s, 120s, 150s

                                var jobId = TranslationJob.Schedule(lessonId, targetLanguage, defaultTranslator, delay);

                                _logger.LogInformation("[HANGFIRE] Scheduled translation job {JobId} for lesson {LessonId}, language {Language}, delay {Delay}s",
                                    jobId, lessonId, targetLanguage, delay.TotalSeconds);
                            }

                            _logger.LogInformation("[HANGFIRE] Successfully queued {JobCount} translation jobs for lesson {LessonId}",
                                targetLanguages.Length, lessonId);
                        }
                        else
                        {
                            _logger.LogDebug("[HANGFIRE] Auto-translation disabled (Translation:EnableAutoTranslation=false). Skipping translation jobs.");
                        }
                    }
                    else
                    {
                        _logger.LogDebug("[HANGFIRE] Transcript language is not English ({Language}). Skipping auto-translation.", language);
                    }
                }
            }
            catch (TimeoutException)
            {
                _logger.LogError("[HANGFIRE] Transcript generation timeout for lesson {LessonId}", lessonId);
                _metricsService.RecordTranscriptJob("timeout");
                throw; // Re-throw to trigger Hangfire retry
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HANGFIRE] Transcript generation failed for lesson {LessonId}", lessonId);
                _metricsService.RecordTranscriptJob("failed");
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
