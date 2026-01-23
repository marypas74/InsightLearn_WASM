using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Bson;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Core.DTOs.VideoTranscript;
using InsightLearn.Infrastructure.Data;

namespace InsightLearn.Infrastructure.Repositories
{
    /// <summary>
    /// Hybrid repository implementation for VideoTranscript (SQL Server + MongoDB).
    /// SQL Server: Metadata (VideoTranscriptMetadata table).
    /// MongoDB: Full transcript data (VideoTranscripts collection).
    /// Part of Student Learning Space v2.1.0.
    /// v2.3.91-dev: Added detailed logging for 90-100% phase debugging.
    /// </summary>
    public class VideoTranscriptRepository : IVideoTranscriptRepository
    {
        private readonly InsightLearnDbContext _context;
        private readonly IMongoCollection<BsonDocument> _mongoCollection;
        private readonly ILogger<VideoTranscriptRepository> _logger;

        public VideoTranscriptRepository(
            InsightLearnDbContext context,
            IMongoDatabase mongoDatabase,
            ILogger<VideoTranscriptRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mongoCollection = mongoDatabase?.GetCollection<BsonDocument>("VideoTranscripts")
                ?? throw new ArgumentNullException(nameof(mongoDatabase));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VideoTranscriptDto?> GetTranscriptAsync(Guid lessonId, CancellationToken ct = default)
        {
            // 1. Get metadata from SQL Server
            var metadata = await GetMetadataAsync(lessonId, ct);
            if (metadata == null || string.IsNullOrEmpty(metadata.MongoDocumentId))
                return null;

            // 2. Get full transcript from MongoDB
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(metadata.MongoDocumentId));
            var mongoDoc = await _mongoCollection.Find(filter).FirstOrDefaultAsync(ct);

            if (mongoDoc == null)
                return null;

            // 3. Map MongoDB document to DTO
            return MapBsonToDto(mongoDoc, metadata);
        }

        public async Task<VideoTranscriptMetadata?> GetMetadataAsync(Guid lessonId, CancellationToken ct = default)
        {
            return await _context.VideoTranscriptMetadata
                .Include(m => m.Lesson)
                .FirstOrDefaultAsync(m => m.LessonId == lessonId, ct);
        }

        public async Task<TranscriptSearchResultDto> SearchTranscriptAsync(Guid lessonId, string query, int limit = 10, CancellationToken ct = default)
        {
            // Get metadata first
            var metadata = await GetMetadataAsync(lessonId, ct);
            if (metadata == null || string.IsNullOrEmpty(metadata.MongoDocumentId))
            {
                return new TranscriptSearchResultDto
                {
                    LessonId = lessonId,
                    Query = query,
                    TotalMatches = 0,
                    Matches = new List<TranscriptSearchMatchDto>()
                };
            }

            // MongoDB text search
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(metadata.MongoDocumentId)),
                Builders<BsonDocument>.Filter.Text(query)
            );

            var projection = Builders<BsonDocument>.Projection
                .Include("transcript")
                .MetaTextScore("score");

            var mongoDoc = await _mongoCollection
                .Find(filter)
                .Project(projection)
                .FirstOrDefaultAsync(ct);

            if (mongoDoc == null || !mongoDoc.Contains("transcript"))
            {
                return new TranscriptSearchResultDto
                {
                    LessonId = lessonId,
                    Query = query,
                    TotalMatches = 0,
                    Matches = new List<TranscriptSearchMatchDto>()
                };
            }

            // Parse transcript segments and filter by query
            var segments = mongoDoc["transcript"].AsBsonArray;
            var matches = new List<TranscriptSearchMatchDto>();

            foreach (var segment in segments)
            {
                var text = segment["text"].AsString;
                if (text.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(new TranscriptSearchMatchDto
                    {
                        Timestamp = segment["startTime"].ToDouble(),
                        Text = text,
                        Speaker = segment.AsBsonDocument.Contains("speaker") ? segment["speaker"].AsString : null,
                        RelevanceScore = 1.0 // Simple relevance (can be enhanced)
                    });
                }
            }

