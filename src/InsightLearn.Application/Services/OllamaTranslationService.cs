using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;

namespace InsightLearn.Application.Services;

/// <summary>
/// Ollama-based subtitle translation service using mistral:7b-instruct
/// Translates transcription segments to target languages
/// </summary>
public class OllamaTranslationService : IOllamaTranslationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaTranslationService> _logger;
    private readonly IMongoDatabase _mongoDb;
    private readonly string _ollamaUrl;
    private readonly string _model = "mistral:7b-instruct";
    private readonly string _transcriptsCollectionName = "VideoTranscripts";

    public OllamaTranslationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OllamaTranslationService> logger,
        IMongoDatabase mongoDatabase)
    {
        _httpClient = httpClient;
        _logger = logger;
        _mongoDb = mongoDatabase;
        _ollamaUrl = configuration["Ollama:BaseUrl"] ?? "http://ollama-service:11434";
    }

    public async Task<TranslationResult> TranslateAsync(Guid lessonId, string sourceLanguage, string targetLanguage)
    {
        _logger.LogInformation("[OllamaTranslate] Translating lesson {LessonId}: {Source} → {Target}",
            lessonId, sourceLanguage, targetLanguage);

        try
        {
            // Load transcription from MongoDB
            var collection = _mongoDb.GetCollection<BsonDocument>(_transcriptsCollectionName);
            var filter = Builders<BsonDocument>.Filter.Eq("lessonId", lessonId.ToString());
            var transcriptDoc = await collection.Find(filter).FirstOrDefaultAsync();

            if (transcriptDoc == null)
            {
                throw new InvalidOperationException($"No transcript found for lesson {lessonId}");
            }

            // Extract segments from MongoDB document
            var segmentsArray = transcriptDoc["segments"].AsBsonArray;
            var translatedSegments = new List<TranslatedSegment>();

            // Translate each segment with context from previous 3 segments
            string? previousContext = null;
            for (int i = 0; i < segmentsArray.Count; i++)
            {
                var segment = segmentsArray[i].AsBsonDocument;
                var originalText = segment["text"].AsString;
                var startSeconds = segment["startSeconds"].AsDouble;
                var endSeconds = segment["endSeconds"].AsDouble;

                // Build context from last 3 translated segments
                if (i > 0)
                {
                    var contextBuilder = new StringBuilder();
                    int contextStart = Math.Max(0, i - 3);
                    for (int j = contextStart; j < i; j++)
                    {
                        contextBuilder.AppendLine(translatedSegments[j].TranslatedText);
                    }
                    previousContext = contextBuilder.ToString();
                }

                // Translate segment
                var translatedText = await TranslateSegmentAsync(originalText, sourceLanguage, targetLanguage, previousContext);

                var translatedSegment = new TranslatedSegment
                {
                    Index = i,
                    StartSeconds = startSeconds,
                    EndSeconds = endSeconds,
                    OriginalText = originalText,
                    TranslatedText = translatedText,
                    Quality = 0.85f // Placeholder quality score
                };

                translatedSegments.Add(translatedSegment);

                _logger.LogDebug("[OllamaTranslate] Translated segment {Index}/{Total}",
                    i + 1, segmentsArray.Count);
            }

            var result = new TranslationResult
            {
                LessonId = lessonId,
                SourceLanguage = sourceLanguage,
                TargetLanguage = targetLanguage,
                Segments = translatedSegments,
                TranslatedAt = DateTime.UtcNow,
                ModelUsed = _model
            };

            _logger.LogInformation("[OllamaTranslate] Translation completed: {Count} segments",
                translatedSegments.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OllamaTranslate] Translation failed for lesson {LessonId}", lessonId);
            throw;
        }
    }

    public async Task<string> TranslateSegmentAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        string? context = null)
    {
        try
        {
            var prompt = BuildTranslationPrompt(text, sourceLanguage, targetLanguage, context);

            _logger.LogDebug("[OllamaTranslate] Translating segment: {Text} ({Source} → {Target})",
                text.Substring(0, Math.Min(text.Length, 50)), sourceLanguage, targetLanguage);

            var request = new
            {
                model = _model,
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.3, // Lower temperature for more deterministic translations
                    num_predict = 200  // Limit response length
                }
            };

            var response = await _httpClient.PostAsJsonAsync($"{_ollamaUrl}/api/generate", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>();
            var translation = result?.Response?.Trim() ?? text;

            // Remove any explanation artifacts (Ollama sometimes adds them)
            translation = CleanTranslation(translation);

            _logger.LogDebug("[OllamaTranslate] Translated: {Original} → {Translation}",
                text.Substring(0, Math.Min(text.Length, 30)),
                translation.Substring(0, Math.Min(translation.Length, 30)));

            return translation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OllamaTranslate] Translation failed for text: {Text}", text);
            return text; // Fallback: return original text
        }
    }

    public async Task<List<LanguagePair>> GetSupportedLanguagesAsync()
    {
        // Common language pairs for InsightLearn platform
        return new List<LanguagePair>
        {
            new() { SourceLanguage = "en", SourceLanguageName = "English", TargetLanguage = "it", TargetLanguageName = "Italian" },
            new() { SourceLanguage = "en", SourceLanguageName = "English", TargetLanguage = "es", TargetLanguageName = "Spanish" },
            new() { SourceLanguage = "en", SourceLanguageName = "English", TargetLanguage = "fr", TargetLanguageName = "French" },
            new() { SourceLanguage = "en", SourceLanguageName = "English", TargetLanguage = "de", TargetLanguageName = "German" },
            new() { SourceLanguage = "en", SourceLanguageName = "English", TargetLanguage = "pt", TargetLanguageName = "Portuguese" },
            new() { SourceLanguage = "it", SourceLanguageName = "Italian", TargetLanguage = "en", TargetLanguageName = "English" },
            new() { SourceLanguage = "es", SourceLanguageName = "Spanish", TargetLanguage = "en", TargetLanguageName = "English" },
            new() { SourceLanguage = "fr", SourceLanguageName = "French", TargetLanguage = "en", TargetLanguageName = "English" },
        };
    }

    public async Task<string> GenerateTranslatedWebVTTAsync(Guid lessonId, string targetLanguage)
    {
        // TODO: Load translation from database and generate WebVTT
        _logger.LogWarning("[OllamaTranslate] GenerateTranslatedWebVTTAsync not implemented - returning placeholder");

        var vtt = new StringBuilder();
        vtt.AppendLine("WEBVTT");
        vtt.AppendLine();
        vtt.AppendLine($"NOTE Translated to {targetLanguage} by InsightLearn Ollama mistral:7b-instruct");
        vtt.AppendLine();

        // Placeholder cue
        vtt.AppendLine("1");
        vtt.AppendLine("00:00:00.000 --> 00:00:05.000");
        vtt.AppendLine($"Translation to {targetLanguage} not yet generated for this lesson.");

        return vtt.ToString();
    }

    /// <summary>
    /// Build translation prompt for Ollama
    /// Uses context from previous segments for better coherence
    /// </summary>
    private string BuildTranslationPrompt(string text, string sourceLang, string targetLang, string? context)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine($"Translate the following text from {GetLanguageName(sourceLang)} to {GetLanguageName(targetLang)}.");
        prompt.AppendLine("Provide ONLY the translation, no explanations.");

        if (!string.IsNullOrEmpty(context))
        {
            prompt.AppendLine();
            prompt.AppendLine("Context from previous segment:");
            prompt.AppendLine(context);
        }

        prompt.AppendLine();
        prompt.AppendLine("Text to translate:");
        prompt.AppendLine(text);
        prompt.AppendLine();
        prompt.AppendLine("Translation:");

        return prompt.ToString();
    }

    /// <summary>
    /// Clean Ollama response to remove artifacts
    /// </summary>
    private string CleanTranslation(string translation)
    {
        // Remove common Ollama artifacts
        translation = translation.Replace("Translation:", "").Trim();
        translation = translation.Replace("Here is the translation:", "").Trim();

        // Remove quotes if present
        if (translation.StartsWith("\"") && translation.EndsWith("\""))
        {
            translation = translation[1..^1];
        }

        return translation;
    }

    /// <summary>
    /// Get full language name from code
    /// </summary>
    private string GetLanguageName(string langCode)
    {
        return langCode.ToLower() switch
        {
            "en" => "English",
            "it" => "Italian",
            "es" => "Spanish",
            "fr" => "French",
            "de" => "German",
            "pt" => "Portuguese",
            "ru" => "Russian",
            "zh" => "Chinese",
            "ja" => "Japanese",
            "ko" => "Korean",
            _ => langCode.ToUpper()
        };
    }

    /// <summary>
    /// Ollama API response model
    /// </summary>
    private class OllamaGenerateResponse
    {
        public string? Model { get; set; }
        public string? Response { get; set; }
        public bool Done { get; set; }
    }
}
