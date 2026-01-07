using InsightLearn.Shared.DTOs;
using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services;

public class CategoryService : ICategoryService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(IApiClient apiClient, EndpointsConfig endpoints, ILogger<CategoryService> logger)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CategoryDto>>> GetAllCategoriesAsync()
    {
        _logger.LogDebug("Fetching all categories");
        var response = await _apiClient.GetAsync<List<CategoryDto>>(_endpoints.Categories.GetAll);

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved {CategoryCount} categories", response.Data.Count);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve categories: {ErrorMessage}",
                response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(Guid id)
    {
        _logger.LogDebug("Fetching category: {CategoryId}", id);
        var response = await _apiClient.GetAsync<CategoryDto>(string.Format(_endpoints.Categories.GetById, id));

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Category retrieved: {CategoryName} (ID: {CategoryId})",
                response.Data.Name, id);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve category {CategoryId}: {ErrorMessage}",
                id, response.Message ?? "Not found");
        }

        return response;
    }

    public async Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CategoryDto category)
    {
        _logger.LogInformation("Creating category: {CategoryName}", category.Name);
        var response = await _apiClient.PostAsync<CategoryDto>(_endpoints.Categories.Create, category);

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Category created successfully: {CategoryName} (ID: {CategoryId})",
                response.Data.Name, response.Data.Id);
        }
        else
        {
            _logger.LogError("Failed to create category {CategoryName}: {ErrorMessage}",
                category.Name, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(Guid id, CategoryDto category)
    {
        _logger.LogInformation("Updating category: {CategoryId} - {CategoryName}", id, category.Name);
        var response = await _apiClient.PutAsync<CategoryDto>(string.Format(_endpoints.Categories.Update, id), category);

        if (response.Success)
        {
            _logger.LogInformation("Category updated successfully: {CategoryId}", id);
        }
        else
        {
            _logger.LogError("Failed to update category {CategoryId}: {ErrorMessage}",
                id, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse> DeleteCategoryAsync(Guid id)
    {
        _logger.LogWarning("Deleting category: {CategoryId}", id);
        var response = await _apiClient.DeleteAsync(string.Format(_endpoints.Categories.Delete, id));

        if (response.Success)
        {
            _logger.LogInformation("Category deleted successfully: {CategoryId}", id);
        }
        else
        {
            _logger.LogError("Failed to delete category {CategoryId}: {ErrorMessage}",
                id, response.Message ?? "Unknown error");
        }

        return response;
    }
}
