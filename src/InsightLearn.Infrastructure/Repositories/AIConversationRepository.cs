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
using InsightLearn.Core.DTOs.AIChat;
using InsightLearn.Infrastructure.Data;

namespace InsightLearn.Infrastructure.Repositories
{
    /// <summary>
    /// Hybrid repository implementation for AI Conversations (SQL Server + MongoDB).
    /// SQL Server: Metadata (AIConversation table).
    /// MongoDB: Message history (AIConversationHistory collection).
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class AIConversationRepository : IAIConversationRepository
    {
        private readonly InsightLearnDbContext _context;
        private readonly IMongoCollection<BsonDocument> _mongoCollection;

        public AIConversationRepository(
            InsightLearnDbContext context,
            IMongoDatabase mongoDatabase)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mongoCollection = mongoDatabase?.GetCollection<BsonDocument>("AIConversationHistory")
                ?? throw new ArgumentNullException(nameof(mongoDatabase));
        }

        public async Task<AIConversationHistoryDto?> GetConversationHistoryAsync(Guid sessionId, int limit = 50, CancellationToken ct = default)
        {
            // 1. Get conversation metadata from SQL Server
            var conversation = await GetConversationMetadataAsync(sessionId, ct);
            if (conversation == null)
                return null;

            // 2. Get message history from MongoDB
            var dto = new AIConversationHistoryDto
            {
                SessionId = sessionId,
                UserId = conversation.UserId,
                LessonId = conversation.LessonId,
                MessageCount = conversation.MessageCount,
                CreatedAt = conversation.CreatedAt,
                LastMessageAt = conversation.LastMessageAt,
                Messages = new List<ConversationMessageDto>()
            };

            if (!string.IsNullOrEmpty(conversation.MongoDocumentId))
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(conversation.MongoDocumentId));
                var mongoDoc = await _mongoCollection.Find(filter).FirstOrDefaultAsync(ct);

