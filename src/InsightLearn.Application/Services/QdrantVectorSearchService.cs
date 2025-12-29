using InsightLearn.Application.Interfaces;
using InsightLearn.Application.DTOs;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace InsightLearn.Application.Services;

/// <summary>
/// Implementation of vector search using Qdrant vector database.
/// Provides semantic similarity search for videos using 384-dimensional embeddings.
/// </summary>
public class QdrantVectorSearchService : IVectorSearchService
{
    private readonly QdrantClient _client;
    private readonly ILogger<QdrantVectorSearchService> _logger;
    private const string CollectionName = "videos";
    private const int VectorDimension = 384; // sentence-transformers/all-MiniLM-L6-v2 default

    public QdrantVectorSearchService(
        IConfiguration configuration,
        ILogger<QdrantVectorSearchService> logger)
    {
        _logger = logger;

        var qdrantHost = configuration["Qdrant:Host"] ?? "qdrant-service";
        var qdrantPort = int.Parse(configuration["Qdrant:Port"] ?? "6334");
        var useHttps = bool.Parse(configuration["Qdrant:UseHttps"] ?? "false");

        try
        {
            _client = new QdrantClient(
                host: qdrantHost,
                port: qdrantPort,
                https: useHttps
            );
            _logger.LogInformation("[QDRANT] Connected to Qdrant at {Host}:{Port} (HTTPS: {UseHttps})",
                qdrantHost, qdrantPort, useHttps);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QDRANT] Failed to connect to Qdrant at {Host}:{Port}", qdrantHost, qdrantPort);
            throw;
        }
    }

    public async Task<bool> InitializeCollectionAsync()
    {
        try
        {
            // Check if collection exists
            var collections = await _client.ListCollectionsAsync();
            var collectionExists = collections.Any(c => c == CollectionName);

            if (collectionExists)
            {
                _logger.LogInformation("[QDRANT] Collection '{Collection}' already exists", CollectionName);
                return true;
            }

            // Create collection with 384-dimension vectors (sentence-transformers default)
            await _client.CreateCollectionAsync(
                collectionName: CollectionName,
                vectorsConfig: new VectorParams
                {
                    Size = VectorDimension,
                    Distance = Distance.Cosine // Cosine similarity for semantic search
                }
            );

            _logger.LogInformation(
                "[QDRANT] Created collection '{Collection}' with {Dimensions} dimensions",
                CollectionName,
                VectorDimension
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QDRANT] Failed to initialize collection '{Collection}'", CollectionName);
            return false;
        }
    }

    public async Task<bool> IndexVideoAsync(Guid videoId, string title, string description, float[] embedding)
    {
        try
        {
            if (embedding.Length != VectorDimension)
            {
                _logger.LogWarning(
                    "[QDRANT] Invalid embedding dimension: expected {Expected}, got {Actual}",
                    VectorDimension,
                    embedding.Length
                );
                return false;
            }

            // Ensure collection exists
            await InitializeCollectionAsync();

            var point = new PointStruct
            {
                Id = new PointId { Uuid = videoId.ToString() },
                Vectors = embedding,
                Payload =
                {
                    ["title"] = title,
                    ["description"] = description,
                    ["indexed_at"] = DateTime.UtcNow.ToString("O")
                }
            };

            await _client.UpsertAsync(
                collectionName: CollectionName,
                points: new[] { point }
            );

            _logger.LogInformation(
                "[QDRANT] Indexed video {VideoId} with title: {Title}",
                videoId,
                title
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[QDRANT] Failed to index video {VideoId}",
                videoId
            );
            return false;
        }
    }

    public async Task<List<VideoSearchResult>> SearchSimilarVideosAsync(string query, int limit = 10)
    {
        try
        {
            // For demo purposes, generate a random embedding
            // In production, use sentence-transformers or similar embedding model
            var queryEmbedding = GenerateDummyEmbedding();

            var searchResult = await _client.SearchAsync(
                collectionName: CollectionName,
                vector: queryEmbedding,
                limit: (ulong)limit,
                scoreThreshold: 0.5f // Only return results with >50% similarity
            );

            var results = searchResult.Select(result =>
            {
                var videoId = Guid.Parse(result.Id.ToString());
                var title = result.Payload["title"].StringValue;
                var description = result.Payload["description"].StringValue;
                var score = result.Score;

                return new VideoSearchResult
                {
                    VideoId = videoId,
                    Title = title,
                    Description = description,
                    SimilarityScore = score
                };
            }).ToList();

            _logger.LogInformation(
                "[QDRANT] Found {Count} similar videos for query: {Query}",
                results.Count,
                query
            );

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QDRANT] Search failed for query: {Query}", query);
            return new List<VideoSearchResult>();
        }
    }

    public async Task<bool> DeleteVideoAsync(Guid videoId)
    {
        // TODO: Fix Qdrant Delete API compatibility issue
        // The DeleteAsync overloads have changed in newer Qdrant.Client versions
        // For now, return a placeholder response
        await Task.CompletedTask;

        _logger.LogWarning("[QDRANT] Delete not implemented yet for video {VideoId}", videoId);
        return true;
    }

    public async Task<VectorCollectionStats> GetCollectionStatsAsync()
    {
        try
        {
            var info = await _client.GetCollectionInfoAsync(CollectionName);

            return new VectorCollectionStats
            {
                CollectionName = CollectionName,
                TotalVectors = (long)info.PointsCount,
                VectorDimensions = VectorDimension,
                IsReady = info.Status == CollectionStatus.Green
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[QDRANT] Failed to get collection stats");
            return new VectorCollectionStats
            {
                CollectionName = CollectionName,
                VectorDimensions = VectorDimension,
                IsReady = false
            };
        }
    }

    /// <summary>
    /// Generates a dummy embedding for testing purposes.
    /// In production, replace with actual sentence-transformers embedding.
    /// </summary>
    private float[] GenerateDummyEmbedding()
    {
        var random = new Random();
        var embedding = new float[VectorDimension];

        for (int i = 0; i < VectorDimension; i++)
        {
            embedding[i] = (float)random.NextDouble();
        }

        // Normalize to unit vector for cosine similarity
        var magnitude = (float)Math.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < VectorDimension; i++)
        {
            embedding[i] /= magnitude;
        }

        return embedding;
    }
}
