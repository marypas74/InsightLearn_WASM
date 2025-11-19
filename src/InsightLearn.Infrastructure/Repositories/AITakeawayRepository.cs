using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Bson;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Core.DTOs.AITakeaways;
using InsightLearn.Infrastructure.Data;

namespace InsightLearn.Infrastructure.Repositories
{
    /// <summary>
    /// Hybrid repository implementation for AI Takeaways (SQL Server + MongoDB).
    /// SQL Server: Metadata (AIKeyTakeawaysMetadata table).
    /// MongoDB: Full takeaways data (VideoKeyTakeaways collection).
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class AITakeawayRepository : IAITakeawayRepository
    {
        private readonly InsightLearnDbContext _context;
        private readonly IMongoCollection<BsonDocument> _mongoCollection;

        public AITakeawayRepository(
            InsightLearnDbContext context,
            IMongoDatabase mongoDatabase)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mongoCollection = mongoDatabase?.GetCollection<BsonDocument>("VideoKeyTakeaways")
                ?? throw new ArgumentNullException(nameof(mongoDatabase));
        }

        public async Task<VideoKeyTakeawaysDto?> GetTakeawaysAsync(Guid lessonId, CancellationToken ct = default)
        {
            // 1. Get metadata from SQL Server
            var metadata = await GetMetadataAsync(lessonId, ct);
            if (metadata == null || string.IsNullOrEmpty(metadata.MongoDocumentId))
                return null;

            // 2. Get full takeaways from MongoDB
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(metadata.MongoDocumentId));
            var mongoDoc = await _mongoCollection.Find(filter).FirstOrDefaultAsync(ct);

            if (mongoDoc == null)
                return null;

