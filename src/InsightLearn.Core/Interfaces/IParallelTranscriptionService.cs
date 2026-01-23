using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service for parallel transcription using multiple providers simultaneously.
/// Splits audio into chunks and distributes them across OpenAI Whisper and faster-whisper
/// for real-time transcription without gaps.
/// Part of InsightLearn v2.3.67-dev.
/// </summary>
public interface IParallelTranscriptionService
{
    /// <summary>
    /// Transcribe video using both providers in parallel for real-time results.
    /// Audio is split into chunks and distributed between OpenAI Whisper and faster-whisper.
    /// </summary>
    /// <param name="videoStream">Video file stream</param>
    /// <param name="language">Language code (e.g., "en", "it")</param>
    /// <param name="lessonId">Lesson ID for tracking</param>
    /// <param name="options">Parallel transcription options</param>
    /// <param name="progress">Optional progress callback for real-time updates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Merged transcription result from both providers</returns>
    Task<TranscriptionResult> TranscribeVideoParallelAsync(
        Stream videoStream,
        string language,
        Guid lessonId,
        ParallelTranscriptionOptions options,
        IProgress<ParallelTranscriptionProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if parallel transcription is available (both providers configured and reachable).
    /// </summary>
    Task<ParallelTranscriptionAvailability> CheckAvailabilityAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration options for parallel transcription.
/// </summary>
public class ParallelTranscriptionOptions
{
    /// <summary>
    /// Duration of each audio chunk in seconds.
    /// Shorter chunks = lower latency, longer chunks = better context for ASR.
    /// Default: 30 seconds (good balance).
    /// </summary>
    public int ChunkDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Overlap between chunks in seconds to prevent word cutoff at boundaries.
    /// Default: 2 seconds.
    /// </summary>
    public int ChunkOverlapSeconds { get; set; } = 2;

    /// <summary>
    /// Distribution strategy for chunks between providers.
    /// </summary>
    public ChunkDistributionStrategy DistributionStrategy { get; set; } = ChunkDistributionStrategy.RoundRobin;

    /// <summary>
    /// Maximum parallel chunks to process at once per provider.
    /// Default: 2 (4 total with 2 providers).
    /// </summary>
    public int MaxParallelChunksPerProvider { get; set; } = 2;

    /// <summary>
    /// Prefer one provider for specific chunk positions.
    /// OpenAI for odd chunks, faster-whisper for even chunks = balanced load.
    /// </summary>
    public bool LoadBalanceByChunkIndex { get; set; } = true;

    /// <summary>
    /// Enable real-time streaming of completed segments.
    /// </summary>
    public bool EnableRealTimeStreaming { get; set; } = true;

    /// <summary>
    /// Fall back to single provider if one becomes unavailable mid-processing.
    /// </summary>
    public bool EnableFallbackOnFailure { get; set; } = true;
}

/// <summary>
/// Strategy for distributing chunks between providers.
/// </summary>
public enum ChunkDistributionStrategy
{
    /// <summary>
    /// Alternate chunks between providers (chunk 0 to A, chunk 1 to B, chunk 2 to A, etc.)
    /// Best for consistent latency.
    /// </summary>
    RoundRobin,

    /// <summary>
    /// Send chunks to whichever provider finishes first.
    /// Best for throughput but may cause uneven load.
    /// </summary>
    FirstAvailable,

    /// <summary>
    /// Send chunks based on current queue length of each provider.
    /// Best for adaptive load balancing.
    /// </summary>
    LeastLoaded,

    /// <summary>
    /// Send all chunks to both providers and use the result that returns first.
    /// Doubles API cost but minimizes latency.
    /// </summary>
    Racing
}

/// <summary>
/// Progress report for parallel transcription.
/// </summary>
public class ParallelTranscriptionProgress
{
    /// <summary>
    /// Total number of chunks to process.
    /// </summary>
    public int TotalChunks { get; set; }

    /// <summary>
    /// Number of chunks completed so far.
    /// </summary>
    public int CompletedChunks { get; set; }

    /// <summary>
    /// Overall progress percentage (0-100).
    /// </summary>
    public int ProgressPercentage => TotalChunks > 0 ? (CompletedChunks * 100) / TotalChunks : 0;

    /// <summary>
    /// Chunks completed by OpenAI Whisper.
    /// </summary>
    public int OpenAIChunksCompleted { get; set; }

    /// <summary>
    /// Chunks completed by faster-whisper.
    /// </summary>
    public int FasterWhisperChunksCompleted { get; set; }

    /// <summary>
    /// Latest completed segment (for real-time display).
    /// </summary>
    public TranscriptionSegment? LatestSegment { get; set; }

    /// <summary>
    /// Current processing status message.
    /// </summary>
    public string StatusMessage { get; set; } = string.Empty;

    /// <summary>
    /// Estimated time remaining in seconds.
    /// </summary>
    public double? EstimatedSecondsRemaining { get; set; }
}

/// <summary>
/// Availability status of parallel transcription providers.
/// </summary>
public class ParallelTranscriptionAvailability
{
    /// <summary>
    /// Whether parallel transcription is available (at least one provider ready).
    /// </summary>
    public bool IsAvailable => OpenAIAvailable || FasterWhisperAvailable;

    /// <summary>
    /// Whether both providers are available for true parallel processing.
    /// </summary>
    public bool IsFullyParallel => OpenAIAvailable && FasterWhisperAvailable;

    /// <summary>
    /// OpenAI Whisper API availability.
    /// </summary>
    public bool OpenAIAvailable { get; set; }

    /// <summary>
    /// OpenAI availability details.
    /// </summary>
    public string? OpenAIStatus { get; set; }

    /// <summary>
    /// faster-whisper service availability.
    /// </summary>
    public bool FasterWhisperAvailable { get; set; }

    /// <summary>
    /// faster-whisper availability details.
    /// </summary>
    public string? FasterWhisperStatus { get; set; }

    /// <summary>
    /// Recommended strategy based on current availability.
    /// </summary>
    public string RecommendedStrategy { get; set; } = "RoundRobin";
}
