using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using InsightLearn.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

/// <summary>
/// Implementazione del servizio per comunicare con Ollama LLM API
/// </summary>
public class OllamaService : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _defaultModel;
    private readonly ILogger<OllamaService> _logger;

    public OllamaService(
        HttpClient httpClient,
        string ollamaUrl,
        string defaultModel,
        ILogger<OllamaService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = !string.IsNullOrWhiteSpace(ollamaUrl)
            ? ollamaUrl.TrimEnd('/')
            : throw new ArgumentException("Ollama URL cannot be null or empty", nameof(ollamaUrl));
        _defaultModel = !string.IsNullOrWhiteSpace(defaultModel)
            ? defaultModel
            : throw new ArgumentException("Default model cannot be null or empty", nameof(defaultModel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(3); // Timeout generoso per LLM (phi3:mini può essere lento)
    }

    public async Task<string> GenerateResponseAsync(string prompt, string? model = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
        }

        var modelToUse = string.IsNullOrWhiteSpace(model) ? _defaultModel : model;

        try
        {
            _logger.LogInformation("Generating response with model {Model} for prompt: {Prompt}",
                modelToUse, prompt.Length > 50 ? prompt.Substring(0, 50) + "..." : prompt);

            var request = new OllamaGenerateRequest
            {
                Model = modelToUse,
                Prompt = prompt,
                Stream = false, // Non vogliamo streaming per semplicità
                System = "You are a helpful assistant for InsightLearn, an online learning platform. " +
                         "Provide brief, clear, and professional responses. Keep answers concise (2-3 sentences max). " +
                         "Focus on being helpful and informative about online courses, learning, and educational topics.",
                Options = new OllamaOptions
                {
                    NumPredict = 50,       // Brief responses (chatbot style) - AGGRESSIVE
                    Temperature = 0.3,     // Lower = faster, more deterministic - AGGRESSIVE
                    TopK = 20,             // Reduced sampling space - AGGRESSIVE
                    TopP = 0.85,           // Slightly reduced nucleus - AGGRESSIVE
                    NumCtx = 1024,         // Half context = 2x faster - AGGRESSIVE
                    NumThread = 6,         // Match available CPU threads
                    NumBatch = 128,        // Optimized for CPU (not GPU)
                    NumGpu = 0             // Force CPU-only (disable GPU detection overhead)
                }
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseJson);

            if (ollamaResponse == null || string.IsNullOrWhiteSpace(ollamaResponse.Response))
            {
                _logger.LogWarning("Ollama returned empty response");
                return "Mi dispiace, non sono riuscito a generare una risposta. Riprova più tardi.";
            }

            _logger.LogInformation("Successfully generated response with {CharCount} characters",
                ollamaResponse.Response.Length);

            return ollamaResponse.Response;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Ollama API at {BaseUrl}", _baseUrl);
            throw new Exception($"Errore di connessione a Ollama: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Ollama API call timed out");
            throw new Exception("La richiesta a Ollama ha impiegato troppo tempo. Riprova.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Ollama API");
            throw new Exception($"Errore imprevisto chiamando Ollama: {ex.Message}", ex);
        }
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            _logger.LogDebug("Checking if Ollama service is available at {BaseUrl}", _baseUrl);

            var response = await _httpClient.GetAsync("/api/tags");
            var isAvailable = response.IsSuccessStatusCode;

            _logger.LogInformation("Ollama service availability: {IsAvailable}", isAvailable);
            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama service is not available at {BaseUrl}", _baseUrl);
            return false;
        }
    }

    public async Task<List<string>> GetAvailableModelsAsync()
    {
        try
        {
            _logger.LogDebug("Fetching available models from Ollama");

            var response = await _httpClient.GetAsync("/api/tags");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(json);

            var modelNames = modelsResponse?.Models?.Select(m => m.Name).ToList() ?? new List<string>();

            _logger.LogInformation("Found {Count} available models", modelNames.Count);
            return modelNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available models from Ollama");
            return new List<string> { _defaultModel }; // Fallback al modello di default
        }
    }

    #region DTOs privati per Ollama API

    private class OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("system")]
        public string? System { get; set; }

        [JsonPropertyName("options")]
        public OllamaOptions? Options { get; set; }
    }

    private class OllamaOptions
    {
        /// <summary>
        /// Maximum number of tokens to generate (lower = faster responses)
        /// </summary>
        [JsonPropertyName("num_predict")]
        public int? NumPredict { get; set; }

        /// <summary>
        /// Temperature for generation (0.0-1.0). Lower = more deterministic and faster
        /// </summary>
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        /// <summary>
        /// Top-k sampling parameter
        /// </summary>
        [JsonPropertyName("top_k")]
        public int? TopK { get; set; }

        /// <summary>
        /// Top-p (nucleus) sampling parameter
        /// </summary>
        [JsonPropertyName("top_p")]
        public double? TopP { get; set; }

        /// <summary>
        /// Context window size (lower = faster)
        /// </summary>
        [JsonPropertyName("num_ctx")]
        public int? NumCtx { get; set; }

        /// <summary>
        /// Number of CPU threads to use
        /// </summary>
        [JsonPropertyName("num_thread")]
        public int? NumThread { get; set; }

        /// <summary>
        /// Batch size for processing (lower = better for CPU)
        /// </summary>
        [JsonPropertyName("num_batch")]
        public int? NumBatch { get; set; }

        /// <summary>
        /// Number of GPUs to use (0 = CPU only)
        /// </summary>
        [JsonPropertyName("num_gpu")]
        public int? NumGpu { get; set; }
    }

    private class OllamaGenerateResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; } = string.Empty;

        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }

    private class OllamaModelsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModel> Models { get; set; } = new();
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
