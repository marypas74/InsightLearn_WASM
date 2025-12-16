using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Real Ollama-backed transcription service that calls backend APIs.
/// Uses backend VideoTranscriptService for transcripts and Ollama for translations.
/// Part of SmartVideoPlayer component stack (v2.2.0-dev)
/// </summary>
public class OllamaTranscriptionService : IAiTranscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly IVideoTranscriptClientService _transcriptClient;
    private readonly ILogger<OllamaTranscriptionService> _logger;

    // Supported languages for AI translation (matches backend)
    private static readonly List<TranslationLanguage> _supportedLanguages = new()
    {
        new() { Code = "en", Name = "English", NativeName = "English", IsRtl = false },
        new() { Code = "es", Name = "Spanish", NativeName = "Español", IsRtl = false },
        new() { Code = "fr", Name = "French", NativeName = "Français", IsRtl = false },
        new() { Code = "de", Name = "German", NativeName = "Deutsch", IsRtl = false },
        new() { Code = "it", Name = "Italian", NativeName = "Italiano", IsRtl = false },
        new() { Code = "pt", Name = "Portuguese", NativeName = "Português", IsRtl = false },
        new() { Code = "zh", Name = "Chinese", NativeName = "中文", IsRtl = false },
        new() { Code = "ja", Name = "Japanese", NativeName = "日本語", IsRtl = false },
        new() { Code = "ko", Name = "Korean", NativeName = "한국어", IsRtl = false },
        new() { Code = "ar", Name = "Arabic", NativeName = "العربية", IsRtl = true },
        new() { Code = "hi", Name = "Hindi", NativeName = "हिन्दी", IsRtl = false },
        new() { Code = "ru", Name = "Russian", NativeName = "Русский", IsRtl = false },
        new() { Code = "nl", Name = "Dutch", NativeName = "Nederlands", IsRtl = false },
        new() { Code = "pl", Name = "Polish", NativeName = "Polski", IsRtl = false },
        new() { Code = "tr", Name = "Turkish", NativeName = "Türkçe", IsRtl = false },
        new() { Code = "vi", Name = "Vietnamese", NativeName = "Tiếng Việt", IsRtl = false },
        new() { Code = "th", Name = "Thai", NativeName = "ไทย", IsRtl = false },
        new() { Code = "sv", Name = "Swedish", NativeName = "Svenska", IsRtl = false },
        new() { Code = "no", Name = "Norwegian", NativeName = "Norsk", IsRtl = false },
        new() { Code = "da", Name = "Danish", NativeName = "Dansk", IsRtl = false }
    };

    public OllamaTranscriptionService(
        HttpClient httpClient,
        IVideoTranscriptClientService transcriptClient,
        ILogger<OllamaTranscriptionService> logger)
    {
        _httpClient = httpClient;
        _transcriptClient = transcriptClient;
        _logger = logger;
    }

    /// <summary>
    /// Get transcript from backend API.
    /// Falls back to empty transcript if backend unavailable.
    /// </summary>
    public async Task<TranscriptData> GetTranscriptAsync(string videoId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[OllamaTranscription] Getting transcript for video: {VideoId}", videoId);

            // Try to get transcript from backend API
            if (Guid.TryParse(videoId, out var lessonId))
            {
                var response = await _transcriptClient.GetTranscriptAsync(lessonId);

                // Access the Data property from ApiResponse<VideoTranscriptDto>
                if (response?.Data != null && response.Data.Segments?.Any() == true)
                {
                    _logger.LogInformation("[OllamaTranscription] Got transcript with {Count} segments from backend",
                        response.Data.Segments.Count());

                    return ConvertToTranscriptData(videoId, response.Data);
                }
            }

            _logger.LogWarning("[OllamaTranscription] No transcript found for {VideoId}, returning empty", videoId);
            return CreateEmptyTranscript(videoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OllamaTranscription] Error getting transcript for {VideoId}", videoId);
            return CreateEmptyTranscript(videoId);
        }
    }

    /// <summary>
    /// Stream translation from backend Ollama service.
    /// Uses backend chat endpoint for Ollama-powered translation.
    /// </summary>
    public async IAsyncEnumerable<string> StreamTranslationAsync(
        string text,
        string targetLanguage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[OllamaTranscription] Translating to {Language}: {Text}", targetLanguage, text.Substring(0, Math.Min(50, text.Length)));

        // Collect translation result first (cannot yield inside try-catch)
        string? translatedText = null;
        string? errorMessage = null;

        // Call backend chatbot endpoint with translation prompt
        var translationPrompt = $"Translate the following text to {GetLanguageName(targetLanguage)}. Only output the translation, nothing else:\n\n{text}";

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/chat/message",
                new { message = translationPrompt, sessionId = $"translate-{Guid.NewGuid()}" },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: cancellationToken);
                if (!string.IsNullOrEmpty(result?.Response))
                {
                    translatedText = result.Response;
                }
            }
            else
            {
                errorMessage = $"Translation API returned {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OllamaTranscription] Translation error");
            errorMessage = ex.Message;
        }

        // Now yield results outside try-catch
        if (!string.IsNullOrEmpty(translatedText))
        {
            // Stream word by word for UI effect
            var words = translatedText.Split(' ');
            foreach (var word in words)
            {
                if (cancellationToken.IsCancellationRequested) yield break;
                yield return word + " ";
                await Task.Delay(30, cancellationToken); // Small delay for streaming effect
            }
        }
        else
        {
            // Fallback: return original text with language prefix
            if (errorMessage != null)
            {
                _logger.LogWarning("[OllamaTranscription] Translation failed: {Error}, using fallback", errorMessage);
            }
            yield return $"[{targetLanguage.ToUpper()}] {text}";
        }
    }

    /// <summary>
    /// Get the currently active segment based on video timestamp.
    /// </summary>
    public TranscriptSegment? GetActiveSegment(TranscriptData transcriptData, double currentTimeSeconds)
    {
        if (transcriptData?.Segments == null || !transcriptData.Segments.Any())
            return null;

        return transcriptData.Segments.FirstOrDefault(s =>
            currentTimeSeconds >= s.StartTime && currentTimeSeconds <= s.EndTime);
    }

    /// <summary>
    /// Check if Ollama backend is available.
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/chat/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get supported translation languages.
    /// </summary>
    public IEnumerable<TranslationLanguage> GetSupportedLanguages() => _supportedLanguages;

    #region Private Helpers

    private TranscriptData ConvertToTranscriptData(string videoId, InsightLearn.Core.DTOs.VideoTranscript.VideoTranscriptDto dto)
    {
        var segments = dto.Segments?.Select((s, index) => new TranscriptSegment
        {
            Index = index,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Text = s.Text,
            Speaker = s.Speaker,
            Confidence = s.Confidence ?? 0.95
        }).ToList() ?? new List<TranscriptSegment>();

        var totalWords = segments.Sum(s => s.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
        var avgConfidence = segments.Any() ? segments.Average(s => s.Confidence) : 0.95;
        var duration = segments.Any() ? segments.Max(s => s.EndTime) : 0;

        return new TranscriptData
        {
            VideoId = videoId,
            Language = dto.Language ?? "en",
            LanguageLabel = "English",
            DurationSeconds = duration,
            TotalWords = totalWords,
            AverageConfidence = avgConfidence,
            Segments = segments,
            GeneratedAt = dto.Metadata?.ProcessedAt ?? DateTime.UtcNow,
            ProcessingEngine = dto.Metadata?.ProcessingEngine ?? "Ollama/qwen2:0.5b"
        };
    }

    private TranscriptData CreateEmptyTranscript(string videoId)
    {
        return new TranscriptData
        {
            VideoId = videoId,
            Language = "en",
            LanguageLabel = "English",
            DurationSeconds = 0,
            TotalWords = 0,
            AverageConfidence = 0,
            Segments = new List<TranscriptSegment>(),
            GeneratedAt = DateTime.UtcNow,
            ProcessingEngine = "None"
        };
    }

    private string GetLanguageName(string code)
    {
        return _supportedLanguages.FirstOrDefault(l => l.Code == code)?.Name ?? code;
    }

    #endregion

    // DTO for chat response
    private record ChatResponse(string Response, string? SessionId);
}
