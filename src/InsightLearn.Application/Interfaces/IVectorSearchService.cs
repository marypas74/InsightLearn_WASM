using InsightLearn.Application.DTOs;

namespace InsightLearn.Application.Interfaces;

/// <summary>
/// Service for vector-based semantic search using Qdrant vector database.
/// Enables similarity search for videos based on content embeddings.
/// </summary>
public interface IVectorSearchService
{
    /// <summary>
    /// Indexes a video with its embedding vector for semantic search.
    /// </summary>
    /// <param name="videoId">Unique identifier for the video</param>
    /// <param name="title">Video title</param>
    /// <param name="description">Video description</param>
    /// <param name="embedding">384-dimensional embedding vector from sentence-transformers</param>
    /// <returns>True if indexing successful, false otherwise</returns>
    Task<bool> IndexVideoAsync(Guid videoId, string title, string description, float[] embedding);

    /// <summary>
    /// Searches for videos similar to the query text using semantic similarity.
    /// </summary>
    /// <param name="query">Search query text</param>
    /// <param name="limit">Maximum number of results to return (default: 10)</param>
    /// <returns>List of similar videos ordered by relevance</returns>
    Task<List<VideoSearchResult>> SearchSimilarVideosAsync(string query, int limit = 10);

    /// <summary>
    /// Removes a video from the vector index.
    /// </summary>
    /// <param name="videoId">Video ID to delete</param>
    /// <returns>True if deletion successful, false otherwise</returns>
    Task<bool> DeleteVideoAsync(Guid videoId);

    /// <summary>
    /// Gets statistics about the vector collection.
    /// </summary>
    /// <returns>Collection statistics including total vectors, dimensions, etc.</returns>
    Task<VectorCollectionStats> GetCollectionStatsAsync();

    /// <summary>
    /// Initializes the vector collection with proper schema if it doesn't exist.
    /// </summary>
    /// <returns>True if initialization successful</returns>
    Task<bool> InitializeCollectionAsync();
}

/// <summary>
/// DTO for video search results from vector similarity search.
/// </summary>
public class VideoSearchResult
{
    public Guid VideoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float SimilarityScore { get; set; }
}

/// <summary>
/// DTO for vector collection statistics.
/// </summary>
public class VectorCollectionStats
{
    public long TotalVectors { get; set; }
    public int VectorDimensions { get; set; }
    public string CollectionName { get; set; } = string.Empty;
    public bool IsReady { get; set; }
}
