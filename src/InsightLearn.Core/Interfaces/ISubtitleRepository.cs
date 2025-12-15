using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

/// <summary>
/// Repository interface for SubtitleTrack entity operations
/// </summary>
public interface ISubtitleRepository
{
    /// <summary>
    /// Get all active subtitle tracks for a specific lesson
    /// </summary>
    /// <param name="lessonId">The lesson ID</param>
    /// <returns>List of subtitle tracks ordered by language</returns>
    Task<List<SubtitleTrack>> GetByLessonIdAsync(Guid lessonId);

    /// <summary>
    /// Get all subtitle tracks for a lesson including inactive ones
    /// </summary>
    /// <param name="lessonId">The lesson ID</param>
    /// <returns>List of all subtitle tracks</returns>
    Task<List<SubtitleTrack>> GetAllByLessonIdAsync(Guid lessonId);

    /// <summary>
    /// Get a specific subtitle track by ID
    /// </summary>
    /// <param name="id">The subtitle track ID</param>
    /// <returns>The subtitle track or null if not found</returns>
    Task<SubtitleTrack?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get a subtitle track for a lesson in a specific language
    /// </summary>
    /// <param name="lessonId">The lesson ID</param>
    /// <param name="language">ISO 639-1 language code (e.g., "en", "it")</param>
    /// <returns>The subtitle track or null if not found</returns>
    Task<SubtitleTrack?> GetByLessonAndLanguageAsync(Guid lessonId, string language);

    /// <summary>
    /// Get the default subtitle track for a lesson
    /// </summary>
    /// <param name="lessonId">The lesson ID</param>
    /// <returns>The default subtitle track or null if none is set</returns>
    Task<SubtitleTrack?> GetDefaultByLessonIdAsync(Guid lessonId);

    /// <summary>
    /// Add a new subtitle track
    /// </summary>
    /// <param name="subtitleTrack">The subtitle track to add</param>
    /// <returns>The added subtitle track</returns>
    Task<SubtitleTrack> AddAsync(SubtitleTrack subtitleTrack);

    /// <summary>
    /// Update an existing subtitle track
    /// </summary>
    /// <param name="subtitleTrack">The subtitle track to update</param>
    /// <returns>The updated subtitle track</returns>
    Task<SubtitleTrack> UpdateAsync(SubtitleTrack subtitleTrack);

    /// <summary>
    /// Delete a subtitle track
    /// </summary>
    /// <param name="id">The subtitle track ID to delete</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteAsync(Guid id);

    /// <summary>
    /// Check if a subtitle track exists for a lesson in a specific language
    /// </summary>
    /// <param name="lessonId">The lesson ID</param>
    /// <param name="language">ISO 639-1 language code</param>
    /// <returns>True if exists, false otherwise</returns>
    Task<bool> ExistsAsync(Guid lessonId, string language);

    /// <summary>
    /// Set a subtitle track as the default for its lesson (clears other defaults)
    /// </summary>
    /// <param name="id">The subtitle track ID to set as default</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> SetAsDefaultAsync(Guid id);

    /// <summary>
    /// Get count of active subtitle tracks for a lesson
    /// </summary>
    /// <param name="lessonId">The lesson ID</param>
    /// <returns>Count of active subtitle tracks</returns>
    Task<int> GetCountByLessonIdAsync(Guid lessonId);
}
