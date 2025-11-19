using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using InsightLearn.WebAssembly.Services.LearningSpace;
using Blazored.Toast.Services;

namespace InsightLearn.WebAssembly.Components.LearningSpace;

/// <summary>
/// Student notes panel component for video lessons.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public partial class StudentNotesPanel : ComponentBase
{
    [Inject] private IStudentNoteClientService NoteService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;

    /// <summary>
    /// Lesson ID to display notes for.
    /// </summary>
    [Parameter] public Guid LessonId { get; set; }

    /// <summary>
    /// Current video timestamp in seconds (for creating notes at current position).
    /// </summary>
    [Parameter] public int CurrentVideoTimestamp { get; set; }

    /// <summary>
    /// Callback when user clicks a note timestamp (to seek video).
    /// </summary>
    [Parameter] public EventCallback<int> OnTimestampClick { get; set; }

    private List<StudentNoteDto> myNotes = new();
    private List<StudentNoteDto> sharedNotes = new();
    private bool isLoading = false;
    private bool showEditor = false;
    private bool showSharedNotes = false;

    // Editor state
    private Guid? editingNoteId = null;
    private string noteText = string.Empty;
    private int noteTimestamp = 0;
    private bool noteIsShared = false;
    private bool noteIsBookmarked = false;

    protected override async Task OnParametersSetAsync()
    {
        if (LessonId != Guid.Empty)
        {
            await LoadNotesAsync();
        }
    }

    private async Task LoadNotesAsync()
    {
        isLoading = true;
        try
        {
            var response = await NoteService.GetNotesByLessonAsync(LessonId);
            if (response.Success && response.Data != null)
            {
                myNotes = response.Data.OrderByDescending(n => n.CreatedAt).ToList();
            }
            else
            {
                ToastService.ShowError(response.Message ?? "Failed to load notes");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error loading notes: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadSharedNotesAsync()
    {
        isLoading = true;
        try
        {
            var response = await NoteService.GetSharedNotesByLessonAsync(LessonId, limit: 100);
            if (response.Success && response.Data != null)
            {
                sharedNotes = response.Data.OrderByDescending(n => n.CreatedAt).ToList();
            }
            else
            {
                ToastService.ShowError(response.Message ?? "Failed to load shared notes");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error loading shared notes: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ShowCreateNoteEditor()
    {
        editingNoteId = null;
        noteText = string.Empty;
        noteTimestamp = CurrentVideoTimestamp;
        noteIsShared = false;
        noteIsBookmarked = false;
        showEditor = true;
    }

    private void ShowEditNoteEditor(StudentNoteDto note)
    {
        editingNoteId = note.Id;
        noteText = note.NoteText;
        noteTimestamp = note.VideoTimestamp;
        noteIsShared = note.IsShared;
        noteIsBookmarked = note.IsBookmarked;
        showEditor = true;
    }

    private void CancelEditor()
    {
        showEditor = false;
        editingNoteId = null;
        noteText = string.Empty;
    }

    private async Task SaveNoteAsync()
    {
        if (string.IsNullOrWhiteSpace(noteText))
        {
            ToastService.ShowWarning("Note text cannot be empty");
            return;
        }

        try
        {
            if (editingNoteId.HasValue)
            {
                // Update existing note
                var updateDto = new UpdateStudentNoteDto
                {
                    NoteText = noteText,
                    IsShared = noteIsShared,
                    IsBookmarked = noteIsBookmarked
                };
                var response = await NoteService.UpdateNoteAsync(editingNoteId.Value, updateDto);

                if (response.Success)
                {
                    ToastService.ShowSuccess("Note updated successfully");
                    await LoadNotesAsync();
                    CancelEditor();
                }
                else
                {
                    ToastService.ShowError(response.Message ?? "Failed to update note");
                }
            }
            else
            {
                // Create new note
                var createDto = new CreateStudentNoteDto
                {
                    LessonId = LessonId,
                    VideoTimestamp = noteTimestamp,
                    NoteText = noteText,
                    IsShared = noteIsShared,
                    IsBookmarked = noteIsBookmarked
                };
                var response = await NoteService.CreateNoteAsync(createDto);

                if (response.Success)
                {
                    ToastService.ShowSuccess("Note created successfully");
                    await LoadNotesAsync();
                    CancelEditor();
                }
                else
                {
                    ToastService.ShowError(response.Message ?? "Failed to create note");
                }
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error saving note: {ex.Message}");
        }
    }

    private async Task DeleteNoteAsync(Guid noteId)
    {
        if (!await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this note?"))
        {
            return;
        }

        try
        {
            var response = await NoteService.DeleteNoteAsync(noteId);
            if (response.Success)
            {
                ToastService.ShowSuccess("Note deleted successfully");
                await LoadNotesAsync();
            }
            else
            {
                ToastService.ShowError(response.Message ?? "Failed to delete note");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error deleting note: {ex.Message}");
        }
    }

    private async Task ToggleBookmarkAsync(Guid noteId)
    {
        try
        {
            var response = await NoteService.ToggleBookmarkAsync(noteId);
            if (response.Success)
            {
                ToastService.ShowSuccess("Bookmark toggled");
                await LoadNotesAsync();
            }
            else
            {
                ToastService.ShowError(response.Message ?? "Failed to toggle bookmark");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error toggling bookmark: {ex.Message}");
        }
    }

    private async Task ToggleShareAsync(Guid noteId)
    {
        try
        {
            var response = await NoteService.ToggleShareAsync(noteId);
            if (response.Success)
            {
                ToastService.ShowSuccess("Share toggled");
                await LoadNotesAsync();
            }
            else
            {
                ToastService.ShowError(response.Message ?? "Failed to toggle share");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error toggling share: {ex.Message}");
        }
    }

    private async Task OnTimestampClickedAsync(int timestamp)
    {
        if (OnTimestampClick.HasDelegate)
        {
            await OnTimestampClick.InvokeAsync(timestamp);
        }
    }

    private async Task ToggleSharedNotesAsync()
    {
        showSharedNotes = !showSharedNotes;
        if (showSharedNotes && sharedNotes.Count == 0)
        {
            await LoadSharedNotesAsync();
        }
    }

    private string FormatTimestamp(int seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.ToString(@"mm\:ss");
    }

    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
}
