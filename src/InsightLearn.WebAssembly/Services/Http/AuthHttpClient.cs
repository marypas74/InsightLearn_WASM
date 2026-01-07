using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.Http;

/// <summary>
/// HTTP client for authentication endpoints (does not attach auth token)
/// Used for login, register, etc. where user is not yet authenticated
/// </summary>
public class AuthHttpClient : IAuthHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthHttpClient> _logger;

    public AuthHttpClient(HttpClient httpClient, ILogger<AuthHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<T>(endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in auth GET request to {Endpoint}", endpoint);
            return default;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            // Costruisci URL completo per debug
            var fullUrl = new Uri(_httpClient.BaseAddress!, endpoint).ToString();
            _logger.LogInformation("🔍 DEBUG POST - BaseAddress: {BaseAddress}, Endpoint: {Endpoint}, Full URL: {FullUrl}",
                _httpClient.BaseAddress, endpoint, fullUrl);

            // Configure JSON options to use PascalCase (matching backend)
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null, // null = PascalCase
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            // Serialize with explicit JSON options
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(data, jsonOptions);
            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("📤 Sending JSON: {JsonContent}", jsonContent);

            var response = await _httpClient.PostAsync(endpoint, content);

            _logger.LogInformation("✅ Response Status: {StatusCode} from {FullUrl}", response.StatusCode, fullUrl);

            // Handle both successful and error responses
            var responseContent = await response.Content.ReadAsStringAsync();

            // Special handling for 4xx errors with valid JSON body
            if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
            {
                // Try to deserialize as the expected type T
                // This is useful when the API returns an AuthResponse even for errors
                try
                {
                    if (!string.IsNullOrWhiteSpace(responseContent))
                    {
                        _logger.LogDebug("📥 Attempting to deserialize 4xx response as {Type}", typeof(T).Name);
                        _logger.LogDebug("📥 Response content: {Content}", responseContent);

                        var result = JsonSerializer.Deserialize<T>(responseContent, jsonOptions);
                        if (result != null)
                        {
                            // Check if this is an AuthResponse with error information
                            if (result is Models.Auth.AuthResponse authResponse)
                            {
                                // Log the business error, not as an HTTP exception
                                _logger.LogWarning("⚠️ Authentication failed: {Errors}",
                                    authResponse.Errors?.Count > 0 ? string.Join(", ", authResponse.Errors) : authResponse.Message);

                                // Return the AuthResponse with error info (don't throw exception)
                                return result;
                            }

                            // For other types, also return the result
                            return result;
                        }
                        else
                        {
                            _logger.LogDebug("📥 Deserialization returned null");
                        }
                    }
                }
                catch (JsonException jsonEx)
                {
                    // Not a valid JSON response or not the expected type
                    _logger.LogDebug("Response is not valid JSON of type {Type}: {Error}", typeof(T).Name, jsonEx.Message);
                }

                // If we couldn't deserialize as T, create an AuthResponse if that's what was expected
                if (typeof(T) == typeof(Models.Auth.AuthResponse))
                {
                    _logger.LogInformation("📦 Creating error AuthResponse for 4xx status");

                    // Try to extract error message from response
                    string errorMsg = "Authentication failed";
                    List<string> errors = new();

                    try
                    {
                        // Try to parse the response as a generic JSON object to extract error info
                        using var doc = JsonDocument.Parse(responseContent);
                        if (doc.RootElement.TryGetProperty("Errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var error in errorsElement.EnumerateArray())
                            {
                                errors.Add(error.GetString() ?? "Unknown error");
                            }
                        }
                        else if (doc.RootElement.TryGetProperty("Message", out var messageElement))
                        {
                            errorMsg = messageElement.GetString() ?? errorMsg;
                        }
                        else if (doc.RootElement.TryGetProperty("ErrorMessage", out var errorMessageElement))
                        {
                            errorMsg = errorMessageElement.GetString() ?? errorMsg;
                        }
                    }
                    catch (JsonException ex)
                    {
                        // JSON parsing failed - use raw response instead
                        _logger.LogDebug(ex, "Failed to parse error response JSON, using raw content");
                        errors.Add(responseContent);
                    }

                    if (!errors.Any() && !string.IsNullOrWhiteSpace(errorMsg))
                    {
                        errors.Add(errorMsg);
                    }

                    var errorResponse = new Models.Auth.AuthResponse
                    {
                        IsSuccess = false,
                        Success = false,
                        Message = errorMsg,
                        Errors = errors
                    };

                    return (T)(object)errorResponse;
                }

                // For other types, throw an exception for 4xx errors
                _logger.LogError("❌ HTTP {StatusCode} from {FullUrl} - Response: {ErrorContent}",
                    response.StatusCode, fullUrl, responseContent);
                throw new HttpRequestException($"HTTP {response.StatusCode}: {responseContent}");
            }

            // For 5xx errors, always throw
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ HTTP {StatusCode} from {FullUrl} - Response: {ErrorContent}",
                    response.StatusCode, fullUrl, responseContent);
                throw new HttpRequestException($"HTTP {response.StatusCode} calling {fullUrl}: {responseContent}");
            }

            // For successful responses, deserialize normally
            return await response.Content.ReadFromJsonAsync<T>(jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            var fullUrl = new Uri(_httpClient.BaseAddress!, endpoint).ToString();
            _logger.LogError(ex, "❌ HTTP ERROR in auth POST to {FullUrl} - Status: {StatusCode}", fullUrl, ex.StatusCode);
            throw; // Re-throw per propagare l'errore con dettagli
        }
        catch (Exception ex)
        {
            var fullUrl = new Uri(_httpClient.BaseAddress!, endpoint).ToString();
            _logger.LogError(ex, "❌ FATAL ERROR in auth POST to {FullUrl}", fullUrl);
            throw; // Re-throw per propagare l'errore con dettagli
        }
    }

    public async Task<bool> PostAsync(string endpoint, object? data = null)
    {
        try
        {
            // Configure JSON options to use PascalCase (matching backend)
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null, // null = PascalCase
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var response = await _httpClient.PostAsJsonAsync(endpoint, data, jsonOptions);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in auth POST request to {Endpoint}", endpoint);
            return false;
        }
    }
}
