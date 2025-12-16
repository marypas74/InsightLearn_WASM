namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Interface for AI-powered transcription and translation services.
/// Communicates with Ollama backend on Kubernetes for real-time processing.
/// Part of SmartVideoPlayer component stack (v2.2.0-dev)
/// </summary>
public interface IAiTranscriptionService
{
    /// <summary>
    /// Get transcript data for a video, including synchronized segments.
    /// </summary>
    /// <param name="videoId">The video identifier (lessonId)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Transcript data with synchronized segments</returns>
    Task<TranscriptData> GetTranscriptAsync(string videoId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream translation in real-time using Ollama AI.
    /// </summary>
    /// <param name="text">Text to translate</param>
    /// <param name="targetLanguage">Target language code (e.g., "es", "fr", "it")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Async stream of translated text chunks</returns>
    IAsyncEnumerable<string> StreamTranslationAsync(
        string text,
        string targetLanguage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current segment based on video timestamp.
    /// </summary>
    /// <param name="transcriptData">Transcript data</param>
    /// <param name="currentTimeSeconds">Current video time in seconds</param>
    /// <returns>Active transcript segment or null</returns>
    TranscriptSegment? GetActiveSegment(TranscriptData transcriptData, double currentTimeSeconds);

    /// <summary>
    /// Check if the AI transcription service is available.
    /// </summary>
    /// <returns>True if service is available</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Get list of supported translation languages.
    /// </summary>
    /// <returns>List of supported language codes and names</returns>
    IEnumerable<TranslationLanguage> GetSupportedLanguages();
}

/// <summary>
/// Complete transcript data for a video.
/// </summary>
public record TranscriptData
{
    public string VideoId { get; init; } = string.Empty;
    public string Language { get; init; } = "en";
    public string LanguageLabel { get; init; } = "English";
    public double DurationSeconds { get; init; }
    public int TotalWords { get; init; }
    public double AverageConfidence { get; init; }
    public List<TranscriptSegment> Segments { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    public string ProcessingEngine { get; init; } = "Ollama/qwen2:0.5b";

    /// <summary>
    /// Get full transcript as concatenated text.
    /// </summary>
    public string FullText => string.Join(" ", Segments.Select(s => s.Text));
}

/// <summary>
/// Single transcript segment with timing information.
/// </summary>
public record TranscriptSegment
{
    public int Index { get; init; }
    public double StartTime { get; init; }
    public double EndTime { get; init; }
    public string Text { get; init; } = string.Empty;
    public string? Speaker { get; init; }
    public double Confidence { get; init; } = 0.95;

    /// <summary>
    /// Duration of this segment in seconds.
    /// </summary>
    public double Duration => EndTime - StartTime;

    /// <summary>
    /// Check if a given timestamp falls within this segment.
    /// </summary>
    public bool ContainsTime(double timeSeconds) =>
        timeSeconds >= StartTime && timeSeconds < EndTime;
}

/// <summary>
/// Supported translation language.
/// </summary>
public record TranslationLanguage
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string NativeName { get; init; } = string.Empty;
    public bool IsRtl { get; init; } = false;
}
