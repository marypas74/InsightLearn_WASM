using System.Text;
using System.Text.RegularExpressions;
using InsightLearn.Application.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace InsightLearn.Application.Services;

/// <summary>
/// Subtitle translation service using Ollama LLM with MongoDB caching
/// Implements context-aware batch translation for WebVTT files
/// </summary>
public class SubtitleTranslationService : ISubtitleTranslationService
{
    private readonly IOllamaService _ollamaService;
    private readonly InsightLearnDbContext _dbContext;
    private readonly IMongoDatabase _mongoDatabase;
    private readonly ILogger<SubtitleTranslationService> _logger;
    private readonly HttpClient _httpClient;

    // MongoDB collection for caching translated subtitles
    private const string TranslatedSubtitlesCollection = "TranslatedSubtitles";

    // Context window: number of previous cues to send for context-aware translation
    private const int ContextWindowSize = 3;

    // Supported languages (ISO 639-1 code -> native name)
    private static readonly Dictionary<string, string> SupportedLanguages = new()
    {
        { "it", "Italiano" },
        { "en", "English" },
        { "es", "Español" },
        { "fr", "Français" },
        { "de", "Deutsch" },
        { "pt", "Português" },
        { "ru", "Русский" },
        { "zh", "中文" },
        { "ja", "日本語" },
        { "ko", "한국어" },
        { "ar", "العربية" },
        { "hi", "हिन्दी" },
        { "nl", "Nederlands" },
        { "pl", "Polski" },
        { "tr", "Türkçe" },
        { "vi", "Tiếng Việt" },
        { "th", "ไทย" },
        { "sv", "Svenska" },
        { "no", "Norsk" },
        { "da", "Dansk" }
    };

    public SubtitleTranslationService(
        IOllamaService ollamaService,
        InsightLearnDbContext dbContext,
        IMongoDatabase mongoDatabase,
        ILogger<SubtitleTranslationService> logger,
        HttpClient httpClient)
    {
        _ollamaService = ollamaService ?? throw new ArgumentNullException(nameof(ollamaService));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mongoDatabase = mongoDatabase ?? throw new ArgumentNullException(nameof(mongoDatabase));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public Dictionary<string, string> GetSupportedLanguages() => new(SupportedLanguages);

    public async Task<string?> GetOrCreateTranslatedSubtitlesAsync(
        Guid lessonId,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        if (!SupportedLanguages.ContainsKey(targetLanguage))
        {
            _logger.LogWarning("Unsupported target language: {TargetLanguage}", targetLanguage);
            return null;
        }

        // Check MongoDB cache first
        var cached = await GetCachedTranslationAsync(lessonId, targetLanguage, cancellationToken);
        if (cached != null)
        {
            _logger.LogInformation("Found cached translation for lesson {LessonId} in {TargetLanguage}",
                lessonId, targetLanguage);
            return cached;
        }

        // Get source subtitle track from database
        var sourceTrack = await _dbContext.SubtitleTracks
            .Where(st => st.LessonId == lessonId && st.IsActive)
            .OrderByDescending(st => st.IsDefault)
            .ThenBy(st => st.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (sourceTrack == null)
        {
            _logger.LogWarning("No source subtitle track found for lesson {LessonId}", lessonId);
            return null;
        }

        _logger.LogInformation("Translating subtitles for lesson {LessonId} from {SourceLang} to {TargetLang}",
            lessonId, sourceTrack.Language, targetLanguage);

        // Translate subtitles
        var translatedVtt = await TranslateSubtitlesAsync(
            sourceTrack.FileUrl,
            sourceTrack.Language,
            targetLanguage,
            cancellationToken);

        // Cache the translation in MongoDB
        await CacheTranslationAsync(lessonId, targetLanguage, translatedVtt, cancellationToken);

        return translatedVtt;
    }

    public async Task<string> TranslateSubtitlesAsync(
        string sourceVttUrl,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceVttUrl))
            throw new ArgumentException("Source VTT URL cannot be empty", nameof(sourceVttUrl));

        if (!SupportedLanguages.ContainsKey(targetLanguage))
            throw new ArgumentException($"Unsupported target language: {targetLanguage}", nameof(targetLanguage));

        // Download source WebVTT file
        var sourceVtt = await DownloadVttFileAsync(sourceVttUrl, cancellationToken);

        // Parse WebVTT cues
        var cues = ParseWebVttCues(sourceVtt);

        _logger.LogInformation("Parsed {CueCount} cues from WebVTT file", cues.Count);

        // Translate cues with context awareness
        var translatedCues = await TranslateCuesWithContextAsync(
            cues,
            sourceLanguage,
            targetLanguage,
            cancellationToken);

        // Rebuild WebVTT file with translated cues
        var translatedVtt = BuildWebVttFile(translatedCues);

        return translatedVtt;
    }

