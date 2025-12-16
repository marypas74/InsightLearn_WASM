using InsightLearn.Core.DTOs.Course;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service for subtitle management with MongoDB GridFS storage for WebVTT files
/// </summary>
public class SubtitleService : ISubtitleService
{
    private readonly ISubtitleRepository _subtitleRepository;
    private readonly InsightLearnDbContext _context;
    private readonly IGridFSBucket _gridFsBucket;
    private readonly ILogger<SubtitleService> _logger;

    // Supported WebVTT MIME types
    private static readonly string[] AllowedContentTypes = new[]
    {
        "text/vtt",
        "text/plain",
        "application/octet-stream"
    };

    // Maximum subtitle file size: 5 MB
    private const long MaxFileSize = 5 * 1024 * 1024;

    public SubtitleService(
        ISubtitleRepository subtitleRepository,
        InsightLearnDbContext context,
        IConfiguration configuration,
        ILogger<SubtitleService> logger)
    {
        _subtitleRepository = subtitleRepository;
        _context = context;
        _logger = logger;

        // Initialize MongoDB GridFS for subtitle storage
        var connectionString = configuration["MongoDb:ConnectionString"]
            ?? configuration.GetConnectionString("MongoDB")
            ?? throw new ArgumentNullException("MongoDB connection string not configured");

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("insightlearn_videos");

        var gridFsOptions = new GridFSBucketOptions
        {
            BucketName = "subtitles",
            ChunkSizeBytes = 262144, // 256KB chunks (subtitles are smaller than videos)
            WriteConcern = WriteConcern.WMajority
        };

        _gridFsBucket = new GridFSBucket(database, gridFsOptions);

        _logger.LogInformation("SubtitleService initialized with MongoDB GridFS storage");
    }

