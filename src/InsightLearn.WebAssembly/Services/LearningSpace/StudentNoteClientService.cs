using InsightLearn.WebAssembly.Models;
using InsightLearn.WebAssembly.Services.Http;

namespace InsightLearn.WebAssembly.Services.LearningSpace;

/// <summary>
/// Frontend service implementation for Student Notes API.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public class StudentNoteClientService : IStudentNoteClientService
{
    private readonly IApiClient _apiClient;
    // v2.1.0-dev: Fixed endpoint to match backend (/api/notes instead of /api/student-notes)
    private const string BaseEndpoint = "/api/notes";

    public StudentNoteClientService(IApiClient apiClient)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
    }

    public async Task<ApiResponse<List<StudentNoteDto>>> GetNotesByLessonAsync(Guid lessonId)
    {
        // Backend uses query parameter: /api/notes?lessonId={id}
        return await _apiClient.GetAsync<List<StudentNoteDto>>($"{BaseEndpoint}?lessonId={lessonId}");
    }

    public async Task<ApiResponse<List<StudentNoteDto>>> GetBookmarkedNotesAsync()
    {
        return await _apiClient.GetAsync<List<StudentNoteDto>>($"{BaseEndpoint}/bookmarked");
    }

    public async Task<ApiResponse<List<StudentNoteDto>>> GetSharedNotesByLessonAsync(Guid lessonId, int limit = 100)
    {
        return await _apiClient.GetAsync<List<StudentNoteDto>>($"{BaseEndpoint}/lesson/{lessonId}/shared?limit={limit}");
    }

    public async Task<ApiResponse<StudentNoteDto>> CreateNoteAsync(CreateStudentNoteDto dto)
    {
        return await _apiClient.PostAsync<StudentNoteDto>(BaseEndpoint, dto);
    }

    public async Task<ApiResponse<StudentNoteDto>> UpdateNoteAsync(Guid noteId, UpdateStudentNoteDto dto)
    {
        return await _apiClient.PutAsync<StudentNoteDto>($"{BaseEndpoint}/{noteId}", dto);
    }

    public async Task<ApiResponse<object>> DeleteNoteAsync(Guid noteId)
    {
        return await _apiClient.DeleteAsync<object>($"{BaseEndpoint}/{noteId}");
    }

    public async Task<ApiResponse<StudentNoteDto>> ToggleBookmarkAsync(Guid noteId)
    {
        return await _apiClient.PostAsync<StudentNoteDto>($"{BaseEndpoint}/{noteId}/toggle-bookmark");
    }

    public async Task<ApiResponse<StudentNoteDto>> ToggleShareAsync(Guid noteId)
    {
        return await _apiClient.PostAsync<StudentNoteDto>($"{BaseEndpoint}/{noteId}/toggle-share");
    }
}