                if (mongoDoc != null && mongoDoc.Contains("messages"))
                {
                    var messages = mongoDoc["messages"].AsBsonArray;
                    var limitedMessages = messages.Take(limit);

                    foreach (var message in limitedMessages)
                    {
                        var msgDoc = message.AsBsonDocument;
                        dto.Messages.Add(new ConversationMessageDto
                        {
                            MessageId = msgDoc["messageId"].AsString,
                            Role = msgDoc["role"].AsString,
                            Content = msgDoc["content"].AsString,
                            Timestamp = msgDoc["timestamp"].ToUniversalTime(),
                            VideoTimestamp = msgDoc.Contains("videoTimestamp") && msgDoc["videoTimestamp"] != BsonNull.Value
                                ? msgDoc["videoTimestamp"].ToInt32()
                                : null
                        });
                    }
                }
            }

            return dto;
        }

        public async Task<AIConversation?> GetConversationMetadataAsync(Guid sessionId, CancellationToken ct = default)
        {
            // Note: Don't include navigation properties - anonymous users may have UserId that doesn't exist in Users table
            // This prevents EF Core issues with lazy loading and navigation properties
            return await _context.AIConversations
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.SessionId == sessionId, ct);
        }

        public async Task<List<AIConversation>> GetUserConversationsAsync(Guid userId, int limit = 50, CancellationToken ct = default)
        {
            return await _context.AIConversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.LastMessageAt)
                .Take(limit)
                .ToListAsync(ct);
        }

        public async Task<List<AIConversation>> GetLessonConversationsAsync(Guid lessonId, int limit = 50, CancellationToken ct = default)
        {
            return await _context.AIConversations
                .Where(c => c.LessonId == lessonId)
                .OrderByDescending(c => c.LastMessageAt)
                .Take(limit)
                .ToListAsync(ct);
        }

        public async Task<AIConversation> CreateConversationAsync(Guid? userId, Guid? lessonId = null, int? videoTimestamp = null, CancellationToken ct = default)
        {
            // 1. Create MongoDB document for message history
            var sessionId = Guid.NewGuid();
            var mongoDoc = new BsonDocument
            {
                { "sessionId", sessionId.ToString() },
                { "userId", userId.HasValue ? userId.Value.ToString() : "anonymous" },
                { "messages", new BsonArray() },
                { "createdAt", DateTime.UtcNow }
            };

            if (lessonId.HasValue)
                mongoDoc.Add("lessonId", lessonId.Value.ToString());
            else
                mongoDoc.Add("lessonId", BsonNull.Value);

            await _mongoCollection.InsertOneAsync(mongoDoc, cancellationToken: ct);
            var mongoDocId = mongoDoc["_id"].AsObjectId.ToString();

            // 2. Create conversation metadata in SQL Server
            var conversation = new AIConversation
            {
                SessionId = sessionId,
                UserId = userId,
                LessonId = lessonId,
                CurrentVideoTimestamp = videoTimestamp,
                MongoDocumentId = mongoDocId,
                MessageCount = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };

            _context.AIConversations.Add(conversation);
            await _context.SaveChangesAsync(ct);

            return conversation;
        }

        public async Task AddMessageAsync(Guid sessionId, string role, string content, int? videoTimestamp = null, CancellationToken ct = default)
        {
            // 1. Get conversation metadata WITH tracking (needed for update)
            // Don't use AsNoTracking() here because we need to modify the entity
            var conversation = await _context.AIConversations
                .FirstOrDefaultAsync(c => c.SessionId == sessionId, ct);

            if (conversation == null)
                throw new KeyNotFoundException($"Conversation {sessionId} not found");

            if (string.IsNullOrEmpty(conversation.MongoDocumentId))
                throw new InvalidOperationException($"Conversation {sessionId} has no MongoDB document");

            // 2. Add message to MongoDB
            var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(conversation.MongoDocumentId));

            var newMessage = new BsonDocument
            {
                { "messageId", Guid.NewGuid().ToString() },
                { "role", role },
                { "content", content },
                { "timestamp", DateTime.UtcNow }
            };

            if (videoTimestamp.HasValue)
                newMessage.Add("videoTimestamp", videoTimestamp.Value);
            else
                newMessage.Add("videoTimestamp", BsonNull.Value);

            var update = Builders<BsonDocument>.Update.Push("messages", newMessage);

            var result = await _mongoCollection.UpdateOneAsync(filter, update, cancellationToken: ct);

            if (result.ModifiedCount == 0)
                throw new InvalidOperationException($"Failed to add message to conversation {sessionId}");

            // 3. Update conversation metadata in SQL Server (entity is tracked, changes will be saved)
            conversation.MessageCount++;
            conversation.LastMessageAt = DateTime.UtcNow;

            if (videoTimestamp.HasValue)
                conversation.CurrentVideoTimestamp = videoTimestamp.Value;

            await _context.SaveChangesAsync(ct);
        }

        public async Task EndConversationAsync(Guid sessionId, CancellationToken ct = default)
        {
            // Use tracked query (not AsNoTracking) because we need to modify the entity
            var conversation = await _context.AIConversations
                .FirstOrDefaultAsync(c => c.SessionId == sessionId, ct);

            if (conversation == null)
                throw new KeyNotFoundException($"Conversation {sessionId} not found");

            conversation.IsActive = false;
            conversation.LastMessageAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
        }

        public async Task DeleteConversationAsync(Guid sessionId, CancellationToken ct = default)
        {
            // 1. Get conversation metadata
            var conversation = await GetConversationMetadataAsync(sessionId, ct);
            if (conversation == null)
                return;

            // 2. Delete message history from MongoDB
            if (!string.IsNullOrEmpty(conversation.MongoDocumentId))
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(conversation.MongoDocumentId));
                await _mongoCollection.DeleteOneAsync(filter, ct);
            }

            // 3. Delete conversation metadata from SQL Server
            _context.AIConversations.Remove(conversation);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<int> DeleteOldConversationsAsync(int daysOld = 90, CancellationToken ct = default)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);

            // 1. Find old inactive conversations
            var oldConversations = await _context.AIConversations
                .Where(c => !c.IsActive && c.LastMessageAt.HasValue && c.LastMessageAt < cutoffDate)
                .ToListAsync(ct);

            if (oldConversations.Count == 0)
                return 0;

            // 2. Delete MongoDB documents
            var mongoDocIds = oldConversations
                .Where(c => !string.IsNullOrEmpty(c.MongoDocumentId))
                .Select(c => ObjectId.Parse(c.MongoDocumentId!))
                .ToList();

            if (mongoDocIds.Any())
            {
                var filter = Builders<BsonDocument>.Filter.In("_id", mongoDocIds);
                await _mongoCollection.DeleteManyAsync(filter, ct);
            }

            // 3. Delete SQL Server metadata
            _context.AIConversations.RemoveRange(oldConversations);
            await _context.SaveChangesAsync(ct);

            return oldConversations.Count;
        }
    }
}
