using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service for hybrid search combining MongoDB full-text and Qdrant semantic search
/// Provides best-of-both-worlds search for video transcriptions and translations
/// </summary>
public interface IHybridSearchService
{
    /// <summary>
    /// Search transcriptions using hybrid approach (MongoDB + Qdrant)
    /// </summary>
    /// <param name="query">Search query text</param>
    /// <param name="lessonId">Optional: limit to specific lesson</param>
    /// <param name="language">Optional: filter by language</param>
    /// <param name="limit">Maximum number of results</param>
    /// <returns>Ranked search results</returns>
    Task<List<SearchResult>> SearchAsync(string query, Guid? lessonId = null, string? language = null, int limit = 20);

    /// <summary>
    /// Search only in MongoDB (keyword-based full-text search)
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="lessonId">Optional lesson filter</param>
    /// <param name="limit">Maximum results</param>
    /// <returns>MongoDB search results</returns>
    Task<List<SearchResult>> SearchMongoDBAsync(string query, Guid? lessonId = null, int limit = 20);

    /// <summary>
    /// Search only in Qdrant (semantic vector search)
    /// </summary>
    /// <param name="query">Search query</param>
    /// <param name="lessonId">Optional lesson filter</param>
    /// <param name="limit">Maximum results</param>
    /// <returns>Qdrant search results</returns>
    Task<List<SearchResult>> SearchQdrantAsync(string query, Guid? lessonId = null, int limit = 20);

    /// <summary>
    /// Find segments related to a specific timestamp
    /// </summary>
    /// <param name="lessonId">Lesson ID</param>
    /// <param name="timestampSeconds">Timestamp in seconds</param>
    /// <param name="windowSeconds">Window around timestamp (default: 30s)</param>
    /// <returns>Segments within time window</returns>
    Task<List<SearchResult>> FindByTimestampAsync(Guid lessonId, double timestampSeconds, double windowSeconds = 30);

    /// <summary>
    /// Get RAG context for AI chatbot
    /// Retrieves relevant segments for augmenting chatbot responses
    /// </summary>
    /// <param name="query">User question</param>
    /// <param name="lessonId">Current lesson context</param>
    /// <param name="limit">Number of context segments</param>
    /// <returns>Relevant context segments</returns>
    Task<string> GetRAGContextAsync(string query, Guid lessonId, int limit = 5);
}

/// <summary>
/// Search result with hybrid scoring
/// </summary>
public class SearchResult
{
    public Guid LessonId { get; set; }
    public string Language { get; set; } = string.Empty;
    public int SegmentIndex { get; set; }
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? TranslatedText { get; set; }

    /// <summary>
    /// Hybrid score combining MongoDB relevance and Qdrant similarity
    /// Range: 0.0 - 1.0
    /// </summary>
    public float Score { get; set; }

    /// <summary>
    /// MongoDB text search score (if applicable)
    /// </summary>
    public float? MongoScore { get; set; }

    /// <summary>
    /// Qdrant vector similarity score (if applicable)
    /// </summary>
    public float? QdrantScore { get; set; }

    /// <summary>
    /// Source of the result
    /// </summary>
    public SearchSource Source { get; set; }
}

/// <summary>
/// Source of search result
/// </summary>
public enum SearchSource
{
    MongoDB,
    Qdrant,
    Hybrid
}
