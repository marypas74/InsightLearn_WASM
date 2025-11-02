using InsightLearn.WebAssembly.Models;

namespace InsightLearn.WebAssembly.Services.Http;

public interface IApiClient
{
    Task<ApiResponse<T>> GetAsync<T>(string endpoint);
    Task<ApiResponse<T>> PostAsync<T>(string endpoint, object? data = null);
    Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data);
    Task<ApiResponse<T>> DeleteAsync<T>(string endpoint);
    Task<ApiResponse> GetAsync(string endpoint);
    Task<ApiResponse> PostAsync(string endpoint, object? data = null);
    Task<ApiResponse> PutAsync(string endpoint, object data);
    Task<ApiResponse> DeleteAsync(string endpoint);
}
