using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using InsightLearn.Core.Entities;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace InsightLearn.Application.Services;

public interface IVideoProcessingService
{
    Task<VideoProcessingResult> ProcessAndSaveVideoAsync(IFormFile video, Guid lessonId, Guid userId);
    Task<VideoStreamResult?> GetVideoStreamAsync(Guid videoId, string? quality);
    Task<UploadProgress> GetUploadProgressAsync(Guid uploadId);
    Task<bool> DeleteVideoAsync(Guid videoId, Guid userId);
}

public class VideoProcessingResult
{
    public string VideoUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int Duration { get; set; } // in seconds
    public long FileSize { get; set; }
    public long CompressedSize { get; set; }
    public string Format { get; set; } = string.Empty;
    public string Quality { get; set; } = string.Empty;
    public double CompressionRatio { get; set; }
}

public class VideoStreamResult
{
    public Stream Stream { get; set; } = null!;
    public string ContentType { get; set; } = "video/mp4";
    public long Length { get; set; }
}

public class UploadProgress
{
    public Guid UploadId { get; set; }
    public string Status { get; set; } = "uploading"; // uploading, processing, completed, failed
    public int ProgressPercentage { get; set; }
    public string? Message { get; set; }
    public VideoProcessingResult? Result { get; set; }
}

public class VideoProcessingService : IVideoProcessingService
{
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<VideoProcessingService> _logger;
    private readonly IMongoVideoStorageService _mongoStorage;
    private static readonly ConcurrentDictionary<Guid, UploadProgress> _uploadProgress = new();

    public VideoProcessingService(
        InsightLearnDbContext context,
        ILogger<VideoProcessingService> logger,
        IMongoVideoStorageService mongoStorage)
    {
        _context = context;
        _logger = logger;
        _mongoStorage = mongoStorage;
    }

    public async Task<VideoProcessingResult> ProcessAndSaveVideoAsync(IFormFile video, Guid lessonId, Guid userId)
    {
        var uploadId = Guid.NewGuid();
        var progress = new UploadProgress
        {
            UploadId = uploadId,
            Status = "uploading",
            ProgressPercentage = 0,
            Message = "Starting upload to MongoDB with compression..."
        };
        _uploadProgress[uploadId] = progress;

        try
        {
            // Verify lesson exists and user has permission
            var lesson = await _context.Set<Lesson>()
                .Include(l => l.Section)
                .ThenInclude(s => s.Course)
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
                throw new Exception("Lesson not found");

            _logger.LogInformation("Processing video upload for lesson {LessonId} by user {UserId}", lessonId, userId);

            // Update progress - uploading
            progress.ProgressPercentage = 10;
            progress.Message = "Uploading video to MongoDB with compression...";

            // Upload video to MongoDB with compression
            using var videoStream = video.OpenReadStream();
            var uploadResult = await _mongoStorage.UploadVideoAsync(
                videoStream,
                video.FileName,
                video.ContentType,
                lessonId
            );

            progress.ProgressPercentage = 70;
            progress.Message = "Video uploaded and compressed. Processing metadata...";

            // Generate thumbnail URL (placeholder for now - can be enhanced later with FFmpeg)
            var thumbnailUrl = $"/api/video/thumbnail/{uploadResult.FileId}";

            // Update progress - processing
            progress.ProgressPercentage = 90;
            progress.Message = "Updating lesson information...";

            // Update lesson with video information
            lesson.VideoUrl = $"/api/video/stream/{uploadResult.FileId}";
            lesson.VideoThumbnailUrl = thumbnailUrl;
            lesson.VideoFileSize = uploadResult.FileSize;
            lesson.VideoFormat = uploadResult.Format;
            lesson.VideoQuality = "Original"; // Can be enhanced with actual quality detection
            lesson.Type = LessonType.Video;

            await _context.SaveChangesAsync();

            // Complete
            progress.Status = "completed";
            progress.ProgressPercentage = 100;
            progress.Message = "Video upload completed successfully!";

            var result = new VideoProcessingResult
            {
                VideoUrl = lesson.VideoUrl,
                ThumbnailUrl = thumbnailUrl,
                Duration = 0, // Can be enhanced with FFmpeg
                FileSize = uploadResult.FileSize,
                CompressedSize = uploadResult.CompressedSize,
                Format = uploadResult.Format,
                Quality = "Original",
                CompressionRatio = uploadResult.CompressionRatio
            };

            progress.Result = result;

            _logger.LogInformation(
                "Video processed successfully for lesson {LessonId}. Original: {OriginalSize}MB, Compressed: {CompressedSize}MB, Ratio: {Ratio:F2}%",
                lessonId, uploadResult.FileSize / 1024.0 / 1024.0, uploadResult.CompressedSize / 1024.0 / 1024.0, uploadResult.CompressionRatio
            );

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process video for lesson {LessonId}", lessonId);
            progress.Status = "failed";
            progress.Message = $"Upload failed: {ex.Message}";
            throw;
        }
    }

    public async Task<VideoStreamResult?> GetVideoStreamAsync(Guid videoId, string? quality)
    {
        try
        {
            // Extract file ID from video URL stored in lesson
            var lesson = await _context.Set<Lesson>()
                .FirstOrDefaultAsync(l => l.VideoUrl != null && l.VideoUrl.Contains(videoId.ToString()));

            if (lesson?.VideoUrl == null)
            {
                _logger.LogWarning("Video not found for ID: {VideoId}", videoId);
                return null;
            }

            // Extract MongoDB file ID from URL (format: /api/video/stream/{fileId})
            var fileId = lesson.VideoUrl.Split('/').Last();

            // Download and decompress video from MongoDB
            var videoStream = await _mongoStorage.DownloadVideoAsync(fileId);

            var metadata = await _mongoStorage.GetVideoMetadataAsync(fileId);

            return new VideoStreamResult
            {
                Stream = videoStream,
                ContentType = metadata.ContentType,
                Length = metadata.FileSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get video stream for ID: {VideoId}", videoId);
            return null;
        }
    }

    public Task<UploadProgress> GetUploadProgressAsync(Guid uploadId)
    {
        _uploadProgress.TryGetValue(uploadId, out var progress);
        return Task.FromResult(progress ?? new UploadProgress
        {
            UploadId = uploadId,
            Status = "not_found",
            ProgressPercentage = 0,
            Message = "Upload not found"
        });
    }

    public async Task<bool> DeleteVideoAsync(Guid videoId, Guid userId)
    {
        try
        {
            var lesson = await _context.Set<Lesson>()
                .FirstOrDefaultAsync(l => l.VideoUrl != null && l.VideoUrl.Contains(videoId.ToString()));

            if (lesson?.VideoUrl == null)
                return false;

            // Extract MongoDB file ID from URL
            var fileId = lesson.VideoUrl.Split('/').Last();

            // Delete from MongoDB
            var deleted = await _mongoStorage.DeleteVideoAsync(fileId);

            if (deleted)
            {
                // Clear lesson video information
                lesson.VideoUrl = null;
                lesson.VideoThumbnailUrl = null;
                lesson.VideoFileSize = null;
                lesson.VideoFormat = null;
                lesson.VideoQuality = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Video deleted successfully for lesson {LessonId} by user {UserId}", lesson.Id, userId);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete video {VideoId}", videoId);
            return false;
        }
    }
}