            return new TranscriptSearchResultDto
            {
                LessonId = lessonId,
                Query = query,
                TotalMatches = matches.Count,
                Matches = matches.Take(limit).ToList()
            };
        }

        public async Task<VideoTranscriptMetadata> CreateAsync(Guid lessonId, VideoTranscriptDto transcriptDto, CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("[REPO:95%] ═══════════════════════════════════════════════════════════════");
            _logger.LogInformation("[REPO:95%] CreateAsync STARTED for lesson {LessonId}", lessonId);
            _logger.LogInformation("[REPO:95%] Input: Language={Language}, Segments={SegmentCount}, Duration={Duration}s",
                transcriptDto.Language, transcriptDto.Transcript?.Count ?? 0, transcriptDto.Metadata?.DurationSeconds ?? 0);

            // Convert language to ISO 639-1 for MongoDB text index compatibility (it-IT -> it)
            // MongoDB text index requires 2-letter language codes: en, it, es, fr, de, etc.
            var mongoLanguage = transcriptDto.Language.Contains("-")
                ? transcriptDto.Language.Split('-')[0].ToLowerInvariant()
                : transcriptDto.Language.ToLowerInvariant();
            _logger.LogInformation("[REPO:95%] Language converted for MongoDB: {Original} → {MongoFormat}",
                transcriptDto.Language, mongoLanguage);

            // 1. Build MongoDB document
            _logger.LogInformation("[REPO:95.5%] Building MongoDB BSON document...");
            var bsonBuildStart = stopwatch.ElapsedMilliseconds;

            var mongoDoc = new BsonDocument
            {
                { "lessonId", lessonId.ToString() },
                { "language", mongoLanguage },
                { "processingStatus", transcriptDto.ProcessingStatus },
                { "transcript", new BsonArray(transcriptDto.Transcript.Select(s => new BsonDocument
                    {
                        { "startTime", s.StartTime },
                        { "endTime", s.EndTime },
                        { "speaker", s.Speaker ?? "" },
                        { "text", s.Text },
                        { "confidence", s.Confidence ?? 0.0 }
                    }))
                },
                { "metadata", transcriptDto.Metadata != null ? new BsonDocument
                    {
                        { "durationSeconds", transcriptDto.Metadata.DurationSeconds },
                        { "wordCount", transcriptDto.Metadata.WordCount },
                        { "averageConfidence", transcriptDto.Metadata.AverageConfidence },
                        { "processingEngine", transcriptDto.Metadata.ProcessingEngine },
                        { "processedAt", transcriptDto.Metadata.ProcessedAt }
                    } : BsonNull.Value
                },
                { "createdAt", DateTime.UtcNow }
            };

            var bsonBuildDuration = stopwatch.ElapsedMilliseconds - bsonBuildStart;
            var docSizeKb = mongoDoc.ToBson().Length / 1024.0;
            _logger.LogInformation("[REPO:96%] BSON document built in {Duration}ms, Size: {Size:F2}KB",
                bsonBuildDuration, docSizeKb);

            // 2. Save to MongoDB
            _logger.LogInformation("[REPO:96%] ▶▶▶ INSERTING INTO MONGODB (VideoTranscripts collection)...");
            var mongoInsertStart = stopwatch.ElapsedMilliseconds;

            try
            {
                await _mongoCollection.InsertOneAsync(mongoDoc, cancellationToken: ct);
                var mongoDocId = mongoDoc["_id"].AsObjectId.ToString();
                var mongoInsertDuration = stopwatch.ElapsedMilliseconds - mongoInsertStart;

                _logger.LogInformation("[REPO:97%] ✓ MongoDB INSERT SUCCESS in {Duration}ms, DocumentId: {DocId}",
                    mongoInsertDuration, mongoDocId);

                // 3. Update or Create metadata in SQL Server (UPSERT pattern)
                _logger.LogInformation("[REPO:97%] ▶▶▶ UPSERTING INTO SQL SERVER (VideoTranscriptMetadata table)...");
                var sqlInsertStart = stopwatch.ElapsedMilliseconds;

                // Check if record already exists (created by UpdateProcessingStatusAsync)
                var existingMetadata = await _context.VideoTranscriptMetadata
                    .FirstOrDefaultAsync(m => m.LessonId == lessonId, ct);

                VideoTranscriptMetadata metadata;

                if (existingMetadata != null)
                {
                    // UPDATE existing record
                    _logger.LogInformation("[REPO:97%] Found existing metadata record, UPDATING...");
                    existingMetadata.MongoDocumentId = mongoDocId;
                    existingMetadata.Language = transcriptDto.Language;
                    existingMetadata.Status = "Completed";
                    existingMetadata.SegmentCount = transcriptDto.Transcript?.Count;
                    existingMetadata.DurationSeconds = transcriptDto.Metadata?.DurationSeconds;
                    existingMetadata.GeneratedAt = DateTime.UtcNow;

                    metadata = existingMetadata;
                    _logger.LogInformation("[REPO:97.5%] SQL entity UPDATED: Id={Id}, MongoDocId={MongoId}, Status={Status}",
                        metadata.Id, mongoDocId, metadata.Status);
                }
                else
                {
                    // INSERT new record
                    _logger.LogInformation("[REPO:97%] No existing record, INSERTING...");
                    metadata = new VideoTranscriptMetadata
                    {
                        Id = Guid.NewGuid(),
                        LessonId = lessonId,
                        MongoDocumentId = mongoDocId,
                        Language = transcriptDto.Language,
                        Status = "Completed",
                        SegmentCount = transcriptDto.Transcript?.Count,
                        DurationSeconds = transcriptDto.Metadata?.DurationSeconds,
                        GeneratedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.VideoTranscriptMetadata.Add(metadata);
                    _logger.LogInformation("[REPO:97.5%] SQL entity prepared: Id={Id}, MongoDocId={MongoId}, Status={Status}",
                        metadata.Id, mongoDocId, metadata.Status);
                }

                _logger.LogInformation("[REPO:97.5%] Calling SaveChangesAsync...");
                await _context.SaveChangesAsync(ct);

                var sqlInsertDuration = stopwatch.ElapsedMilliseconds - sqlInsertStart;
                _logger.LogInformation("[REPO:98%] ✓ SQL Server INSERT SUCCESS in {Duration}ms", sqlInsertDuration);

                stopwatch.Stop();
                _logger.LogInformation("[REPO:98%] ═══════════════════════════════════════════════════════════════");
                _logger.LogInformation("[REPO:98%] CreateAsync COMPLETED in {TotalDuration}ms (MongoDB: {MongoDuration}ms, SQL: {SqlDuration}ms)",
                    stopwatch.ElapsedMilliseconds, mongoInsertDuration, sqlInsertDuration);
                _logger.LogInformation("[REPO:98%] ═══════════════════════════════════════════════════════════════");

                return metadata;
            }
            catch (MongoWriteException mongoEx)
            {
                stopwatch.Stop();
                _logger.LogError(mongoEx, "[REPO:ERROR] ✗ MongoDB WRITE FAILED after {Duration}ms: {Message}",
                    stopwatch.ElapsedMilliseconds, mongoEx.Message);
                _logger.LogError("[REPO:ERROR] MongoDB WriteError: Category={Category}, Code={Code}, Details={Details}",
                    mongoEx.WriteError?.Category, mongoEx.WriteError?.Code, mongoEx.WriteError?.Details);
                throw;
            }
            catch (DbUpdateException sqlEx)
            {
                stopwatch.Stop();
                _logger.LogError(sqlEx, "[REPO:ERROR] ✗ SQL Server UPDATE FAILED after {Duration}ms: {Message}",
                    stopwatch.ElapsedMilliseconds, sqlEx.Message);
                _logger.LogError("[REPO:ERROR] SQL InnerException: {InnerMessage}",
                    sqlEx.InnerException?.Message ?? "None");
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[REPO:ERROR] ✗ CreateAsync FAILED after {Duration}ms: {ExType} - {Message}",
                    stopwatch.ElapsedMilliseconds, ex.GetType().Name, ex.Message);
                throw;
            }
        }

        public async Task UpdateProcessingStatusAsync(Guid lessonId, string status, string? errorMessage = null, CancellationToken ct = default)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("[REPO:STATUS] UpdateProcessingStatusAsync: LessonId={LessonId}, Status={Status}",
                lessonId, status);

            // Use atomic SQL MERGE to avoid race conditions with duplicate key errors
            // This handles concurrent calls safely without check-then-insert pattern
            var newId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            var generatedAt = status == "Completed" ? now : (DateTime?)null;

            try
            {
                var rowsAffected = await _context.Database.ExecuteSqlRawAsync(@"
                    MERGE VideoTranscriptMetadata WITH (HOLDLOCK) AS target
                    USING (SELECT @p0 AS LessonId) AS source
                    ON target.LessonId = source.LessonId
                    WHEN MATCHED THEN
                        UPDATE SET
                            Status = @p1,
                            GeneratedAt = CASE WHEN @p1 = 'Completed' THEN @p2 ELSE target.GeneratedAt END
                    WHEN NOT MATCHED THEN
                        INSERT (Id, LessonId, MongoDocumentId, Language, Status, SegmentCount, DurationSeconds, CreatedAt, GeneratedAt)
                        VALUES (@p3, @p0, '', 'en-US', @p1, 0, 0, @p2, @p4);",
                    lessonId, status, now, newId, generatedAt);

                stopwatch.Stop();
                _logger.LogInformation("[REPO:STATUS] ✓ Status updated to '{Status}' in {Duration}ms (RowsAffected: {Rows})",
                    status, stopwatch.ElapsedMilliseconds, rowsAffected);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "[REPO:STATUS] ✗ Failed to update status to '{Status}' after {Duration}ms: {Message}",
                    status, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }

        public async Task DeleteAsync(Guid lessonId, CancellationToken ct = default)
        {
            // 1. Get metadata
            var metadata = await GetMetadataAsync(lessonId, ct);
            if (metadata == null)
                return;

            // 2. Delete from MongoDB
            if (!string.IsNullOrEmpty(metadata.MongoDocumentId))
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(metadata.MongoDocumentId));
                await _mongoCollection.DeleteOneAsync(filter, ct);
            }

            // 3. Delete metadata from SQL Server
            _context.VideoTranscriptMetadata.Remove(metadata);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<bool> ExistsAsync(Guid lessonId, CancellationToken ct = default)
        {
            return await _context.VideoTranscriptMetadata
                .AnyAsync(m => m.LessonId == lessonId, ct);
        }

        // Helper method to map MongoDB BSON to DTO
        private VideoTranscriptDto MapBsonToDto(BsonDocument mongoDoc, VideoTranscriptMetadata metadata)
        {
            var dto = new VideoTranscriptDto
            {
                LessonId = metadata.LessonId,
                Language = metadata.Language,
                ProcessingStatus = metadata.Status,
                Transcript = new List<TranscriptSegmentDto>()
            };

            if (mongoDoc.Contains("transcript"))
            {
                var segments = mongoDoc["transcript"].AsBsonArray;
                foreach (var segment in segments)
                {
                    dto.Transcript.Add(new TranscriptSegmentDto
                    {
                        StartTime = segment["startTime"].ToDouble(),
                        EndTime = segment["endTime"].ToDouble(),
                        Speaker = segment.AsBsonDocument.Contains("speaker") ? segment["speaker"].AsString : null,
                        Text = segment["text"].AsString,
                        Confidence = segment.AsBsonDocument.Contains("confidence") ? segment["confidence"].ToDouble() : null
                    });
                }
            }

            if (mongoDoc.Contains("metadata") && mongoDoc["metadata"] != BsonNull.Value)
            {
                var meta = mongoDoc["metadata"].AsBsonDocument;
                dto.Metadata = new TranscriptMetadataDto
                {
                    DurationSeconds = meta["durationSeconds"].ToInt32(),
                    WordCount = meta["wordCount"].ToInt32(),
                    AverageConfidence = meta["averageConfidence"].ToDouble(),
                    ProcessingEngine = meta["processingEngine"].AsString,
                    ProcessedAt = meta["processedAt"].ToUniversalTime()
                };
            }

            return dto;
        }
    }
}
