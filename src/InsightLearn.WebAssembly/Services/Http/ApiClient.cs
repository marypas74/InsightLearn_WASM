using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace InsightLearn.WebAssembly.Services.Http;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ITokenService _tokenService;
    private readonly ILogger<ApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IJSRuntime _jsRuntime;

    public ApiClient(HttpClient httpClient, ITokenService tokenService, ILogger<ApiClient> logger, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _logger = logger;
        _jsRuntime = jsRuntime;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            // CRITICAL: Keep PascalCase for serialization to match ASP.NET backend expectations
            PropertyNamingPolicy = null,  // null = PascalCase (default C#)
            // v2.1.0-dev: Support enum serialization as strings (e.g., LessonType "Video" instead of 0)
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            await AttachAuthTokenAsync();
            var response = await _httpClient.GetAsync(endpoint);
            return await HandleResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GET request to {Endpoint}", endpoint);
            return new ApiResponse<T>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? data = null)
    {
        try
        {
            await AttachAuthTokenAsync();
            await AttachCsrfTokenAsync();  // SECURITY: CSRF protection for POST requests
            // CRITICAL: Pass _jsonOptions to preserve PascalCase (ASP.NET backend expects PascalCase)
            var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);
            return await HandleResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in POST request to {Endpoint}", endpoint);
            return new ApiResponse<T>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            await AttachAuthTokenAsync();
            await AttachCsrfTokenAsync();  // SECURITY: CSRF protection for PUT requests
            // CRITICAL: Pass _jsonOptions to preserve PascalCase
            var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions);
            return await HandleResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PUT request to {Endpoint}", endpoint);
            return new ApiResponse<T>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<ApiResponse<T>> DeleteAsync<T>(string endpoint)
    {
        try
        {
            await AttachAuthTokenAsync();
            await AttachCsrfTokenAsync();  // SECURITY: CSRF protection for DELETE requests
            var response = await _httpClient.DeleteAsync(endpoint);
            return await HandleResponseAsync<T>(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DELETE request to {Endpoint}", endpoint);
            return new ApiResponse<T>
            {
                Success = false,
                Message = "An error occurred while processing your request",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    // Non-generic versions
    public async Task<ApiResponse> GetAsync(string endpoint)
    {
        var result = await GetAsync<object>(endpoint);
        return new ApiResponse
        {
            Success = result.Success,
            Message = result.Message,
            Errors = result.Errors
        };
    }

    public async Task<ApiResponse> PostAsync(string endpoint, object? data = null)
    {
        var result = await PostAsync<object>(endpoint, data);
        return new ApiResponse
        {
            Success = result.Success,
            Message = result.Message,
            Errors = result.Errors
        };
    }

    public async Task<ApiResponse> PutAsync(string endpoint, object data)
    {
        var result = await PutAsync<object>(endpoint, data);
        return new ApiResponse
        {
            Success = result.Success,
            Message = result.Message,
            Errors = result.Errors
        };
    }

    public async Task<ApiResponse> DeleteAsync(string endpoint)
    {
        var result = await DeleteAsync<object>(endpoint);
        return new ApiResponse
        {
            Success = result.Success,
            Message = result.Message,
            Errors = result.Errors
        };
    }

    private async Task AttachAuthTokenAsync()
    {
        var token = await _tokenService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    /// <summary>
    /// Attaches CSRF token from cookie to request header for state-changing operations
    /// Required for POST, PUT, DELETE, PATCH requests (PCI DSS 6.5.9)
    /// </summary>
    private async Task AttachCsrfTokenAsync()
    {
        try
        {
            // Read CSRF token from XSRF-TOKEN cookie using JavaScript interop
            var csrfToken = await _jsRuntime.InvokeAsync<string?>("getCookie", "XSRF-TOKEN");

            if (!string.IsNullOrEmpty(csrfToken))
            {
                // Remove existing header if present
                if (_httpClient.DefaultRequestHeaders.Contains("X-CSRF-Token"))
                {
                    _httpClient.DefaultRequestHeaders.Remove("X-CSRF-Token");
                }

                // Add CSRF token to request header
                _httpClient.DefaultRequestHeaders.Add("X-CSRF-Token", csrfToken);
                _logger.LogDebug("[CSRF] Token attached to request");
            }
            else
            {
                _logger.LogDebug("[CSRF] No token found in cookie");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[CSRF] Failed to attach CSRF token");
        }
    }

    private async Task<ApiResponse<T>> HandleResponseAsync<T>(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return new ApiResponse<T> { Success = true };
            }

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                return new ApiResponse<T> { Success = true };
            }

            // CRITICAL FIX v12: Two-phase deserialization with detailed logging
            // v2.3.69: Added detailed logging for debugging
            _logger.LogDebug("[ApiClient] Received content: {Content}", content.Length > 200 ? content[..200] + "..." : content);

            // Phase 1: Try to deserialize as ApiResponse<T> (backend might return wrapped response)
            try
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                // Only return if it looks like a valid ApiResponse (has Success=true OR Data)
                if (apiResponse != null && (apiResponse.Success || apiResponse.Data != null))
                {
                    _logger.LogDebug("[ApiClient] Phase 1 success: ApiResponse<T> with Success={Success}", apiResponse.Success);
                    return apiResponse;
                }
                _logger.LogDebug("[ApiClient] Phase 1: ApiResponse<T> parsed but Success=false and Data=null");
            }
            catch (JsonException ex1)
            {
                // First attempt failed (backend returns raw T, not ApiResponse<T>)
                // Continue to Phase 2
                _logger.LogDebug("[ApiClient] Phase 1 failed: {Error}", ex1.Message);
            }

            // Phase 2: Try to deserialize directly as T (backend returns raw array/object)
            try
            {
                var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                _logger.LogDebug("[ApiClient] Phase 2 success: raw T deserialized, data is null: {IsNull}", data == null);
                return new ApiResponse<T>
                {
                    Success = true,
                    Data = data
                };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[ApiClient] Phase 2 failed: Could not deserialize as T. Content: {Content}", content.Length > 100 ? content[..100] : content);
                return new ApiResponse<T>
                {
                    Success = false,
                    Message = "Failed to parse server response",
                    Errors = new List<string> { ex.Message }
                };
            }
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("API request failed with status {StatusCode}: {Error}",
                response.StatusCode, errorContent);

            try
            {
                var errorResponse = JsonSerializer.Deserialize<ApiResponse<T>>(errorContent, _jsonOptions);
                if (errorResponse != null)
                {
                    return errorResponse;
                }
            }
            catch (JsonException ex)
            {
                // Error response deserialization failed - not critical, fall through to generic error
                _logger.LogDebug(ex, "Failed to deserialize error response, using generic error message");
            }

            return new ApiResponse<T>
            {
                Success = false,
                Message = $"Request failed with status {response.StatusCode}",
                Errors = new List<string> { errorContent }
            };
        }
    }
}
