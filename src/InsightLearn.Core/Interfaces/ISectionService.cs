using InsightLearn.Core.DTOs.Course;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service interface for Section business logic
/// </summary>
public interface ISectionService
{
    /// <summary>
    /// Gets all sections for a course ordered by OrderIndex
    /// </summary>
    Task<List<SectionDto>> GetCourseSectionsAsync(Guid courseId);

    /// <summary>
    /// Gets a section by its unique identifier
    /// </summary>
    Task<SectionDto?> GetSectionByIdAsync(Guid id);

    /// <summary>
    /// Creates a new section for a course
    /// </summary>
    Task<SectionDto> CreateSectionAsync(Guid courseId, string title, string? description, int orderIndex);

    /// <summary>
    /// Updates an existing section
    /// </summary>
    Task<SectionDto?> UpdateSectionAsync(Guid id, string? title, string? description, int? orderIndex);

    /// <summary>
    /// Deletes a section if it has no lessons
    /// </summary>
    Task<bool> DeleteSectionAsync(Guid id);

    /// <summary>
    /// Reorders sections within a course
    /// </summary>
    Task<bool> ReorderSectionsAsync(Guid courseId, List<Guid> sectionIds);
}