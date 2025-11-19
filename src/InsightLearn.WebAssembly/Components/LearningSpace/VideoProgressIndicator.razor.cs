using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using InsightLearn.WebAssembly.Services.LearningSpace;
using Blazored.Toast.Services;
using InsightLearn.Core.DTOs;
using InsightLearn.Core.DTOs.VideoBookmarks;

namespace InsightLearn.WebAssembly.Components.LearningSpace;

/// <summary>
/// Video progress indicator with visual timeline and bookmark markers.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public partial class VideoProgressIndicator : ComponentBase
{
    [Inject] private IVideoProgressClientService ProgressService { get; set; } = default!;
    [Inject] private IVideoBookmarkClientService BookmarkService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Lesson ID to track progress for.
    /// </summary>
    [Parameter] public Guid LessonId { get; set; }

    /// <summary>
    /// Current video position in seconds.
    /// </summary>
    [Parameter] public double CurrentPosition { get; set; }

    /// <summary>
    /// Total video duration in seconds.
    /// </summary>
    [Parameter] public double TotalDuration { get; set; }

    /// <summary>
    /// Callback when user seeks to a new position.
    /// </summary>
    [Parameter] public EventCallback<double> OnSeek { get; set; }

    /// <summary>
    /// Update progress every N seconds (for auto-save).
    /// </summary>
    [Parameter] public int UpdateIntervalSeconds { get; set; } = 5;

    private List<VideoBookmarkDto> bookmarks = new();
    private double progressPercentage = 0;
    private string currentTimeFormatted = "0:00";
    private string totalTimeFormatted = "0:00";
    private bool isLoading = false;
    private DateTime lastProgressUpdate = DateTime.MinValue;

    protected override async Task OnParametersSetAsync()
    {
        // Update formatted times
        currentTimeFormatted = FormatTime(CurrentPosition);
        totalTimeFormatted = FormatTime(TotalDuration);

        // Calculate progress percentage
        if (TotalDuration > 0)
        {
            progressPercentage = (CurrentPosition / TotalDuration) * 100;
        }

        // Load bookmarks if lesson changed
        if (LessonId != Guid.Empty && bookmarks.Count == 0)
        {
            await LoadBookmarksAsync();
        }

        // Auto-save progress at intervals
        if (ShouldUpdateProgress())
        {
            await UpdateProgressAsync();
        }
    }

    private async Task LoadBookmarksAsync()
    {
        try
        {
            var response = await BookmarkService.GetBookmarksByLessonAsync(LessonId);
            if (response.Success && response.Data != null)
            {
                bookmarks = response.Data;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading bookmarks: {ex.Message}");
        }
    }

    private bool ShouldUpdateProgress()
    {
        // Don't update too frequently to avoid API spam
        var timeSinceLastUpdate = DateTime.UtcNow - lastProgressUpdate;
        return timeSinceLastUpdate.TotalSeconds >= UpdateIntervalSeconds && CurrentPosition > 0;
    }

    private async Task UpdateProgressAsync()
    {
        lastProgressUpdate = DateTime.UtcNow;

        try
        {
            var dto = new TrackVideoProgressDto
            {
                LessonId = LessonId,
                CurrentTimestampSeconds = (int)CurrentPosition,
                TotalDurationSeconds = (int)TotalDuration,
                PlaybackSpeed = 1.0m,
                TabActive = true
            };

            var response = await ProgressService.TrackProgressAsync(dto);
            if (!response.Success)
            {
                Console.WriteLine($"Progress update failed: {response.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating progress: {ex.Message}");
        }
    }

    private async Task OnProgressBarClickAsync(MouseEventArgs e)
    {
        if (TotalDuration <= 0)
            return;

        try
        {
            // Get progress bar element width via JS interop
            var progressBarWidth = await JSRuntime.InvokeAsync<double>(
                "eval",
                "document.querySelector('.video-progress-bar').offsetWidth"
            );

            if (progressBarWidth <= 0)
                return;

            // Calculate clicked position as percentage
            var clickX = e.OffsetX;
            var clickPercentage = (clickX / progressBarWidth) * 100;

            // Calculate new video position
            var newPosition = (clickPercentage / 100) * TotalDuration;

            // Invoke seek callback
            if (OnSeek.HasDelegate)
            {
                await OnSeek.InvokeAsync(newPosition);
            }

            // Update progress immediately
            await UpdateProgressAsync();
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Seek error: {ex.Message}");
        }
    }

    private async Task OnBookmarkClickAsync(VideoBookmarkDto bookmark)
    {
        if (OnSeek.HasDelegate)
        {
            await OnSeek.InvokeAsync(bookmark.VideoTimestamp);
        }
    }

    private string GetBookmarkPosition(VideoBookmarkDto bookmark)
    {
        if (TotalDuration <= 0)
            return "0%";

        var percentage = (bookmark.VideoTimestamp / TotalDuration) * 100;
        return $"{percentage:F2}%";
    }

    private string GetBookmarkTooltip(VideoBookmarkDto bookmark)
    {
        var time = FormatTime(bookmark.VideoTimestamp);
        return bookmark.Label ?? $"Bookmark at {time}";
    }

    private string FormatTime(double seconds)
    {
        if (seconds < 0)
            seconds = 0;

        var ts = TimeSpan.FromSeconds(seconds);

        if (ts.Hours > 0)
            return ts.ToString(@"h\:mm\:ss");

        return ts.ToString(@"mm\:ss");
    }

    /// <summary>
    /// Public method to reload bookmarks (called from parent component).
    /// </summary>
    public async Task ReloadBookmarksAsync()
    {
        bookmarks.Clear();
        await LoadBookmarksAsync();
        StateHasChanged();
    }

    /// <summary>
    /// Public method to force progress update (called from parent component).
    /// </summary>
    public async Task ForceProgressUpdateAsync()
    {
        await UpdateProgressAsync();
    }
}
