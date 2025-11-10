using InsightLearn.Core.DTOs.Category;
using InsightLearn.Core.DTOs.Course;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service for category business logic operations
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        ICategoryRepository categoryRepository,
        ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all categories");

            var categories = await _categoryRepository.GetAllAsync();
            var categoryList = categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.OrderIndex)
                .Select(MapToDto)
                .ToList();

            _logger.LogInformation("Retrieved {Count} active categories", categoryList.Count);
            return categoryList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all categories");
            throw;
        }
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Fetching category by ID: {CategoryId}", id);

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category not found: {CategoryId}", id);
                return null;
            }

            return MapToDto(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching category by ID: {CategoryId}", id);
            throw;
        }
    }

    public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug)
    {
        try
        {
            _logger.LogInformation("Fetching category by slug: {Slug}", slug);

            var category = await _categoryRepository.GetBySlugAsync(slug);
            if (category == null)
            {
                _logger.LogWarning("Category not found with slug: {Slug}", slug);
                return null;
            }

            return MapToDto(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching category by slug: {Slug}", slug);
            throw;
        }
    }

    public async Task<CategoryWithCoursesDto?> GetCategoryWithCoursesAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Fetching category with courses: {CategoryId}", id);

            var category = await _categoryRepository.GetWithCoursesAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category not found: {CategoryId}", id);
                return null;
            }

            var publishedCourses = category.Courses
                .Where(c => c.Status == CourseStatus.Published && c.IsActive)
                .ToList();

            var courseCards = publishedCourses.Select(course => new CourseCardDto
            {
                Id = course.Id,
                Title = course.Title,
                ShortDescription = course.ShortDescription,
                InstructorName = $"{course.Instructor?.FirstName ?? ""} {course.Instructor?.LastName ?? ""}".Trim(),
                CategoryName = category.Name,
                Price = course.Price,
                CurrentPrice = course.CurrentPrice,
                DiscountPercentage = course.DiscountPercentage,
                ThumbnailUrl = course.ThumbnailUrl,
                Level = course.Level.ToString(),
                Slug = course.Slug,
                AverageRating = course.AverageRating,
                ReviewCount = course.ReviewCount,
                EnrollmentCount = course.EnrollmentCount,
                EstimatedDurationMinutes = course.EstimatedDurationMinutes
            }).ToList();

            return new CategoryWithCoursesDto
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                IconUrl = category.IconUrl,
                ColorCode = category.ColorCode,
                Courses = courseCards,
                TotalCourses = publishedCourses.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching category with courses: {CategoryId}", id);
            throw;
        }
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        try
        {
            _logger.LogInformation("Creating new category: {Name}", dto.Name);

            // Check if category name already exists
            var existingCategories = await _categoryRepository.GetAllAsync();
            if (existingCategories.Any(c => c.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Category with name '{dto.Name}' already exists");
            }

            // Generate slug if not provided
            var slug = string.IsNullOrWhiteSpace(dto.Slug)
                ? GenerateSlug(dto.Name)
                : dto.Slug.ToLowerInvariant();

            // Check if slug already exists
            var existingSlug = await _categoryRepository.GetBySlugAsync(slug);
            if (existingSlug != null)
            {
                slug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            var category = new Category
            {
                Name = dto.Name,
                Slug = slug,
                IconUrl = dto.IconUrl,
                ColorCode = dto.ColorCode ?? "#007bff",
                OrderIndex = dto.OrderIndex,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createdCategory = await _categoryRepository.CreateAsync(category);
            _logger.LogInformation("Category created successfully: {CategoryId}", createdCategory.Id);

            return MapToDto(createdCategory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            throw;
        }
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(Guid id, UpdateCategoryDto dto)
    {
        try
        {
            _logger.LogInformation("Updating category: {CategoryId}", id);

            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category not found: {CategoryId}", id);
                return null;
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                // Check if new name conflicts with existing categories
                var existingCategories = await _categoryRepository.GetAllAsync();
                if (existingCategories.Any(c =>
                    c.Id != id && c.Name.Equals(dto.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException($"Category with name '{dto.Name}' already exists");
                }

                category.Name = dto.Name;
                category.Slug = GenerateSlug(dto.Name);
            }

            if (dto.IconUrl != null)
            {
                category.IconUrl = dto.IconUrl;
            }

            if (dto.ColorCode != null)
            {
                category.ColorCode = dto.ColorCode;
            }

            if (dto.OrderIndex.HasValue)
            {
                category.OrderIndex = dto.OrderIndex.Value;
            }

            category.UpdatedAt = DateTime.UtcNow;

            var updatedCategory = await _categoryRepository.UpdateAsync(category);
            _logger.LogInformation("Category updated successfully: {CategoryId}", id);

            return MapToDto(updatedCategory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category: {CategoryId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting category: {CategoryId}", id);

            var category = await _categoryRepository.GetWithCoursesAsync(id);
            if (category == null)
            {
                _logger.LogWarning("Category not found: {CategoryId}", id);
                return false;
            }

            // Check if category has courses
            if (category.Courses.Any())
            {
                _logger.LogWarning("Cannot delete category {CategoryId} - has {Count} associated courses",
                    id, category.Courses.Count);
                throw new InvalidOperationException(
                    $"Cannot delete category '{category.Name}' - it has {category.Courses.Count} associated courses");
            }

            await _categoryRepository.DeleteAsync(id);
            _logger.LogInformation("Category deleted successfully: {CategoryId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category: {CategoryId}", id);
            throw;
        }
    }

    // Private helper methods

    private CategoryDto MapToDto(Category category)
    {
        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            IconUrl = category.IconUrl,
            ColorCode = category.ColorCode,
            OrderIndex = category.OrderIndex,
            CourseCount = category.Courses.Count(c => c.IsActive && c.Status == CourseStatus.Published)
        };
    }

    private string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Guid.NewGuid().ToString();
        }

        // Convert to lowercase
        text = text.ToLowerInvariant();

        // Remove special characters
        text = Regex.Replace(text, @"[^a-z0-9\s-]", "");

        // Replace spaces with hyphens
        text = Regex.Replace(text, @"\s+", "-");

        // Remove duplicate hyphens
        text = Regex.Replace(text, @"-+", "-");

        // Trim hyphens from start and end
        text = text.Trim('-');

        // Truncate to reasonable length
        if (text.Length > 100)
        {
            text = text.Substring(0, 100).TrimEnd('-');
        }

        return text;
    }
}