    public async Task<bool> TranslationExistsAsync(
        Guid lessonId,
        string targetLanguage,
        CancellationToken cancellationToken = default)
    {
        var collection = _mongoDatabase.GetCollection<BsonDocument>(TranslatedSubtitlesCollection);
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("lessonId", lessonId.ToString()),
            Builders<BsonDocument>.Filter.Eq("targetLanguage", targetLanguage)
        );

        var count = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }

    public async Task DeleteTranslationsForLessonAsync(
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        var collection = _mongoDatabase.GetCollection<BsonDocument>(TranslatedSubtitlesCollection);
        var filter = Builders<BsonDocument>.Filter.Eq("lessonId", lessonId.ToString());

        var result = await collection.DeleteManyAsync(filter, cancellationToken);

        _logger.LogInformation("Deleted {Count} translations for lesson {LessonId}",
            result.DeletedCount, lessonId);
    }

    #region Private Helper Methods

    private async Task<string?> GetCachedTranslationAsync(
        Guid lessonId,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        try
        {
            var collection = _mongoDatabase.GetCollection<BsonDocument>(TranslatedSubtitlesCollection);
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("lessonId", lessonId.ToString()),
                Builders<BsonDocument>.Filter.Eq("targetLanguage", targetLanguage)
            );

            var document = await collection.Find(filter)
                .FirstOrDefaultAsync(cancellationToken);

            return document?["translatedVtt"]?.AsString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached translation from MongoDB");
            return null;
        }
    }

    private async Task CacheTranslationAsync(
        Guid lessonId,
        string targetLanguage,
        string translatedVtt,
        CancellationToken cancellationToken)
    {
        try
        {
            var collection = _mongoDatabase.GetCollection<BsonDocument>(TranslatedSubtitlesCollection);

            var document = new BsonDocument
            {
                { "lessonId", lessonId.ToString() },
                { "targetLanguage", targetLanguage },
                { "translatedVtt", translatedVtt },
                { "createdAt", DateTime.UtcNow },
                { "cueCount", CountCues(translatedVtt) },
                { "fileSize", Encoding.UTF8.GetByteCount(translatedVtt) }
            };

            // Upsert: replace if exists, insert if not
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("lessonId", lessonId.ToString()),
                Builders<BsonDocument>.Filter.Eq("targetLanguage", targetLanguage)
            );

            await collection.ReplaceOneAsync(
                filter,
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);

            _logger.LogInformation("Cached translation for lesson {LessonId} in {TargetLanguage}",
                lessonId, targetLanguage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching translation in MongoDB");
            // Don't throw - caching failure shouldn't break translation
        }
    }

    private async Task<string> DownloadVttFileAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            // If URL is a local path, read from file system
            if (!url.StartsWith("http://") && !url.StartsWith("https://"))
            {
                if (File.Exists(url))
                {
                    return await File.ReadAllTextAsync(url, cancellationToken);
                }
                throw new FileNotFoundException($"VTT file not found at path: {url}");
            }

            // Download from HTTP URL
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading VTT file from {Url}", url);
            throw new Exception($"Failed to download VTT file: {ex.Message}", ex);
        }
    }

    private List<SubtitleCue> ParseWebVttCues(string vttContent)
    {
        var cues = new List<SubtitleCue>();

        // WebVTT format: WEBVTT header, followed by cue blocks (timestamp --> timestamp \n text)
        var lines = vttContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Regex for timestamp line: 00:00:01.000 --> 00:00:04.500
        var timestampRegex = new Regex(@"(\d{2}:\d{2}:\d{2}\.\d{3})\s*-->\s*(\d{2}:\d{2}:\d{2}\.\d{3})");

        SubtitleCue? currentCue = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip header and empty lines
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("WEBVTT") || trimmed.StartsWith("NOTE"))
                continue;

            // Check if this is a timestamp line
            var match = timestampRegex.Match(trimmed);
            if (match.Success)
            {
                // Save previous cue if exists
                if (currentCue != null)
                {
                    cues.Add(currentCue);
                }

                // Start new cue
                currentCue = new SubtitleCue
                {
                    StartTime = match.Groups[1].Value,
                    EndTime = match.Groups[2].Value,
                    Text = string.Empty
                };
            }
            else if (currentCue != null)
            {
                // This is text content of the current cue
                if (!string.IsNullOrEmpty(currentCue.Text))
                    currentCue.Text += "\n";
                currentCue.Text += trimmed;
            }
        }

        // Add last cue
        if (currentCue != null)
        {
            cues.Add(currentCue);
        }

        return cues;
    }

    private async Task<List<SubtitleCue>> TranslateCuesWithContextAsync(
        List<SubtitleCue> cues,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var translatedCues = new List<SubtitleCue>();
        var sourceLangName = SupportedLanguages.GetValueOrDefault(sourceLanguage, sourceLanguage);
        var targetLangName = SupportedLanguages.GetValueOrDefault(targetLanguage, targetLanguage);

        for (int i = 0; i < cues.Count; i++)
        {
            var currentCue = cues[i];

            // Build context: previous 3 cues (if available)
            var context = new StringBuilder();
            var contextStart = Math.Max(0, i - ContextWindowSize);

            for (int j = contextStart; j < i; j++)
            {
                context.AppendLine($"[Previous]: {cues[j].Text}");
            }

            // Build translation prompt with context
            var prompt = BuildTranslationPrompt(
                currentCue.Text,
                context.ToString(),
                sourceLangName,
                targetLangName);

            try
            {
                // Call Ollama for translation
                var translatedText = await _ollamaService.GenerateResponseAsync(
                    prompt,
                    cancellationToken: cancellationToken);

                // Clean up the response (remove any extra explanations, keep only translated text)
                translatedText = CleanTranslationResponse(translatedText);

                translatedCues.Add(new SubtitleCue
                {
                    StartTime = currentCue.StartTime,
                    EndTime = currentCue.EndTime,
                    Text = translatedText
                });

                _logger.LogDebug("Translated cue {Index}/{Total}: '{Original}' -> '{Translated}'",
                    i + 1, cues.Count, currentCue.Text, translatedText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating cue {Index}/{Total}", i + 1, cues.Count);

                // Fallback: keep original text with error marker
                translatedCues.Add(new SubtitleCue
                {
                    StartTime = currentCue.StartTime,
                    EndTime = currentCue.EndTime,
                    Text = $"[Translation error] {currentCue.Text}"
                });
            }

            // Small delay to avoid overwhelming Ollama (rate limiting)
            if (i < cues.Count - 1)
            {
                await Task.Delay(100, cancellationToken);
            }
        }

        return translatedCues;
    }

    private string BuildTranslationPrompt(
        string textToTranslate,
        string context,
        string sourceLang,
        string targetLang)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine($"You are a professional subtitle translator. Translate the following subtitle text from {sourceLang} to {targetLang}.");
        prompt.AppendLine();
        prompt.AppendLine("IMPORTANT RULES:");
        prompt.AppendLine("1. Translate ONLY the text, do not add explanations or notes");
        prompt.AppendLine("2. Preserve the meaning and tone");
        prompt.AppendLine("3. Keep the translation concise (subtitles must be readable quickly)");
        prompt.AppendLine("4. Use natural, conversational language");
        prompt.AppendLine("5. Consider the context from previous subtitles");
        prompt.AppendLine();

        if (!string.IsNullOrWhiteSpace(context))
        {
            prompt.AppendLine("CONTEXT (previous subtitles for continuity):");
            prompt.AppendLine(context);
            prompt.AppendLine();
        }

        prompt.AppendLine("TEXT TO TRANSLATE:");
        prompt.AppendLine(textToTranslate);
        prompt.AppendLine();
        prompt.AppendLine($"Provide ONLY the {targetLang} translation below:");

        return prompt.ToString();
    }

    private string CleanTranslationResponse(string response)
    {
        // Remove common AI response patterns
        var cleaned = response.Trim();

        // Remove markdown formatting if present
        cleaned = Regex.Replace(cleaned, @"^\*\*.*?\*\*:?\s*", "");
        cleaned = Regex.Replace(cleaned, @"^Translation:?\s*", "", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"^Result:?\s*", "", RegexOptions.IgnoreCase);

        // Remove quotes if the entire response is wrapped in quotes
        if (cleaned.StartsWith("\"") && cleaned.EndsWith("\""))
        {
            cleaned = cleaned.Substring(1, cleaned.Length - 2);
        }

        return cleaned.Trim();
    }

    private string BuildWebVttFile(List<SubtitleCue> cues)
    {
        var vtt = new StringBuilder();

        // WebVTT header
        vtt.AppendLine("WEBVTT");
        vtt.AppendLine();

        // Add each cue
        for (int i = 0; i < cues.Count; i++)
        {
            var cue = cues[i];

            // Cue number (optional but helpful)
            vtt.AppendLine((i + 1).ToString());

            // Timestamp
            vtt.AppendLine($"{cue.StartTime} --> {cue.EndTime}");

            // Text content
            vtt.AppendLine(cue.Text);

            // Empty line between cues
            vtt.AppendLine();
        }

        return vtt.ToString();
    }

    private int CountCues(string vttContent)
    {
        var timestampRegex = new Regex(@"\d{2}:\d{2}:\d{2}\.\d{3}\s*-->\s*\d{2}:\d{2}:\d{2}\.\d{3}");
        return timestampRegex.Matches(vttContent).Count;
    }

    #endregion

    #region Helper Classes

    private class SubtitleCue
    {
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    #endregion
}
