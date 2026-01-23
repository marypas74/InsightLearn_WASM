using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InsightLearn.Application.Services;

/// <summary>
/// OpenAI GPT-4 powered Learning Assistant
/// Replaces Ollama for AI chat, translation, and educational content generation
/// Uses GPT-4-turbo for high-quality responses
/// </summary>
public class OpenAILearningAssistantService : IOpenAILearningAssistantService
{
    private readonly ILogger<OpenAILearningAssistantService> _logger;
    private readonly HttpClient _httpClient;
    private readonly MetricsService _metricsService;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly double _temperature;

    public OpenAILearningAssistantService(
        ILogger<OpenAILearningAssistantService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        MetricsService metricsService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClientFactory.CreateClient("OpenAI");
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));

        // Load OpenAI configuration
        _apiKey = configuration["OpenAI:ApiKey"]
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OpenAI API Key not configured");

        _model = configuration["OpenAI:ChatModel"] ?? "gpt-4-turbo-preview"; // or gpt-4, gpt-3.5-turbo
        _temperature = double.Parse(configuration["OpenAI:Temperature"] ?? "0.7");

        // Note: BaseAddress, Timeout, and Authorization header are already configured
        // by the "OpenAI" HttpClient factory in Program.cs (lines 663-670)
        // Do NOT set them here to avoid "header does not support multiple values" error

        _logger.LogInformation("[OpenAI Assistant] Initialized with model: {Model}, Temperature: {Temperature}",
            _model, _temperature);
    }

    /// <summary>
    /// Send message to AI Learning Assistant with context awareness
    /// </summary>
    public async Task<string> SendMessageAsync(
        string userMessage,
        string? systemPrompt = null,
        List<ChatMessage>? conversationHistory = null,
        string operationType = "chat",
        CancellationToken ct = default)
    {
        _logger.LogInformation("[OpenAI Assistant] Sending message (length: {Length} chars)", userMessage.Length);

        try
        {
            // Build messages array
            var messages = new List<object>();

            // Add system prompt (instruction for AI behavior)
            var systemMessage = systemPrompt ?? GetDefaultSystemPrompt();
            messages.Add(new { role = "system", content = systemMessage });

            // Add conversation history (for context)
            if (conversationHistory != null && conversationHistory.Any())
            {
                foreach (var msg in conversationHistory.TakeLast(10)) // Keep last 10 messages for context
                {
                    messages.Add(new
                    {
                        role = msg.Role.ToLower(), // "user" or "assistant"
                        content = msg.Content
                    });
                }
            }

            // Add current user message
            messages.Add(new { role = "user", content = userMessage });

            // Call OpenAI Chat Completions API
            var requestBody = new
            {
                model = _model,
                messages = messages,
                temperature = _temperature,
                max_tokens = 1500,
                top_p = 0.9,
                frequency_penalty = 0.0,
                presence_penalty = 0.6
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/chat/completions", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"OpenAI API error: {response.StatusCode} - {errorBody}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<OpenAIChatResponse>(jsonResponse);

            if (result?.Choices == null || !result.Choices.Any())
            {
                throw new InvalidOperationException("OpenAI API returned no choices");
            }

            var aiResponse = result.Choices[0].Message.Content?.Trim() ?? string.Empty;

            _logger.LogInformation("[OpenAI Assistant] Response received (length: {Length} chars, tokens: {Tokens})",
                aiResponse.Length, result.Usage?.TotalTokens ?? 0);

            // Record OpenAI token usage for Prometheus metrics
            if (result.Usage != null)
            {
                _metricsService.RecordOpenAITokens(
                    operationType,
                    _model,
                    result.Usage.PromptTokens,
                    result.Usage.CompletionTokens,
                    result.Usage.TotalTokens);
            }

            return aiResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OpenAI Assistant] API call failed: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Send message to AI Learning Assistant with streaming response (solves 30-second timeout)
    /// </summary>
    /// <param name="userMessage">User's message</param>
    /// <param name="systemPrompt">Optional system prompt for AI behavior</param>
    /// <param name="conversationHistory">Optional conversation history for context</param>
    /// <param name="operationType">Operation type for metrics (default: chat)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Async enumerable of response chunks</returns>
    public async IAsyncEnumerable<string> SendMessageStreamAsync(
        string userMessage,
        string? systemPrompt = null,
        List<ChatMessage>? conversationHistory = null,
        string operationType = "chat",
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        _logger.LogInformation("[OpenAI Assistant] Sending streaming message (length: {Length} chars)", userMessage.Length);

        // Build messages array (same as non-streaming version)
        var messages = new List<object>();

        var systemMessage = systemPrompt ?? GetDefaultSystemPrompt();
        messages.Add(new { role = "system", content = systemMessage });

        if (conversationHistory != null && conversationHistory.Any())
        {
            foreach (var msg in conversationHistory.TakeLast(10))
            {
                messages.Add(new
                {
                    role = msg.Role.ToLower(),
                    content = msg.Content
                });
            }
        }

        messages.Add(new { role = "user", content = userMessage });

        // Call OpenAI Chat Completions API with streaming enabled
        var requestBody = new
        {
            model = _model,
            messages = messages,
            temperature = _temperature,
            max_tokens = 1500,
            top_p = 0.9,
            frequency_penalty = 0.0,
            presence_penalty = 0.6,
            stream = true  // Enable streaming
        };

        var jsonRequest = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        try
        {
            // Use SendAsync with HttpCompletionOption.ResponseHeadersRead for streaming
            using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions");
            request.Content = content;
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"OpenAI API error: {response.StatusCode} - {errorBody}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OpenAI Assistant] Streaming API call failed: {Message}", ex.Message);
            throw;
        }

        // Parse Server-Sent Events (SSE) stream
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        var fullResponse = new StringBuilder();
        int totalPromptTokens = 0;
        int totalCompletionTokens = 0;

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // SSE format: "data: {...}" or "data: [DONE]"
            if (!line.StartsWith("data: ")) continue;

            var data = line.Substring(6); // Remove "data: " prefix

            if (data == "[DONE]")
            {
                _logger.LogInformation("[OpenAI Assistant] Streaming response completed (length: {Length} chars)",
                    fullResponse.Length);
                break;
            }

            // Parse chunk outside try-catch to allow yield return
            StreamChunkResponse? chunk = null;
            try
            {
                chunk = JsonSerializer.Deserialize<StreamChunkResponse>(data);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[OpenAI Assistant] Failed to parse streaming chunk: {Data}", data);
                // Continue processing next chunks
                continue;
            }

            if (chunk?.Choices != null && chunk.Choices.Any())
            {
                var delta = chunk.Choices[0].Delta?.Content;
                if (!string.IsNullOrEmpty(delta))
                {
                    fullResponse.Append(delta);
                    yield return delta;
                }

                // Track token usage if available
                if (chunk.Usage != null)
                {
                    totalPromptTokens = chunk.Usage.PromptTokens;
                    totalCompletionTokens = chunk.Usage.CompletionTokens;
                }
            }
        }

        // Record OpenAI token usage for Prometheus metrics
        if (totalPromptTokens > 0 || totalCompletionTokens > 0)
        {
            _metricsService.RecordOpenAITokens(
                operationType,
                _model,
                totalPromptTokens,
                totalCompletionTokens,
                totalPromptTokens + totalCompletionTokens);
        }
    }

    /// <summary>
    /// Translate subtitle text using GPT-4
    /// </summary>
    public async Task<string> TranslateTextAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        string? context = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[OpenAI Assistant] Translating text from {Source} to {Target}",
            sourceLanguage, targetLanguage);

        var systemPrompt = $@"You are a professional translator specializing in educational content.