            // 3. Map MongoDB document to DTO
            return MapBsonToDto(mongoDoc, metadata);
        }

        public async Task<AIKeyTakeawaysMetadata?> GetMetadataAsync(Guid lessonId, CancellationToken ct = default)
        {
            return await _context.AIKeyTakeawaysMetadata
                .Include(m => m.Lesson)
                .FirstOrDefaultAsync(m => m.LessonId == lessonId, ct);
        }

        public async Task<AIKeyTakeawaysMetadata> CreateAsync(Guid lessonId, VideoKeyTakeawaysDto takeawaysDto, CancellationToken ct = default)
        {
            // 1. Save takeaways to MongoDB
            var mongoDoc = new BsonDocument
            {
                { "lessonId", lessonId.ToString() },
                { "takeaways", new BsonArray(takeawaysDto.Takeaways.Select(t => new BsonDocument
                    {
                        { "takeawayId", t.TakeawayId },
                        { "text", t.Text },
                        { "category", t.Category },
                        { "relevanceScore", t.RelevanceScore },
                        { "timestampStart", t.TimestampStart.HasValue ? (BsonValue)t.TimestampStart.Value : BsonNull.Value },
                        { "timestampEnd", t.TimestampEnd.HasValue ? (BsonValue)t.TimestampEnd.Value : BsonNull.Value },
                        { "userFeedback", t.UserFeedback.HasValue ? (BsonValue)t.UserFeedback.Value : BsonNull.Value }
                    }))
                },
                { "metadata", takeawaysDto.Metadata != null ? new BsonDocument
                    {
                        { "totalTakeaways", takeawaysDto.Metadata.TotalTakeaways },
                        { "processingModel", takeawaysDto.Metadata.ProcessingModel },
                        { "processedAt", takeawaysDto.Metadata.ProcessedAt }
                    } : BsonNull.Value
                },
                { "createdAt", DateTime.UtcNow }
            };

            await _mongoCollection.InsertOneAsync(mongoDoc, cancellationToken: ct);
            var mongoDocId = mongoDoc["_id"].AsObjectId.ToString();

            // 2. Create metadata in SQL Server
            var metadata = new AIKeyTakeawaysMetadata
            {
                Id = Guid.NewGuid(),
                LessonId = lessonId,
                MongoDocumentId = mongoDocId,
                TakeawayCount = takeawaysDto.Takeaways.Count,
                ProcessingStatus = "Completed",
                ProcessedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.AIKeyTakeawaysMetadata.Add(metadata);
            await _context.SaveChangesAsync(ct);

            return metadata;
        }

        public async Task UpdateProcessingStatusAsync(Guid lessonId, string status, string? errorMessage = null, CancellationToken ct = default)
        {
            var metadata = await GetMetadataAsync(lessonId, ct);
            if (metadata == null)
                throw new KeyNotFoundException($"Takeaway metadata for lesson {lessonId} not found");

            metadata.ProcessingStatus = status;
            if (status == "Completed")
                metadata.ProcessedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
        }

        public async Task SubmitFeedbackAsync(Guid lessonId, string takeawayId, int feedback, CancellationToken ct = default)
        {
            // Get metadata to find MongoDB document
            var metadata = await GetMetadataAsync(lessonId, ct);
            if (metadata == null || string.IsNullOrEmpty(metadata.MongoDocumentId))
                throw new KeyNotFoundException($"Takeaways for lesson {lessonId} not found");

            // Update feedback in MongoDB
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(metadata.MongoDocumentId)),
                Builders<BsonDocument>.Filter.ElemMatch<BsonValue>("takeaways",
                    new BsonDocument("takeawayId", takeawayId))
            );

            var update = Builders<BsonDocument>.Update.Set("takeaways.$.userFeedback", feedback);

            var result = await _mongoCollection.UpdateOneAsync(filter, update, cancellationToken: ct);

            if (result.ModifiedCount == 0)
                throw new KeyNotFoundException($"Takeaway {takeawayId} not found in lesson {lessonId}");
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
            _context.AIKeyTakeawaysMetadata.Remove(metadata);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<bool> ExistsAsync(Guid lessonId, CancellationToken ct = default)
        {
            return await _context.AIKeyTakeawaysMetadata
                .AnyAsync(m => m.LessonId == lessonId, ct);
        }

        // Helper method to map MongoDB BSON to DTO
        private VideoKeyTakeawaysDto MapBsonToDto(BsonDocument mongoDoc, AIKeyTakeawaysMetadata metadata)
        {
            var dto = new VideoKeyTakeawaysDto
            {
                LessonId = metadata.LessonId,
                Takeaways = new System.Collections.Generic.List<TakeawayDto>()
            };

            if (mongoDoc.Contains("takeaways"))
            {
                var takeaways = mongoDoc["takeaways"].AsBsonArray;
                foreach (var takeaway in takeaways)
                {
                    var doc = takeaway.AsBsonDocument;
                    dto.Takeaways.Add(new TakeawayDto
                    {
                        TakeawayId = doc["takeawayId"].AsString,
                        Text = doc["text"].AsString,
                        Category = doc["category"].AsString,
                        RelevanceScore = doc["relevanceScore"].ToDouble(),
                        TimestampStart = doc.Contains("timestampStart") && doc["timestampStart"] != BsonNull.Value
                            ? doc["timestampStart"].ToDouble()
                            : null,
                        TimestampEnd = doc.Contains("timestampEnd") && doc["timestampEnd"] != BsonNull.Value
                            ? doc["timestampEnd"].ToDouble()
                            : null,
                        UserFeedback = doc.Contains("userFeedback") && doc["userFeedback"] != BsonNull.Value
                            ? doc["userFeedback"].ToInt32()
                            : null
                    });
                }
            }

            if (mongoDoc.Contains("metadata") && mongoDoc["metadata"] != BsonNull.Value)
            {
                var meta = mongoDoc["metadata"].AsBsonDocument;
                dto.Metadata = new TakeawayMetadataDto
                {
                    TotalTakeaways = meta["totalTakeaways"].ToInt32(),
                    ProcessingModel = meta["processingModel"].AsString,
                    ProcessedAt = meta["processedAt"].ToUniversalTime()
                };
            }

            return dto;
        }
    }
}
