using InsightLearn.Application.DTOs;
using InsightLearn.WebAssembly.Models;

namespace InsightLearn.WebAssembly.Services;

public interface ICategoryService
{
    Task<ApiResponse<List<CategoryDto>>> GetAllCategoriesAsync();
    Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(Guid id);
    Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CategoryDto category);
    Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(Guid id, CategoryDto category);
    Task<ApiResponse> DeleteCategoryAsync(Guid id);
}
