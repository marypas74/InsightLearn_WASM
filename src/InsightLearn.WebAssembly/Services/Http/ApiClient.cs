using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Auth;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.Http;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ITokenService _tokenService;
    private readonly ILogger<ApiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient, ITokenService tokenService, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _tokenService = tokenService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            // CRITICAL: Keep PascalCase for serialization to match ASP.NET backend expectations
            PropertyNamingPolicy = null  // null = PascalCase (default C#)
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

            try
            {
                // Try to deserialize as ApiResponse<T> first
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                // CRITICAL FIX: Only return apiResponse if it actually has Success=true OR Data is not null
                // This prevents returning empty ApiResponse when backend returns T directly
                if (apiResponse != null && (apiResponse.Success || apiResponse.Data != null))
                {
                    return apiResponse;
                }

                // If that fails, try to deserialize directly as T
                var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                return new ApiResponse<T>
                {
                    Success = true,
                    Data = data
                };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize response");
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
            catch
            {
                // Ignore deserialization errors for error responses
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
