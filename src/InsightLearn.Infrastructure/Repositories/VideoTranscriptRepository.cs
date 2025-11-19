using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
    /// </summary>
    public class VideoTranscriptRepository : IVideoTranscriptRepository
    {
        private readonly InsightLearnDbContext _context;
        private readonly IMongoCollection<BsonDocument> _mongoCollection;

        public VideoTranscriptRepository(
            InsightLearnDbContext context,
            IMongoDatabase mongoDatabase)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mongoCollection = mongoDatabase?.GetCollection<BsonDocument>("VideoTranscripts")
                ?? throw new ArgumentNullException(nameof(mongoDatabase));
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
            // 1. Save transcript to MongoDB
            var mongoDoc = new BsonDocument
            {
                { "lessonId", lessonId.ToString() },
                { "language", transcriptDto.Language },
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

            await _mongoCollection.InsertOneAsync(mongoDoc, cancellationToken: ct);
            var mongoDocId = mongoDoc["_id"].AsObjectId.ToString();

            // 2. Create metadata in SQL Server
            var metadata = new VideoTranscriptMetadata
            {
                Id = Guid.NewGuid(),
                LessonId = lessonId,
                MongoDocumentId = mongoDocId,
                Language = transcriptDto.Language,
                ProcessingStatus = "Completed",
                WordCount = transcriptDto.Metadata?.WordCount,
                DurationSeconds = transcriptDto.Metadata?.DurationSeconds,
                AverageConfidence = (decimal?)transcriptDto.Metadata?.AverageConfidence,
                ProcessedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.VideoTranscriptMetadata.Add(metadata);
            await _context.SaveChangesAsync(ct);

            return metadata;
        }

        public async Task UpdateProcessingStatusAsync(Guid lessonId, string status, string? errorMessage = null, CancellationToken ct = default)
        {
            var metadata = await GetMetadataAsync(lessonId, ct);
            if (metadata == null)
                throw new KeyNotFoundException($"Transcript metadata for lesson {lessonId} not found");

            metadata.ProcessingStatus = status;
            if (status == "Completed")
                metadata.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
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
                ProcessingStatus = metadata.ProcessingStatus,
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
