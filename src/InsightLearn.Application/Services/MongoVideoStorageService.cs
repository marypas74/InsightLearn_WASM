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

            // Get content type from metadata, or auto-detect from file header
            var metadataContentType = file.Metadata.Contains("contentType")
                ? file.Metadata["contentType"].AsString
                : "unknown";

            _logger.LogInformation("[VIDEO] Metadata contentType for {FileId}: {ContentType}", fileId, metadataContentType);

            // ALWAYS auto-detect from magic bytes (metadata is often wrong)
            var detectedType = await DetectContentTypeFromFileAsync(objectId);
            _logger.LogInformation("[VIDEO] Auto-detected contentType for {FileId}: {DetectedType}", fileId, detectedType);

            // Use detected type (more reliable than metadata)
            var contentType = detectedType;

            return new VideoMetadata
            {
                FileId = file.Id.ToString(),
                FileName = file.Filename,
                FileSize = file.Metadata.Contains("originalSize") ? file.Metadata["originalSize"].AsInt64 : file.Length,
                CompressedSize = file.Length,
                ContentType = contentType,
                UploadDate = file.UploadDateTime,
                LessonId = file.Metadata.Contains("lessonId") ? Guid.Parse(file.Metadata["lessonId"].AsString) : Guid.Empty,
                Format = file.Metadata.Contains("format") ? file.Metadata["format"].AsString : GetFormatFromContentType(contentType)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get video metadata: {FileId}", fileId);
            throw;
        }
    }

    /// <summary>
    /// Auto-detect video content type from file header magic bytes.
    /// Supports: MP4, MPEG-TS, WebM, OGG
    /// </summary>
    private async Task<string> DetectContentTypeFromFileAsync(ObjectId objectId)
    {
        try
        {
            // Read first 32 bytes for format detection
            var stream = await _gridFsBucket.OpenDownloadStreamAsync(objectId);
            var header = new byte[32];
            var bytesRead = await stream.ReadAsync(header, 0, 32);
            await stream.DisposeAsync();

            if (bytesRead < 4)
                return "video/mp4"; // Default if file too small

            // MPEG-TS: starts with 0x47 sync byte (repeated pattern)
            if (header[0] == 0x47)
            {
                _logger.LogInformation("[VIDEO] Detected MPEG-TS format (0x47 sync byte), first bytes: {Bytes}",
                    BitConverter.ToString(header, 0, 8));
                return "video/MP2T";
            }

            // WebM/Matroska: starts with 0x1A 0x45 0xDF 0xA3 (EBML header)
            if (header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3)
            {
                _logger.LogDebug("[VIDEO] Detected WebM/Matroska format");
                return "video/webm";
            }

            // OGG: starts with "OggS"
            if (header[0] == 'O' && header[1] == 'g' && header[2] == 'g' && header[3] == 'S')
            {
                _logger.LogDebug("[VIDEO] Detected OGG format");
                return "video/ogg";
            }

            // MP4/MOV: look for "ftyp" atom (usually at byte 4-7)
            // Format: [4-byte size][4-byte type='ftyp'][brand]
            if (bytesRead >= 8 && header[4] == 'f' && header[5] == 't' && header[6] == 'y' && header[7] == 'p')
            {
                _logger.LogDebug("[VIDEO] Detected MP4/MOV format (ftyp atom)");
                return "video/mp4";
            }

            // QuickTime MOV: sometimes has different header
            if (bytesRead >= 8 && header[4] == 'm' && header[5] == 'o' && header[6] == 'o' && header[7] == 'v')
            {
                _logger.LogDebug("[VIDEO] Detected QuickTime MOV format");
                return "video/quicktime";
            }

            _logger.LogInformation("[VIDEO] Could not detect format, defaulting to video/mp4. First 16 bytes: {Header}",
                BitConverter.ToString(header, 0, Math.Min(bytesRead, 16)));
            return "video/mp4";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[VIDEO] Failed to auto-detect content type, defaulting to video/mp4");
            return "video/mp4";
        }
    }

    private static string GetFormatFromContentType(string contentType)
    {
        return contentType switch
        {
            "video/mp4" => "mp4",
            "video/MP2T" => "ts",
            "video/webm" => "webm",
            "video/ogg" => "ogg",
            "video/quicktime" => "mov",
            _ => "mp4"
        };
    }
}
