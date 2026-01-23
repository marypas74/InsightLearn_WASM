using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InsightLearn.Application.Interfaces;
using InsightLearn.Core.DTOs;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using InsightLearn.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service for managing AI service configurations
/// Handles switching between OpenAI and Ollama providers with encryption and caching
/// </summary>
public class AIConfigurationService : IAIConfigurationService
{
    private readonly InsightLearnDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIConfigurationService> _logger;
    private readonly IErrorLoggingService _errorLoggingService;
    private readonly byte[] _encryptionKey;

    private const string CacheKeyPrefix = "ai_config:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    public AIConfigurationService(
        InsightLearnDbContext dbContext,
        IDistributedCache cache,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AIConfigurationService> logger,
        IErrorLoggingService errorLoggingService)
    {
        _dbContext = dbContext;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _errorLoggingService = errorLoggingService;

        // Get encryption key from environment or generate deterministic one
        var keyString = Environment.GetEnvironmentVariable("AI_CONFIG_ENCRYPTION_KEY")
            ?? configuration["Security:EncryptionKey"]
            ?? "InsightLearn-AI-Config-Key-2024"; // Fallback for development
        _encryptionKey = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
    }

    public async Task<List<AIServiceConfigurationDto>> GetAllConfigurationsAsync()
    {
        try
        {
            // Ensure table exists
            await EnsureTableExistsAsync();

            var configs = await _dbContext.AIServiceConfigurations.ToListAsync();

            if (!configs.Any())
            {
                await InitializeDefaultConfigurationsAsync();
                configs = await _dbContext.AIServiceConfigurations.ToListAsync();
            }

            return configs.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AIConfig] Error getting configurations, attempting table creation");
            await _errorLoggingService.LogErrorAsync(ex, component: "AIConfigurationService", severity: "Error",
                additionalData: JsonSerializer.Serialize(new { Method = "GetAllConfigurationsAsync" }));
            await EnsureTableExistsAsync();
            return new List<AIServiceConfigurationDto>();
        }
    }

    public async Task<AIServiceConfigurationDto?> GetConfigurationAsync(string serviceType)
    {
        // Try cache first
        var cacheKey = $"{CacheKeyPrefix}{serviceType}";
        var cached = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<AIServiceConfigurationDto>(cached);
        }

        try
        {
            // Ensure table exists
            await EnsureTableExistsAsync();

            var config = await _dbContext.AIServiceConfigurations
                .FirstOrDefaultAsync(c => c.ServiceType == serviceType);

            if (config == null)
            {
                await InitializeDefaultConfigurationsAsync();
                config = await _dbContext.AIServiceConfigurations
                    .FirstOrDefaultAsync(c => c.ServiceType == serviceType);
            }

            if (config == null) return null;

            var dto = MapToDto(config);

            // Cache the result
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration });

            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AIConfig] Error getting configuration for {ServiceType}", serviceType);
            await _errorLoggingService.LogErrorAsync(ex, component: "AIConfigurationService", severity: "Error",
                additionalData: JsonSerializer.Serialize(new { Method = "GetConfigurationAsync", ServiceType = serviceType }));
            return null;
        }
    }

    public async Task<AIServiceConfigurationDto> UpdateConfigurationAsync(
        string serviceType, UpdateAIServiceConfigurationDto dto, Guid? userId = null)
    {
        var config = await _dbContext.AIServiceConfigurations
            .FirstOrDefaultAsync(c => c.ServiceType == serviceType);

        if (config == null)
        {
            config = new AIServiceConfiguration
            {
                ServiceType = serviceType,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.AIServiceConfigurations.Add(config);
        }

        // Update fields
        config.ActiveProvider = dto.ActiveProvider;
        config.OpenAIModel = dto.OpenAIModel;
        config.OllamaBaseUrl = dto.OllamaBaseUrl;
        config.OllamaModel = dto.OllamaModel;
        config.FasterWhisperUrl = dto.FasterWhisperUrl;
        config.FasterWhisperModel = dto.FasterWhisperModel;
        config.Temperature = dto.Temperature;
        config.TimeoutSeconds = dto.TimeoutSeconds;
        config.MaxTokens = dto.MaxTokens;
        config.IsEnabled = dto.IsEnabled;
        config.EnableFallback = dto.EnableFallback;
        config.FallbackProvider = dto.FallbackProvider;
        config.UpdatedAt = DateTime.UtcNow;
        config.UpdatedBy = userId;

        // Handle API key update
        if (dto.OpenAIApiKey != null)
        {
            if (string.IsNullOrEmpty(dto.OpenAIApiKey))
            {
                // Empty string = remove key
                config.OpenAIApiKey = null;
                _logger.LogInformation("[AIConfig] OpenAI API key removed for {ServiceType}", serviceType);
            }
            else
            {
                // Encrypt and store new key
                config.OpenAIApiKey = EncryptApiKey(dto.OpenAIApiKey);
                _logger.LogInformation("[AIConfig] OpenAI API key updated for {ServiceType}", serviceType);
            }
        }

        await _dbContext.SaveChangesAsync();

        // Invalidate cache
        await InvalidateCacheAsync(serviceType);

        _logger.LogInformation("[AIConfig] Configuration updated for {ServiceType}: Provider={Provider}",
            serviceType, dto.ActiveProvider);

        return MapToDto(config);
    }

    public async Task<bool> RemoveOpenAIApiKeyAsync(string serviceType)
    {
        var config = await _dbContext.AIServiceConfigurations
            .FirstOrDefaultAsync(c => c.ServiceType == serviceType);

        if (config == null) return false;

        config.OpenAIApiKey = null;
        config.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        await InvalidateCacheAsync(serviceType);

        _logger.LogInformation("[AIConfig] OpenAI API key removed for {ServiceType}", serviceType);
        return true;
    }

    public async Task<TestConnectionResultDto> TestConnectionAsync(TestConnectionRequestDto request)
    {
        var sw = Stopwatch.StartNew();
        var result = new TestConnectionResultDto();

        // Ensure table and columns exist before any DB operations (v2.3.71-dev fix)
        await EnsureTableExistsAsync();

        try
        {
            switch (request.Provider)
            {
                case AIProviders.OpenAI:
                    result = await TestOpenAIConnectionAsync(request);
                    break;

                case AIProviders.Ollama:
                    result = await TestOllamaConnectionAsync(request);
                    break;

                case AIProviders.FasterWhisper:
                    result = await TestFasterWhisperConnectionAsync(request);
                    break;

                default:
                    result.Success = false;
                    result.Message = $"Unknown provider: {request.Provider}";
                    break;
            }

            // Update last connection test in database
            var config = await _dbContext.AIServiceConfigurations
                .FirstOrDefaultAsync(c => c.ServiceType == request.ServiceType);

            if (config != null)
            {
                config.LastConnectionTest = DateTime.UtcNow;
                config.ConnectionTestSuccess = result.Success;
                config.LastErrorMessage = result.Success ? null : result.Message;
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = $"Connection test failed: {ex.Message}";
            _logger.LogError(ex, "[AIConfig] Connection test failed for {Provider}", request.Provider);
            await _errorLoggingService.LogErrorAsync(ex, component: "AIConfigurationService", severity: "Error",
                additionalData: JsonSerializer.Serialize(new { Method = "TestConnectionAsync", Provider = request.Provider, ServiceType = request.ServiceType }));
        }

        result.ResponseTimeMs = (int)sw.ElapsedMilliseconds;
        return result;
    }

    public async Task<List<OllamaModelDto>> GetOllamaModelsAsync(string? baseUrl = null)
    {
        var url = baseUrl ?? _configuration["Ollama:BaseUrl"] ?? "http://ollama-service:11434";

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync($"{url.TrimEnd('/')}/api/tags");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(json);

            return modelsResponse?.Models?.Select(m => new OllamaModelDto
            {
                Name = m.Name,
                ModifiedAt = m.ModifiedAt,
                Size = m.Size
            }).ToList() ?? new List<OllamaModelDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AIConfig] Failed to get Ollama models from {Url}", url);
            _ = _errorLoggingService.LogErrorAsync(ex, component: "AIConfigurationService", severity: "Warning",
                additionalData: JsonSerializer.Serialize(new { Method = "GetOllamaModelsAsync", BaseUrl = url }));
            return new List<OllamaModelDto>();
        }
    }

    public async Task<List<AIConfigurationSummaryDto>> GetConfigurationSummaryAsync()
    {
        var configs = await _dbContext.AIServiceConfigurations.ToListAsync();

        if (!configs.Any())
        {
            await InitializeDefaultConfigurationsAsync();
            configs = await _dbContext.AIServiceConfigurations.ToListAsync();
        }

        return configs.Select(c => new AIConfigurationSummaryDto
        {
            ServiceType = c.ServiceType,
            ActiveProvider = c.ActiveProvider,
            IsEnabled = c.IsEnabled,
            ConnectionTestSuccess = c.ConnectionTestSuccess,
            LastConnectionTest = c.LastConnectionTest,
            StatusMessage = GetStatusMessage(c)
        }).ToList();
    }

    public async Task<string> GetActiveProviderAsync(string serviceType)
    {
        var config = await GetConfigurationAsync(serviceType);
        return config?.ActiveProvider ?? AIProviders.OpenAI;
    }

    public async Task<bool> HasOpenAIApiKeyAsync(string serviceType)
    {
        var config = await _dbContext.AIServiceConfigurations
            .FirstOrDefaultAsync(c => c.ServiceType == serviceType);

        return !string.IsNullOrEmpty(config?.OpenAIApiKey);
    }

    public async Task<string?> GetOpenAIApiKeyAsync(string serviceType)
    {
        var config = await _dbContext.AIServiceConfigurations
            .FirstOrDefaultAsync(c => c.ServiceType == serviceType);

        if (string.IsNullOrEmpty(config?.OpenAIApiKey))
        {
            // Fallback to appsettings or environment
            return _configuration["OpenAI:ApiKey"]
                ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        }

        return DecryptApiKey(config.OpenAIApiKey);
    }

    public async Task InitializeDefaultConfigurationsAsync()
    {
        var existingTypes = await _dbContext.AIServiceConfigurations
            .Select(c => c.ServiceType)
            .ToListAsync();

        var defaultConfigs = new List<AIServiceConfiguration>();

        // Chat configuration
        if (!existingTypes.Contains(AIServiceTypes.Chat))
        {
            defaultConfigs.Add(new AIServiceConfiguration
            {
                ServiceType = AIServiceTypes.Chat,
                ActiveProvider = AIProviders.OpenAI,
                OpenAIModel = _configuration["OpenAI:ChatModel"] ?? "gpt-4-turbo-preview",
                OllamaBaseUrl = _configuration["Ollama:BaseUrl"] ?? "http://ollama-service:11434",
                OllamaModel = _configuration["Ollama:Model"] ?? "qwen2.5:3b",
                Temperature = 0.7,
                TimeoutSeconds = 120,
                IsEnabled = true,
                EnableFallback = true,
                FallbackProvider = AIProviders.Ollama
            });
        }

        // Transcription configuration
        if (!existingTypes.Contains(AIServiceTypes.Transcription))
        {
            defaultConfigs.Add(new AIServiceConfiguration
            {
                ServiceType = AIServiceTypes.Transcription,
                ActiveProvider = AIProviders.OpenAI,
                OpenAIModel = _configuration["OpenAI:WhisperModel"] ?? "whisper-1",
                FasterWhisperUrl = _configuration["Whisper:BaseUrl"] ?? "http://faster-whisper-service:8000",
                FasterWhisperModel = _configuration["Whisper:Model"] ?? "base",
                TimeoutSeconds = 12000, // 200 minutes for long videos
                IsEnabled = true,
                EnableFallback = true,
                FallbackProvider = AIProviders.FasterWhisper
            });
        }

        // Translation configuration
        if (!existingTypes.Contains(AIServiceTypes.Translation))
        {
            defaultConfigs.Add(new AIServiceConfiguration
            {
                ServiceType = AIServiceTypes.Translation,
                ActiveProvider = AIProviders.OpenAI,
                OpenAIModel = "gpt-4-turbo-preview",
                OllamaBaseUrl = _configuration["Ollama:BaseUrl"] ?? "http://ollama-service:11434",
                OllamaModel = _configuration["Ollama:Model"] ?? "qwen2.5:3b",
                Temperature = 0.3,
                TimeoutSeconds = 120,
                IsEnabled = true,
                EnableFallback = true,
                FallbackProvider = AIProviders.Ollama
            });
        }

        if (defaultConfigs.Any())
        {
            _dbContext.AIServiceConfigurations.AddRange(defaultConfigs);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("[AIConfig] Initialized {Count} default AI configurations", defaultConfigs.Count);
        }
    }

    #region Private Helper Methods

    private AIServiceConfigurationDto MapToDto(AIServiceConfiguration config)
    {
        return new AIServiceConfigurationDto
        {
            Id = config.Id,
            ServiceType = config.ServiceType,
            ActiveProvider = config.ActiveProvider,
            HasOpenAIApiKey = !string.IsNullOrEmpty(config.OpenAIApiKey),
            OpenAIApiKeyMasked = MaskApiKey(config.OpenAIApiKey),
            OpenAIModel = config.OpenAIModel,
            OllamaBaseUrl = config.OllamaBaseUrl,
            OllamaModel = config.OllamaModel,
            FasterWhisperUrl = config.FasterWhisperUrl,
            FasterWhisperModel = config.FasterWhisperModel,
            Temperature = config.Temperature,
            TimeoutSeconds = config.TimeoutSeconds,
            MaxTokens = config.MaxTokens,
            IsEnabled = config.IsEnabled,
            EnableFallback = config.EnableFallback,
            FallbackProvider = config.FallbackProvider,
            LastConnectionTest = config.LastConnectionTest,
            ConnectionTestSuccess = config.ConnectionTestSuccess,
            LastErrorMessage = config.LastErrorMessage,
            UpdatedAt = config.UpdatedAt,
            // Parallel transcription settings (v2.3.67-dev)
            EnableParallelTranscription = config.EnableParallelTranscription,
            ParallelChunkDurationSeconds = config.ParallelChunkDurationSeconds,
            ParallelChunkOverlapSeconds = config.ParallelChunkOverlapSeconds,
            ParallelDistributionStrategy = config.ParallelDistributionStrategy,
            ParallelMaxChunksPerProvider = config.ParallelMaxChunksPerProvider
        };
    }

    private string? MaskApiKey(string? encryptedKey)
    {
        if (string.IsNullOrEmpty(encryptedKey)) return null;

        try
        {
            var decrypted = DecryptApiKey(encryptedKey);
            if (string.IsNullOrEmpty(decrypted)) return null;

            if (decrypted.Length <= 8) return "****";
            return $"****{decrypted[^4..]}";
        }
        catch
        {
            return "****";
        }
    }

    private string EncryptApiKey(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _encryptionKey;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Combine IV + encrypted data
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    private string? DecryptApiKey(string encryptedBase64)
    {
        try
        {
            var combined = Convert.FromBase64String(encryptedBase64);

            using var aes = Aes.Create();
            aes.Key = _encryptionKey;

            var iv = new byte[aes.IV.Length];
            var encrypted = new byte[combined.Length - iv.Length];
            Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(combined, iv.Length, encrypted, 0, encrypted.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var decryptedBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AIConfig] Failed to decrypt API key");
            _ = _errorLoggingService.LogErrorAsync(ex, component: "AIConfigurationService", severity: "Warning",
                additionalData: JsonSerializer.Serialize(new { Method = "DecryptApiKey" }));
            return null;
        }
    }

    private async Task InvalidateCacheAsync(string serviceType)
    {
        await _cache.RemoveAsync($"{CacheKeyPrefix}{serviceType}");
    }

    private static string GetStatusMessage(AIServiceConfiguration config)
    {
        if (!config.IsEnabled) return "Disabled";
        if (config.ConnectionTestSuccess == null) return "Not tested";
        if (config.ConnectionTestSuccess == true) return "Connected";
        return config.LastErrorMessage ?? "Connection failed";
    }

    private async Task<TestConnectionResultDto> TestOpenAIConnectionAsync(TestConnectionRequestDto request)
    {
        var apiKey = request.ApiKey ?? await GetOpenAIApiKeyAsync(request.ServiceType);

        if (string.IsNullOrEmpty(apiKey))
        {
            return new TestConnectionResultDto
            {
                Success = false,
                Message = "OpenAI API key not configured"
            };
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var response = await client.GetAsync("https://api.openai.com/v1/models");

            if (response.IsSuccessStatusCode)
            {
                return new TestConnectionResultDto
                {
                    Success = true,
                    Message = "OpenAI connection successful",
                    CurrentModel = request.Model ?? "gpt-4-turbo-preview"
                };
            }

            var error = await response.Content.ReadAsStringAsync();
            return new TestConnectionResultDto
            {
                Success = false,
                Message = $"OpenAI API error: {response.StatusCode} - {error}"
            };
        }
        catch (Exception ex)
        {
            _ = _errorLoggingService.LogErrorAsync(ex, component: "AIConfigurationService", severity: "Warning",
                additionalData: JsonSerializer.Serialize(new { Method = "TestOpenAIConnectionAsync", ServiceType = request.ServiceType }));
            return new TestConnectionResultDto
            {
                Success = false,
                Message = $"Connection failed: {ex.Message}"
            };
        }
    }

    private async Task<TestConnectionResultDto> TestOllamaConnectionAsync(TestConnectionRequestDto request)
    {
        var baseUrl = request.BaseUrl ?? _configuration["Ollama:BaseUrl"] ?? "http://ollama-service:11434";

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync($"{baseUrl.TrimEnd('/')}/api/tags");

            if (response.IsSuccessStatusCode)
            {
                var models = await GetOllamaModelsAsync(baseUrl);
                return new TestConnectionResultDto
                {
                    Success = true,
                    Message = $"Ollama connection successful ({models.Count} models available)",
                    AvailableModels = models.Select(m => m.Name).ToList(),
                    CurrentModel = request.Model ?? models.FirstOrDefault()?.Name
                };
            }

            return new TestConnectionResultDto
            {
                Success = false,
                Message = $"Ollama not responding: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _ = _errorLoggingService.LogErrorAsync(ex, component: "AIConfigurationService", severity: "Warning",
                additionalData: JsonSerializer.Serialize(new { Method = "TestOllamaConnectionAsync", BaseUrl = baseUrl }));
            return new TestConnectionResultDto
            {
                Success = false,
                Message = $"Connection failed: {ex.Message}"
            };
        }
    }

    private async Task<TestConnectionResultDto> TestFasterWhisperConnectionAsync(TestConnectionRequestDto request)
    {
        var baseUrl = request.BaseUrl ?? _configuration["Whisper:BaseUrl"] ?? "http://faster-whisper-service:8000";

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync($"{baseUrl.TrimEnd('/')}/v1/models");

            if (response.IsSuccessStatusCode)
            {
                return new TestConnectionResultDto
                {
                    Success = true,
                    Message = "faster-whisper connection successful",
                    CurrentModel = request.Model ?? "base"
                };
            }

            return new TestConnectionResultDto
            {
                Success = false,
                Message = $"faster-whisper not responding: {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _ = _errorLoggingService.LogErrorAsync(ex, component: "AIConfigurationService", severity: "Warning",
                additionalData: JsonSerializer.Serialize(new { Method = "TestFasterWhisperConnectionAsync", BaseUrl = baseUrl }));
            return new TestConnectionResultDto
            {
                Success = false,
                Message = $"Connection failed: {ex.Message}"
            };
        }
    }

    #endregion

    /// <summary>
    /// Ensures the AIServiceConfigurations table exists in the database.
    /// Creates it if missing using raw SQL, and adds new columns if they don't exist.
    /// Updated in v2.3.67-dev to include parallel transcription columns.
    /// </summary>
    private async Task EnsureTableExistsAsync()
    {
        try
        {
            // Check if table exists and create if not
            await _dbContext.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AIServiceConfigurations')
                BEGIN
                    CREATE TABLE [dbo].[AIServiceConfigurations] (
                        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
                        [ServiceType] NVARCHAR(50) NOT NULL,
                        [ActiveProvider] NVARCHAR(50) NOT NULL DEFAULT 'OpenAI',
                        [OpenAIApiKey] NVARCHAR(MAX) NULL,
                        [OpenAIModel] NVARCHAR(100) NULL,
                        [OllamaBaseUrl] NVARCHAR(500) NULL,
                        [OllamaModel] NVARCHAR(100) NULL,
                        [FasterWhisperUrl] NVARCHAR(500) NULL,
                        [FasterWhisperModel] NVARCHAR(100) NULL,
                        [Temperature] FLOAT NOT NULL DEFAULT 0.7,
                        [TimeoutSeconds] INT NOT NULL DEFAULT 120,
                        [MaxTokens] INT NULL,
                        [IsEnabled] BIT NOT NULL DEFAULT 1,
                        [EnableFallback] BIT NOT NULL DEFAULT 1,
                        [FallbackProvider] NVARCHAR(50) NULL,
                        [LastConnectionTest] DATETIME2 NULL,
                        [ConnectionTestSuccess] BIT NULL,
                        [LastErrorMessage] NVARCHAR(MAX) NULL,
                        [EnableParallelTranscription] BIT NOT NULL DEFAULT 0,
                        [ParallelChunkDurationSeconds] INT NOT NULL DEFAULT 30,
                        [ParallelChunkOverlapSeconds] INT NOT NULL DEFAULT 2,
                        [ParallelDistributionStrategy] NVARCHAR(50) NOT NULL DEFAULT 'RoundRobin',
                        [ParallelMaxChunksPerProvider] INT NOT NULL DEFAULT 2,
                        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                        [UpdatedAt] DATETIME2 NULL,
                        [UpdatedBy] UNIQUEIDENTIFIER NULL
                    );

                    CREATE UNIQUE INDEX [IX_AIServiceConfigurations_ServiceType]
                    ON [dbo].[AIServiceConfigurations] ([ServiceType]);
                END
            ");

            // Add parallel transcription columns if they don't exist (v2.3.70-dev migration)
            // Execute each ALTER TABLE separately to avoid SQL Server batch compilation issues
            _logger.LogInformation("[AIConfig] Checking for missing parallel transcription columns...");

            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AIServiceConfigurations') AND name = 'EnableParallelTranscription')
                    ALTER TABLE [dbo].[AIServiceConfigurations] ADD [EnableParallelTranscription] BIT NOT NULL DEFAULT 0");
                _logger.LogInformation("[AIConfig] Column EnableParallelTranscription checked/added");
            }
            catch (Exception ex) { _logger.LogWarning("[AIConfig] EnableParallelTranscription: {Msg}", ex.Message); }

            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AIServiceConfigurations') AND name = 'ParallelChunkDurationSeconds')
                    ALTER TABLE [dbo].[AIServiceConfigurations] ADD [ParallelChunkDurationSeconds] INT NOT NULL DEFAULT 30");
                _logger.LogInformation("[AIConfig] Column ParallelChunkDurationSeconds checked/added");
            }
            catch (Exception ex) { _logger.LogWarning("[AIConfig] ParallelChunkDurationSeconds: {Msg}", ex.Message); }

            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AIServiceConfigurations') AND name = 'ParallelChunkOverlapSeconds')
                    ALTER TABLE [dbo].[AIServiceConfigurations] ADD [ParallelChunkOverlapSeconds] INT NOT NULL DEFAULT 2");
                _logger.LogInformation("[AIConfig] Column ParallelChunkOverlapSeconds checked/added");
            }
            catch (Exception ex) { _logger.LogWarning("[AIConfig] ParallelChunkOverlapSeconds: {Msg}", ex.Message); }

            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AIServiceConfigurations') AND name = 'ParallelDistributionStrategy')
                    ALTER TABLE [dbo].[AIServiceConfigurations] ADD [ParallelDistributionStrategy] NVARCHAR(50) NOT NULL DEFAULT 'RoundRobin'");
                _logger.LogInformation("[AIConfig] Column ParallelDistributionStrategy checked/added");
            }
            catch (Exception ex) { _logger.LogWarning("[AIConfig] ParallelDistributionStrategy: {Msg}", ex.Message); }

            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AIServiceConfigurations') AND name = 'ParallelMaxChunksPerProvider')
                    ALTER TABLE [dbo].[AIServiceConfigurations] ADD [ParallelMaxChunksPerProvider] INT NOT NULL DEFAULT 2");
                _logger.LogInformation("[AIConfig] Column ParallelMaxChunksPerProvider checked/added");
            }
            catch (Exception ex) { _logger.LogWarning("[AIConfig] ParallelMaxChunksPerProvider: {Msg}", ex.Message); }

            _logger.LogInformation("[AIConfig] ✅ Ensured AIServiceConfigurations table exists with all parallel transcription columns");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[AIConfig] Could not verify/create table or columns - may already exist");
            _ = _errorLoggingService.LogErrorAsync(ex, component: "AIConfigurationService", severity: "Warning",
                additionalData: JsonSerializer.Serialize(new { Method = "EnsureTableExistsAsync" }));
        }
    }

    #region DTOs for Ollama API

    private class OllamaModelsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModel>? Models { get; set; }
    }

    private class OllamaModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("modified_at")]
        public string ModifiedAt { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    #endregion
}
