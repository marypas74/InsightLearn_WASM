using Hangfire;
using InsightLearn.Core.DTOs.Course;
using InsightLearn.Core.DTOs.VideoTranscript;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.Text;
using System.Text.Json;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service for automatic subtitle generation using Whisper ASR.
/// Converts video transcription to WebVTT format and stores in MongoDB GridFS.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public class SubtitleGenerationService : ISubtitleGenerationService
{
    private readonly IVideoTranscriptService _transcriptService;
    private readonly ISubtitleRepository _subtitleRepository;
    private readonly InsightLearnDbContext _context;
    private readonly IGridFSBucket _gridFsBucket;
    private readonly IGridFSBucket _videoBucket;
    private readonly IDistributedCache _cache;
    private readonly IBackgroundJobClient _jobClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubtitleGenerationService> _logger;

    private const string StatusKeyPrefix = "subtitle-gen-status:";
    private static readonly TimeSpan StatusCacheExpiry = TimeSpan.FromHours(24);

    /// <summary>
    /// Language code to human-readable label mapping.
    /// </summary>
    private static readonly Dictionary<string, string> LanguageLabels = new()
    {
        { "it", "Italiano" },
        { "it-IT", "Italiano" },
        { "en", "English" },
        { "en-US", "English" },
        { "en-GB", "English (UK)" },
        { "es", "Español" },
        { "es-ES", "Español" },
        { "fr", "Français" },
        { "fr-FR", "Français" },
        { "de", "Deutsch" },
        { "de-DE", "Deutsch" },
        { "pt", "Português" },
        { "pt-BR", "Português (Brasil)" },
        { "ru", "Русский" },
        { "zh", "中文" },
        { "ja", "日本語" },
        { "ko", "한국어" },
        { "ar", "العربية" },
        { "hi", "हिन्दी" }
    };

    public SubtitleGenerationService(
        IVideoTranscriptService transcriptService,
        ISubtitleRepository subtitleRepository,
        InsightLearnDbContext context,
        IDistributedCache cache,
        IBackgroundJobClient jobClient,
        IConfiguration configuration,
        ILogger<SubtitleGenerationService> logger)
    {
        _transcriptService = transcriptService;
        _subtitleRepository = subtitleRepository;
        _context = context;
        _cache = cache;
        _jobClient = jobClient;
        _configuration = configuration;
        _logger = logger;

        // Initialize MongoDB GridFS connections
        var connectionString = configuration["MongoDb:ConnectionString"]
            ?? configuration.GetConnectionString("MongoDB")
            ?? throw new ArgumentNullException("MongoDB connection string not configured");

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("insightlearn_videos");

        // Subtitles bucket
        var subtitleBucketOptions = new GridFSBucketOptions
        {
            BucketName = "subtitles",
            ChunkSizeBytes = 262144, // 256KB chunks
            WriteConcern = WriteConcern.WMajority
        };
        _gridFsBucket = new GridFSBucket(database, subtitleBucketOptions);

        // Videos bucket (for reading video URLs)
        var videoBucketOptions = new GridFSBucketOptions
        {
            BucketName = "videos", // Custom GridFS bucket for videos (same as MongoVideoStorageService)
            WriteConcern = WriteConcern.WMajority
        };
        _videoBucket = new GridFSBucket(database, videoBucketOptions);

        _logger.LogInformation("SubtitleGenerationService initialized with Whisper ASR integration");
    }

    public async Task<string> QueueSubtitleGenerationAsync(
        Guid lessonId,
        string videoFileId,
        string language = "it-IT",
        CancellationToken ct = default)
    {
        try
        {
            // Validate lesson exists
            var lesson = await _context.Lessons.FindAsync(new object[] { lessonId }, ct);
            if (lesson == null)
            {
                throw new InvalidOperationException($"Lesson with ID {lessonId} not found");
            }

            // Check if subtitles already exist for this language
            var shortLang = GetShortLanguageCode(language);
            if (await HasSubtitlesAsync(lessonId, shortLang, ct))
            {
                throw new InvalidOperationException($"Subtitles in language '{language}' already exist for this lesson");
            }

            // Initialize status in cache
            var status = new SubtitleGenerationStatusDto
            {
                LessonId = lessonId,
                Status = "Queued",
                Progress = 0,
                CurrentStep = "Waiting in queue...",
                StartedAt = DateTime.UtcNow,
                Language = language
            };

            // Queue Hangfire job
            var jobId = _jobClient.Enqueue<ISubtitleGenerationService>(
                service => service.GenerateSubtitlesAsync(lessonId, videoFileId, language, CancellationToken.None));

            status.JobId = jobId;

            // Save status to cache
            await SetStatusAsync(lessonId, status, ct);

            _logger.LogInformation(
                "Queued subtitle generation job {JobId} for lesson {LessonId}, language {Language}",
                jobId, lessonId, language);

            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing subtitle generation for lesson {LessonId}", lessonId);
            throw;
        }
    }

    public async Task<SubtitleTrackDto> GenerateSubtitlesAsync(
        Guid lessonId,
        string videoFileId,
        string language = "it-IT",
        CancellationToken ct = default)
    {
        var status = await GetGenerationStatusAsync(lessonId, ct)
            ?? new SubtitleGenerationStatusDto
            {
                LessonId = lessonId,
                Language = language,
                StartedAt = DateTime.UtcNow
            };

        try
        {
            // Update status: Processing
            status.Status = "Processing";
            status.Progress = 10;
            status.CurrentStep = "Preparing video for transcription...";
            await SetStatusAsync(lessonId, status, ct);

            // Get video URL for Whisper API
            var videoUrl = await GetVideoStreamUrlAsync(videoFileId, ct);
            if (string.IsNullOrEmpty(videoUrl))
            {
                throw new InvalidOperationException($"Could not get video URL for file ID {videoFileId}");
            }

            _logger.LogInformation("Starting transcription for lesson {LessonId}, video {VideoFileId}", lessonId, videoFileId);

            // Update status: Transcribing
            status.Progress = 20;
            status.CurrentStep = "Transcribing audio with Whisper ASR...";
            await SetStatusAsync(lessonId, status, ct);

            // Call Whisper API to get transcript segments
            var transcriptResult = await _transcriptService.GenerateTranscriptAsync(lessonId, videoUrl, language, ct);

            if (transcriptResult?.Segments == null || transcriptResult.Segments.Count == 0)
            {
                throw new InvalidOperationException("Whisper API returned no transcript segments");
            }

            _logger.LogInformation(
                "Transcription completed for lesson {LessonId}: {SegmentCount} segments",
                lessonId, transcriptResult.Segments.Count);

            // Update status: Converting
            status.Progress = 70;
            status.CurrentStep = "Converting to WebVTT format...";
            await SetStatusAsync(lessonId, status, ct);

            // Convert to WebVTT format
            var shortLang = GetShortLanguageCode(language);
            var webVttContent = ConvertToWebVTT(transcriptResult.Segments, shortLang);

            // Update status: Saving
            status.Progress = 85;
            status.CurrentStep = "Saving subtitle file to storage...";
            await SetStatusAsync(lessonId, status, ct);

            // Save WebVTT to MongoDB GridFS
            var fileId = await SaveWebVTTToGridFSAsync(lessonId, shortLang, webVttContent, ct);

            // Calculate duration from segments (last segment end time)
            var durationSeconds = transcriptResult.Segments.Count > 0
                ? (int)Math.Ceiling(transcriptResult.Segments.Max(s => s.EndTime))
                : transcriptResult.Metadata?.DurationSeconds ?? 0;

            // Create SubtitleTrack entity in SQL Server
            var label = LanguageLabels.GetValueOrDefault(language, language);
            var subtitleTrack = new SubtitleTrack
            {
                LessonId = lessonId,
                Language = shortLang,
                Label = $"{label} (Auto)",
                FileUrl = $"/api/subtitles/stream/{fileId}",
                Kind = "subtitles",
                IsDefault = true, // Auto-generated subtitles are default
                FileSize = Encoding.UTF8.GetByteCount(webVttContent),
                CueCount = transcriptResult.Segments.Count,
                DurationSeconds = durationSeconds,
                CreatedByUserId = null, // System generated
                IsAutoGenerated = true,
                GenerationSource = "whisper-asr"
            };

            var savedTrack = await _subtitleRepository.AddAsync(subtitleTrack);

            // Update status: Completed
            status.Status = "Completed";
            status.Progress = 100;
            status.CurrentStep = "Subtitle generation completed successfully";
            status.CompletedAt = DateTime.UtcNow;
            await SetStatusAsync(lessonId, status, ct);

            _logger.LogInformation(
                "Subtitle generation completed for lesson {LessonId}: {CueCount} cues, {Duration}s",
                lessonId, subtitleTrack.CueCount, subtitleTrack.DurationSeconds);

            return new SubtitleTrackDto
            {
                Id = savedTrack.Id,
                Url = savedTrack.FileUrl,
                Language = savedTrack.Language,
                Label = savedTrack.Label,
                Kind = savedTrack.Kind,
                IsDefault = savedTrack.IsDefault
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating subtitles for lesson {LessonId}", lessonId);

            // Update status: Failed
            status.Status = "Failed";
            status.Progress = 0;
            status.CurrentStep = "Generation failed";
            status.ErrorMessage = ex.Message;
            status.CompletedAt = DateTime.UtcNow;
            await SetStatusAsync(lessonId, status, ct);

            throw;
        }
    }

    public async Task<SubtitleGenerationStatusDto> GetGenerationStatusAsync(
        Guid lessonId,
        CancellationToken ct = default)
    {
        try
        {
            var key = $"{StatusKeyPrefix}{lessonId}";
            var cached = await _cache.GetStringAsync(key, ct);

            if (string.IsNullOrEmpty(cached))
            {
                return new SubtitleGenerationStatusDto
                {
                    LessonId = lessonId,
                    Status = "NotStarted",
                    Progress = 0,
                    CurrentStep = "No generation job found"
                };
            }

            return JsonSerializer.Deserialize<SubtitleGenerationStatusDto>(cached)
                ?? new SubtitleGenerationStatusDto { LessonId = lessonId, Status = "Unknown" };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting subtitle generation status for lesson {LessonId}", lessonId);
            return new SubtitleGenerationStatusDto
            {
                LessonId = lessonId,
                Status = "Unknown",
                ErrorMessage = ex.Message
            };
        }
    }

    public string ConvertToWebVTT(List<TranscriptSegmentDto> segments, string language)
    {
        var sb = new StringBuilder();

        // WebVTT header
        sb.AppendLine("WEBVTT");
        sb.AppendLine($"Language: {language}");
        sb.AppendLine();

        // Metadata comment
        sb.AppendLine("NOTE");
        sb.AppendLine($"Auto-generated by InsightLearn using Whisper ASR");
        sb.AppendLine($"Generated at: {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}");
        sb.AppendLine();

        // Convert each segment to a WebVTT cue
        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            // Cue identifier (optional but useful for debugging)
            sb.AppendLine($"{i + 1}");

            // Timing line: 00:00:00.000 --> 00:00:05.000
            var startTime = FormatWebVTTTimestamp(segment.StartTime);
            var endTime = FormatWebVTTTimestamp(segment.EndTime);
            sb.AppendLine($"{startTime} --> {endTime}");

            // Cue text (with basic cleanup)
            var text = CleanTranscriptText(segment.Text);
            sb.AppendLine(text);

            // Empty line between cues
            sb.AppendLine();
        }

        return sb.ToString();
    }

    public async Task<bool> HasSubtitlesAsync(Guid lessonId, string language, CancellationToken ct = default)
    {
        try
        {
            var existing = await _subtitleRepository.GetByLessonAndLanguageAsync(lessonId, language);
            return existing != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking subtitles for lesson {LessonId}, language {Language}", lessonId, language);
            return false;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Get video streaming URL for Whisper API.
    /// </summary>
    private async Task<string> GetVideoStreamUrlAsync(string videoFileId, CancellationToken ct)
    {
        try
        {
            // Try to get video metadata from GridFS
            if (ObjectId.TryParse(videoFileId, out var objectId))
            {
                var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", objectId);
                using var cursor = await _videoBucket.FindAsync(filter, cancellationToken: ct);
                var fileInfo = await cursor.FirstOrDefaultAsync(ct);

                if (fileInfo != null)
                {
                    // Return internal API URL for video streaming
                    var baseUrl = _configuration["Api:BaseUrl"] ?? "http://api-service:80";
                    return $"{baseUrl}/api/video/stream/{videoFileId}";
                }
            }

            // Fallback: try to get from Lesson.VideoUrl
            return null!;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting video URL for file {FileId}", videoFileId);
            return null!;
        }
    }

    /// <summary>
    /// Save WebVTT content to MongoDB GridFS.
    /// </summary>
    private async Task<string> SaveWebVTTToGridFSAsync(
        Guid lessonId,
        string language,
        string webVttContent,
        CancellationToken ct)
    {
        var metadata = new BsonDocument
        {
            { "lessonId", lessonId.ToString() },
            { "language", language },
            { "generatedAt", DateTime.UtcNow },
            { "isAutoGenerated", true },
            { "source", "whisper-asr" }
        };

        var fileName = $"{lessonId}_{language}_auto.vtt";
        var contentBytes = Encoding.UTF8.GetBytes(webVttContent);

        using var stream = new MemoryStream(contentBytes);
        var fileId = await _gridFsBucket.UploadFromStreamAsync(
            fileName,
            stream,
            new GridFSUploadOptions { Metadata = metadata },
            ct);

        _logger.LogDebug("Saved WebVTT file to GridFS: {FileName}, ID: {FileId}", fileName, fileId);

        return fileId.ToString();
    }

    /// <summary>
    /// Format seconds as WebVTT timestamp (HH:MM:SS.mmm).
    /// </summary>
    private string FormatWebVTTTimestamp(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
    }

    /// <summary>
    /// Clean transcript text for WebVTT display.
    /// </summary>
    private string CleanTranscriptText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Basic cleanup
        var cleaned = text.Trim();

        // Remove multiple spaces
        while (cleaned.Contains("  "))
            cleaned = cleaned.Replace("  ", " ");

        // Escape WebVTT special characters
        cleaned = cleaned
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

        return cleaned;
    }

    /// <summary>
    /// Get short language code (e.g., "it" from "it-IT").
    /// </summary>
    private string GetShortLanguageCode(string language)
    {
        if (string.IsNullOrEmpty(language))
            return "en";

        var parts = language.Split('-');
        return parts[0].ToLowerInvariant();
    }

    /// <summary>
    /// Save status to distributed cache.
    /// </summary>
    private async Task SetStatusAsync(Guid lessonId, SubtitleGenerationStatusDto status, CancellationToken ct)
    {
        try
        {
            var key = $"{StatusKeyPrefix}{lessonId}";
            var json = JsonSerializer.Serialize(status);
            await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = StatusCacheExpiry
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error caching subtitle generation status for lesson {LessonId}", lessonId);
        }
    }

    #endregion
}