Translate the following text from {sourceLanguage} to {targetLanguage}.
Provide ONLY the translation, no explanations or extra text.
Maintain the educational tone and technical accuracy.";

        if (!string.IsNullOrEmpty(context))
        {
            systemPrompt += $"\n\nContext from previous subtitles:\n{context}";
        }

        var userMessage = $"Translate this text:\n\n{text}";

        var translation = await SendMessageAsync(userMessage, systemPrompt, null, "translate", ct);

        // Clean up common artifacts
        translation = CleanTranslationArtifacts(translation);

        return translation;
    }

    /// <summary>
    /// Generate educational summary from lesson transcript
    /// </summary>
    public async Task<string> GenerateLessonSummaryAsync(
        string transcriptText,
        string lessonTitle,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[OpenAI Assistant] Generating lesson summary for: {Title}", lessonTitle);

        var systemPrompt = @"You are an expert educational content analyst.
Generate a concise summary of the lesson highlighting:
1. Main learning objectives
2. Key concepts covered
3. Important takeaways
4. Practical applications

Format as bullet points. Keep it under 200 words.";

        var userMessage = $@"Lesson Title: {lessonTitle}

Transcript:
{transcriptText.Substring(0, Math.Min(transcriptText.Length, 4000))} [...]";

        return await SendMessageAsync(userMessage, systemPrompt, null, "summary", ct);
    }

    /// <summary>
    /// Extract key concepts from lesson transcript
    /// </summary>
    public async Task<List<KeyConcept>> ExtractKeyConceptsAsync(
        string transcriptText,
        string lessonTitle,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[OpenAI Assistant] Extracting key concepts from: {Title}", lessonTitle);

        var systemPrompt = @"You are an educational content expert.
Extract 5-8 key concepts from the lesson transcript.
For each concept, provide:
1. Concept name (short title)
2. Brief explanation (1-2 sentences)
3. Timestamp reference if mentioned in transcript

Format as JSON array:
[
  {""name"": ""Concept Name"", ""explanation"": ""Brief explanation"", ""timestamp"": ""MM:SS""},
  ...
]";

        var userMessage = $@"Lesson Title: {lessonTitle}

Transcript:
{transcriptText.Substring(0, Math.Min(transcriptText.Length, 6000))}";

        var response = await SendMessageAsync(userMessage, systemPrompt, null, "concepts", ct);

        try
        {
            // Try to parse JSON response
            var concepts = JsonSerializer.Deserialize<List<KeyConcept>>(response);
            return concepts ?? new List<KeyConcept>();
        }
        catch (JsonException)
        {
            _logger.LogWarning("[OpenAI Assistant] Failed to parse JSON, returning empty list");
            return new List<KeyConcept>();
        }
    }

    /// <summary>
    /// Get default system prompt for Learning Assistant
    /// </summary>
    private string GetDefaultSystemPrompt()
    {
        return @"You are an AI Learning Assistant for InsightLearn, an e-learning platform.
Your role is to help students understand course content, answer questions, and provide educational guidance.

Guidelines:
- Be helpful, friendly, and encouraging
- Provide clear, concise explanations
- Use examples when appropriate
- If you don't know something, say so honestly
- Focus on education and learning
- Avoid off-topic discussions";
    }

    /// <summary>
    /// Clean common translation artifacts from GPT-4 responses
    /// </summary>
    private string CleanTranslationArtifacts(string translation)
    {
        // Remove "Translation:", "Here is the translation:", etc.
        var patterns = new[]
        {
            "translation:", "translated:", "here is the translation:",
            "here's the translation:", "the translation is:"
        };

        foreach (var pattern in patterns)
        {
            if (translation.ToLower().StartsWith(pattern))
            {
                translation = translation.Substring(pattern.Length).TrimStart(' ', ':', '\n');
            }
        }

        // Remove surrounding quotes if present
        translation = translation.Trim('"', '\'');

        return translation.Trim();
    }

    /// <summary>
    /// Check if OpenAI service is available (API key configured and reachable)
    /// </summary>
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            // Simple availability check - verify API key is configured and not empty
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("[OpenAI Assistant] API key not configured");
                return false;
            }

            // Test API connectivity with a minimal request to models endpoint
            var response = await _httpClient.GetAsync("/v1/models", HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("[OpenAI Assistant] Service available - API reachable");
                return true;
            }

            _logger.LogWarning("[OpenAI Assistant] API returned status {StatusCode}", response.StatusCode);
            return response.StatusCode != System.Net.HttpStatusCode.Unauthorized; // Available but maybe rate limited
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[OpenAI Assistant] Service availability check failed");
            return false;
        }
    }
}

