namespace InsightLearn.WebAssembly.Services.Http;

/// <summary>
/// HTTP client for authentication endpoints (does not attach auth token)
/// </summary>
public interface IAuthHttpClient
{
    Task<T?> GetAsync<T>(string endpoint);
    Task<T?> PostAsync<T>(string endpoint, object? data = null);
    Task<bool> PostAsync(string endpoint, object? data = null);
}
