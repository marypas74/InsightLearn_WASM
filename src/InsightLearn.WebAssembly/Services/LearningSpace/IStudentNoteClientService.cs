using InsightLearn.WebAssembly.Models;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Frontend service for Student Notes API.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public interface IStudentNoteClientService
{
    /// <summary>
    /// Get notes by lesson ID for current user.
    /// </summary>
    Task<ApiResponse<List<StudentNoteDto>>> GetNotesByLessonAsync(Guid lessonId);

    /// <summary>
    /// Get bookmarked notes for current user.
    /// </summary>
    Task<ApiResponse<List<StudentNoteDto>>> GetBookmarkedNotesAsync();

    /// <summary>
    /// Get shared notes by lesson (visible to all students).
    /// </summary>
    Task<ApiResponse<List<StudentNoteDto>>> GetSharedNotesByLessonAsync(Guid lessonId, int limit = 100);

    /// <summary>
    /// Create a new note.
    /// </summary>
    Task<ApiResponse<StudentNoteDto>> CreateNoteAsync(CreateStudentNoteDto dto);

    /// <summary>
    /// Update note text, bookmark, or share status.
    /// </summary>
    Task<ApiResponse<StudentNoteDto>> UpdateNoteAsync(Guid noteId, UpdateStudentNoteDto dto);

    /// <summary>
    /// Delete a note.
    /// </summary>
    Task<ApiResponse<object>> DeleteNoteAsync(Guid noteId);

    /// <summary>
    /// Toggle bookmark on/off.
    /// </summary>
    Task<ApiResponse<StudentNoteDto>> ToggleBookmarkAsync(Guid noteId);

    /// <summary>
    /// Toggle share on/off.
    /// </summary>
    Task<ApiResponse<StudentNoteDto>> ToggleShareAsync(Guid noteId);
}

/// <summary>
/// Student note DTO for frontend display.
/// </summary>
public class StudentNoteDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public int VideoTimestamp { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public bool IsShared { get; set; }
    public bool IsBookmarked { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new note.
/// </summary>
public class CreateStudentNoteDto
{
    public Guid LessonId { get; set; }
    public int VideoTimestamp { get; set; }
    public string NoteText { get; set; } = string.Empty;
    public bool IsShared { get; set; } = false;
    public bool IsBookmarked { get; set; } = false;
}

/// <summary>
/// DTO for updating a note.
/// </summary>
public class UpdateStudentNoteDto
{
    public string? NoteText { get; set; }
    public bool? IsShared { get; set; }
    public bool? IsBookmarked { get; set; }
}
