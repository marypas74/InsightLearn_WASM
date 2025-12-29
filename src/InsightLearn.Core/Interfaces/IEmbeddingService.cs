using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service for generating vector embeddings using Ollama nomic-embed-text model
/// Used for semantic search in Qdrant vector database
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generate embedding vector for text
    /// </summary>
    /// <param name="text">Input text to embed</param>
    /// <returns>384-dimensional embedding vector</returns>
    Task<float[]> GenerateEmbeddingAsync(string text);

    /// <summary>
    /// Generate embeddings for multiple texts in batch
    /// </summary>
    /// <param name="texts">List of texts to embed</param>
    /// <returns>List of embedding vectors</returns>
    Task<List<float[]>> GenerateEmbeddingsBatchAsync(List<string> texts);

    /// <summary>
    /// Index transcription segments into Qdrant
    /// </summary>
    /// <param name="lessonId">Lesson ID</param>
    /// <param name="segments">Transcription segments with text and timestamps</param>
    /// <returns>Number of indexed segments</returns>
    Task<int> IndexTranscriptionAsync(Guid lessonId, List<TranscriptionSegment> segments);

    /// <summary>
    /// Index translation segments into Qdrant
    /// </summary>
    /// <param name="lessonId">Lesson ID</param>
    /// <param name="language">Target language</param>
    /// <param name="segments">Translated segments</param>
    /// <returns>Number of indexed segments</returns>
    Task<int> IndexTranslationAsync(Guid lessonId, string language, List<TranslatedSegment> segments);

    /// <summary>
    /// Get embedding model information
    /// </summary>
    Task<EmbeddingModelInfo> GetModelInfoAsync();
}

/// <summary>
/// Embedding model information
/// </summary>
public class EmbeddingModelInfo
{
    public string ModelName { get; set; } = "nomic-embed-text";
    public int Dimensions { get; set; } = 384;
    public int MaxTokens { get; set; } = 8192;
    public string Version { get; set; } = "latest";
}
