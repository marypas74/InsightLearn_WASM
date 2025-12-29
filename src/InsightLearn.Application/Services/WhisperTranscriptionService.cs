using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Whisper.net;
using Whisper.net.Ggml;
using FFMpegCore;
using FFMpegCore.Pipes;

namespace InsightLearn.Application.Services;

/// <summary>
/// Whisper.net-based automatic speech recognition service
/// Transcribes video/audio to text with timestamps
/// </summary>
public class WhisperTranscriptionService : IWhisperTranscriptionService
{
    private readonly ILogger<WhisperTranscriptionService> _logger;
    private readonly string _modelPath;
    private static readonly object _modelLock = new();
    private static string? _cachedModelPath;

    public WhisperTranscriptionService(ILogger<WhisperTranscriptionService> logger)
    {
        _logger = logger;
        _modelPath = GetOrDownloadModel();
    }

    /// <summary>
    /// Get or download Whisper GGML model (base model - 74MB)
    /// </summary>
    private string GetOrDownloadModel()
    {
        lock (_modelLock)
        {
            if (_cachedModelPath != null && File.Exists(_cachedModelPath))
            {
                return _cachedModelPath;
            }

            try
            {
                // Download base model (74MB) on first use
                _logger.LogInformation("[WhisperASR] Downloading Whisper base model (74MB)...");

                // Create cache directory in user home
                var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "whisper");
                Directory.CreateDirectory(cacheDir);
                var modelPath = Path.Combine(cacheDir, "ggml-base.bin");

                // Download model stream and save to file
                using (var modelStream = WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.Base).GetAwaiter().GetResult())
                using (var fileStream = File.Create(modelPath))
                {
                    modelStream.CopyTo(fileStream);
                }

                _cachedModelPath = modelPath;
                _logger.LogInformation("[WhisperASR] Model downloaded to: {Path}", modelPath);
                return modelPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WhisperASR] Failed to download Whisper model");
                throw;
            }
        }
    }

    public async Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string language, Guid lessonId)
    {
        _logger.LogInformation("[WhisperASR] Starting transcription for lesson {LessonId}, language: {Language}",
            lessonId, language);

        var segments = new List<TranscriptionSegment>();
        double duration = 0;

        try
        {
            using var whisperFactory = WhisperFactory.FromPath(_modelPath);
            using var processor = whisperFactory.CreateBuilder()
                .WithLanguage(language)
                .WithPrompt("This is a video lecture about programming, technology, and education.")
                .Build();

            await foreach (var result in processor.ProcessAsync(audioStream))
            {
                var segment = new TranscriptionSegment
                {
                    Index = segments.Count,
                    StartSeconds = result.Start.TotalSeconds,
                    EndSeconds = result.End.TotalSeconds,
                    Text = result.Text.Trim(),
                    Confidence = result.Probability
                };

                segments.Add(segment);
                duration = Math.Max(duration, result.End.TotalSeconds);

                _logger.LogDebug("[WhisperASR] Segment {Index}: [{Start:F2}s - {End:F2}s] {Text}",
                    segment.Index, segment.StartSeconds, segment.EndSeconds, segment.Text);
            }

            _logger.LogInformation("[WhisperASR] Transcription completed: {SegmentCount} segments, {Duration:F2}s duration",
                segments.Count, duration);

            return new TranscriptionResult
            {
                LessonId = lessonId,
                Language = language,
                Segments = segments,
                DurationSeconds = duration,
                TranscribedAt = DateTime.UtcNow,
                ModelUsed = "whisper-base"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WhisperASR] Transcription failed for lesson {LessonId}", lessonId);
            throw;
        }
    }

    public async Task<TranscriptionResult> TranscribeVideoAsync(Stream videoStream, string language, Guid lessonId)
    {
        _logger.LogInformation("[WhisperASR] Extracting audio from video for lesson {LessonId}", lessonId);

        try
        {
            // Save video stream to temporary file (FFMpegCore requires file path)
            var tempVideoPath = Path.Combine(Path.GetTempPath(), $"{lessonId}_video.mp4");
            var tempAudioPath = Path.Combine(Path.GetTempPath(), $"{lessonId}_audio.wav");

            try
            {
                // Save video stream to temp file
                using (var fileStream = File.Create(tempVideoPath))
                {
                    await videoStream.CopyToAsync(fileStream);
                }

                _logger.LogInformation("[WhisperASR] Video saved to temp file: {Path}", tempVideoPath);

                // Extract audio using FFMpegCore
                // Convert to 16kHz mono WAV (optimal for Whisper)
                await FFMpegArguments
                    .FromFileInput(tempVideoPath)
                    .OutputToFile(tempAudioPath, overwrite: true, options => options
                        .WithAudioCodec("pcm_s16le")  // PCM 16-bit signed little-endian
                        .WithAudioSamplingRate(16000) // 16kHz (Whisper optimal)
                        .WithCustomArgument("-ac 1")  // Mono
                        .WithCustomArgument("-vn"))   // No video
                    .ProcessAsynchronously();

                _logger.LogInformation("[WhisperASR] Audio extracted to: {Path}", tempAudioPath);

                // Read extracted audio and transcribe
                using var audioStream = File.OpenRead(tempAudioPath);
                var result = await TranscribeAsync(audioStream, language, lessonId);

                _logger.LogInformation("[WhisperASR] Video transcription completed for lesson {LessonId}", lessonId);
                return result;
            }
            finally
            {
                // Clean up temp files
                if (File.Exists(tempVideoPath))
                    File.Delete(tempVideoPath);
                if (File.Exists(tempAudioPath))
                    File.Delete(tempAudioPath);

                _logger.LogDebug("[WhisperASR] Temp files cleaned up for lesson {LessonId}", lessonId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WhisperASR] Video transcription failed for lesson {LessonId}", lessonId);
            throw;
        }
    }

    public async Task<TranscriptionStatus> GetStatusAsync(Guid lessonId)
    {
        // TODO: Implement status tracking via database or cache
        // For now, return placeholder
        _logger.LogWarning("[WhisperASR] GetStatusAsync not implemented - returning placeholder");

        return new TranscriptionStatus
        {
            LessonId = lessonId,
            Status = "NotImplemented",
            Progress = 0,
            ErrorMessage = "Status tracking not implemented"
        };
    }

    public async Task<string> GenerateWebVTTAsync(Guid lessonId)
    {
        // TODO: Load transcription from database and generate WebVTT
        // For now, return placeholder
        _logger.LogWarning("[WhisperASR] GenerateWebVTTAsync not implemented - returning placeholder");

        var vtt = new StringBuilder();
        vtt.AppendLine("WEBVTT");
        vtt.AppendLine();
        vtt.AppendLine("NOTE Generated by InsightLearn Whisper ASR");
        vtt.AppendLine();

        // Placeholder cue
        vtt.AppendLine("1");
        vtt.AppendLine("00:00:00.000 --> 00:00:05.000");
        vtt.AppendLine("Transcription not yet generated for this lesson.");

        return vtt.ToString();
    }

    /// <summary>
    /// Format timestamp for WebVTT (HH:MM:SS.mmm)
    /// </summary>
    private string FormatWebVTTTimestamp(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }
}
