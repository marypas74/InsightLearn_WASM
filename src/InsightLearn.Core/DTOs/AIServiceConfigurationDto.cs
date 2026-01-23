using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs;

/// <summary>
/// DTO for AI Service Configuration - used for API responses
/// API Key is masked for security
/// </summary>
public class AIServiceConfigurationDto
{
    public Guid Id { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string ActiveProvider { get; set; } = string.Empty;

    /// <summary>
    /// Masked API key (shows only last 4 characters)
    /// </summary>
    public string? OpenAIApiKeyMasked { get; set; }

    /// <summary>
    /// Whether OpenAI API key is configured
    /// </summary>
    public bool HasOpenAIApiKey { get; set; }

    public string? OpenAIModel { get; set; }
    public string? OllamaBaseUrl { get; set; }
    public string? OllamaModel { get; set; }
    public string? FasterWhisperUrl { get; set; }
    public string? FasterWhisperModel { get; set; }
    public double Temperature { get; set; }
    public int TimeoutSeconds { get; set; }
    public int? MaxTokens { get; set; }
    public bool IsEnabled { get; set; }
    public bool EnableFallback { get; set; }
    public string? FallbackProvider { get; set; }
    public DateTime? LastConnectionTest { get; set; }
    public bool? ConnectionTestSuccess { get; set; }
    public string? LastErrorMessage { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ============ Parallel Transcription Settings (v2.3.67-dev) ============
    /// <summary>
    /// Enable parallel transcription using both providers simultaneously.
    /// </summary>
    public bool EnableParallelTranscription { get; set; }

    /// <summary>
    /// Chunk duration in seconds for parallel transcription.
    /// </summary>
    public int ParallelChunkDurationSeconds { get; set; }

    /// <summary>
    /// Overlap between chunks in seconds.
    /// </summary>
    public int ParallelChunkOverlapSeconds { get; set; }

    /// <summary>
    /// Distribution strategy: RoundRobin, FirstAvailable, LeastLoaded, Racing
    /// </summary>
    public string? ParallelDistributionStrategy { get; set; }

    /// <summary>
    /// Maximum parallel chunks per provider.
    /// </summary>
    public int ParallelMaxChunksPerProvider { get; set; }
}

/// <summary>
/// DTO for updating AI Service Configuration
/// </summary>
public class UpdateAIServiceConfigurationDto
{
    [Required]
    [StringLength(50)]
    public string ActiveProvider { get; set; } = string.Empty;

    /// <summary>
    /// OpenAI API Key - only set if user wants to change it
    /// Null means keep existing, empty string means remove
    /// </summary>
    [StringLength(500)]
    public string? OpenAIApiKey { get; set; }

    [StringLength(100)]
    public string? OpenAIModel { get; set; }

    [StringLength(255)]
    public string? OllamaBaseUrl { get; set; }

    [StringLength(100)]
    public string? OllamaModel { get; set; }

    [StringLength(255)]
    public string? FasterWhisperUrl { get; set; }

    [StringLength(50)]
    public string? FasterWhisperModel { get; set; }

    [Range(0.0, 2.0)]
    public double Temperature { get; set; } = 0.7;

    [Range(10, 36000)]
    public int TimeoutSeconds { get; set; } = 120;

    public int? MaxTokens { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool EnableFallback { get; set; } = true;

    [StringLength(50)]
    public string? FallbackProvider { get; set; }

    // ============ Parallel Transcription Settings (v2.3.67-dev) ============

    /// <summary>
    /// Enable parallel transcription using both providers (only for Transcription service).
    /// </summary>
    public bool EnableParallelTranscription { get; set; } = false;

    /// <summary>
    /// Chunk duration in seconds for parallel transcription.
    /// </summary>
    [Range(10, 120)]
    public int ParallelChunkDurationSeconds { get; set; } = 30;

    /// <summary>
    /// Overlap between chunks in seconds.
    /// </summary>
    [Range(0, 10)]
    public int ParallelChunkOverlapSeconds { get; set; } = 2;

    /// <summary>
    /// Distribution strategy: RoundRobin, FirstAvailable, LeastLoaded, Racing
    /// </summary>
    [StringLength(50)]
    public string? ParallelDistributionStrategy { get; set; } = "RoundRobin";

    /// <summary>
    /// Maximum parallel chunks per provider.
    /// </summary>
    [Range(1, 5)]
    public int ParallelMaxChunksPerProvider { get; set; } = 2;
}

/// <summary>
/// DTO for connection test request
/// </summary>
public class TestConnectionRequestDto
{
    [Required]
    public string ServiceType { get; set; } = string.Empty;

    [Required]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Optional API key to test (for OpenAI)
    /// If not provided, uses stored key
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Optional base URL to test (for Ollama/FasterWhisper)
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Model to test
    /// </summary>
    public string? Model { get; set; }
}

/// <summary>
/// DTO for connection test result
/// </summary>
public class TestConnectionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ResponseTimeMs { get; set; }
    public List<string>? AvailableModels { get; set; }
    public string? CurrentModel { get; set; }
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for listing all AI configurations summary
/// </summary>
public class AIConfigurationSummaryDto
{
    public string ServiceType { get; set; } = string.Empty;
    public string ActiveProvider { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool? ConnectionTestSuccess { get; set; }
    public DateTime? LastConnectionTest { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
}

/// <summary>
/// DTO for available Ollama models
/// </summary>
public class OllamaModelDto
{
    public string Name { get; set; } = string.Empty;
    public string ModifiedAt { get; set; } = string.Empty;
    public long Size { get; set; }
    public string SizeFormatted => FormatSize(Size);

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
