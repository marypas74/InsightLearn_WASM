using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System.IO.Compression;

namespace InsightLearn.Application.Services;

public interface IMongoVideoStorageService
{
    Task<VideoUploadResult> UploadVideoAsync(Stream videoStream, string fileName, string contentType, Guid lessonId);
    Task<Stream> DownloadVideoAsync(string fileId);
    Task<(Stream Stream, long ContentLength)> DownloadVideoWithLengthAsync(string fileId);
    Task<bool> DeleteVideoAsync(string fileId);
    Task<VideoMetadata> GetVideoMetadataAsync(string fileId);
}

public class VideoUploadResult
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public long CompressedSize { get; set; }
    public string Format { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public double CompressionRatio { get; set; }
}

public class VideoMetadata
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public long CompressedSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
    public Guid LessonId { get; set; }
    public string Format { get; set; } = string.Empty;
}

public class MongoVideoStorageService : IMongoVideoStorageService
{
    private readonly IMongoDatabase _database;
    private readonly IGridFSBucket _gridFsBucket;
    private readonly ILogger<MongoVideoStorageService> _logger;
    private readonly CompressionLevel _compressionLevel = CompressionLevel.Optimal;

    public MongoVideoStorageService(
        IConfiguration configuration,
        ILogger<MongoVideoStorageService> logger)
    {
        _logger = logger;

        // Get MongoDB connection string from configuration
        var connectionString = configuration["MongoDb:ConnectionString"]
            ?? configuration.GetConnectionString("MongoDB")
            ?? throw new ArgumentNullException("MongoDB connection string not configured");

        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("insightlearn_videos");

        // Configure GridFS bucket with compression
        var gridFsOptions = new GridFSBucketOptions
        {
            BucketName = "videos",
            ChunkSizeBytes = 1048576, // 1MB chunks
            WriteConcern = WriteConcern.WMajority
        };

        _gridFsBucket = new GridFSBucket(_database, gridFsOptions);

        _logger.LogInformation("MongoVideoStorageService initialized with compression level: {CompressionLevel}", _compressionLevel);
    }

    public async Task<VideoUploadResult> UploadVideoAsync(
        Stream videoStream,
        string fileName,
        string contentType,
        Guid lessonId)
    {
        try
        {
            _logger.LogInformation("Starting video upload: {FileName} for lesson {LessonId}", fileName, lessonId);

            var fileSize = videoStream.Length;

            // VIDEO STREAMING FIX (2025-12-11): Upload without GZip compression
            // Video codecs (H.264, H.265, VP9) are already heavily compressed
            // GZip adds minimal benefit (~2-5%) while breaking seekability
            // Uncompressed uploads enable:
            // - HTTP Range requests (206 Partial Content) for seeking
            // - Safari video playback compatibility
            // - Faster streaming (no decompression overhead)

            // Store metadata (no compression)
            var metadata = new BsonDocument
            {
                { "lessonId", lessonId.ToString() },
                { "originalSize", fileSize },
                { "compressedSize", fileSize },  // Same size - no compression
                { "contentType", contentType },
                { "format", Path.GetExtension(fileName).TrimStart('.').ToLower() },
                { "compressed", false },  // Mark as uncompressed for streaming
                { "compressionType", "none" },
                { "compressionRatio", 0.0 },
                { "uploadDate", DateTime.UtcNow },
                { "seekable", true }  // Explicitly mark as seekable for Range requests
            };

            var uploadOptions = new GridFSUploadOptions
            {
                Metadata = metadata
            };

            // Upload video directly to GridFS (no compression)
            var fileId = await _gridFsBucket.UploadFromStreamAsync(
                fileName,
                videoStream,
                uploadOptions
            );

            _logger.LogInformation(
                "Video uploaded successfully (uncompressed): {FileName} (FileId: {FileId}) - Size: {Size}MB",
                fileName, fileId.ToString(), fileSize / 1024.0 / 1024.0
            );

            return new VideoUploadResult
            {
                FileId = fileId.ToString(),
                FileName = fileName,
                FileSize = fileSize,
                CompressedSize = fileSize,  // Same size - no compression
                Format = Path.GetExtension(fileName).TrimStart('.').ToLower(),
                UploadDate = DateTime.UtcNow,
                CompressionRatio = 0  // No compression
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload video: {FileName} for lesson {LessonId}", fileName, lessonId);
            throw;
        }
    }

    public async Task<Stream> DownloadVideoAsync(string fileId)
    {
        try
        {
            var objectId = ObjectId.Parse(fileId);

            // Get metadata first to check compression status
            var fileInfo = await _gridFsBucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq("_id", objectId));
            var file = await fileInfo.FirstOrDefaultAsync();

            if (file == null)
            {
                throw new FileNotFoundException($"Video file not found: {fileId}");
            }

            // VIDEO STREAMING FIX (2025-12-11): Use seekable stream for Range request support
            // GZipStream is NOT seekable, breaking Safari and browser seeking functionality
            // Solution: Use GridFSDownloadOptions { Seekable = true } for uncompressed videos
            // For legacy compressed videos: decompress to MemoryStream first (seekable)

            if (file.Metadata.Contains("compressed") && file.Metadata["compressed"].AsBoolean)
            {
                _logger.LogDebug("Decompressing legacy compressed video to seekable stream: {FileId}", fileId);

                // Legacy compressed video - decompress to MemoryStream for seekability
                var compressedStream = await _gridFsBucket.OpenDownloadStreamAsync(objectId);
                var decompressedStream = new MemoryStream();

                using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    await gzipStream.CopyToAsync(decompressedStream);
                }

                decompressedStream.Position = 0;
                _logger.LogDebug("Video decompressed: {FileId}, Size: {Size}MB", fileId, decompressedStream.Length / 1024.0 / 1024.0);
                return decompressedStream;
            }

            // Uncompressed video - use seekable GridFS stream
            _logger.LogDebug("Streaming uncompressed video with seekable stream: {FileId}", fileId);
            var options = new GridFSDownloadOptions { Seekable = true };
            return await _gridFsBucket.OpenDownloadStreamAsync(objectId, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download video: {FileId}", fileId);
            throw;
        }
    }

