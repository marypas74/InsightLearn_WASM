using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Repository interface for Lesson entity operations
/// </summary>
public interface ILessonRepository
{
    /// <summary>
    /// Gets all lessons for a section ordered by OrderIndex
    /// </summary>
    Task<IEnumerable<Lesson>> GetBySectionIdAsync(Guid sectionId);

    /// <summary>
    /// Gets a lesson by its unique identifier
    /// </summary>
    Task<Lesson?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new lesson
    /// </summary>
    Task<Lesson> CreateAsync(Lesson lesson);

    /// <summary>
    /// Updates an existing lesson
    /// </summary>
    Task<Lesson> UpdateAsync(Lesson lesson);

    /// <summary>
    /// Deletes a lesson by ID
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Reorders lessons within a section
    /// </summary>
    Task ReorderAsync(Guid sectionId, List<Guid> lessonIds);

    /// <summary>
    /// Get all lessons that do NOT have transcripts in MongoDB.
    /// Used by BatchTranscriptProcessor to find lessons needing transcript generation.
    /// v2.3.23-dev - Part of Batch Transcription System.
    /// </summary>
    Task<List<Lesson>> GetLessonsWithoutTranscriptsAsync();
}
