using static InsightLearn.Application.Services.MockOllamaService;

namespace InsightLearn.Application.Interfaces;

/// <summary>
/// Extended interface for Mock Ollama service with transcript and translation capabilities
/// Used for testing and development environments
/// </summary>
public interface IMockOllamaService : IOllamaService
{
    /// <summary>
    /// Gets dummy transcript data for testing video transcription features
    /// </summary>
    /// <param name="videoId">The video ID</param>
    /// <returns>List of transcript segments with timing information</returns>
    Task<List<TranscriptSegment>> GetDummyTranscriptAsync(string videoId);

    /// <summary>
    /// Translates transcript segments to target language (simulated)
    /// </summary>
    /// <param name="segments">Original transcript segments</param>
    /// <param name="targetLanguage">Target language code (e.g., "es", "fr", "de")</param>
    /// <returns>Translated transcript segments</returns>
    Task<List<TranscriptSegment>> TranslateDummyTranscriptAsync(
        List<TranscriptSegment> segments,
        string targetLanguage);

    /// <summary>
    /// Generates AI key takeaways from transcript (simulated)
    /// </summary>
    /// <param name="transcript">Full transcript text</param>
    /// <returns>List of key takeaways</returns>
    Task<List<KeyTakeaway>> GenerateDummyTakeawaysAsync(string transcript);
}
