using Hangfire;
using InsightLearn.Application.Services;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.BackgroundJobs;

/// <summary>
/// Hangfire background job for automatic subtitle generation.
/// Processes video files through Whisper ASR and creates WebVTT subtitle files.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public class SubtitleGenerationJob
{
    private readonly ISubtitleGenerationService _subtitleService;
    private readonly ILogger<SubtitleGenerationJob> _logger;

    public SubtitleGenerationJob(
        ISubtitleGenerationService subtitleService,
        ILogger<SubtitleGenerationJob> logger)
    {
        _subtitleService = subtitleService;
        _logger = logger;
    }

    /// <summary>
    /// Execute subtitle generation for a lesson.
    /// Called by Hangfire with automatic retry on failure.
    /// </summary>
    /// <param name="lessonId">Lesson ID to generate subtitles for.</param>
    /// <param name="videoFileId">MongoDB GridFS file ID of the video.</param>
    /// <param name="language">BCP-47 language code (e.g., "it-IT", "en-US").</param>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 600 })]
    [Queue("subtitles")]
    [JobDisplayName("Generate Subtitles for Lesson {0}")]
    public async Task ExecuteAsync(Guid lessonId, string videoFileId, string language = "it-IT")
    {
        _logger.LogInformation(
            "Starting subtitle generation job for lesson {LessonId}, video {VideoFileId}, language {Language}",
            lessonId, videoFileId, language);

        var startTime = DateTime.UtcNow;

        try
        {
            var result = await _subtitleService.GenerateSubtitlesAsync(lessonId, videoFileId, language);

            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Subtitle generation completed for lesson {LessonId} in {Elapsed:F2}s - Created track: {Language} ({CueCount} cues)",
                lessonId, elapsed.TotalSeconds, result.Language, result.Url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Subtitle generation failed for lesson {LessonId} after {Elapsed:F2}s",
                lessonId, (DateTime.UtcNow - startTime).TotalSeconds);
            throw; // Re-throw for Hangfire retry mechanism
        }
    }

    /// <summary>
    /// Queue subtitle generation for multiple languages.
    /// </summary>
    [Queue("subtitles")]
    [JobDisplayName("Generate Multi-Language Subtitles for Lesson {0}")]
    public async Task ExecuteMultiLanguageAsync(Guid lessonId, string videoFileId, string[] languages)
    {
        _logger.LogInformation(
            "Starting multi-language subtitle generation for lesson {LessonId}, languages: {Languages}",
            lessonId, string.Join(", ", languages));

        foreach (var language in languages)
        {
            try
            {
                // Check if subtitles already exist for this language
                var shortLang = language.Split('-')[0].ToLowerInvariant();
                if (await _subtitleService.HasSubtitlesAsync(lessonId, shortLang))
                {
                    _logger.LogInformation("Subtitles already exist for lesson {LessonId} in language {Language}, skipping",
                        lessonId, language);
                    continue;
                }

                await _subtitleService.GenerateSubtitlesAsync(lessonId, videoFileId, language);
                _logger.LogInformation("Generated subtitles for lesson {LessonId} in language {Language}",
                    lessonId, language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate subtitles for lesson {LessonId} in language {Language}",
                    lessonId, language);
                // Continue with other languages even if one fails
            }
        }
    }
}

/// <summary>
/// Extension methods for scheduling subtitle generation jobs.
/// </summary>
public static class SubtitleGenerationJobExtensions
{
    /// <summary>
    /// Schedule subtitle generation for a video immediately.
    /// </summary>
    public static string EnqueueSubtitleGeneration(
        this IBackgroundJobClient jobClient,
        Guid lessonId,
        string videoFileId,
        string language = "it-IT")
    {
        return jobClient.Enqueue<SubtitleGenerationJob>(
            job => job.ExecuteAsync(lessonId, videoFileId, language));
    }

    /// <summary>
    /// Schedule subtitle generation with a delay.
    /// </summary>
    public static string ScheduleSubtitleGeneration(
        this IBackgroundJobClient jobClient,
        Guid lessonId,
        string videoFileId,
        TimeSpan delay,
        string language = "it-IT")
    {
        return jobClient.Schedule<SubtitleGenerationJob>(
            job => job.ExecuteAsync(lessonId, videoFileId, language),
            delay);
    }

    /// <summary>
    /// Schedule multi-language subtitle generation.
    /// </summary>
    public static string EnqueueMultiLanguageSubtitleGeneration(
        this IBackgroundJobClient jobClient,
        Guid lessonId,
        string videoFileId,
        string[] languages)
    {
        return jobClient.Enqueue<SubtitleGenerationJob>(
            job => job.ExecuteMultiLanguageAsync(lessonId, videoFileId, languages));
    }
}
