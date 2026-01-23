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
/// Chunked faster-whisper transcription service with real-time progress tracking.
/// v2.3.97-dev: Splits audio into 30-second chunks and reports progress after each chunk.
/// Fixes the 90% bottleneck issue by providing true chunk-by-chunk progress.
/// </summary>
public class ChunkedWhisperTranscriptionService : IWhisperTranscriptionService
{
    private readonly ILogger<ChunkedWhisperTranscriptionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ITranscriptJobStatusService _jobStatusService;
    private readonly string _baseUrl;
    private readonly int _timeoutSeconds;
    private readonly string _model;
    private readonly int _chunkDurationSeconds;

    public ChunkedWhisperTranscriptionService(
        ILogger<ChunkedWhisperTranscriptionService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ITranscriptJobStatusService jobStatusService)
    {
        _logger = logger;
        _jobStatusService = jobStatusService;
        _httpClient = httpClientFactory.CreateClient("FasterWhisper");

        _baseUrl = configuration["Whisper:BaseUrl"] ?? "http://faster-whisper-service:8000";
        _timeoutSeconds = int.Parse(configuration["Whisper:Timeout"] ?? "12000");
        _model = configuration["Whisper:Model"] ?? "base";
        _chunkDurationSeconds = int.Parse(configuration["Whisper:ChunkDurationSeconds"] ?? "30");

        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);

        try
        {
            FFMpegCore.GlobalFFOptions.Configure(new FFMpegCore.FFOptions
            {
                BinaryFolder = "/usr/bin",
                TemporaryFilesFolder = "/tmp"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ChunkedWhisper] Failed to configure FFMpegCore");
        }

        _logger.LogInformation("[ChunkedWhisper] Initialized: BaseUrl={BaseUrl}, ChunkDuration={ChunkDuration}s",
            _baseUrl, _chunkDurationSeconds);
    }

