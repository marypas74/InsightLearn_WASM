using InsightLearn.Application.Interfaces;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

/// <summary>
/// Factory for creating AI service instances based on configuration
/// Allows runtime switching between OpenAI and Ollama providers
/// </summary>
public interface IAIServiceFactory
{
    /// <summary>
    /// Get the configured chat service (OpenAI GPT-4 or Ollama)
    /// </summary>
    Task<IOpenAILearningAssistantService?> GetChatServiceAsync();

    /// <summary>
    /// Get the configured transcription service (OpenAI Whisper or faster-whisper)
    /// </summary>
    Task<IWhisperTranscriptionService> GetTranscriptionServiceAsync();

    /// <summary>
    /// Get the configured Ollama service for fallback
    /// </summary>
    IOllamaService? GetOllamaService();

    /// <summary>
    /// Check if the active chat provider is available
    /// </summary>
    Task<bool> IsChatServiceAvailableAsync();

    /// <summary>
    /// Check if the active transcription provider is available
    /// </summary>
    Task<bool> IsTranscriptionServiceAvailableAsync();

    /// <summary>
    /// Get the active provider name for a service type
    /// </summary>
    Task<string> GetActiveProviderAsync(string serviceType);
}

/// <summary>
/// Implementation of AI Service Factory
/// </summary>
public class AIServiceFactory : IAIServiceFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IAIConfigurationService _configService;
    private readonly ILogger<AIServiceFactory> _logger;

    public AIServiceFactory(
        IServiceProvider serviceProvider,
        IAIConfigurationService configService,
        ILogger<AIServiceFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _configService = configService;
        _logger = logger;
    }

    public async Task<IOpenAILearningAssistantService?> GetChatServiceAsync()
    {
        var provider = await _configService.GetActiveProviderAsync(AIServiceTypes.Chat);
        _logger.LogDebug("[AIFactory] Getting chat service for provider: {Provider}", provider);

        switch (provider)
        {
            case AIProviders.OpenAI:
                // Check if API key is available
                if (!await _configService.HasOpenAIApiKeyAsync(AIServiceTypes.Chat))
                {
                    var config = await _configService.GetConfigurationAsync(AIServiceTypes.Chat);
                    if (config?.EnableFallback == true && config.FallbackProvider == AIProviders.Ollama)
                    {
                        _logger.LogWarning("[AIFactory] OpenAI API key not available, falling back to Ollama");
                        // Return null to signal that Ollama should be used via AIChatService
                        return null;
                    }
                }
                return _serviceProvider.GetService<IOpenAILearningAssistantService>();

            case AIProviders.Ollama:
                // Ollama uses the IOllamaService, not IOpenAILearningAssistantService
                // Return null to signal that the caller should use Ollama directly
                return null;

            default:
                _logger.LogWarning("[AIFactory] Unknown chat provider: {Provider}, defaulting to OpenAI", provider);
                return _serviceProvider.GetService<IOpenAILearningAssistantService>();
        }
    }

    public async Task<IWhisperTranscriptionService> GetTranscriptionServiceAsync()
    {
        var provider = await _configService.GetActiveProviderAsync(AIServiceTypes.Transcription);
        _logger.LogDebug("[AIFactory] Getting transcription service for provider: {Provider}", provider);

        switch (provider)
        {
            case AIProviders.OpenAI:
                // Check if API key is available
                if (!await _configService.HasOpenAIApiKeyAsync(AIServiceTypes.Transcription))
                {
                    var config = await _configService.GetConfigurationAsync(AIServiceTypes.Transcription);
                    if (config?.EnableFallback == true)
                    {
                        _logger.LogWarning("[AIFactory] OpenAI API key not available, falling back to faster-whisper (chunked)");
                        // v2.3.97-dev: Use IWhisperTranscriptionService which resolves to ChunkedWhisperTranscriptionService
                        return _serviceProvider.GetRequiredService<IWhisperTranscriptionService>();
                    }
                }
                return _serviceProvider.GetRequiredService<OpenAIWhisperService>();

            case AIProviders.FasterWhisper:
                // v2.3.97-dev: Use IWhisperTranscriptionService which resolves to ChunkedWhisperTranscriptionService
                return _serviceProvider.GetRequiredService<IWhisperTranscriptionService>();

            default:
                _logger.LogWarning("[AIFactory] Unknown transcription provider: {Provider}, defaulting to OpenAI", provider);
                return _serviceProvider.GetRequiredService<OpenAIWhisperService>();
        }
    }

    public IOllamaService? GetOllamaService()
    {
        return _serviceProvider.GetService<IOllamaService>();
    }

    public async Task<bool> IsChatServiceAvailableAsync()
    {
        var provider = await _configService.GetActiveProviderAsync(AIServiceTypes.Chat);

        switch (provider)
        {
            case AIProviders.OpenAI:
                return await _configService.HasOpenAIApiKeyAsync(AIServiceTypes.Chat);

            case AIProviders.Ollama:
                var ollama = _serviceProvider.GetService<IOllamaService>();
                if (ollama == null) return false;
                return await ollama.IsAvailableAsync();

            default:
                return false;
        }
    }

    public async Task<bool> IsTranscriptionServiceAvailableAsync()
    {
        var provider = await _configService.GetActiveProviderAsync(AIServiceTypes.Transcription);

        switch (provider)
        {
            case AIProviders.OpenAI:
                return await _configService.HasOpenAIApiKeyAsync(AIServiceTypes.Transcription);

            case AIProviders.FasterWhisper:
                // Check if faster-whisper service is responding
                var config = await _configService.GetConfigurationAsync(AIServiceTypes.Transcription);
                if (config?.FasterWhisperUrl == null) return false;

                var testResult = await _configService.TestConnectionAsync(new Core.DTOs.TestConnectionRequestDto
                {
                    ServiceType = AIServiceTypes.Transcription,
                    Provider = AIProviders.FasterWhisper,
                    BaseUrl = config.FasterWhisperUrl
                });
                return testResult.Success;

            default:
                return false;
        }
    }

    public async Task<string> GetActiveProviderAsync(string serviceType)
    {
        return await _configService.GetActiveProviderAsync(serviceType);
    }
}