/// <summary>
/// Interface for OpenAI Learning Assistant Service
/// </summary>
public interface IOpenAILearningAssistantService
{
    Task<string> SendMessageAsync(
        string userMessage,
        string? systemPrompt = null,
        List<ChatMessage>? conversationHistory = null,
        string operationType = "chat",
        CancellationToken ct = default);

    Task<string> TranslateTextAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        string? context = null,
        CancellationToken ct = default);

    Task<string> GenerateLessonSummaryAsync(
        string transcriptText,
        string lessonTitle,
        CancellationToken ct = default);

    Task<List<KeyConcept>> ExtractKeyConceptsAsync(
        string transcriptText,
        string lessonTitle,
        CancellationToken ct = default);

    /// <summary>
    /// Send message to AI Learning Assistant with streaming support (Server-Sent Events)
    /// Yields partial responses as they arrive for real-time UI updates
    /// </summary>
    IAsyncEnumerable<string> SendMessageStreamAsync(
        string userMessage,
        string? systemPrompt = null,
        List<ChatMessage>? conversationHistory = null,
        string operationType = "chat",
        CancellationToken ct = default);

    /// <summary>
    /// Check if OpenAI service is available (API key configured and reachable)
    /// </summary>
    Task<bool> IsAvailableAsync();
}

/// <summary>
/// Chat message model
/// </summary>
public class ChatMessage
{
    public string Role { get; set; } = "user"; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Key concept extracted from lesson
/// </summary>
public class KeyConcept
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }
}

/// <summary>
/// OpenAI Chat Completions API response
/// </summary>
internal class OpenAIChatResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("choices")]
    public List<Choice>? Choices { get; set; }

    [JsonPropertyName("usage")]
    public UsageInfo? Usage { get; set; }

    public class UsageInfo
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public Message Message { get; set; } = new();

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class Message
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}

/// <summary>
/// OpenAI Chat Completions API streaming response (Server-Sent Events)
/// </summary>
internal class StreamChunkResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("created")]
    public long Created { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("choices")]
    public List<StreamChoice>? Choices { get; set; }

    [JsonPropertyName("usage")]
    public OpenAIChatResponse.UsageInfo? Usage { get; set; }

    public class StreamChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("delta")]
        public Delta? Delta { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class Delta
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
