using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service for professional subtitle translation using Azure Cognitive Services Translator API v3.0
/// Provides higher quality translations compared to Ollama LLM for premium features
/// Phase 8: Multi-Language Subtitle Support - LinkedIn Learning parity
/// </summary>
public interface IAzureTranslatorService
{
    /// <summary>
    /// Translate transcript segments in batch using Azure Translator
    /// </summary>
    /// <param name="lessonId">Lesson ID for caching</param>
    /// <param name="segments">List of transcript segments to translate</param>
    /// <param name="sourceLanguage">Source language code (ISO 639-1, e.g., "en")</param>
    /// <param name="targetLanguage">Target language code (ISO 639-1, e.g., "it")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Translation result with translated segments</returns>
    Task<AzureTranslationResult> TranslateBatchAsync(
        Guid lessonId,
        List<TranscriptSegment> segments,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Translate a single text segment
    /// </summary>
    /// <param name="text">Text to translate</param>
    /// <param name="sourceLanguage">Source language (ISO 639-1)</param>
    /// <param name="targetLanguage">Target language (ISO 639-1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Translated text</returns>
    Task<string> TranslateSingleAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get supported languages for Azure Translator
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of language code -> native language name</returns>
    Task<Dictionary<string, AzureLanguageInfo>> GetSupportedLanguagesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect language of source text
    /// </summary>
    /// <param name="text">Text to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detected language code and confidence score (0-1)</returns>
    Task<(string LanguageCode, double Confidence)> DetectLanguageAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if translation service is available and configured
    /// </summary>
    /// <returns>True if Azure Translator is available</returns>
    Task<bool> IsAvailableAsync();
}

/// <summary>
/// Result of Azure translation operation
/// </summary>
public class AzureTranslationResult
{
    public Guid LessonId { get; set; }
    public string SourceLanguage { get; set; } = string.Empty;
    public string TargetLanguage { get; set; } = string.Empty;
    public List<AzureTranslatedSegment> Segments { get; set; } = new();
    public DateTime TranslatedAt { get; set; }
    public int TotalCharacters { get; set; }
    public double EstimatedCost { get; set; } // In USD
    public string TranslatorVersion { get; set; } = "Azure Translator v3.0";
}

/// <summary>
/// Single translated segment from Azure Translator
/// </summary>
public class AzureTranslatedSegment
{
    public int Index { get; set; }
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
    public string OriginalText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; } // Azure's confidence 0-1
    public bool IsProfanity { get; set; } // Azure can detect profanity
}

/// <summary>
/// Metadata for a supported language in Azure Translator
/// </summary>
public class AzureLanguageInfo
{
    public string Code { get; set; } = string.Empty; // ISO 639-1 code
    public string Name { get; set; } = string.Empty; // Language name in English
    public string NativeName { get; set; } = string.Empty; // Language name in native script
    public string Direction { get; set; } = "ltr"; // Text direction: ltr or rtl
    public bool SupportsProfanityDetection { get; set; }
}

/// <summary>
/// Transcript segment for translation (input model)
/// </summary>
public class TranscriptSegment
{
    public int Index { get; set; }
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
    public string Text { get; set; } = string.Empty;
}
