using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

/// <summary>
/// Configuration for AI services (Chat, Transcription, Translation)
/// Allows admin to switch between OpenAI and Ollama providers
/// </summary>
[Table("AIServiceConfigurations")]
public class AIServiceConfiguration
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of AI service: "Chat", "Transcription", "Translation"
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ServiceType { get; set; } = string.Empty;

    /// <summary>
    /// Active provider: "OpenAI", "Ollama", "FasterWhisper", "Azure"
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ActiveProvider { get; set; } = "OpenAI";

    /// <summary>
    /// OpenAI API Key (encrypted in database)
    /// </summary>
    [StringLength(500)]
    public string? OpenAIApiKey { get; set; }

    /// <summary>
    /// OpenAI Model name (e.g., gpt-4-turbo-preview, whisper-1)
    /// </summary>
    [StringLength(100)]
    public string? OpenAIModel { get; set; }

    /// <summary>
    /// Ollama base URL (e.g., http://ollama-service:11434)
    /// </summary>
    [StringLength(255)]
    public string? OllamaBaseUrl { get; set; }

    /// <summary>
    /// Ollama model name (e.g., qwen2.5:3b, llama2)
    /// </summary>
    [StringLength(100)]
    public string? OllamaModel { get; set; }

    /// <summary>
    /// faster-whisper service URL (e.g., http://faster-whisper-service:8000)
    /// </summary>
    [StringLength(255)]
    public string? FasterWhisperUrl { get; set; }

    /// <summary>
    /// faster-whisper model (e.g., base, small, medium, large)
    /// </summary>
    [StringLength(50)]
    public string? FasterWhisperModel { get; set; }

    /// <summary>
    /// Temperature for AI responses (0.0 - 1.0)
    /// </summary>
    [Range(0.0, 2.0)]
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    [Range(10, 36000)]
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Maximum tokens for response (OpenAI)
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Whether this service is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Enable automatic fallback to alternative provider if primary fails
    /// </summary>
    public bool EnableFallback { get; set; } = true;

    /// <summary>
    /// Fallback provider if primary fails
    /// </summary>
    [StringLength(50)]
    public string? FallbackProvider { get; set; }

    /// <summary>
    /// Last successful connection test timestamp
    /// </summary>
    public DateTime? LastConnectionTest { get; set; }

    /// <summary>
    /// Connection test result
    /// </summary>
    public bool? ConnectionTestSuccess { get; set; }

    /// <summary>
    /// Last error message from connection test
    /// </summary>
    [StringLength(1000)]
    public string? LastErrorMessage { get; set; }

    // ============ Parallel Transcription Settings (v2.3.67-dev) ============

    /// <summary>
    /// Enable parallel transcription using both providers simultaneously.
    /// Only applies to Transcription service type.
    /// </summary>
    public bool EnableParallelTranscription { get; set; } = false;

    /// <summary>
    /// Chunk duration in seconds for parallel transcription (10-120).
    /// Shorter = lower latency, longer = better context.
    /// </summary>
    [Range(10, 120)]
    public int ParallelChunkDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Overlap between chunks in seconds to prevent word cutoff.
    /// </summary>
    [Range(0, 10)]
    public int ParallelChunkOverlapSeconds { get; set; } = 2;

    /// <summary>
    /// Distribution strategy for parallel chunks: RoundRobin, FirstAvailable, LeastLoaded, Racing
    /// </summary>
    [StringLength(50)]
    public string ParallelDistributionStrategy { get; set; } = "RoundRobin";

    /// <summary>
    /// Maximum parallel chunks per provider (1-5).
    /// </summary>
    [Range(1, 5)]
    public int ParallelMaxChunksPerProvider { get; set; } = 2;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who last updated this configuration
    /// </summary>
    public Guid? UpdatedBy { get; set; }
}

/// <summary>
/// AI Service types enumeration
/// </summary>
public static class AIServiceTypes
{
    public const string Chat = "Chat";
    public const string Transcription = "Transcription";
    public const string Translation = "Translation";
}

/// <summary>
/// AI Provider types enumeration
/// </summary>
public static class AIProviders
{
    public const string OpenAI = "OpenAI";
    public const string Ollama = "Ollama";
    public const string FasterWhisper = "FasterWhisper";
    public const string Azure = "Azure";
}
