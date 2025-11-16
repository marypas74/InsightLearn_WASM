using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Repository interface for Category entity operations
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Gets all categories ordered by OrderIndex with pagination (PERF-4 fix)
    /// </summary>
    Task<IEnumerable<Category>> GetAllAsync(int page = 1, int pageSize = 100);

    /// <summary>
    /// Gets a category by its unique identifier
    /// </summary>
    Task<Category?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a category by its URL slug
    /// </summary>
    Task<Category?> GetBySlugAsync(string slug);

    /// <summary>
    /// Gets a category with its related courses
    /// </summary>
    Task<Category?> GetWithCoursesAsync(Guid id);

    /// <summary>
    /// Creates a new category
    /// </summary>
    Task<Category> CreateAsync(Category category);

    /// <summary>
    /// Updates an existing category
    /// </summary>
    Task<Category> UpdateAsync(Category category);

    /// <summary>
    /// Deletes a category by ID
    /// </summary>
    Task DeleteAsync(Guid id);
}
