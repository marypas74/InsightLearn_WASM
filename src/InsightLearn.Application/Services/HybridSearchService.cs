using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace InsightLearn.Application.Services;

/// <summary>
/// Hybrid search service combining MongoDB full-text and Qdrant semantic search
/// Provides best-of-both-worlds for video transcription search
/// </summary>
public class HybridSearchService : IHybridSearchService
{
    private readonly IMongoDatabase _mongoDb;
    private readonly QdrantClient _qdrantClient;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<HybridSearchService> _logger;
    private readonly string _collectionName = "video_transcriptions";
    private readonly string _mongoCollectionName = "VideoTranscripts";

    public HybridSearchService(
        IMongoDatabase mongoDb,
        QdrantClient qdrantClient,
        IEmbeddingService embeddingService,
        ILogger<HybridSearchService> logger)
    {
        _mongoDb = mongoDb;
        _qdrantClient = qdrantClient;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<List<SearchResult>> SearchAsync(
        string query,
        Guid? lessonId = null,
        string? language = null,
        int limit = 20)
    {
        _logger.LogInformation("[HybridSearch] Executing hybrid search: query='{Query}', lesson={LessonId}, language={Language}",
            query, lessonId, language);

        // Execute both searches in parallel
        var mongoTask = SearchMongoDBAsync(query, lessonId, limit);
        var qdrantTask = SearchQdrantAsync(query, lessonId, limit);

        await Task.WhenAll(mongoTask, qdrantTask);

        var mongoResults = await mongoTask;
        var qdrantResults = await qdrantTask;

        // Combine and rank results
        var combinedResults = CombineResults(mongoResults, qdrantResults, limit);

        _logger.LogInformation("[HybridSearch] Found {Count} results (MongoDB: {MongoCount}, Qdrant: {QdrantCount})",
            combinedResults.Count, mongoResults.Count, qdrantResults.Count);

        return combinedResults;
    }

    public async Task<List<SearchResult>> SearchMongoDBAsync(string query, Guid? lessonId = null, int limit = 20)
    {
        try
        {
            var collection = _mongoDb.GetCollection<MongoDB.Bson.BsonDocument>(_mongoCollectionName);

            // Build text search filter
            var filterBuilder = Builders<MongoDB.Bson.BsonDocument>.Filter;
            var filters = new List<FilterDefinition<MongoDB.Bson.BsonDocument>>
            {
                filterBuilder.Text(query)
            };

            if (lessonId.HasValue)
            {
                filters.Add(filterBuilder.Eq("lessonId", lessonId.ToString()));
            }

            var combinedFilter = filterBuilder.And(filters);

            // Execute search
            var results = await collection.Find(combinedFilter)
                .Limit(limit)
                .ToListAsync();

            var searchResults = results.Select(doc => new SearchResult
            {
                LessonId = Guid.Parse(doc["lessonId"].AsString),
                Language = doc.Contains("language") ? doc["language"].AsString : "unknown",
                SegmentIndex = doc.Contains("segmentIndex") ? doc["segmentIndex"].AsInt32 : 0,
                StartSeconds = doc.Contains("startSeconds") ? doc["startSeconds"].AsDouble : 0,
                EndSeconds = doc.Contains("endSeconds") ? doc["endSeconds"].AsDouble : 0,
                Text = doc.Contains("text") ? doc["text"].AsString : "",
                MongoScore = 1.0f, // MongoDB doesn't provide relevance score
                Source = SearchSource.MongoDB
            }).ToList();

            _logger.LogDebug("[HybridSearch] MongoDB returned {Count} results", searchResults.Count);
            return searchResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HybridSearch] MongoDB search failed");
            return new List<SearchResult>();
        }
    }

    public async Task<List<SearchResult>> SearchQdrantAsync(string query, Guid? lessonId = null, int limit = 20)
    {
        try
        {
            // Generate query embedding
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

            // Build filter
            Filter? filter = null;
            if (lessonId.HasValue)
            {
                filter = new Filter
                {
                    Must = { MatchKeyword("lesson_id", lessonId.ToString()) }
                };
            }

            // Execute semantic search
            var searchResults = await _qdrantClient.SearchAsync(
                collectionName: _collectionName,
                vector: queryEmbedding,
                filter: filter,
                limit: (ulong)limit,
                scoreThreshold: 0.5f // Minimum similarity threshold
            );

            var results = searchResults.Select(result => new SearchResult
            {
                LessonId = Guid.Parse(result.Payload["lesson_id"].StringValue),
                Language = result.Payload.ContainsKey("language") ? result.Payload["language"].StringValue : "unknown",
                SegmentIndex = (int)result.Payload["segment_index"].IntegerValue,
                StartSeconds = result.Payload["start_seconds"].DoubleValue,
                EndSeconds = result.Payload["end_seconds"].DoubleValue,
                Text = result.Payload["text"].StringValue,
                QdrantScore = result.Score,
                Source = SearchSource.Qdrant
            }).ToList();

            _logger.LogDebug("[HybridSearch] Qdrant returned {Count} results", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HybridSearch] Qdrant search failed");
            return new List<SearchResult>();
        }
    }

