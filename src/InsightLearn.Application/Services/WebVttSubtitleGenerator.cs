using System.Text;
using System.Text.RegularExpressions;
using InsightLearn.Core.DTOs.VideoTranscript;
using InsightLearn.Core.Interfaces;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service for generating WebVTT and SRT subtitle files from video transcripts.
/// Phase 7 Task 7.1 - WebVTT Subtitle Generation
/// </summary>
public class WebVttSubtitleGenerator : ISubtitleGenerator
{
    private readonly ILogger<WebVttSubtitleGenerator> _logger;

    public WebVttSubtitleGenerator(ILogger<WebVttSubtitleGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Converts a video transcript to WebVTT format.
    /// Format: WEBVTT\n\nCUE_ID\nTIMESTAMP\nTEXT\n\n
    /// </summary>
    public string TranscriptToWebVTT(VideoTranscriptDto transcript)
    {
        if (transcript == null)
            throw new ArgumentNullException(nameof(transcript));

        if (transcript.Segments == null || !transcript.Segments.Any())
        {
            _logger.LogWarning("Transcript {LessonId} has no segments", transcript.LessonId);
            return "WEBVTT\n\n";
        }

        var sb = new StringBuilder();
        
        // WebVTT file header
        sb.AppendLine("WEBVTT");
        sb.AppendLine($"Kind: subtitles");
        sb.AppendLine($"Language: {transcript.Language ?? "en-US"}");
        sb.AppendLine();

        // Convert each transcript segment to a WebVTT cue
        int cueNumber = 1;
        foreach (var segment in transcript.Segments.OrderBy(s => s.StartTime))
        {
            // Cue identifier (optional but recommended)
            sb.AppendLine($"{cueNumber}");

            // Cue timings: 00:00:05.000 --> 00:00:10.000
            string startTime = FormatWebVTTTimestamp((decimal)segment.StartTime);
            string endTime = FormatWebVTTTimestamp((decimal)segment.EndTime);
            sb.AppendLine($"{startTime} --> {endTime}");

            // Cue text (with speaker label if available)
            string cueText = segment.Text?.Trim() ?? "";
            
            // Add speaker label if available (WebVTT voice tag format)
            if (!string.IsNullOrEmpty(segment.Speaker))
            {
                cueText = $"<v {segment.Speaker}>{cueText}</v>";
            }

            sb.AppendLine(cueText);
            sb.AppendLine(); // Blank line after each cue

            cueNumber++;
        }

        _logger.LogInformation("Generated WebVTT subtitle with {CueCount} cues for lesson {LessonId}", 
            cueNumber - 1, transcript.LessonId);

        return sb.ToString();
    }

    /// <summary>
    /// Converts a video transcript to SRT format (SubRip).
    /// Format: ID\nTIMESTAMP\nTEXT\n\n
    /// </summary>
    public string TranscriptToSRT(VideoTranscriptDto transcript)
    {
        if (transcript == null)
            throw new ArgumentNullException(nameof(transcript));

        if (transcript.Segments == null || !transcript.Segments.Any())
        {
            _logger.LogWarning("Transcript {LessonId} has no segments for SRT conversion", transcript.LessonId);
            return "";
        }

        var sb = new StringBuilder();

        // Convert each transcript segment to an SRT subtitle
        int subtitleNumber = 1;
        foreach (var segment in transcript.Segments.OrderBy(s => s.StartTime))
        {
            // Subtitle number
            sb.AppendLine($"{subtitleNumber}");

            // Timings: 00:00:05,000 --> 00:00:10,000
            string startTime = FormatSRTTimestamp((decimal)segment.StartTime);
            string endTime = FormatSRTTimestamp((decimal)segment.EndTime);
            sb.AppendLine($"{startTime} --> {endTime}");

            // Subtitle text (speaker label on separate line if available)
            if (!string.IsNullOrEmpty(segment.Speaker))
            {
                sb.AppendLine($"[{segment.Speaker}]");
            }

            sb.AppendLine(segment.Text?.Trim() ?? "");
            sb.AppendLine(); // Blank line after each subtitle

            subtitleNumber++;
        }

        _logger.LogInformation("Generated SRT subtitle with {SubtitleCount} subtitles for lesson {LessonId}", 
            subtitleNumber - 1, transcript.LessonId);

        return sb.ToString();
    }

    /// <summary>
    /// Validates WebVTT format syntax.
    /// Checks for:
    /// - WEBVTT header
    /// - Valid timestamp format (HH:MM:SS.mmm)
    /// - Proper cue structure
    /// </summary>
    public bool ValidateWebVTT(string vttContent)
    {
        if (string.IsNullOrWhiteSpace(vttContent))
            return false;

        // Must start with "WEBVTT"
        if (!vttContent.TrimStart().StartsWith("WEBVTT", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("WebVTT validation failed: Missing WEBVTT header");
            return false;
        }

        // Check for at least one valid timestamp pattern
        // Format: 00:00:05.000 --> 00:00:10.000
        var timestampPattern = @"\d{2}:\d{2}:\d{2}\.\d{3}\s*-->\s*\d{2}:\d{2}:\d{2}\.\d{3}";
        var regex = new Regex(timestampPattern, RegexOptions.Multiline);

        if (!regex.IsMatch(vttContent))
        {
            _logger.LogWarning("WebVTT validation failed: No valid timestamps found");
            return false;
        }

        _logger.LogDebug("WebVTT content validated successfully");
        return true;
    }

    /// <summary>
    /// Gets the MIME type for a subtitle format.
    /// </summary>
    public string GetMimeType(string format)
    {
        return format?.ToLowerInvariant() switch
        {
            "vtt" => "text/vtt",
            "webvtt" => "text/vtt",
            "srt" => "text/srt",
            "subrip" => "text/srt",
            _ => "text/plain"
        };
    }

    /// <summary>
    /// Formats seconds to WebVTT timestamp format: HH:MM:SS.mmm
    /// Example: 65.5 seconds = 00:01:05.500
    /// </summary>
    private string FormatWebVTTTimestamp(decimal seconds)
    {
        var timeSpan = TimeSpan.FromSeconds((double)seconds);
        
        int hours = (int)timeSpan.TotalHours;
        int minutes = timeSpan.Minutes;
        int secs = timeSpan.Seconds;
        int milliseconds = timeSpan.Milliseconds;

        return $"{hours:D2}:{minutes:D2}:{secs:D2}.{milliseconds:D3}";
    }

    /// <summary>
    /// Formats seconds to SRT timestamp format: HH:MM:SS,mmm
    /// Example: 65.5 seconds = 00:01:05,500
    /// Note: SRT uses comma instead of period for milliseconds
    /// </summary>
    private string FormatSRTTimestamp(decimal seconds)
    {
        var timeSpan = TimeSpan.FromSeconds((double)seconds);
        
        int hours = (int)timeSpan.TotalHours;
        int minutes = timeSpan.Minutes;
        int secs = timeSpan.Seconds;
        int milliseconds = timeSpan.Milliseconds;

        return $"{hours:D2}:{minutes:D2}:{secs:D2},{milliseconds:D3}";
    }
}
