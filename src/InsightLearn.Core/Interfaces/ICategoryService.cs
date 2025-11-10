using InsightLearn.Core.DTOs.Category;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service interface for category business logic operations
/// </summary>
public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(Guid id);
    Task<CategoryDto?> GetCategoryBySlugAsync(string slug);
    Task<CategoryWithCoursesDto?> GetCategoryWithCoursesAsync(Guid id);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto);
    Task<CategoryDto?> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto);
    Task<bool> DeleteCategoryAsync(Guid id);
}
