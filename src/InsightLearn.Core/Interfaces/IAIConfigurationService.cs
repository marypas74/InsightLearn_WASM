using InsightLearn.Core.DTOs;
using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service for managing AI service configurations
/// Allows switching between OpenAI and Ollama providers
/// </summary>
public interface IAIConfigurationService
{
    /// <summary>
    /// Get all AI service configurations
    /// </summary>
    Task<List<AIServiceConfigurationDto>> GetAllConfigurationsAsync();

    /// <summary>
    /// Get configuration for a specific service type
    /// </summary>
    Task<AIServiceConfigurationDto?> GetConfigurationAsync(string serviceType);

    /// <summary>
    /// Update configuration for a service type
    /// </summary>
    Task<AIServiceConfigurationDto> UpdateConfigurationAsync(string serviceType, UpdateAIServiceConfigurationDto dto, Guid? userId = null);

    /// <summary>
    /// Remove OpenAI API key for a service
    /// </summary>
    Task<bool> RemoveOpenAIApiKeyAsync(string serviceType);

    /// <summary>
    /// Test connection to a provider
    /// </summary>
    Task<TestConnectionResultDto> TestConnectionAsync(TestConnectionRequestDto request);

    /// <summary>
    /// Get available Ollama models
    /// </summary>
    Task<List<OllamaModelDto>> GetOllamaModelsAsync(string? baseUrl = null);

    /// <summary>
    /// Get summary of all configurations
    /// </summary>
    Task<List<AIConfigurationSummaryDto>> GetConfigurationSummaryAsync();

    /// <summary>
    /// Get the active provider for a service type
    /// </summary>
    Task<string> GetActiveProviderAsync(string serviceType);

    /// <summary>
    /// Check if OpenAI API key is configured for a service
    /// </summary>
    Task<bool> HasOpenAIApiKeyAsync(string serviceType);

    /// <summary>
    /// Get decrypted OpenAI API key (for internal use only)
    /// </summary>
    Task<string?> GetOpenAIApiKeyAsync(string serviceType);

    /// <summary>
    /// Initialize default configurations if not present
    /// </summary>
    Task InitializeDefaultConfigurationsAsync();
}
