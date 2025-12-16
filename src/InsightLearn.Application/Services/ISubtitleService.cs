using InsightLearn.Core.DTOs.Course;
using Microsoft.AspNetCore.Http;

namespace InsightLearn.Application.Services;

/// <summary>
/// Service interface for subtitle operations with business logic
/// </summary>
public interface ISubtitleService
{
    /// <summary>
    /// Get all subtitle tracks for a lesson
    /// </summary>
    /// <param name="lessonId">The lesson ID</param>
    /// <returns>List of subtitle track DTOs</returns>
    Task<List<SubtitleTrackDto>> GetSubtitlesByLessonIdAsync(Guid lessonId);

    /// <summary>
    /// Upload a new subtitle file (WebVTT format)
    /// </summary>
    /// <param name="lessonId">The lesson ID</param>
    /// <param name="language">ISO 639-1 language code</param>
    /// <param name="label">Human-readable label for the language</param>
    /// <param name="file">The WebVTT file to upload</param>
    /// <param name="userId">ID of the user uploading (instructor or admin)</param>
    /// <param name="isDefault">Whether this should be the default subtitle track</param>
    /// <param name="kind">Track kind: subtitles, captions, or descriptions</param>
    /// <returns>The created subtitle track DTO</returns>
    Task<SubtitleTrackDto> UploadSubtitleAsync(
        Guid lessonId,
        string language,
        string label,
        IFormFile file,
        Guid userId,
        bool isDefault = false,
        string kind = "subtitles");

    /// <summary>
    /// Delete a subtitle track
    /// </summary>
    /// <param name="subtitleId">The subtitle track ID</param>
    /// <param name="userId">ID of the user requesting deletion (must be instructor or admin)</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteSubtitleAsync(Guid subtitleId, Guid userId);

    /// <summary>
    /// Update subtitle track metadata (label, default, kind)
    /// </summary>
    /// <param name="subtitleId">The subtitle track ID</param>
    /// <param name="label">New label</param>
    /// <param name="isDefault">Whether this should be default</param>
    /// <param name="kind">Track kind</param>
    /// <param name="userId">ID of the user requesting update</param>
    /// <returns>Updated subtitle track DTO</returns>
    Task<SubtitleTrackDto> UpdateSubtitleMetadataAsync(
        Guid subtitleId,
        string label,
        bool isDefault,
        string kind,
        Guid userId);

    /// <summary>
    /// Get subtitle file content (WebVTT text)
    /// </summary>
    /// <param name="subtitleId">The subtitle track ID</param>
    /// <returns>WebVTT file content as string</returns>
    Task<string> GetSubtitleContentAsync(Guid subtitleId);

    /// <summary>
    /// Get subtitle file content by MongoDB GridFS file ID
    /// </summary>
    /// <param name="gridFsFileId">The MongoDB GridFS ObjectId as string</param>
    /// <returns>WebVTT file content as string</returns>
    Task<string> GetSubtitleContentByGridFsIdAsync(string gridFsFileId);

    /// <summary>
    /// Check if user has permission to manage subtitles for a lesson
    /// </summary>
    /// <param name="lessonId">The lesson ID</param>
    /// <param name="userId">The user ID</param>
    /// <returns>True if user is the course instructor or admin</returns>
    Task<bool> CanManageSubtitlesAsync(Guid lessonId, Guid userId);
}