    public async Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string language, Guid lessonId, CancellationToken cancellationToken = default)
    {
        // Simple transcription (non-chunked) - delegate to video transcription
        _logger.LogInformation("[ChunkedWhisper] TranscribeAsync called, using chunked processing");

        using var memoryStream = new MemoryStream();
        await audioStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        return await TranscribeVideoAsync(memoryStream, language, lessonId, cancellationToken);
    }

    public async Task<TranscriptionResult> TranscribeVideoAsync(Stream videoStream, string language, Guid lessonId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[ChunkedWhisper] === STARTING CHUNKED TRANSCRIPTION for lesson {LessonId} ===", lessonId);

        var totalSw = Stopwatch.StartNew();
        var jobInstanceId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var tempVideoPath = Path.Combine(Path.GetTempPath(), $"{lessonId}_{jobInstanceId}_video.mp4");
        var tempAudioPath = Path.Combine(Path.GetTempPath(), $"{lessonId}_{jobInstanceId}_audio.wav");
        var chunkDir = Path.Combine(Path.GetTempPath(), $"{lessonId}_{jobInstanceId}_chunks");

        // Convert language format
        var whisperLanguage = language.Contains("-") ? language.Split('-')[0].ToLowerInvariant() : language.ToLowerInvariant();

        try
        {
            // PHASE 1: Save video to temp file (0-5%)
            _logger.LogInformation("[ChunkedWhisper] PHASE 1: Saving video stream to temp file");
            await _jobStatusService.UpdateChunkProgressAsync(lessonId, 0, 0, "Downloading", "Saving video to temporary storage...", cancellationToken);

            using (var fileStream = File.Create(tempVideoPath))
            {
                await videoStream.CopyToAsync(fileStream, cancellationToken);
            }
            var videoSizeKB = new FileInfo(tempVideoPath).Length / 1024;
            _logger.LogInformation("[ChunkedWhisper] Video saved: {SizeKB}KB", videoSizeKB);

            // PHASE 2: Extract audio (5-10%)
            _logger.LogInformation("[ChunkedWhisper] PHASE 2: Extracting audio with FFmpeg");
            await _jobStatusService.UpdateChunkProgressAsync(lessonId, 0, 0, "ExtractingAudio", "Extracting audio from video...", cancellationToken);

            await ExtractAudioAsync(tempVideoPath, tempAudioPath, cancellationToken);
            var audioSizeKB = new FileInfo(tempAudioPath).Length / 1024;

            // Get audio duration
            var mediaInfo = await FFMpegCore.FFProbe.AnalyseAsync(tempAudioPath, null, cancellationToken);
            var audioDuration = mediaInfo.Duration.TotalSeconds;

            _logger.LogInformation("[ChunkedWhisper] Audio extracted: {SizeKB}KB, {Duration}s duration", audioSizeKB, audioDuration);

            // PHASE 3: Split audio into chunks (10-15%)
            _logger.LogInformation("[ChunkedWhisper] PHASE 3: Splitting audio into {ChunkDuration}s chunks", _chunkDurationSeconds);

            Directory.CreateDirectory(chunkDir);
            var chunkPaths = await SplitAudioIntoChunksAsync(tempAudioPath, chunkDir, _chunkDurationSeconds, cancellationToken);
            var chunkCount = chunkPaths.Count;

            _logger.LogInformation("[ChunkedWhisper] Audio split into {ChunkCount} chunks", chunkCount);

            // Update job with chunk count
            await _jobStatusService.UpdateChunkProgressAsync(lessonId, 0, 1, "Transcribing",
                $"Starting transcription of {chunkCount} audio chunks...", cancellationToken);

            // PHASE 4: Transcribe each chunk (15-95%)
            _logger.LogInformation("[ChunkedWhisper] PHASE 4: Transcribing {ChunkCount} chunks", chunkCount);

            var allSegments = new List<TranscriptionSegment>();
            var totalDuration = 0.0;

            for (int i = 0; i < chunkPaths.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var chunkPath = chunkPaths[i];
                var chunkNumber = i + 1;
                var chunkStartTime = i * _chunkDurationSeconds;

                _logger.LogInformation("[ChunkedWhisper] Transcribing chunk {Current}/{Total} (starts at {StartTime}s)",
                    chunkNumber, chunkCount, chunkStartTime);

                await _jobStatusService.UpdateChunkProgressAsync(
                    lessonId,
                    i, // completed chunks
                    chunkNumber, // current chunk
                    "Transcribing",
                    $"Transcribing chunk {chunkNumber} of {chunkCount}...",
                    cancellationToken);

                try
                {
                    // Transcribe this chunk
                    var chunkResult = await TranscribeChunkAsync(chunkPath, whisperLanguage, cancellationToken);

                    // Adjust segment times to account for chunk offset
                    foreach (var segment in chunkResult.Segments)
                    {
                        allSegments.Add(new TranscriptionSegment
                        {
                            Index = allSegments.Count,
                            StartSeconds = segment.StartSeconds + chunkStartTime,
                            EndSeconds = segment.EndSeconds + chunkStartTime,
                            Text = segment.Text,
                            Confidence = segment.Confidence
                        });
                    }

                    totalDuration = Math.Max(totalDuration, chunkStartTime + chunkResult.DurationSeconds);

                    _logger.LogInformation("[ChunkedWhisper] Chunk {Current}/{Total} completed: {SegmentCount} segments",
                        chunkNumber, chunkCount, chunkResult.Segments.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ChunkedWhisper] Error transcribing chunk {ChunkNumber}", chunkNumber);
                    // Continue with other chunks - don't fail entire job for one chunk
                }
            }

            // PHASE 5: Merge and finalize (95-100%)
            _logger.LogInformation("[ChunkedWhisper] PHASE 5: Merging {SegmentCount} segments from all chunks", allSegments.Count);
            await _jobStatusService.UpdateChunkProgressAsync(lessonId, chunkCount, chunkCount, "Saving",
                "Merging transcription segments...", cancellationToken);

            // Sort segments by start time (in case of any overlap issues)
            allSegments = allSegments.OrderBy(s => s.StartSeconds).ToList();

            // Re-index segments
            for (int i = 0; i < allSegments.Count; i++)
            {
                allSegments[i].Index = i;
            }

            totalSw.Stop();

            _logger.LogInformation("[ChunkedWhisper] === TRANSCRIPTION COMPLETE for lesson {LessonId} ===", lessonId);
            _logger.LogInformation("[ChunkedWhisper] Total: {SegmentCount} segments, {Duration}s audio, {ProcessingTime}ms processing",
                allSegments.Count, totalDuration, totalSw.ElapsedMilliseconds);

            return new TranscriptionResult
            {
                LessonId = lessonId,
                Language = language,
                Segments = allSegments,
                DurationSeconds = totalDuration,
                TranscribedAt = DateTime.UtcNow,
                ModelUsed = $"chunked-faster-whisper-{_model}"
            };
        }
        finally
        {
            // Cleanup temp files
            SafeDelete(tempVideoPath);
            SafeDelete(tempAudioPath);
            if (Directory.Exists(chunkDir))
            {
                try { Directory.Delete(chunkDir, true); } catch { }
            }
        }
    }

    private async Task ExtractAudioAsync(string videoPath, string audioPath, CancellationToken ct)
    {
        var success = await FFMpegArguments
            .FromFileInput(videoPath)
            .OutputToFile(audioPath, overwrite: true, options => options
                .WithAudioCodec("pcm_s16le")
                .WithAudioSamplingRate(16000)
                .WithCustomArgument("-ac 1")
                .WithCustomArgument("-vn"))
            .CancellableThrough(ct)
            .ProcessAsynchronously();

        if (!success || !File.Exists(audioPath))
        {
            throw new InvalidOperationException("FFmpeg audio extraction failed");
        }
    }

    private async Task<List<string>> SplitAudioIntoChunksAsync(string audioPath, string outputDir, int chunkSeconds, CancellationToken ct)
    {
        var mediaInfo = await FFMpegCore.FFProbe.AnalyseAsync(audioPath, null, ct);
        var totalDuration = mediaInfo.Duration.TotalSeconds;
        var chunkCount = (int)Math.Ceiling(totalDuration / chunkSeconds);

        var chunkPaths = new List<string>();

        for (int i = 0; i < chunkCount; i++)
        {
            ct.ThrowIfCancellationRequested();

            var startTime = i * chunkSeconds;
            var chunkPath = Path.Combine(outputDir, $"chunk_{i:D4}.wav");

            // Use FFmpeg to extract chunk
            var args = $"-i \"{audioPath}\" -ss {startTime} -t {chunkSeconds} -c copy \"{chunkPath}\"";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/ffmpeg",
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(ct);

            if (File.Exists(chunkPath) && new FileInfo(chunkPath).Length > 0)
            {
                chunkPaths.Add(chunkPath);
            }
        }

        return chunkPaths;
    }

    private async Task<ChunkTranscriptionResult> TranscribeChunkAsync(string chunkPath, string language, CancellationToken ct)
    {
        var audioBytes = await File.ReadAllBytesAsync(chunkPath, ct);

        using var content = new MultipartFormDataContent();
        using var audioContent = new ByteArrayContent(audioBytes);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(audioContent, "file", "chunk.wav");
        content.Add(new StringContent(_model), "model");
        content.Add(new StringContent(language), "language");
        content.Add(new StringContent("verbose_json"), "response_format");
        content.Add(new StringContent("0.0"), "temperature");

        var response = await _httpClient.PostAsync("/v1/audio/transcriptions", content, ct);
        response.EnsureSuccessStatusCode();

        var jsonResponse = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<FasterWhisperResponse>(jsonResponse);

        if (result?.Segments == null)
        {
            return new ChunkTranscriptionResult { Segments = new List<TranscriptionSegment>(), DurationSeconds = 0 };
        }

        var segments = result.Segments.Select((s, index) => new TranscriptionSegment
        {
            Index = index,
            StartSeconds = s.Start,
            EndSeconds = s.End,
            Text = s.Text.Trim(),
            Confidence = (float)(s.AvgLogprob > -1.0 ? Math.Exp(s.AvgLogprob) : 0.8)
        }).ToList();

        return new ChunkTranscriptionResult
        {
            Segments = segments,
            DurationSeconds = result.Duration
        };
    }

    private void SafeDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    public async Task<TranscriptionStatus> GetStatusAsync(Guid lessonId)
    {
        var job = await _jobStatusService.GetJobStatusAsync(lessonId);
        if (job == null)
        {
            return new TranscriptionStatus
            {
                LessonId = lessonId,
                Status = "NotFound",
                Progress = 0
            };
        }

        return new TranscriptionStatus
        {
            LessonId = lessonId,
            Status = job.Status,
            Progress = job.ProgressPercentage,
            ErrorMessage = job.ErrorMessage
        };
    }

    public Task<string> GenerateWebVTTAsync(Guid lessonId)
    {
        var vtt = new StringBuilder();
        vtt.AppendLine("WEBVTT");
        vtt.AppendLine();
        vtt.AppendLine("NOTE Generated by InsightLearn chunked-faster-whisper ASR v2.3.97-dev");
        return Task.FromResult(vtt.ToString());
    }

    private class ChunkTranscriptionResult
    {
        public List<TranscriptionSegment> Segments { get; set; } = new();
        public double DurationSeconds { get; set; }
    }

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
