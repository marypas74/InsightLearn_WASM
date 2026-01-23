using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FFMpegCore;

namespace InsightLearn.Application.Services;

/// <summary>
/// faster-whisper-server (CTranslate2) based automatic speech recognition service
/// Provides 4x speed improvement over Whisper.net
/// Uses OpenAI-compatible HTTP API
/// </summary>
public class WhisperTranscriptionService : IWhisperTranscriptionService
{
    private readonly ILogger<WhisperTranscriptionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly int _timeoutSeconds;
    private readonly string _model;

    public WhisperTranscriptionService(
        ILogger<WhisperTranscriptionService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("FasterWhisper");

        // Load configuration
        _baseUrl = configuration["Whisper:BaseUrl"] ?? "http://faster-whisper-service:8000";
        _timeoutSeconds = int.Parse(configuration["Whisper:Timeout"] ?? "12000"); // 200 minutes (must exceed 200-min video timeout)
        _model = configuration["Whisper:Model"] ?? "base";

        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

        // Configure FFMpegCore with explicit binary path for containerized environment
        try
        {
            FFMpegCore.GlobalFFOptions.Configure(new FFMpegCore.FFOptions
            {
                BinaryFolder = "/usr/bin",
                TemporaryFilesFolder = "/tmp"
            });
            _logger.LogInformation("[FasterWhisper] FFMpegCore configured with binary path: /usr/bin");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FasterWhisper] Failed to configure FFMpegCore explicitly, using defaults");
        }