    public async Task<List<SearchResult>> FindByTimestampAsync(
        Guid lessonId,
        double timestampSeconds,
        double windowSeconds = 30)
    {
        _logger.LogInformation("[HybridSearch] Finding segments at timestamp {Timestamp}s (±{Window}s) for lesson {LessonId}",
            timestampSeconds, windowSeconds, lessonId);

        try
        {
            var filter = new Filter
            {
                Must =
                {
                    MatchKeyword("lesson_id", lessonId.ToString()),
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "start_seconds",
                            Range = new Qdrant.Client.Grpc.Range
                            {
                                Gte = timestampSeconds - windowSeconds,
                                Lte = timestampSeconds + windowSeconds
                            }
                        }
                    }
                }
            };

            var results = await _qdrantClient.ScrollAsync(
                collectionName: _collectionName,
                filter: filter,
                limit: 20
            );

            var searchResults = results.Result.Select(point => new SearchResult
            {
                LessonId = Guid.Parse(point.Payload["lesson_id"].StringValue),
                Language = point.Payload["language"].StringValue,
                SegmentIndex = (int)point.Payload["segment_index"].IntegerValue,
                StartSeconds = point.Payload["start_seconds"].DoubleValue,
                EndSeconds = point.Payload["end_seconds"].DoubleValue,
                Text = point.Payload["text"].StringValue,
                Score = 1.0f, // Not ranked by similarity
                Source = SearchSource.Qdrant
            }).OrderBy(r => r.StartSeconds).ToList();

            _logger.LogDebug("[HybridSearch] Found {Count} segments near timestamp", searchResults.Count);
            return searchResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HybridSearch] Timestamp search failed");
            return new List<SearchResult>();
        }
    }

    public async Task<string> GetRAGContextAsync(string query, Guid lessonId, int limit = 5)
    {
        _logger.LogInformation("[HybridSearch] Getting RAG context for query: '{Query}' (lesson: {LessonId})",
            query, lessonId);

        // Use Qdrant semantic search for RAG context
        var results = await SearchQdrantAsync(query, lessonId, limit);

        if (results.Count == 0)
        {
            return "No relevant context found in the video.";
        }

        var context = new StringBuilder();
        context.AppendLine("Relevant context from the video:");
        context.AppendLine();

        foreach (var result in results.Take(limit))
        {
            context.AppendLine($"[{FormatTimestamp(result.StartSeconds)} - {FormatTimestamp(result.EndSeconds)}]");
            context.AppendLine(result.Text);
            context.AppendLine();
        }

        return context.ToString();
    }

    /// <summary>
    /// Combine MongoDB and Qdrant results with hybrid ranking
    /// </summary>
    private List<SearchResult> CombineResults(
        List<SearchResult> mongoResults,
        List<SearchResult> qdrantResults,
        int limit)
    {
        // Create a dictionary to merge results by unique key (lessonId + segmentIndex)
        var resultDict = new Dictionary<string, SearchResult>();

        // Add MongoDB results
        foreach (var result in mongoResults)
        {
            var key = $"{result.LessonId}_{result.SegmentIndex}";
            result.Score = result.MongoScore ?? 0.5f;
            resultDict[key] = result;
        }

        // Merge Qdrant results
        foreach (var result in qdrantResults)
        {
            var key = $"{result.LessonId}_{result.SegmentIndex}";

            if (resultDict.ContainsKey(key))
            {
                // Hybrid: average of MongoDB and Qdrant scores
                var existing = resultDict[key];
                existing.QdrantScore = result.QdrantScore;
                existing.Score = ((existing.MongoScore ?? 0.5f) + (result.QdrantScore ?? 0.5f)) / 2f;
                existing.Source = SearchSource.Hybrid;
            }
            else
            {
                // Qdrant-only result
                result.Score = result.QdrantScore ?? 0.5f;
                resultDict[key] = result;
            }
        }

        // Return top results sorted by hybrid score
        return resultDict.Values
            .OrderByDescending(r => r.Score)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Helper to create keyword match condition for Qdrant
    /// </summary>
    private Condition MatchKeyword(string key, string value)
    {
        return new Condition
        {
            Field = new FieldCondition
            {
                Key = key,
                Match = new Match { Keyword = value }
            }
        };
    }

    /// <summary>
    /// Format seconds to MM:SS
    /// </summary>
    private string FormatTimestamp(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return $"{ts.Minutes:D2}:{ts.Seconds:D2}";
    }
}
