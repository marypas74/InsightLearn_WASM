using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace InsightLearn.Application.Services;

/// <summary>
/// Embedding generation service using Ollama nomic-embed-text
/// Generates 384-dimensional vectors for Qdrant semantic search
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly QdrantClient _qdrantClient;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly string _ollamaUrl;
    private readonly string _embeddingModel = "nomic-embed-text:latest";
    private readonly string _collectionName = "video_transcriptions";

    public EmbeddingService(
        HttpClient httpClient,
        QdrantClient qdrantClient,
        IConfiguration configuration,
        ILogger<EmbeddingService> logger)
    {
        _httpClient = httpClient;
        _qdrantClient = qdrantClient;
        _logger = logger;
        _ollamaUrl = configuration["Ollama:BaseUrl"] ?? "http://ollama-service:11434";
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        try
        {
            _logger.LogDebug("[Embedding] Generating embedding for text: {Text}",
                text.Substring(0, Math.Min(text.Length, 50)));

            var request = new { model = _embeddingModel, prompt = text };
            var response = await _httpClient.PostAsJsonAsync($"{_ollamaUrl}/api/embeddings", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();
            var embedding = result?.Embedding ?? throw new Exception("No embedding returned from Ollama");

            _logger.LogDebug("[Embedding] Generated {Dimensions}-dimensional embedding", embedding.Length);

            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Embedding] Failed to generate embedding for text: {Text}",
                text.Substring(0, Math.Min(text.Length, 50)));
            throw;
        }
    }

    public async Task<List<float[]>> GenerateEmbeddingsBatchAsync(List<string> texts)
    {
        _logger.LogInformation("[Embedding] Generating embeddings for {Count} texts", texts.Count);

        var embeddings = new List<float[]>();
        foreach (var text in texts)
        {
            var embedding = await GenerateEmbeddingAsync(text);
            embeddings.Add(embedding);

            // Small delay to avoid overwhelming Ollama
            await Task.Delay(50);
        }

        return embeddings;
    }

    public async Task<int> IndexTranscriptionAsync(Guid lessonId, List<TranscriptionSegment> segments)
    {
        _logger.LogInformation("[Embedding] Indexing {Count} transcription segments for lesson {LessonId}",
            segments.Count, lessonId);

        try
        {
            // Ensure collection exists
            await EnsureCollectionExistsAsync();

            var points = new List<PointStruct>();
            ulong pointId = (ulong)DateTime.UtcNow.Ticks; // Use timestamp as base ID

            foreach (var segment in segments)
            {
                // Generate embedding for segment text
                var embedding = await GenerateEmbeddingAsync(segment.Text);

                // Create Qdrant point
                var point = new PointStruct
                {
                    Id = new PointId { Num = pointId++ },
                    Vectors = embedding,
                    Payload =
                    {
                        ["lesson_id"] = lessonId.ToString(),
                        ["segment_index"] = segment.Index,
                        ["start_seconds"] = segment.StartSeconds,
                        ["end_seconds"] = segment.EndSeconds,
                        ["text"] = segment.Text,
                        ["confidence"] = segment.Confidence,
                        ["type"] = "transcription",
                        ["language"] = "original"
                    }
                };

                points.Add(point);

                // Batch insert every 50 points
                if (points.Count >= 50)
                {
                    await _qdrantClient.UpsertAsync(_collectionName, points);
                    _logger.LogDebug("[Embedding] Indexed batch of {Count} points", points.Count);
                    points.Clear();
                }
            }

            // Insert remaining points
            if (points.Count > 0)
            {
                await _qdrantClient.UpsertAsync(_collectionName, points);
                _logger.LogDebug("[Embedding] Indexed final batch of {Count} points", points.Count);
            }

            _logger.LogInformation("[Embedding] Successfully indexed {Count} segments for lesson {LessonId}",
                segments.Count, lessonId);

            return segments.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Embedding] Failed to index transcription for lesson {LessonId}", lessonId);
            throw;
        }
    }

    public async Task<int> IndexTranslationAsync(Guid lessonId, string language, List<TranslatedSegment> segments)
    {
        _logger.LogInformation("[Embedding] Indexing {Count} translated segments (language: {Language}) for lesson {LessonId}",
            segments.Count, language, lessonId);

        try
        {
            await EnsureCollectionExistsAsync();

            var points = new List<PointStruct>();
            ulong pointId = (ulong)DateTime.UtcNow.Ticks;

            foreach (var segment in segments)
            {
                // Generate embedding for TRANSLATED text
                var embedding = await GenerateEmbeddingAsync(segment.TranslatedText);

                var point = new PointStruct
                {
                    Id = new PointId { Num = pointId++ },
                    Vectors = embedding,
                    Payload =
                    {
                        ["lesson_id"] = lessonId.ToString(),
                        ["segment_index"] = segment.Index,
                        ["start_seconds"] = segment.StartSeconds,
                        ["end_seconds"] = segment.EndSeconds,
                        ["text"] = segment.TranslatedText,
                        ["original_text"] = segment.OriginalText,
                        ["quality"] = segment.Quality,
                        ["type"] = "translation",
                        ["language"] = language
                    }
                };

                points.Add(point);

                if (points.Count >= 50)
                {
                    await _qdrantClient.UpsertAsync(_collectionName, points);
                    points.Clear();
                }
            }

            if (points.Count > 0)
            {
                await _qdrantClient.UpsertAsync(_collectionName, points);
            }

            _logger.LogInformation("[Embedding] Successfully indexed {Count} translated segments", segments.Count);
            return segments.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Embedding] Failed to index translation for lesson {LessonId}", lessonId);
            throw;
        }
    }

    public async Task<EmbeddingModelInfo> GetModelInfoAsync()
    {
        return new EmbeddingModelInfo
        {
            ModelName = _embeddingModel,
            Dimensions = 384,
            MaxTokens = 8192,
            Version = "latest"
        };
    }

    /// <summary>
    /// Ensure Qdrant collection exists with correct configuration
    /// </summary>
    private async Task EnsureCollectionExistsAsync()
    {
        try
        {
            var collections = await _qdrantClient.ListCollectionsAsync();
            if (collections.Any(c => c == _collectionName))
            {
                _logger.LogDebug("[Embedding] Collection '{Collection}' already exists", _collectionName);
                return;
            }

            _logger.LogInformation("[Embedding] Creating collection '{Collection}'", _collectionName);

            await _qdrantClient.CreateCollectionAsync(
                collectionName: _collectionName,
                vectorsConfig: new VectorParams
                {
                    Size = 384, // nomic-embed-text dimensions
                    Distance = Distance.Cosine
                });

            // Create index for faster filtering
            await _qdrantClient.CreatePayloadIndexAsync(
                collectionName: _collectionName,
                fieldName: "lesson_id",
                schemaType: PayloadSchemaType.Keyword);

            _logger.LogInformation("[Embedding] Collection '{Collection}' created successfully", _collectionName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Embedding] Failed to ensure collection exists");
            throw;
        }
    }

    /// <summary>
    /// Ollama embedding API response model
    /// </summary>
    private class OllamaEmbeddingResponse
    {
        public float[]? Embedding { get; set; }
    }
}