        _logger.LogInformation("[FasterWhisper] Initialized with BaseUrl: {BaseUrl}, Model: {Model}, Timeout: {Timeout}s",
            _baseUrl, _model, _timeoutSeconds);
    }

    public async Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string language, Guid lessonId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[FasterWhisper] Starting transcription for lesson {LessonId}, language: {Language}",
            lessonId, language);

        var sw = Stopwatch.StartNew();

        try
        {
            // Read audio stream into memory for multipart upload
            // NOTE: Using MemoryStream to avoid file locking issues with temp files
            using var memoryStream = new MemoryStream();
            await audioStream.CopyToAsync(memoryStream, cancellationToken);
            var audioBytes = memoryStream.ToArray();

            var audioSizeKB = audioBytes.Length / 1024;
            _logger.LogInformation("[FasterWhisper] Audio stream read into memory ({AudioSizeKB}KB)", audioSizeKB);

            // Convert language from locale format (it-IT) to ISO 639-1 (it) for Whisper API
            // Whisper API expects 2-letter codes: en, it, es, fr, de, etc.
            var whisperLanguage = language.Contains("-") ? language.Split('-')[0].ToLowerInvariant() : language.ToLowerInvariant();
            _logger.LogInformation("[FasterWhisper] Converted language '{OriginalLanguage}' to Whisper format '{WhisperLanguage}'", language, whisperLanguage);

            // Call faster-whisper-server API (OpenAI-compatible endpoint)
            using var content = new MultipartFormDataContent();
            using var audioContent = new ByteArrayContent(audioBytes);
                audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
                content.Add(audioContent, "file", "audio.wav");
                content.Add(new StringContent(_model), "model");
                content.Add(new StringContent(whisperLanguage), "language");
                content.Add(new StringContent("verbose_json"), "response_format");
                content.Add(new StringContent("0.0"), "temperature");

                _logger.LogInformation("[FasterWhisper] Sending transcription request to {BaseUrl}/v1/audio/transcriptions", _baseUrl);
                var response = await _httpClient.PostAsync("/v1/audio/transcriptions", content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<FasterWhisperResponse>(jsonResponse);

                if (result == null || result.Segments == null)
                {
                    throw new InvalidOperationException("Invalid response from faster-whisper-server");
                }

                // Convert faster-whisper segments to our format
                var segments = result.Segments.Select((s, index) => new TranscriptionSegment
                {
                    Index = index,
                    StartSeconds = s.Start,
                    EndSeconds = s.End,
                    Text = s.Text.Trim(),
                    Confidence = (float)(s.AvgLogprob > -1.0 ? Math.Exp(s.AvgLogprob) : 0.8) // Convert log probability to confidence
                }).ToList();

                sw.Stop();
                var elapsedMs = sw.ElapsedMilliseconds;
                var segmentCount = segments.Count;
                var duration = result.Duration;
                _logger.LogInformation("[FasterWhisper] Transcription completed in {ElapsedMs}ms: {SegmentCount} segments, {Duration}s duration",
                    elapsedMs, segmentCount, duration);

            return new TranscriptionResult
            {
                LessonId = lessonId,
                Language = language, // Use original normalized language (xx-XX format), not Whisper API response (xx only)
                Segments = segments,
                DurationSeconds = result.Duration,
                TranscribedAt = DateTime.UtcNow,
                ModelUsed = "faster-whisper-" + _model
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[FasterWhisper] HTTP request failed for lesson {LessonId}: {Message}",
                lessonId, ex.Message);
            throw new Exception($"faster-whisper-server connection failed: {ex.Message}", ex);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("[FasterWhisper] Transcription cancelled by user for lesson {LessonId} after {ElapsedMs}ms",
                lessonId, sw.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FasterWhisper] Transcription failed for lesson {LessonId} after {ElapsedMs}ms",
                lessonId, sw.ElapsedMilliseconds);
            throw;
        }
    }

    public async Task<TranscriptionResult> TranscribeVideoAsync(Stream videoStream, string language, Guid lessonId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[FasterWhisper] Video transcription started for lesson {LessonId}, language: {Language}",
            lessonId, language);

        // Create linked cancellation token with 200-minute timeout (increased for long videos up to 180 min)
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMinutes(200));
        var timeoutToken = cts.Token;

        var totalSw = Stopwatch.StartNew();

        try
        {
            // Save video stream to temporary file (FFMpegCore requires file path)
            // Add unique suffix to prevent race conditions when Hangfire retries or concurrent jobs run
            var jobInstanceId = Guid.NewGuid().ToString("N").Substring(0, 8); // 8-char unique ID
            var tempVideoPath = Path.Combine(Path.GetTempPath(), $"{lessonId}_{jobInstanceId}_video.mp4");
            var tempAudioPath = Path.Combine(Path.GetTempPath(), $"{lessonId}_{jobInstanceId}_audio.wav");

            try
            {
                _logger.LogInformation("[FasterWhisper] Saving video stream to temp file: {Path}", tempVideoPath);
                var saveSw = Stopwatch.StartNew();

                // Save video stream to temp file
                using (var fileStream = File.Create(tempVideoPath))
                {
                    await videoStream.CopyToAsync(fileStream, timeoutToken);
                }

                saveSw.Stop();
                var videoSizeKB = new FileInfo(tempVideoPath).Length / 1024;
                _logger.LogInformation("[FasterWhisper] Video saved ({VideoSizeKB}KB) in {ElapsedMs}ms",
                    videoSizeKB, saveSw.ElapsedMilliseconds);

                // Extract audio using FFMpegCore
                // Convert to 16kHz mono WAV (optimal for Whisper)
                _logger.LogInformation("[FasterWhisper] Extracting audio with FFMpegCore (16kHz mono WAV, PCM 16-bit)...");
                _logger.LogInformation("[FasterWhisper] Input video: {VideoPath} ({VideoSizeKB}KB)", tempVideoPath, videoSizeKB);
                _logger.LogInformation("[FasterWhisper] Output audio will be: {AudioPath}", tempAudioPath);

                var ffmpegSw = Stopwatch.StartNew();

                try
                {
                    var ffmpegArgs = FFMpegArguments
                        .FromFileInput(tempVideoPath)
                        .OutputToFile(tempAudioPath, overwrite: true, options => options
                            .WithAudioCodec("pcm_s16le")  // PCM 16-bit signed little-endian
                            .WithAudioSamplingRate(16000) // 16kHz (Whisper optimal)
                            .WithCustomArgument("-ac 1")  // Mono
                            .WithCustomArgument("-vn"));  // No video

                    _logger.LogInformation("[FasterWhisper] FFMpegCore arguments configured, starting ProcessAsynchronously()...");

                    var success = await ffmpegArgs
                        .CancellableThrough(timeoutToken)
                        .ProcessAsynchronously();

                    ffmpegSw.Stop();

                    _logger.LogInformation("[FasterWhisper] ProcessAsynchronously() returned {Success} after {ElapsedMs}ms",
                        success, ffmpegSw.ElapsedMilliseconds);

                    // Verify output file was created
                    if (!File.Exists(tempAudioPath))
                    {
                        _logger.LogError("[FasterWhisper] CRITICAL: FFMpegCore reported success={Success} but audio file does not exist at {Path}",
                            success, tempAudioPath);
                        throw new FileNotFoundException($"FFMpegCore failed to create audio file at {tempAudioPath} (ProcessAsynchronously returned {success})");
                    }

                    var audioSizeKB = new FileInfo(tempAudioPath).Length / 1024;
                    _logger.LogInformation("[FasterWhisper] Audio extraction completed successfully in {ElapsedMs}ms ({AudioSizeKB}KB WAV file created)",
                        ffmpegSw.ElapsedMilliseconds, audioSizeKB);
                }
                catch (Exception ex) when (ex is not FileNotFoundException)
                {
                    ffmpegSw.Stop();
                    _logger.LogError(ex, "[FasterWhisper] FFMpegCore ProcessAsynchronously() threw exception after {ElapsedMs}ms. Exception type: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                        ffmpegSw.ElapsedMilliseconds, ex.GetType().Name, ex.Message, ex.StackTrace);
                    throw new InvalidOperationException($"FFmpeg audio extraction failed: {ex.Message}", ex);
                }

                // Wait for FFmpeg to fully release file handle with retry loop (fix race condition)
                // Simple delays (500ms, 2000ms) were insufficient - implementing exponential backoff
                Stream? audioStream = null;
                int maxRetries = 5;
                int retryDelayMs = 500;

                for (int retry = 0; retry <= maxRetries; retry++)
                {
                    try
                    {
                        if (retry > 0)
                        {
                            _logger.LogInformation("[FasterWhisper] Retry {Retry}/{MaxRetries}: Waiting {DelayMs}ms before attempting to open audio file...",
                                retry, maxRetries, retryDelayMs);
                            await Task.Delay(retryDelayMs, timeoutToken);
                            retryDelayMs *= 2; // Exponential backoff: 500ms, 1000ms, 2000ms, 4000ms, 8000ms
                        }
                        else
                        {
                            _logger.LogInformation("[FasterWhisper] Initial attempt: Waiting 500ms for FFmpeg to release file handle...");
                            await Task.Delay(500, timeoutToken);
                        }

                        _logger.LogInformation("[FasterWhisper] Opening audio file for transcription...");
                        audioStream = File.OpenRead(tempAudioPath);
                        _logger.LogInformation("[FasterWhisper] Audio file opened successfully, starting transcription...");
                        break; // Success - exit retry loop
                    }
                    catch (IOException ioEx) when (retry < maxRetries)
                    {
                        _logger.LogWarning(ioEx, "[FasterWhisper] IOException on attempt {Retry}/{MaxRetries}: File still locked by FFmpeg process",
                            retry + 1, maxRetries + 1);
                        // Continue to next retry
                    }
                }

                if (audioStream == null)
                {
                    throw new IOException($"Failed to open audio file after {maxRetries + 1} attempts. FFmpeg did not release file handle.");
                }

                using (audioStream)
                {
                    var result = await TranscribeAsync(audioStream, language, lessonId, timeoutToken);

                    totalSw.Stop();
                    _logger.LogInformation("[FasterWhisper] Video transcription COMPLETED for lesson {LessonId} - Total time: {TotalMs}ms ({SegmentCount} segments)",
                        lessonId, totalSw.ElapsedMilliseconds, result.Segments.Count);
                    return result;
                }
            }
            finally
            {
                // Clean up temp files
                if (File.Exists(tempVideoPath))
                {
                    File.Delete(tempVideoPath);
                    _logger.LogDebug("[FasterWhisper] Deleted temp video file: {Path}", tempVideoPath);
                }
                if (File.Exists(tempAudioPath))
                {
                    File.Delete(tempAudioPath);
                    _logger.LogDebug("[FasterWhisper] Deleted temp audio file: {Path}", tempAudioPath);
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred (not user cancellation)
            totalSw.Stop();
            _logger.LogError("[FasterWhisper] Video transcription TIMEOUT after {ElapsedMs}ms (200-minute limit exceeded) for lesson {LessonId}",
                totalSw.ElapsedMilliseconds, lessonId);
            throw new TimeoutException($"Video transcription exceeded 200-minute timeout (Lesson: {lessonId}, Elapsed: {totalSw.Elapsed})");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // User cancellation
            totalSw.Stop();
            _logger.LogWarning("[FasterWhisper] Video transcription CANCELLED by user after {ElapsedMs}ms for lesson {LessonId}",
                totalSw.ElapsedMilliseconds, lessonId);
            throw;
        }
        catch (Exception ex)
        {
            totalSw.Stop();
            _logger.LogError(ex, "[FasterWhisper] Video transcription FAILED after {ElapsedMs}ms for lesson {LessonId}: {ErrorMessage}",
                totalSw.ElapsedMilliseconds, lessonId, ex.Message);
            throw;
        }
    }

    public async Task<TranscriptionStatus> GetStatusAsync(Guid lessonId)
    {
        // TODO: Implement status tracking via database or cache
        // For now, return placeholder
        _logger.LogWarning("[FasterWhisper] GetStatusAsync not implemented - returning placeholder");

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
        _logger.LogWarning("[FasterWhisper] GenerateWebVTTAsync not implemented - returning placeholder");

        var vtt = new StringBuilder();
        vtt.AppendLine("WEBVTT");
        vtt.AppendLine();
        vtt.AppendLine("NOTE Generated by InsightLearn faster-whisper ASR");
        vtt.AppendLine();

        // Placeholder cue
        vtt.AppendLine("1");
        vtt.AppendLine("00:00:00.000 --> 00:00:05.000");
        vtt.AppendLine("Transcription not yet generated for this lesson.");

        return vtt.ToString();
    }

    /// <summary>
    /// Response format from faster-whisper-server (OpenAI-compatible)
    /// Maps camelCase JSON to PascalCase C# properties
    /// </summary>
    private class FasterWhisperResponse
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("segments")]
        public List<FasterWhisperSegment>? Segments { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("duration")]
        public double Duration { get; set; }
    }

    private class FasterWhisperSegment
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("start")]
        public double Start { get; set; }

        [JsonPropertyName("end")]
        public double End { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("avg_logprob")]
        public double AvgLogprob { get; set; }

        [JsonPropertyName("no_speech_prob")]
        public double NoSpeechProb { get; set; }
    }
}
