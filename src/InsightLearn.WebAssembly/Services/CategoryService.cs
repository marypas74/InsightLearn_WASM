using InsightLearn.Application.DTOs;
using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Models.Config;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services;

public class CategoryService : ICategoryService
{
    private readonly IApiClient _apiClient;
    private readonly EndpointsConfig _endpoints;

    public CategoryService(IApiClient apiClient, EndpointsConfig endpoints)
    {
        _apiClient = apiClient;
        _endpoints = endpoints;
    }

    public async Task<ApiResponse<List<CategoryDto>>> GetAllCategoriesAsync()
    {
        return await _apiClient.GetAsync<List<CategoryDto>>(_endpoints.Categories.GetAll);
    }

    public async Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(Guid id)
    {
        return await _apiClient.GetAsync<CategoryDto>(string.Format(_endpoints.Categories.GetById, id));
    }

    public async Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CategoryDto category)
    {
        return await _apiClient.PostAsync<CategoryDto>(_endpoints.Categories.Create, category);
    }

    public async Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(Guid id, CategoryDto category)
    {
        return await _apiClient.PutAsync<CategoryDto>(string.Format(_endpoints.Categories.Update, id), category);
    }

    public async Task<ApiResponse> DeleteCategoryAsync(Guid id)
    {
        return await _apiClient.DeleteAsync(string.Format(_endpoints.Categories.Delete, id));
    }
}
