using InsightLearn.Core.DTOs.VideoTranscript;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Interface for subtitle file generation from video transcripts.
/// Supports WebVTT and SRT formats.
/// Phase 7 Task 7.1 - WebVTT Subtitle Generation
/// </summary>
public interface ISubtitleGenerator
{
    /// <summary>
    /// Converts a video transcript to WebVTT format.
    /// </summary>
    /// <param name="transcript">The video transcript with segments</param>
    /// <returns>WebVTT formatted string content</returns>
    string TranscriptToWebVTT(VideoTranscriptDto transcript);

    /// <summary>
    /// Converts a video transcript to SRT format (future).
    /// </summary>
    /// <param name="transcript">The video transcript with segments</param>
    /// <returns>SRT formatted string content</returns>
    string TranscriptToSRT(VideoTranscriptDto transcript);

    /// <summary>
    /// Validates WebVTT format syntax.
    /// </summary>
    /// <param name="vttContent">The WebVTT content to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool ValidateWebVTT(string vttContent);

    /// <summary>
    /// Gets the MIME type for a subtitle format.
    /// </summary>
    /// <param name="format">Format name (vtt, srt)</param>
    /// <returns>MIME type string</returns>
    string GetMimeType(string format);
}
