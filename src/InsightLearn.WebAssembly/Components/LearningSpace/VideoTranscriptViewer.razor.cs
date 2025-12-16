using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using InsightLearn.WebAssembly.Services.LearningSpace;
using Blazored.Toast.Services;
using InsightLearn.Core.DTOs.VideoTranscript;

namespace InsightLearn.WebAssembly.Components.LearningSpace;

/// <summary>
/// Video transcript viewer component with search and timestamp navigation.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public partial class VideoTranscriptViewer : ComponentBase
{
    [Inject] private IVideoTranscriptClientService TranscriptService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    /// <summary>
    /// Lesson ID to display transcript for.
    /// </summary>
    [Parameter] public Guid LessonId { get; set; }

    /// <summary>
    /// Current video timestamp in seconds (for auto-scroll).
    /// </summary>
    [Parameter] public int CurrentVideoTimestamp { get; set; }

    /// <summary>
    /// Callback when user clicks a transcript segment (to seek video).
    /// </summary>
    [Parameter] public EventCallback<double> OnTimestampClick { get; set; }

    /// <summary>
    /// Lesson title for auto-generation context.
    /// </summary>
    [Parameter] public string? LessonTitle { get; set; }

    /// <summary>
    /// Video duration in seconds for segment timing.
    /// </summary>
    [Parameter] public int VideoDurationSeconds { get; set; } = 300;

    private VideoTranscriptDto? transcript = null;
    private List<TranscriptSegmentDto> segments = new();
    private List<TranscriptSegmentDto> filteredSegments = new();
    private bool isLoading = false;
    private bool hasTranscript = false;
    private string processingStatus = "Unknown";
    private string searchText = string.Empty;
    private bool isSearching = false;
    private int? activeSegmentIndex = null;
    private string? errorMessage = null;
    private bool isGenerating = false;

    protected override async Task OnParametersSetAsync()
    {
        if (LessonId != Guid.Empty && !hasTranscript)
        {
            await LoadTranscriptAsync();
        }

        // Update active segment based on current video time
        var previousActiveIndex = activeSegmentIndex;
        UpdateActiveSegment();

        // Auto-scroll to active segment if it changed
        if (activeSegmentIndex != previousActiveIndex && activeSegmentIndex.HasValue)
        {
            await ScrollToActiveSegmentAsync();
        }
    }

    private async Task LoadTranscriptAsync()
    {
        isLoading = true;
        errorMessage = null;
        try
        {
            var response = await TranscriptService.GetTranscriptAsync(LessonId);
            if (response.Success && response.Data != null && response.Data.Transcript.Count > 0)
            {
                transcript = response.Data;
                segments = transcript.Transcript ?? new();
                filteredSegments = segments;
                hasTranscript = true;
                processingStatus = transcript.ProcessingStatus;
            }
            else
            {
                // No transcript found - try auto-generation
                await AutoGenerateTranscriptAsync();
            }
        }
        catch (Exception ex)
        {
            // Check for authentication-related exceptions
            if (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
            {
                errorMessage = "Please log in to view the transcript";
                processingStatus = "Unauthorized";
            }
            else
            {
                // Try auto-generation on error
                await AutoGenerateTranscriptAsync();
            }
        }
        finally
        {
            isLoading = false;
        }
    }

    /// <summary>
    /// Auto-generate transcript using Ollama when none exists.
    /// </summary>
    private async Task AutoGenerateTranscriptAsync()
    {
        if (isGenerating) return;

        isGenerating = true;
        errorMessage = "Generating transcript with AI...";
        processingStatus = "Generating";
        StateHasChanged();

        try
        {
            var request = new AutoGenerateTranscriptRequest
            {
                LessonId = LessonId,
                LessonTitle = LessonTitle ?? "Educational Lesson",
                DurationSeconds = VideoDurationSeconds > 0 ? VideoDurationSeconds : 300,
                Language = "en-US"
            };

            var response = await TranscriptService.AutoGenerateTranscriptAsync(request);

            if (response.Success && response.Data != null && response.Data.Transcript.Count > 0)
            {
                transcript = response.Data;
                segments = transcript.Transcript ?? new();
                filteredSegments = segments;
                hasTranscript = true;
                processingStatus = "Completed";
                errorMessage = null;
                ToastService.ShowSuccess($"Transcript generated: {segments.Count} segments");
            }
            else
            {
                errorMessage = response.Message ?? "Failed to generate transcript";
                processingStatus = "Failed";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error generating transcript: {ex.Message}";
            processingStatus = "Failed";
        }
        finally
        {
            isGenerating = false;
        }
    }

    private async Task SearchTranscriptAsync()
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            filteredSegments = segments;
            return;
        }

        isSearching = true;
        try
        {
            var response = await TranscriptService.SearchTranscriptAsync(LessonId, searchText);
            if (response.Success && response.Data != null)
            {
                // Map search results back to segment indices
                var searchResultTimestamps = response.Data.Select(r => r.StartTime).ToHashSet();
                filteredSegments = segments
                    .Where(s => searchResultTimestamps.Contains(s.StartTime))
                    .ToList();

                if (filteredSegments.Count == 0)
                {
                    ToastService.ShowInfo("No results found");
                }
                else
                {
                    ToastService.ShowSuccess($"Found {filteredSegments.Count} results");
                }
            }
            else
            {
                ToastService.ShowError(response.Message ?? "Search failed");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Search error: {ex.Message}");
        }
        finally
        {
            isSearching = false;
        }
    }

    private void ClearSearch()
    {
        searchText = string.Empty;
        filteredSegments = segments;
    }

    private async Task OnSegmentClickAsync(TranscriptSegmentDto segment, int index)
    {
        activeSegmentIndex = index;
        if (OnTimestampClick.HasDelegate)
        {
            await OnTimestampClick.InvokeAsync(segment.StartTime);
        }
    }

    private void UpdateActiveSegment()
    {
        if (filteredSegments.Count == 0)
            return;

        // Find segment that contains current video time
        for (int i = 0; i < filteredSegments.Count; i++)
        {
            var segment = filteredSegments[i];
            if (CurrentVideoTimestamp >= segment.StartTime && CurrentVideoTimestamp < segment.EndTime)
            {
                activeSegmentIndex = i;
                break;
            }
        }
    }

    private string FormatTimestamp(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.Hours > 0)
            return ts.ToString(@"h\:mm\:ss");
        return ts.ToString(@"mm\:ss");
    }

    private string HighlightSearchText(string text)
    {
        if (string.IsNullOrWhiteSpace(searchText) || !text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            return text;

        // Simple highlight (in production, use a proper HTML sanitizer)
        var startIndex = text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1)
            return text;

        var before = text.Substring(0, startIndex);
        var match = text.Substring(startIndex, searchText.Length);
        var after = text.Substring(startIndex + searchText.Length);

        return $"{before}<mark>{match}</mark>{after}";
    }

    private async Task RequestTranscriptGenerationAsync()
    {
        // Trigger auto-generation using Ollama
        await AutoGenerateTranscriptAsync();
    }

    private string GetConfidenceColor(double? confidence)
    {
        if (!confidence.HasValue)
            return "#999";

        if (confidence.Value >= 0.9)
            return "#4caf50"; // Green (high confidence)
        if (confidence.Value >= 0.7)
            return "#ff9800"; // Orange (medium confidence)
        return "#f44336"; // Red (low confidence)
    }

    private string GetConfidenceLabel(double? confidence)
    {
        if (!confidence.HasValue)
            return "Unknown";

        if (confidence.Value >= 0.9)
            return "High";
        if (confidence.Value >= 0.7)
            return "Medium";
        return "Low";
    }

    /// <summary>
    /// Get Tailwind CSS class for confidence indicator badge.
    /// </summary>
    private string GetConfidenceClass(double? confidence)
    {
        if (!confidence.HasValue)
            return "bg-gray-100 text-gray-600";

        if (confidence.Value >= 0.9)
            return "bg-green-100 text-green-700"; // High confidence
        if (confidence.Value >= 0.7)
            return "bg-amber-100 text-amber-700"; // Medium confidence
        return "bg-red-100 text-red-700"; // Low confidence
    }

    /// <summary>
    /// Scroll to active segment when video time changes.
    /// </summary>
    private async Task ScrollToActiveSegmentAsync()
    {
        if (activeSegmentIndex.HasValue)
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("eval", $@"
                    const element = document.getElementById('segment-{activeSegmentIndex.Value}');
                    const container = document.getElementById('transcript-scroll-container');
                    if (element && container) {{
                        const elementRect = element.getBoundingClientRect();
                        const containerRect = container.getBoundingClientRect();
                        if (elementRect.top < containerRect.top || elementRect.bottom > containerRect.bottom) {{
                            element.scrollIntoView({{ behavior: 'smooth', block: 'center' }});
                        }}
                    }}
                ");
            }
            catch (Exception ex)
            {
                // Silently ignore scroll errors
                Console.WriteLine($"[VideoTranscriptViewer] Scroll error: {ex.Message}");
            }
        }
    }
}
