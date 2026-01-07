using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;
using Microsoft.Extensions.Logging;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Frontend service implementation for Student Notes API.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public class StudentNoteClientService : IStudentNoteClientService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<StudentNoteClientService> _logger;
    // v2.1.0-dev: Fixed endpoint to match backend (/api/notes instead of /api/student-notes)
    private const string BaseEndpoint = "/api/notes";

    public StudentNoteClientService(IApiClient apiClient, ILogger<StudentNoteClientService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger;
    }

    public async Task<ApiResponse<List<StudentNoteDto>>> GetNotesByLessonAsync(Guid lessonId)
    {
        _logger.LogDebug("Fetching notes for lesson: {LessonId}", lessonId);
        // Backend uses query parameter: /api/notes?lessonId={id}
        var response = await _apiClient.GetAsync<List<StudentNoteDto>>($"{BaseEndpoint}?lessonId={lessonId}");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved {NoteCount} notes for lesson {LessonId}",
                response.Data.Count, lessonId);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve notes for lesson {LessonId}: {ErrorMessage}",
                lessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<List<StudentNoteDto>>> GetBookmarkedNotesAsync()
    {
        _logger.LogDebug("Fetching bookmarked notes");
        var response = await _apiClient.GetAsync<List<StudentNoteDto>>($"{BaseEndpoint}/bookmarked");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved {NoteCount} bookmarked notes", response.Data.Count);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve bookmarked notes: {ErrorMessage}",
                response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<List<StudentNoteDto>>> GetSharedNotesByLessonAsync(Guid lessonId, int limit = 100)
    {
        _logger.LogDebug("Fetching shared notes for lesson {LessonId} (limit: {Limit})", lessonId, limit);
        var response = await _apiClient.GetAsync<List<StudentNoteDto>>($"{BaseEndpoint}/lesson/{lessonId}/shared?limit={limit}");

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Retrieved {NoteCount} shared notes for lesson {LessonId}",
                response.Data.Count, lessonId);
        }
        else
        {
            _logger.LogWarning("Failed to retrieve shared notes for lesson {LessonId}: {ErrorMessage}",
                lessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<StudentNoteDto>> CreateNoteAsync(CreateStudentNoteDto dto)
    {
        _logger.LogInformation("Creating note for lesson {LessonId} at timestamp {Timestamp}",
            dto.LessonId, dto.VideoTimestamp);
        var response = await _apiClient.PostAsync<StudentNoteDto>(BaseEndpoint, dto);

        if (response.Success && response.Data != null)
        {
            _logger.LogInformation("Note created successfully (NoteId: {NoteId}) for lesson {LessonId}",
                response.Data.Id, dto.LessonId);
        }
        else
        {
            _logger.LogError("Failed to create note for lesson {LessonId}: {ErrorMessage}",
                dto.LessonId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<StudentNoteDto>> UpdateNoteAsync(Guid noteId, UpdateStudentNoteDto dto)
    {
        _logger.LogInformation("Updating note {NoteId}", noteId);
        var response = await _apiClient.PutAsync<StudentNoteDto>($"{BaseEndpoint}/{noteId}", dto);

        if (response.Success)
        {
            _logger.LogInformation("Note updated successfully: {NoteId}", noteId);
        }
        else
        {
            _logger.LogError("Failed to update note {NoteId}: {ErrorMessage}",
                noteId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<object>> DeleteNoteAsync(Guid noteId)
    {
        _logger.LogWarning("Deleting note: {NoteId}", noteId);
        var response = await _apiClient.DeleteAsync<object>($"{BaseEndpoint}/{noteId}");

        if (response.Success)
        {
            _logger.LogInformation("Note deleted successfully: {NoteId}", noteId);
        }
        else
        {
            _logger.LogError("Failed to delete note {NoteId}: {ErrorMessage}",
                noteId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<StudentNoteDto>> ToggleBookmarkAsync(Guid noteId)
    {
        _logger.LogInformation("Toggling bookmark for note {NoteId}", noteId);
        var response = await _apiClient.PostAsync<StudentNoteDto>($"{BaseEndpoint}/{noteId}/toggle-bookmark");

        if (response.Success)
        {
            _logger.LogInformation("Bookmark toggled successfully for note {NoteId}", noteId);
        }
        else
        {
            _logger.LogWarning("Failed to toggle bookmark for note {NoteId}: {ErrorMessage}",
                noteId, response.Message ?? "Unknown error");
        }

        return response;
    }

    public async Task<ApiResponse<StudentNoteDto>> ToggleShareAsync(Guid noteId)
    {
        _logger.LogInformation("Toggling share for note {NoteId}", noteId);
        var response = await _apiClient.PostAsync<StudentNoteDto>($"{BaseEndpoint}/{noteId}/toggle-share");

        if (response.Success)
        {
            _logger.LogInformation("Share toggled successfully for note {NoteId}", noteId);
        }
        else
        {
            _logger.LogWarning("Failed to toggle share for note {NoteId}: {ErrorMessage}",
                noteId, response.Message ?? "Unknown error");
        }

        return response;
    }
}
