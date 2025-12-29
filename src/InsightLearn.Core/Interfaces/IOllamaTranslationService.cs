using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service for subtitle translation using Ollama mistral:7b-instruct model
/// Translates transcriptions to target languages
/// </summary>
public interface IOllamaTranslationService
{
    /// <summary>
    /// Translate transcription segments to target language
    /// </summary>
    /// <param name="lessonId">Lesson ID</param>
    /// <param name="sourceLanguage">Source language code (e.g., "en")</param>
    /// <param name="targetLanguage">Target language code (e.g., "it")</param>
    /// <returns>Translation result with segments</returns>
    Task<TranslationResult> TranslateAsync(Guid lessonId, string sourceLanguage, string targetLanguage);

    /// <summary>
    /// Translate single text segment
    /// </summary>
    /// <param name="text">Text to translate</param>
    /// <param name="sourceLanguage">Source language</param>
    /// <param name="targetLanguage">Target language</param>
    /// <param name="context">Optional context from previous segments</param>
    /// <returns>Translated text</returns>
    Task<string> TranslateSegmentAsync(string text, string sourceLanguage, string targetLanguage, string? context = null);

    /// <summary>
    /// Get supported language pairs
    /// </summary>
    Task<List<LanguagePair>> GetSupportedLanguagesAsync();

    /// <summary>
    /// Generate WebVTT subtitle file from translation
    /// </summary>
    Task<string> GenerateTranslatedWebVTTAsync(Guid lessonId, string targetLanguage);
}

/// <summary>
/// Result of translation operation
/// </summary>
public class TranslationResult
{
    public Guid LessonId { get; set; }
    public string SourceLanguage { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public List<TranslatedSegment> Segments { get; set; } = new();
    public DateTime TranslatedAt { get; set; }
    public string ModelUsed { get; set; } = "mistral:7b-instruct";
}

/// <summary>
/// Single translated segment with original and translation
/// </summary>
public class TranslatedSegment
{
    public int Index { get; set; }
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public float Quality { get; set; } // Translation quality score 0-1
}

/// <summary>
/// Supported language pair for translation
/// </summary>
public class LanguagePair
{
    public string SourceLanguage { get; set; } = string.Empty;
    public string SourceLanguageName { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public string TargetLanguageName { get; set; } = string.Empty;
}