    public async Task<(Stream Stream, long ContentLength)> DownloadVideoWithLengthAsync(string fileId)
    {
        try
        {
            var objectId = ObjectId.Parse(fileId);

            // Get metadata to determine content length and compression status
            var fileInfo = await _gridFsBucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq("_id", objectId));
            var file = await fileInfo.FirstOrDefaultAsync();

            if (file == null)
            {
                throw new FileNotFoundException($"Video file not found: {fileId}");
            }

            // VIDEO STREAMING FIX (2025-12-11): Return seekable stream with accurate content length
            // This enables HTTP Range requests (206 Partial Content) required for:
            // - Safari video playback (mandatory)
            // - Video seeking/scrubbing in all browsers
            // - Resume interrupted downloads

            if (file.Metadata.Contains("compressed") && file.Metadata["compressed"].AsBoolean)
            {
                _logger.LogDebug("Decompressing legacy compressed video: {FileId}", fileId);

                // Legacy compressed video - decompress to MemoryStream
                var compressedStream = await _gridFsBucket.OpenDownloadStreamAsync(objectId);
                var decompressedStream = new MemoryStream();

                using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    await gzipStream.CopyToAsync(decompressedStream);
                }

                decompressedStream.Position = 0;
                var originalSize = file.Metadata.Contains("originalSize")
                    ? file.Metadata["originalSize"].AsInt64
                    : decompressedStream.Length;

                _logger.LogDebug("Video decompressed: {FileId}, Size: {Size}MB", fileId, decompressedStream.Length / 1024.0 / 1024.0);
                return (decompressedStream, decompressedStream.Length);
            }

            // Uncompressed video - use seekable GridFS stream with known length
            _logger.LogDebug("Streaming uncompressed video: {FileId}, Size: {Size}MB", fileId, file.Length / 1024.0 / 1024.0);
            var options = new GridFSDownloadOptions { Seekable = true };
            var stream = await _gridFsBucket.OpenDownloadStreamAsync(objectId, options);

            return (stream, file.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download video with length: {FileId}", fileId);
            throw;
        }
    }

    public async Task<bool> DeleteVideoAsync(string fileId)
    {
        try
        {
            var objectId = ObjectId.Parse(fileId);
            await _gridFsBucket.DeleteAsync(objectId);

            _logger.LogInformation("Video deleted successfully: {FileId}", fileId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete video: {FileId}", fileId);
            return false;
        }
    }

    public async Task<VideoMetadata> GetVideoMetadataAsync(string fileId)
    {
        try
        {
            var objectId = ObjectId.Parse(fileId);
            var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", objectId);
            var fileInfo = await _gridFsBucket.FindAsync(filter);
            var file = await fileInfo.FirstOrDefaultAsync();

            if (file == null)
            {
                throw new FileNotFoundException($"Video file not found: {fileId}");
            }

            return new VideoMetadata
            {
                FileId = file.Id.ToString(),
                FileName = file.Filename,
                FileSize = file.Metadata.Contains("originalSize") ? file.Metadata["originalSize"].AsInt64 : file.Length,
                CompressedSize = file.Length,
                ContentType = file.Metadata.Contains("contentType") ? file.Metadata["contentType"].AsString : "video/mp4",
                UploadDate = file.UploadDateTime,
                LessonId = file.Metadata.Contains("lessonId") ? Guid.Parse(file.Metadata["lessonId"].AsString) : Guid.Empty,
                Format = file.Metadata.Contains("format") ? file.Metadata["format"].AsString : "mp4"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get video metadata: {FileId}", fileId);
            throw;
        }
    }
}
