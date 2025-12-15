namespace InsightLearn.Application.Interfaces;

/// <summary>
/// Service for translating subtitle tracks using Ollama LLM
/// Implements context-aware translation with caching in MongoDB
/// </summary>
public interface ISubtitleTranslationService
{
    /// <summary>
    /// Translates an entire WebVTT subtitle file from source to target language
    /// Uses batch translation with context awareness (previous 3-5 cues)
    /// Results are cached in MongoDB for reuse
    /// </summary>
    /// <param name="sourceVttUrl">URL or path to the source WebVTT file</param>
    /// <param name="sourceLanguage">ISO 639-1 language code of source (e.g., "en", "it")</param>
    /// <param name="targetLanguage">ISO 639-1 language code of target (e.g., "es", "fr")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Translated WebVTT content as string</returns>
    Task<string> TranslateSubtitlesAsync(
        string sourceVttUrl,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates translated subtitles for a specific lesson
    /// Checks MongoDB cache first, then translates if not found
    /// </summary>
    /// <param name="lessonId">Lesson GUID</param>
    /// <param name="targetLanguage">ISO 639-1 target language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Translated WebVTT content or null if source subtitles not found</returns>
    Task<string?> GetOrCreateTranslatedSubtitlesAsync(
        Guid lessonId,
        string targetLanguage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of supported languages for translation
    /// </summary>
    /// <returns>Dictionary of language code -> language name (e.g., "it" -> "Italiano")</returns>
    Dictionary<string, string> GetSupportedLanguages();

    /// <summary>
    /// Checks if translated subtitles exist in MongoDB cache
    /// </summary>
    /// <param name="lessonId">Lesson GUID</param>
    /// <param name="targetLanguage">Target language code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if translation exists, false otherwise</returns>
    Task<bool> TranslationExistsAsync(
        Guid lessonId,
        string targetLanguage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all cached translations for a specific lesson
    /// Used when source subtitles are updated
    /// </summary>
    /// <param name="lessonId">Lesson GUID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteTranslationsForLessonAsync(
        Guid lessonId,
        CancellationToken cancellationToken = default);
}
