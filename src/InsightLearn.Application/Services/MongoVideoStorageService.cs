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

            var originalSize = videoStream.Length;

            // Create compressed stream
            using var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, _compressionLevel, leaveOpen: true))
            {
                await videoStream.CopyToAsync(gzipStream);
            }

            compressedStream.Position = 0;
            var compressedSize = compressedStream.Length;

            // Calculate compression ratio
            var compressionRatio = originalSize > 0
                ? (double)(originalSize - compressedSize) / originalSize * 100
                : 0;

            // Store metadata
            var metadata = new BsonDocument
            {
                { "lessonId", lessonId.ToString() },
                { "originalSize", originalSize },
                { "compressedSize", compressedSize },
                { "contentType", contentType },
                { "format", Path.GetExtension(fileName).TrimStart('.').ToLower() },
                { "compressed", true },
                { "compressionType", "gzip" },
                { "compressionRatio", compressionRatio },
                { "uploadDate", DateTime.UtcNow }
            };

            var uploadOptions = new GridFSUploadOptions
            {
                Metadata = metadata
            };

            // Upload compressed video to GridFS
            var fileId = await _gridFsBucket.UploadFromStreamAsync(
                fileName,
                compressedStream,
                uploadOptions
            );

            _logger.LogInformation(
                "Video uploaded successfully: {FileName} (FileId: {FileId}) - Original: {OriginalSize}MB, Compressed: {CompressedSize}MB, Ratio: {Ratio:F2}%",
                fileName, fileId.ToString(), originalSize / 1024.0 / 1024.0, compressedSize / 1024.0 / 1024.0, compressionRatio
            );

            return new VideoUploadResult
            {
                FileId = fileId.ToString(),
                FileName = fileName,
                FileSize = originalSize,
                CompressedSize = compressedSize,
                Format = Path.GetExtension(fileName).TrimStart('.').ToLower(),
                UploadDate = DateTime.UtcNow,
                CompressionRatio = compressionRatio
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

            // Download compressed stream from GridFS
            var compressedStream = await _gridFsBucket.OpenDownloadStreamAsync(objectId);

            // Get metadata to check if file is compressed
            var fileInfo = await _gridFsBucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq("_id", objectId));
            var file = await fileInfo.FirstOrDefaultAsync();

            if (file == null)
            {
                throw new FileNotFoundException($"Video file not found: {fileId}");
            }

            // PERFORMANCE FIX (PERF-5): Return GZipStream directly for on-the-fly decompression
            // Previous implementation loaded entire video into memory (500MB video = 500MB RAM)
            // New implementation streams decompression as data is read (constant memory ~4-8MB)
            if (file.Metadata.Contains("compressed") && file.Metadata["compressed"].AsBoolean)
            {
                _logger.LogDebug("Streaming compressed video with on-the-fly decompression: {FileId}", fileId);

                // Return GZipStream directly - decompresses as data is read (chunked streaming)
                // leaveOpen: false ensures underlying compressedStream is disposed when GZipStream is disposed
                return new GZipStream(compressedStream, CompressionMode.Decompress, leaveOpen: false);
            }

            _logger.LogDebug("Streaming uncompressed video: {FileId}", fileId);
            return compressedStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download video: {FileId}", fileId);
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
