using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace InsightLearn.Application.Services;

/// <summary>
/// Azure Cognitive Services Translator implementation for professional subtitle translation
/// Phase 8: Multi-Language Subtitle Support - LinkedIn Learning parity feature
/// Uses Azure Translator API v3.0 for high-quality translations
/// </summary>
public class AzureTranslatorService : IAzureTranslatorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureTranslatorService> _logger;
    private readonly IMongoDatabase _mongoDatabase;
    private readonly string? _azureTranslatorKey;
    private readonly string? _azureTranslatorRegion;
    private readonly string _azureTranslatorEndpoint;

    // Azure Translator API v3.0 endpoints
    private const string TranslateEndpoint = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0";
    private const string DetectEndpoint = "https://api.cognitive.microsofttranslator.com/detect?api-version=3.0";
    private const string LanguagesEndpoint = "https://api.cognitive.microsofttranslator.com/languages?api-version=3.0&scope=translation";

    // Azure Translator pricing (as of 2024): $10 per 1M characters for standard tier
    private const double CostPerMillionCharacters = 10.0;

    // Batch size limits
    private const int MaxBatchSize = 100; // Azure allows up to 100 texts per request
    private const int MaxTextLength = 10000; // Azure limit: 10k characters per text

    // MongoDB collection for caching Azure translations
    private const string AzureTranslationsCollection = "AzureTranslatedSubtitles";

    // Retry configuration
    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays = { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) };

    public AzureTranslatorService(
        HttpClient httpClient,
        ILogger<AzureTranslatorService> logger,
        IMongoDatabase mongoDatabase,
        IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mongoDatabase = mongoDatabase ?? throw new ArgumentNullException(nameof(mongoDatabase));

        // Load Azure credentials from configuration
        _azureTranslatorKey = configuration["Azure:TranslatorKey"] ?? Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_KEY");
        _azureTranslatorRegion = configuration["Azure:TranslatorRegion"] ?? Environment.GetEnvironmentVariable("AZURE_TRANSLATOR_REGION") ?? "westeurope";
        _azureTranslatorEndpoint = configuration["Azure:TranslatorEndpoint"] ?? TranslateEndpoint;

        // Configure HTTP client
        if (!string.IsNullOrEmpty(_azureTranslatorKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _azureTranslatorKey);
            if (!string.IsNullOrEmpty(_azureTranslatorRegion))
            {
                _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Region", _azureTranslatorRegion);
            }
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (string.IsNullOrEmpty(_azureTranslatorKey))
        {
            _logger.LogWarning("[AZURE TRANSLATOR] Azure Translator key not configured");
            return false;
        }

        try
        {
            // Test connectivity by fetching supported languages
            var response = await _httpClient.GetAsync(LanguagesEndpoint);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE TRANSLATOR] Failed to connect to Azure Translator");
            return false;
        }
    }

    public async Task<AzureTranslationResult> TranslateBatchAsync(
        Guid lessonId,
        List<TranscriptSegment> segments,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        if (segments == null || segments.Count == 0)
            throw new ArgumentException("Segments cannot be null or empty", nameof(segments));

        if (string.IsNullOrEmpty(_azureTranslatorKey))
            throw new InvalidOperationException("Azure Translator API key is not configured");

        _logger.LogInformation(
            "[AZURE TRANSLATOR] Starting batch translation for lesson {LessonId}: {SegmentCount} segments from {SourceLang} to {TargetLang}",
            lessonId, segments.Count, sourceLanguage, targetLanguage);

        // Check MongoDB cache first
        var cachedResult = await GetCachedTranslationAsync(lessonId, targetLanguage, cancellationToken);
        if (cachedResult != null)
        {
            _logger.LogInformation("[AZURE TRANSLATOR] Returning cached translation for lesson {LessonId}", lessonId);
            return cachedResult;
        }

        var translatedSegments = new List<AzureTranslatedSegment>();
        var totalCharacters = 0;

        // Process segments in batches of 100 (Azure limit)
        for (int i = 0; i < segments.Count; i += MaxBatchSize)
        {
            var batch = segments.Skip(i).Take(MaxBatchSize).ToList();

            try
            {
                var batchResults = await TranslateBatchWithRetryAsync(
                    batch,
                    sourceLanguage,
                    targetLanguage,
                    cancellationToken);

                translatedSegments.AddRange(batchResults);
                totalCharacters += batch.Sum(s => s.Text.Length);

                _logger.LogDebug("[AZURE TRANSLATOR] Batch {BatchIndex}/{TotalBatches} completed: {Count} segments",
                    (i / MaxBatchSize) + 1, (segments.Count + MaxBatchSize - 1) / MaxBatchSize, batchResults.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AZURE TRANSLATOR] Failed to translate batch {BatchIndex}", (i / MaxBatchSize) + 1);
                throw;
            }
        }

        var result = new AzureTranslationResult
        {
            LessonId = lessonId,
            SourceLanguage = sourceLanguage,
            TargetLanguage = targetLanguage,
            Segments = translatedSegments,
            TranslatedAt = DateTime.UtcNow,
            TotalCharacters = totalCharacters,
            EstimatedCost = CalculateCost(totalCharacters),
            TranslatorVersion = "Azure Translator v3.0"
        };

        // Cache the result in MongoDB
        await CacheTranslationAsync(result, cancellationToken);

        _logger.LogInformation(
            "[AZURE TRANSLATOR] Batch translation completed: {SegmentCount} segments, {Characters} chars, estimated cost ${Cost:F4}",
            translatedSegments.Count, totalCharacters, result.EstimatedCost);

        return result;
    }

    public async Task<string> TranslateSingleAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or whitespace", nameof(text));

        if (string.IsNullOrEmpty(_azureTranslatorKey))
            throw new InvalidOperationException("Azure Translator API key is not configured");

        var segments = new List<TranscriptSegment>
        {
            new TranscriptSegment { Index = 0, Text = text, StartSeconds = 0, EndSeconds = 0 }
        };

        var results = await TranslateBatchWithRetryAsync(segments, sourceLanguage, targetLanguage, cancellationToken);

        return results.FirstOrDefault()?.TranslatedText ?? text;
    }

    public async Task<Dictionary<string, AzureLanguageInfo>> GetSupportedLanguagesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(LanguagesEndpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var languagesData = JsonSerializer.Deserialize<AzureLanguagesResponse>(json);

            if (languagesData?.Translation == null)
                return new Dictionary<string, AzureLanguageInfo>();

            return languagesData.Translation.ToDictionary(
                kvp => kvp.Key,
                kvp => new AzureLanguageInfo
                {
                    Code = kvp.Key,
                    Name = kvp.Value.Name,
                    NativeName = kvp.Value.NativeName,
                    Direction = kvp.Value.Dir ?? "ltr",
                    SupportsProfanityDetection = true // Azure supports profanity detection for all languages
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE TRANSLATOR] Failed to fetch supported languages");
            return GetFallbackLanguages();
        }
    }

    public async Task<(string LanguageCode, double Confidence)> DetectLanguageAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or whitespace", nameof(text));

        if (string.IsNullOrEmpty(_azureTranslatorKey))
            throw new InvalidOperationException("Azure Translator API key is not configured");

        try
        {
            var requestBody = new[] { new { Text = text } };
            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(DetectEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var detectionResults = JsonSerializer.Deserialize<List<AzureDetectionResponse>>(json);

            if (detectionResults != null && detectionResults.Count > 0 && detectionResults[0].Language != null)
            {
                return (detectionResults[0].Language, detectionResults[0].Score);
            }

            return ("unknown", 0.0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE TRANSLATOR] Language detection failed for text: {TextPreview}", text.Substring(0, Math.Min(100, text.Length)));
            return ("unknown", 0.0);
        }
    }

    #region Private Helper Methods

    private async Task<List<AzureTranslatedSegment>> TranslateBatchWithRetryAsync(
        List<TranscriptSegment> segments,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                return await TranslateBatchInternalAsync(segments, sourceLanguage, targetLanguage, cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < MaxRetries - 1)
            {
                lastException = ex;
                _logger.LogWarning(ex, "[AZURE TRANSLATOR] Retry {Attempt}/{MaxRetries} after error", attempt + 1, MaxRetries);
                await Task.Delay(RetryDelays[attempt], cancellationToken);
            }
        }

        throw new Exception($"Azure Translator failed after {MaxRetries} retries", lastException);
    }

    private async Task<List<AzureTranslatedSegment>> TranslateBatchInternalAsync(
        List<TranscriptSegment> segments,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        // Build request URL with source and target language
        var url = $"{_azureTranslatorEndpoint}&from={sourceLanguage}&to={targetLanguage}&profanityAction=Marked&includeAlignment=false&includeSentenceLength=false";

        // Build request body: array of { Text: "..." } objects
        var requestBody = segments.Select(s => new { Text = TruncateText(s.Text, MaxTextLength) }).ToArray();
        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        // Send request
        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("[AZURE TRANSLATOR] API error {StatusCode}: {Error}", response.StatusCode, errorBody);
            response.EnsureSuccessStatusCode();
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var translationResults = JsonSerializer.Deserialize<List<AzureTranslationResponse>>(json);

        if (translationResults == null || translationResults.Count != segments.Count)
        {
            throw new Exception($"Azure Translator returned unexpected response: expected {segments.Count} translations, got {translationResults?.Count ?? 0}");
        }

        var translatedSegments = new List<AzureTranslatedSegment>();

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            var translation = translationResults[i];

            if (translation.Translations != null && translation.Translations.Count > 0)
            {
                var firstTranslation = translation.Translations[0];

                translatedSegments.Add(new AzureTranslatedSegment
                {
                    Index = segment.Index,
                    StartSeconds = segment.StartSeconds,
                    EndSeconds = segment.EndSeconds,
                    OriginalText = segment.Text,
                    TranslatedText = firstTranslation.Text ?? segment.Text,
                    ConfidenceScore = 0.95, // Azure doesn't provide confidence in v3.0, use default high value
                    IsProfanity = DetectProfanityMarker(firstTranslation.Text)
                });
            }
            else
            {
                // Fallback: keep original text if translation failed
                translatedSegments.Add(new AzureTranslatedSegment
                {
                    Index = segment.Index,
                    StartSeconds = segment.StartSeconds,
                    EndSeconds = segment.EndSeconds,
                    OriginalText = segment.Text,
                    TranslatedText = segment.Text,
                    ConfidenceScore = 0.0,
                    IsProfanity = false
                });
            }
        }

        return translatedSegments;
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        _logger.LogWarning("[AZURE TRANSLATOR] Text truncated from {Original} to {MaxLength} characters", text.Length, maxLength);
        return text.Substring(0, maxLength);
    }

    private bool DetectProfanityMarker(string? text)
    {
        // Azure returns profanity masked as *** when profanityAction=Marked
        return !string.IsNullOrEmpty(text) && text.Contains("***");
    }

    private double CalculateCost(int totalCharacters)
    {
        // Azure Translator pricing: $10 per 1M characters (standard tier)
        return (totalCharacters / 1_000_000.0) * CostPerMillionCharacters;
    }

    private async Task<AzureTranslationResult?> GetCachedTranslationAsync(
        Guid lessonId,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        try
        {
            var collection = _mongoDatabase.GetCollection<BsonDocument>(AzureTranslationsCollection);
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("lessonId", lessonId.ToString()),
                Builders<BsonDocument>.Filter.Eq("targetLanguage", targetLanguage)
            );

            var document = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            if (document == null)
                return null;

            // Deserialize cached result
            var result = new AzureTranslationResult
            {
                LessonId = Guid.Parse(document["lessonId"].AsString),
                SourceLanguage = document["sourceLanguage"].AsString,
                TargetLanguage = document["targetLanguage"].AsString,
                TranslatedAt = document["translatedAt"].ToUniversalTime(),
                TotalCharacters = document["totalCharacters"].AsInt32,
                EstimatedCost = document["estimatedCost"].AsDouble,
                TranslatorVersion = document.GetValue("translatorVersion", "Azure Translator v3.0").AsString,
                Segments = document["segments"].AsBsonArray.Select(seg =>
                {
                    var segDoc = seg.AsBsonDocument;
                    return new AzureTranslatedSegment
                    {
                        Index = segDoc["index"].AsInt32,
                        StartSeconds = segDoc["startSeconds"].AsDouble,
                        EndSeconds = segDoc["endSeconds"].AsDouble,
                        OriginalText = segDoc["originalText"].AsString,
                        TranslatedText = segDoc["translatedText"].AsString,
                        ConfidenceScore = segDoc.GetValue("confidenceScore", 0.95).AsDouble,
                        IsProfanity = segDoc.GetValue("isProfanity", false).AsBoolean
                    };
                }).ToList()
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE TRANSLATOR] Error retrieving cached translation from MongoDB");
            return null;
        }
    }

    private async Task CacheTranslationAsync(AzureTranslationResult result, CancellationToken cancellationToken)
    {
        try
        {
            var collection = _mongoDatabase.GetCollection<BsonDocument>(AzureTranslationsCollection);

            var document = new BsonDocument
            {
                { "lessonId", result.LessonId.ToString() },
                { "sourceLanguage", result.SourceLanguage },
                { "targetLanguage", result.TargetLanguage },
                { "translatedAt", result.TranslatedAt },
                { "totalCharacters", result.TotalCharacters },
                { "estimatedCost", result.EstimatedCost },
                { "translatorVersion", result.TranslatorVersion },
                { "segments", new BsonArray(result.Segments.Select(seg => new BsonDocument
                    {
                        { "index", seg.Index },
                        { "startSeconds", seg.StartSeconds },
                        { "endSeconds", seg.EndSeconds },
                        { "originalText", seg.OriginalText },
                        { "translatedText", seg.TranslatedText },
                        { "confidenceScore", seg.ConfidenceScore },
                        { "isProfanity", seg.IsProfanity }
                    }))
                }
            };

            // Upsert: replace if exists, insert if not
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("lessonId", result.LessonId.ToString()),
                Builders<BsonDocument>.Filter.Eq("targetLanguage", result.TargetLanguage)
            );

            await collection.ReplaceOneAsync(
                filter,
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);

            _logger.LogInformation(
                "[AZURE TRANSLATOR] Cached translation for lesson {LessonId} in {TargetLanguage}",
                result.LessonId, result.TargetLanguage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AZURE TRANSLATOR] Error caching translation in MongoDB");
            // Don't throw - caching failure shouldn't break translation
        }
    }

    private Dictionary<string, AzureLanguageInfo> GetFallbackLanguages()
    {
        // Fallback list of supported languages (top 20 most common)
        return new Dictionary<string, AzureLanguageInfo>
        {
            { "en", new AzureLanguageInfo { Code = "en", Name = "English", NativeName = "English", Direction = "ltr" } },
            { "it", new AzureLanguageInfo { Code = "it", Name = "Italian", NativeName = "Italiano", Direction = "ltr" } },
            { "es", new AzureLanguageInfo { Code = "es", Name = "Spanish", NativeName = "Español", Direction = "ltr" } },
            { "fr", new AzureLanguageInfo { Code = "fr", Name = "French", NativeName = "Français", Direction = "ltr" } },
            { "de", new AzureLanguageInfo { Code = "de", Name = "German", NativeName = "Deutsch", Direction = "ltr" } },
            { "pt", new AzureLanguageInfo { Code = "pt", Name = "Portuguese", NativeName = "Português", Direction = "ltr" } },
            { "ru", new AzureLanguageInfo { Code = "ru", Name = "Russian", NativeName = "Русский", Direction = "ltr" } },
            { "zh-Hans", new AzureLanguageInfo { Code = "zh-Hans", Name = "Chinese Simplified", NativeName = "中文(简体)", Direction = "ltr" } },
            { "ja", new AzureLanguageInfo { Code = "ja", Name = "Japanese", NativeName = "日本語", Direction = "ltr" } },
            { "ko", new AzureLanguageInfo { Code = "ko", Name = "Korean", NativeName = "한국어", Direction = "ltr" } },
            { "ar", new AzureLanguageInfo { Code = "ar", Name = "Arabic", NativeName = "العربية", Direction = "rtl" } },
            { "hi", new AzureLanguageInfo { Code = "hi", Name = "Hindi", NativeName = "हिन्दी", Direction = "ltr" } },
            { "nl", new AzureLanguageInfo { Code = "nl", Name = "Dutch", NativeName = "Nederlands", Direction = "ltr" } },
            { "pl", new AzureLanguageInfo { Code = "pl", Name = "Polish", NativeName = "Polski", Direction = "ltr" } },
            { "tr", new AzureLanguageInfo { Code = "tr", Name = "Turkish", NativeName = "Türkçe", Direction = "ltr" } },
            { "vi", new AzureLanguageInfo { Code = "vi", Name = "Vietnamese", NativeName = "Tiếng Việt", Direction = "ltr" } },
            { "th", new AzureLanguageInfo { Code = "th", Name = "Thai", NativeName = "ไทย", Direction = "ltr" } },
            { "sv", new AzureLanguageInfo { Code = "sv", Name = "Swedish", NativeName = "Svenska", Direction = "ltr" } },
            { "no", new AzureLanguageInfo { Code = "no", Name = "Norwegian", NativeName = "Norsk", Direction = "ltr" } },
            { "da", new AzureLanguageInfo { Code = "da", Name = "Danish", NativeName = "Dansk", Direction = "ltr" } }
        };
    }

    #endregion

    #region Azure API Response Models

    private class AzureLanguagesResponse
    {
        public Dictionary<string, AzureLanguageMetadata>? Translation { get; set; }
    }

    private class AzureLanguageMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string NativeName { get; set; } = string.Empty;
        public string? Dir { get; set; }
    }

    private class AzureTranslationResponse
    {
        public List<AzureTranslationItem>? Translations { get; set; }
    }

    private class AzureTranslationItem
    {
        public string? Text { get; set; }
        public string? To { get; set; }
    }

    private class AzureDetectionResponse
    {
        public string Language { get; set; } = string.Empty;
        public double Score { get; set; }
    }

    #endregion
}
