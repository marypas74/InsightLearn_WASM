using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service for automatic speech recognition (ASR) using Whisper.net
/// Transcribes video/audio files to text with timestamps
/// </summary>
public interface IWhisperTranscriptionService
{
    /// <summary>
    /// Transcribe audio stream to text with timestamps
    /// </summary>
    /// <param name="audioStream">Audio stream (WAV 16kHz mono preferred)</param>
    /// <param name="language">Language code (e.g., "en", "it", "es")</param>
    /// <param name="lessonId">Lesson ID for tracking</param>
    /// <param name="cancellationToken">Cancellation token (automatically times out after 30 minutes)</param>
    /// <returns>Transcription result with segments and metadata</returns>
    Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string language, Guid lessonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract audio from video file and transcribe with 30-minute timeout
    /// </summary>
    /// <param name="videoStream">Video file stream</param>
    /// <param name="language">Language code</param>
    /// <param name="lessonId">Lesson ID for tracking</param>
    /// <param name="cancellationToken">Cancellation token (automatically times out after 30 minutes)</param>
    /// <returns>Transcription result</returns>
    Task<TranscriptionResult> TranscribeVideoAsync(Stream videoStream, string language, Guid lessonId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get transcription status for a lesson
    /// </summary>
    Task<TranscriptionStatus> GetStatusAsync(Guid lessonId);

    /// <summary>
    /// Generate WebVTT subtitle file from transcription
    /// </summary>
    Task<string> GenerateWebVTTAsync(Guid lessonId);
}

/// <summary>
/// Result of transcription operation
/// </summary>
public class TranscriptionResult
{
    public Guid LessonId { get; set; }
    public string Language { get; set; } = string.Empty;
    public List<TranscriptionSegment> Segments { get; set; } = new();
    public double DurationSeconds { get; set; }
    public DateTime TranscribedAt { get; set; }
    public string ModelUsed { get; set; } = "whisper-base";
}

/// <summary>
/// Single segment of transcription with timestamp
/// </summary>
public class TranscriptionSegment
{
    public int Index { get; set; }
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
    public string Text { get; set; } = string.Empty;
    public float Confidence { get; set; }
}

/// <summary>
/// Transcription processing status
/// </summary>
public class TranscriptionStatus
{
    public Guid LessonId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
    public int Progress { get; set; } // 0-100
    public string? ErrorMessage { get; set; }
    public DateTime? CompletedAt { get; set; }
}
