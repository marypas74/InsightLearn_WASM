using InsightLearn.Core.DTOs.Course;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Service interface for Lesson business logic
/// </summary>
public interface ILessonService
{
    /// <summary>
    /// Gets all lessons for a section ordered by OrderIndex
    /// </summary>
    Task<List<LessonDto>> GetSectionLessonsAsync(Guid sectionId);

    /// <summary>
    /// Gets a lesson by its unique identifier
    /// </summary>
    Task<LessonDto?> GetLessonByIdAsync(Guid id);

    /// <summary>
    /// Creates a new lesson
    /// </summary>
    Task<LessonDto> CreateLessonAsync(LessonDto dto);

    /// <summary>
    /// Updates an existing lesson
    /// </summary>
    Task<LessonDto?> UpdateLessonAsync(Guid id, LessonDto dto);

    /// <summary>
    /// Deletes a lesson (soft delete if has progress)
    /// </summary>
    Task<bool> DeleteLessonAsync(Guid id);

    /// <summary>
    /// Reorders lessons within a section
    /// </summary>
    Task<bool> ReorderLessonsAsync(Guid sectionId, List<Guid> lessonIds);
}