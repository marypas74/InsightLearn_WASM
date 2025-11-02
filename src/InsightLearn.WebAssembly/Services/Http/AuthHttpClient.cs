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

            // Use standard HttpClient - CORS is now solved by nginx proxy in same origin
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);

            _logger.LogInformation("✅ Response Status: {StatusCode} from {FullUrl}", response.StatusCode, fullUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ HTTP {StatusCode} from {FullUrl} - Response: {ErrorContent}",
                    response.StatusCode, fullUrl, errorContent);
                throw new HttpRequestException($"HTTP {response.StatusCode} calling {fullUrl}: {errorContent}");
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
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
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in auth POST request to {Endpoint}", endpoint);
            return false;
        }
    }
}