    public async Task<List<SubtitleTrackDto>> GetSubtitlesByLessonIdAsync(Guid lessonId)
    {
        try
        {
            var subtitles = await _subtitleRepository.GetByLessonIdAsync(lessonId);

            return subtitles.Select(st => new SubtitleTrackDto
            {
                Url = st.FileUrl,
                Language = st.Language,
                Label = st.Label,
                Kind = st.Kind,
                IsDefault = st.IsDefault
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subtitles for lesson {LessonId}", lessonId);
            throw;
        }
    }

    public async Task<SubtitleTrackDto> UploadSubtitleAsync(
        Guid lessonId,
        string language,
        string label,
        IFormFile file,
        Guid userId,
        bool isDefault = false,
        string kind = "subtitles")
    {
        try
        {
            // Validate inputs
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("No file provided");
            }

            if (file.Length > MaxFileSize)
            {
                throw new ArgumentException($"File size exceeds maximum allowed size of {MaxFileSize / (1024 * 1024)} MB");
            }

            if (!AllowedContentTypes.Contains(file.ContentType.ToLower()))
            {
                throw new ArgumentException($"Invalid file type. Expected WebVTT file (text/vtt or text/plain), got {file.ContentType}");
            }

            // Validate that lesson exists
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null)
            {
                throw new InvalidOperationException($"Lesson with ID {lessonId} not found");
            }

            // Check if user has permission (must be instructor or admin)
            if (!await CanManageSubtitlesAsync(lessonId, userId))
            {
                throw new UnauthorizedAccessException("You do not have permission to manage subtitles for this lesson");
            }

            // Check if subtitle already exists for this language
            var existingSubtitle = await _subtitleRepository.GetByLessonAndLanguageAsync(lessonId, language);
            if (existingSubtitle != null)
            {
                throw new InvalidOperationException($"Subtitle track for language '{language}' already exists. Delete the existing one first.");
            }

            _logger.LogInformation("Uploading subtitle file: {FileName} ({Size} bytes) for lesson {LessonId} in language {Language}",
                file.FileName, file.Length, lessonId, language);

            // Upload file to MongoDB GridFS
            using var stream = file.OpenReadStream();
            var metadata = new BsonDocument
            {
                { "lessonId", lessonId.ToString() },
                { "language", language },
                { "label", label },
                { "kind", kind },
                { "uploadedBy", userId.ToString() },
                { "originalFileName", file.FileName }
            };

            var gridFsFileName = $"{lessonId}_{language}.vtt";
            var fileId = await _gridFsBucket.UploadFromStreamAsync(gridFsFileName, stream, new GridFSUploadOptions
            {
                Metadata = metadata
            });

            // Parse WebVTT to count cues and duration
            stream.Position = 0;
            var (cueCount, durationSeconds) = await ParseWebVTTAsync(stream);

            // Create subtitle track entity
            var subtitleTrack = new SubtitleTrack
            {
                LessonId = lessonId,
                Language = language,
                Label = label,
                FileUrl = $"/api/subtitles/stream/{fileId}",
                Kind = kind,
                IsDefault = isDefault,
                FileSize = file.Length,
                CueCount = cueCount,
                DurationSeconds = durationSeconds,
                CreatedByUserId = userId
            };

            // Save to SQL Server
            var savedTrack = await _subtitleRepository.AddAsync(subtitleTrack);

            _logger.LogInformation("Subtitle track created: {Id} for lesson {LessonId} in language {Language} (GridFS ID: {FileId})",
                savedTrack.Id, lessonId, language, fileId);

            return new SubtitleTrackDto
            {
                Url = savedTrack.FileUrl,
                Language = savedTrack.Language,
                Label = savedTrack.Label,
                Kind = savedTrack.Kind,
                IsDefault = savedTrack.IsDefault
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading subtitle for lesson {LessonId}", lessonId);
            throw;
        }
    }

    public async Task<bool> DeleteSubtitleAsync(Guid subtitleId, Guid userId)
    {
        try
        {
            var subtitle = await _subtitleRepository.GetByIdAsync(subtitleId);
            if (subtitle == null)
            {
                return false;
            }

            // Check permissions
            if (!await CanManageSubtitlesAsync(subtitle.LessonId, userId))
            {
                throw new UnauthorizedAccessException("You do not have permission to delete this subtitle");
            }

            // Extract GridFS file ID from URL (format: /api/subtitles/stream/{fileId})
            var fileId = subtitle.FileUrl.Split('/').Last();
            if (ObjectId.TryParse(fileId, out var objectId))
            {
                await _gridFsBucket.DeleteAsync(objectId);
                _logger.LogInformation("Deleted subtitle file from GridFS: {FileId}", fileId);
            }

            // Delete from SQL Server
            var deleted = await _subtitleRepository.DeleteAsync(subtitleId);

            _logger.LogInformation("Deleted subtitle track {Id} for lesson {LessonId}", subtitleId, subtitle.LessonId);

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting subtitle {SubtitleId}", subtitleId);
            throw;
        }
    }

    public async Task<SubtitleTrackDto> UpdateSubtitleMetadataAsync(
        Guid subtitleId,
        string label,
        bool isDefault,
        string kind,
        Guid userId)
    {
        try
        {
            var subtitle = await _subtitleRepository.GetByIdAsync(subtitleId);
            if (subtitle == null)
            {
                throw new InvalidOperationException($"Subtitle track with ID {subtitleId} not found");
            }

            // Check permissions
            if (!await CanManageSubtitlesAsync(subtitle.LessonId, userId))
            {
                throw new UnauthorizedAccessException("You do not have permission to update this subtitle");
            }

            subtitle.Label = label;
            subtitle.IsDefault = isDefault;
            subtitle.Kind = kind;

            var updated = await _subtitleRepository.UpdateAsync(subtitle);

            _logger.LogInformation("Updated subtitle track {Id} metadata", subtitleId);

            return new SubtitleTrackDto
            {
                Url = updated.FileUrl,
                Language = updated.Language,
                Label = updated.Label,
                Kind = updated.Kind,
                IsDefault = updated.IsDefault
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subtitle metadata {SubtitleId}", subtitleId);
            throw;
        }
    }

    public async Task<string> GetSubtitleContentAsync(Guid subtitleId)
    {
        try
        {
            var subtitle = await _subtitleRepository.GetByIdAsync(subtitleId);
            if (subtitle == null)
            {
                throw new InvalidOperationException($"Subtitle track with ID {subtitleId} not found");
            }

            // Extract GridFS file ID from URL
            var fileId = subtitle.FileUrl.Split('/').Last();
            if (!ObjectId.TryParse(fileId, out var objectId))
            {
                throw new InvalidOperationException($"Invalid file ID in subtitle URL: {subtitle.FileUrl}");
            }

            // Download from GridFS
            using var stream = await _gridFsBucket.OpenDownloadStreamAsync(objectId);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subtitle content for {SubtitleId}", subtitleId);
            throw;
        }
    }

    public async Task<string> GetSubtitleContentByGridFsIdAsync(string gridFsFileId)
    {
        try
        {
            if (!ObjectId.TryParse(gridFsFileId, out var objectId))
            {
                throw new InvalidOperationException($"Invalid GridFS file ID: {gridFsFileId}");
            }

            _logger.LogInformation("Downloading subtitle from GridFS with ID: {FileId}", gridFsFileId);

            // Download directly from GridFS
            using var stream = await _gridFsBucket.OpenDownloadStreamAsync(objectId);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subtitle content for GridFS ID {FileId}", gridFsFileId);
            throw;
        }
    }

    public async Task<bool> CanManageSubtitlesAsync(Guid lessonId, Guid userId)
    {
        try
        {
            // Check if user is admin
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            var isAdmin = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId &&
                                _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin"));

            if (isAdmin)
            {
                return true;
            }

            // Check if user is the course instructor
            var lesson = await _context.Lessons
                .Include(l => l.Section)
                .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
            {
                return false;
            }

            return lesson.Section.Course.InstructorId == userId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking subtitle management permissions for lesson {LessonId} and user {UserId}",
                lessonId, userId);
            return false;
        }
    }

    /// <summary>
    /// Parse WebVTT file to extract cue count and duration
    /// </summary>
    private async Task<(int cueCount, int durationSeconds)> ParseWebVTTAsync(Stream stream)
    {
        try
        {
            using var reader = new StreamReader(stream, leaveOpen: true);
            var content = await reader.ReadToEndAsync();
            var lines = content.Split('\n');

            int cueCount = 0;
            int maxDurationSeconds = 0;

            foreach (var line in lines)
            {
                // WebVTT timing line format: 00:00:00.000 --> 00:00:05.000
                if (line.Contains("-->"))
                {
                    cueCount++;

                    // Extract end timestamp
                    var parts = line.Split("-->");
                    if (parts.Length == 2)
                    {
                        var endTime = parts[1].Trim().Split(' ')[0]; // Get timestamp before any positioning
                        var seconds = ParseWebVTTTimestamp(endTime);
                        if (seconds > maxDurationSeconds)
                        {
                            maxDurationSeconds = seconds;
                        }
                    }
                }
            }

            return (cueCount, maxDurationSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing WebVTT file, using defaults");
            return (0, 0);
        }
    }

    /// <summary>
    /// Parse WebVTT timestamp (HH:MM:SS.mmm) to seconds
    /// </summary>
    private int ParseWebVTTTimestamp(string timestamp)
    {
        try
        {
            var parts = timestamp.Split(':');
            if (parts.Length == 3)
            {
                var hours = int.Parse(parts[0]);
                var minutes = int.Parse(parts[1]);
                var secondsParts = parts[2].Split('.');
                var seconds = int.Parse(secondsParts[0]);

                return hours * 3600 + minutes * 60 + seconds;
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return 0;
    }
}
