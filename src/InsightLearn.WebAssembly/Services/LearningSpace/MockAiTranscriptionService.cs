using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Mock implementation of IAiTranscriptionService for development/testing.
/// Returns dummy synchronized transcript data without requiring K8s Ollama backend.
/// Part of SmartVideoPlayer component stack (v2.2.0-dev)
/// </summary>
public class MockAiTranscriptionService : IAiTranscriptionService
{
    private readonly ILogger<MockAiTranscriptionService> _logger;
    private readonly List<TranslationLanguage> _supportedLanguages;

    public MockAiTranscriptionService(ILogger<MockAiTranscriptionService> logger)
    {
        _logger = logger;
        _supportedLanguages = InitializeSupportedLanguages();
    }

    /// <summary>
    /// Get mock transcript data for a video.
    /// </summary>
    public async Task<TranscriptData> GetTranscriptAsync(string videoId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MockAiTranscription] Getting transcript for video: {VideoId}", videoId);

        // Simulate network delay
        await Task.Delay(500, cancellationToken);

        var segments = GenerateMockSegments();

        return new TranscriptData
        {
            VideoId = videoId,
            Language = "en",
            LanguageLabel = "English",
            DurationSeconds = segments.Last().EndTime,
            TotalWords = segments.Sum(s => s.Text.Split(' ').Length),
            AverageConfidence = 0.94,
            Segments = segments,
            GeneratedAt = DateTime.UtcNow,
            ProcessingEngine = "Mock/Development"
        };
    }

    /// <summary>
    /// Stream mock translation chunks.
    /// </summary>
    public async IAsyncEnumerable<string> StreamTranslationAsync(
        string text,
        string targetLanguage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[MockAiTranscription] Translating to {Language}: {Text}", targetLanguage, text.Substring(0, Math.Min(50, text.Length)));

        // Mock translation - just prefix with language code
        var mockTranslation = GetMockTranslation(text, targetLanguage);
        var words = mockTranslation.Split(' ');

        foreach (var word in words)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            await Task.Delay(50, cancellationToken); // Simulate streaming delay
            yield return word + " ";
        }
    }

    /// <summary>
    /// Get the active segment for a given timestamp.
    /// </summary>
    public TranscriptSegment? GetActiveSegment(TranscriptData transcriptData, double currentTimeSeconds)
    {
        return transcriptData.Segments.FirstOrDefault(s => s.ContainsTime(currentTimeSeconds));
    }

    /// <summary>
    /// Check if service is available (always true for mock).
    /// </summary>
    public Task<bool> IsAvailableAsync()
    {
        _logger.LogDebug("[MockAiTranscription] Service availability check - returning true (mock mode)");
        return Task.FromResult(true);
    }

    /// <summary>
    /// Get supported languages for translation.
    /// </summary>
    public IEnumerable<TranslationLanguage> GetSupportedLanguages()
    {
        return _supportedLanguages;
    }

    #region Private Methods

    private static List<TranslationLanguage> InitializeSupportedLanguages()
    {
        return new List<TranslationLanguage>
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
    }

    private static List<TranscriptSegment> GenerateMockSegments()
    {
        // Generate realistic educational video transcript segments
        return new List<TranscriptSegment>
        {
            new() { Index = 0, StartTime = 0.0, EndTime = 5.2, Text = "Welcome to this comprehensive course on modern web development.", Speaker = "Instructor", Confidence = 0.98 },
            new() { Index = 1, StartTime = 5.2, EndTime = 10.5, Text = "In this lesson, we'll explore the fundamentals of building scalable applications.", Speaker = "Instructor", Confidence = 0.96 },
            new() { Index = 2, StartTime = 10.5, EndTime = 16.8, Text = "Before we dive in, make sure you have your development environment set up correctly.", Speaker = "Instructor", Confidence = 0.95 },
            new() { Index = 3, StartTime = 16.8, EndTime = 23.1, Text = "The first concept we need to understand is the separation of concerns principle.", Speaker = "Instructor", Confidence = 0.97 },
            new() { Index = 4, StartTime = 23.1, EndTime = 30.4, Text = "This means organizing your code into distinct sections, each handling a specific responsibility.", Speaker = "Instructor", Confidence = 0.94 },
            new() { Index = 5, StartTime = 30.4, EndTime = 37.2, Text = "Let me show you a practical example of how this works in a real-world application.", Speaker = "Instructor", Confidence = 0.96 },
            new() { Index = 6, StartTime = 37.2, EndTime = 44.5, Text = "Here we have a typical project structure with models, views, and controllers.", Speaker = "Instructor", Confidence = 0.93 },
            new() { Index = 7, StartTime = 44.5, EndTime = 51.8, Text = "The model layer handles all data operations and business logic.", Speaker = "Instructor", Confidence = 0.95 },
            new() { Index = 8, StartTime = 51.8, EndTime = 58.3, Text = "Views are responsible for presenting information to the user in an intuitive way.", Speaker = "Instructor", Confidence = 0.94 },
            new() { Index = 9, StartTime = 58.3, EndTime = 65.7, Text = "Controllers act as intermediaries, processing user input and coordinating responses.", Speaker = "Instructor", Confidence = 0.96 },
            new() { Index = 10, StartTime = 65.7, EndTime = 72.4, Text = "Now let's implement our first feature using these principles.", Speaker = "Instructor", Confidence = 0.95 },
            new() { Index = 11, StartTime = 72.4, EndTime = 79.8, Text = "We'll start by creating a simple user registration system.", Speaker = "Instructor", Confidence = 0.97 },
            new() { Index = 12, StartTime = 79.8, EndTime = 86.2, Text = "First, define the user model with the required properties and validation rules.", Speaker = "Instructor", Confidence = 0.94 },
            new() { Index = 13, StartTime = 86.2, EndTime = 93.5, Text = "Pay attention to how we handle input validation at the model level.", Speaker = "Instructor", Confidence = 0.93 },
            new() { Index = 14, StartTime = 93.5, EndTime = 100.1, Text = "This approach ensures data integrity regardless of where the input comes from.", Speaker = "Instructor", Confidence = 0.95 },
            new() { Index = 15, StartTime = 100.1, EndTime = 107.8, Text = "Next, we'll create the corresponding view for user registration.", Speaker = "Instructor", Confidence = 0.96 },
            new() { Index = 16, StartTime = 107.8, EndTime = 114.3, Text = "The view should be clean, accessible, and provide clear feedback to users.", Speaker = "Instructor", Confidence = 0.94 },
            new() { Index = 17, StartTime = 114.3, EndTime = 121.6, Text = "Finally, the controller ties everything together and handles the form submission.", Speaker = "Instructor", Confidence = 0.97 },
            new() { Index = 18, StartTime = 121.6, EndTime = 128.9, Text = "Let's run through the complete flow one more time to solidify these concepts.", Speaker = "Instructor", Confidence = 0.95 },
            new() { Index = 19, StartTime = 128.9, EndTime = 135.5, Text = "In the next lesson, we'll add authentication and session management.", Speaker = "Instructor", Confidence = 0.96 },
            new() { Index = 20, StartTime = 135.5, EndTime = 142.0, Text = "Thank you for watching. Don't forget to practice these concepts on your own.", Speaker = "Instructor", Confidence = 0.98 }
        };
    }

    private static string GetMockTranslation(string text, string targetLanguage)
    {
        // Simple mock translation - in production, this would call Ollama
        return targetLanguage switch
        {
            "es" => $"[ES] {text}",
            "fr" => $"[FR] {text}",
            "de" => $"[DE] {text}",
            "it" => $"[IT] {text}",
            "pt" => $"[PT] {text}",
            "zh" => $"[ZH] {text}",
            "ja" => $"[JA] {text}",
            "ko" => $"[KO] {text}",
            "ar" => $"[AR] {text}",
            "hi" => $"[HI] {text}",
            "ru" => $"[RU] {text}",
            _ => text
        };
    }

    #endregion
}
