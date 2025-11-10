using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Repository interface for Section entity operations
/// </summary>
public interface ISectionRepository
{
    /// <summary>
    /// Gets all sections for a course ordered by OrderIndex
    /// </summary>
    Task<IEnumerable<Section>> GetByCourseIdAsync(Guid courseId);

    /// <summary>
    /// Gets a section by its unique identifier
    /// </summary>
    Task<Section?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new section
    /// </summary>
    Task<Section> CreateAsync(Section section);

    /// <summary>
    /// Updates an existing section
    /// </summary>
    Task<Section> UpdateAsync(Section section);

    /// <summary>
    /// Deletes a section by ID
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Reorders sections for a course
    /// </summary>
    Task ReorderAsync(Guid courseId, List<Guid> sectionIds);
}
