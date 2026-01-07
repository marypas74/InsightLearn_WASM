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
    private int transcriptProgress = 0; // Phase 1 Task 1.3: Progress tracking for HTTP 202 polling

    // Phase 8.5: Multi-Language Subtitle Support
    private ElementReference languageDropdownRef;
    private bool showLanguageDropdown = false;
    private string selectedLanguage = "en"; // Default to English (original)
    private Dictionary<string, string> translationStatuses = new(); // languageCode -> status
    private List<LanguageOption> availableLanguages = new()
    {
        new() { Code = "en", Name = "English", Flag = "🇬🇧" },
        new() { Code = "es", Name = "Spanish", Flag = "🇪🇸" },
        new() { Code = "fr", Name = "French", Flag = "🇫🇷" },
        new() { Code = "de", Name = "German", Flag = "🇩🇪" },
        new() { Code = "pt", Name = "Portuguese", Flag = "🇵🇹" },
        new() { Code = "it", Name = "Italian", Flag = "🇮🇹" }
    };

    protected override async Task OnParametersSetAsync()
    {
        if (LessonId != Guid.Empty && !hasTranscript)
        {
            await LoadTranscriptAsync();

            // Phase 8.5: Load translation statuses after transcript loads
            if (hasTranscript)
            {
                _ = LoadTranslationStatusesAsync(); // Fire and forget - don't block UI
                _ = AutoDetectBrowserLanguageAsync(); // Auto-select user's browser language
            }
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
    /// Auto-generate transcript using Hangfire background job with HTTP 202 polling pattern.
    /// Phase 1 Task 1.3: Updated to handle async job queue and poll for completion.
    /// </summary>
    private async Task AutoGenerateTranscriptAsync()
    {
        if (isGenerating) return;

        isGenerating = true;
        transcriptProgress = 0;
        errorMessage = "Queueing transcript generation...";
        processingStatus = "Queueing";
        StateHasChanged();

        try
        {
            // Use the existing AutoGenerateTranscriptAsync which calls /auto-generate endpoint
            // The backend /auto-generate endpoint returns HTTP 202 and queues Hangfire job
            var request = new AutoGenerateTranscriptRequest
            {
                LessonId = LessonId,
                LessonTitle = LessonTitle ?? "Educational Lesson",
                DurationSeconds = VideoDurationSeconds > 0 ? VideoDurationSeconds : 300,
                Language = "en-US"
            };

            var response = await TranscriptService.AutoGenerateTranscriptAsync(request);

            if (response.Success && response.Data != null && response.Data.Transcript != null && response.Data.Transcript.Count > 0)
            {
                // HTTP 200 OK - Transcript already exists
                transcript = response.Data;
                segments = transcript.Transcript ?? new();
                filteredSegments = segments;
                hasTranscript = true;
                processingStatus = "Completed";
                errorMessage = null;
                ToastService.ShowSuccess($"Transcript loaded: {segments.Count} segments");
                isGenerating = false;
                StateHasChanged();
                return;
            }
            else if (response.Success)
            {
                // HTTP 202 Accepted - Job queued (response.Data might be job info or null)
                // Backend queued the job, start polling for completion
                errorMessage = "Transcript generation in progress...";
                processingStatus = "Processing";
                ToastService.ShowInfo("Transcript generation started. This may take 1-2 minutes...");
                StateHasChanged();

                await PollTranscriptStatusAsync(maxAttempts: 60, intervalMs: 2000);
            }
            else
            {
                // API error
                errorMessage = response.Message ?? "Failed to generate transcript";
                processingStatus = "Failed";
                isGenerating = false;
                ToastService.ShowError(errorMessage);
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error generating transcript: {ex.Message}";
            processingStatus = "Failed";
            isGenerating = false;
            ToastService.ShowError(errorMessage);
        }

        StateHasChanged();
    }

    /// <summary>
    /// Poll transcript status endpoint every 2 seconds until completion or timeout.
    /// Phase 1 Task 1.3: Polling logic for HTTP 202 pattern.
    /// </summary>
    private async Task PollTranscriptStatusAsync(int maxAttempts = 60, int intervalMs = 2000)
    {
        int attempts = 0;
        while (attempts < maxAttempts && isGenerating)
        {
            attempts++;

            try
            {
                var statusResponse = await TranscriptService.GetStatusAsync(LessonId);

                if (statusResponse.Success && statusResponse.Data != null)
                {
                    var status = statusResponse.Data;

                    // Update progress (0-100%)
                    transcriptProgress = (int)(status.Progress ?? 0);
                    processingStatus = status.Status;
                    StateHasChanged();

                    if (status.Status == "Completed")
                    {
                        // Fetch completed transcript
                        var transcriptResponse = await TranscriptService.GetTranscriptAsync(LessonId);
                        if (transcriptResponse.Success && transcriptResponse.Data != null && transcriptResponse.Data.Transcript.Count > 0)
                        {
                            transcript = transcriptResponse.Data;
                            segments = transcript.Transcript ?? new();
                            filteredSegments = segments;
                            hasTranscript = true;
                            processingStatus = "Completed";
                            errorMessage = null;
                            ToastService.ShowSuccess($"Transcript generated: {segments.Count} segments");
                        }
                        isGenerating = false;
                        StateHasChanged();
                        return;
                    }
                    else if (status.Status == "Failed")
                    {
                        errorMessage = status.ErrorMessage ?? "Transcript generation failed";
                        processingStatus = "Failed";
                        isGenerating = false;
                        StateHasChanged();
                        return;
                    }
                    // Status is "Pending" or "Processing" - continue polling
                }
                else if (!statusResponse.Success)
                {
                    // Status endpoint failed - retry
                    await Task.Delay(intervalMs);
                    continue;
                }

                // Wait before next poll
                await Task.Delay(intervalMs);
            }
            catch (Exception ex)
            {
                // Network error or exception - retry after delay
                Console.WriteLine($"[VideoTranscriptViewer] Polling error (attempt {attempts}): {ex.Message}");
                await Task.Delay(intervalMs);
            }
        }

        // Max attempts reached - timeout
        if (isGenerating)
        {
            errorMessage = "Transcript generation timed out (2 minutes). The job may still be processing in the background. Please refresh to check status.";
            processingStatus = "Timeout";
            isGenerating = false;
            ToastService.ShowWarning("Transcript generation timed out. Please try again later.");
            StateHasChanged();
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

    // ========== Phase 7.4: Subtitle Download (LinkedIn Learning parity) ==========

    private ElementReference downloadDropdownRef;
    private bool showDownloadDropdown = false;

    /// <summary>
    /// Toggle subtitle download dropdown menu.
    /// </summary>
    private void ToggleDownloadDropdown()
    {
        showDownloadDropdown = !showDownloadDropdown;
    }

    /// <summary>
    /// Download subtitle file in WebVTT or SRT format.
    /// Phase 7: WebVTT Subtitle Generation - LinkedIn Learning parity feature.
    /// </summary>
    /// <param name="format">Subtitle format: "webvtt" or "srt"</param>
    private async Task DownloadSubtitleAsync(string format)
    {
        try
        {
            // Close dropdown
            showDownloadDropdown = false;
            StateHasChanged();

            // Construct download URL (endpoint created in Phase 7.2)
            // Note: Using relative URL since we're in Blazor WASM (client-side)
            var downloadUrl = $"/api/transcripts/{LessonId}/download?format={format}";

            // Trigger browser download using JavaScript
            // The browser will handle the file download with proper filename from Content-Disposition header
            await JSRuntime.InvokeVoidAsync("eval", $@"
                fetch('{downloadUrl}', {{
                    method: 'GET',
                    headers: {{
                        'Authorization': 'Bearer ' + (localStorage.getItem('authToken') || '')
                    }}
                }})
                .then(response => {{
                    if (!response.ok) {{
                        throw new Error('Download failed: ' + response.statusText);
                    }}
                    return response.blob();
                }})
                .then(blob => {{
                    const url = window.URL.createObjectURL(blob);
                    const a = document.createElement('a');
                    a.style.display = 'none';
                    a.href = url;
                    a.download = 'lesson-{LessonId}.{(format == "webvtt" ? "vtt" : "srt")}';
                    document.body.appendChild(a);
                    a.click();
                    window.URL.revokeObjectURL(url);
                    document.body.removeChild(a);
                }})
                .catch(error => {{
                    console.error('Download error:', error);
                    alert('Failed to download subtitle: ' + error.message);
                }});
            ");

            ToastService.ShowSuccess($"{format.ToUpper()} subtitle downloaded successfully");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Download failed: {ex.Message}");
        }
    }

    // ========== Phase 8.5: Multi-Language Subtitle Support ==========

    /// <summary>
    /// Toggle language selector dropdown menu.
    /// </summary>
    private void ToggleLanguageDropdown()
    {
        showLanguageDropdown = !showLanguageDropdown;
    }

    /// <summary>
    /// Get human-readable language name from code.
    /// </summary>
    private string GetLanguageName(string languageCode)
    {
        var language = availableLanguages.FirstOrDefault(l => l.Code == languageCode);
        return language?.Name ?? "English";
    }

    /// <summary>
    /// Handle language selection - switch to translated transcript.
    /// Phase 8.5: Multi-Language Subtitle Support.
    /// </summary>
    private async Task OnLanguageSelected(string languageCode)
    {
        // Close dropdown
        showLanguageDropdown = false;

        // If already selected, do nothing
        if (selectedLanguage == languageCode)
        {
            StateHasChanged();
            return;
        }

        try
        {
            isLoading = true;
            StateHasChanged();

            if (languageCode == "en")
            {
                // Switch back to original English transcript
                selectedLanguage = "en";
                await LoadTranscriptAsync(); // Reload original
                ToastService.ShowSuccess("Switched to original English transcript");
            }
            else
            {
                // Fetch translated transcript
                var response = await TranscriptService.GetTranslationAsync(LessonId, languageCode);

                if (response.Success && response.Data != null)
                {
                    var translationData = response.Data;

                    if (translationData.Status == "Completed" && translationData.Segments != null)
                    {
                        // Translation is ready - update UI with translated segments
                        selectedLanguage = languageCode;

                        // Convert TranslatedSegmentDto to TranscriptSegmentDto for display
                        segments = translationData.Segments.Select(s => new TranscriptSegmentDto
                        {
                            Text = s.TranslatedText,
                            StartTime = s.StartTime,
                            EndTime = s.EndTime,
                            Speaker = null, // Translations don't have speaker labels
                            Confidence = s.Confidence
                        }).ToList();

                        filteredSegments = segments;
                        hasTranscript = true;

                        // Update translation status
                        translationStatuses[languageCode] = "Completed";

                        var languageName = GetLanguageName(languageCode);
                        ToastService.ShowSuccess($"Switched to {languageName} translation");
                    }
                    else if (translationData.Status == "Processing")
                    {
                        translationStatuses[languageCode] = "Processing";
                        ToastService.ShowWarning("Translation is being processed. Try again in a few moments.");
                    }
                    else if (translationData.Status == "Failed")
                    {
                        translationStatuses[languageCode] = "Failed";
                        ToastService.ShowError($"Translation failed: {translationData.ErrorMessage}");
                    }
                    else if (translationData.Status == "NotFound")
                    {
                        translationStatuses[languageCode] = "NotFound";
                        ToastService.ShowInfo("Translation not available yet");
                    }
                }
                else
                {
                    ToastService.ShowError("Failed to load translation");
                }
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error switching language: {ex.Message}");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Load translation statuses for all available languages.
    /// Called on component initialization to show which translations are available.
    /// </summary>
    private async Task LoadTranslationStatusesAsync()
    {
        if (LessonId == Guid.Empty) return;

        try
        {
            // Check translation status for each language (except English)
            foreach (var lang in availableLanguages.Where(l => l.Code != "en"))
            {
                try
                {
                    var response = await TranscriptService.GetTranslationAsync(LessonId, lang.Code);
                    if (response.Success && response.Data != null)
                    {
                        translationStatuses[lang.Code] = response.Data.Status;
                    }
                }
                catch
                {
                    // Silently ignore errors for individual language checks
                    translationStatuses[lang.Code] = "NotFound";
                }
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VideoTranscriptViewer] Error loading translation statuses: {ex.Message}");
        }
    }

    /// <summary>
    /// Auto-detect browser language and switch to it if translation is available.
    /// Phase 8.5: Multi-Language Subtitle Support - Browser language detection.
    /// </summary>
    private async Task AutoDetectBrowserLanguageAsync()
    {
        try
        {
            // Get browser language using JSInterop
            var browserLanguage = await JSRuntime.InvokeAsync<string>("eval",
                "navigator.language || navigator.userLanguage || 'en'");

            // Extract 2-letter language code (e.g., "en-US" → "en", "es-MX" → "es")
            var languageCode = browserLanguage?.Split('-').FirstOrDefault()?.ToLower() ?? "en";

            // If browser language is English or not in our supported languages, stay on English
            if (languageCode == "en" || !availableLanguages.Any(l => l.Code == languageCode))
            {
                return;
            }

            // Wait a bit for translation statuses to load (they run in parallel)
            await Task.Delay(1000);

            // Check if translation is available and completed
            if (translationStatuses.ContainsKey(languageCode) && translationStatuses[languageCode] == "Completed")
            {
                // Auto-select the browser language
                var languageName = GetLanguageName(languageCode);
                Console.WriteLine($"[VideoTranscriptViewer] Auto-selecting browser language: {languageName} ({languageCode})");

                // Don't show toast for auto-selection (silent)
                await OnLanguageSelectedSilent(languageCode);
            }
        }
        catch (Exception ex)
        {
            // Silently ignore browser language detection errors
            Console.WriteLine($"[VideoTranscriptViewer] Browser language detection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Silent version of OnLanguageSelected (no toast notifications).
    /// Used for auto-detection to avoid annoying the user.
    /// </summary>
    private async Task OnLanguageSelectedSilent(string languageCode)
    {
        if (selectedLanguage == languageCode) return;

        try
        {
            isLoading = true;
            StateHasChanged();

            // Fetch translated transcript
            var response = await TranscriptService.GetTranslationAsync(LessonId, languageCode);

            if (response.Success && response.Data != null &&
                response.Data.Status == "Completed" && response.Data.Segments != null)
            {
                // Translation is ready - update UI with translated segments
                selectedLanguage = languageCode;

                // Convert TranslatedSegmentDto to TranscriptSegmentDto for display
                segments = response.Data.Segments.Select(s => new TranscriptSegmentDto
                {
                    Text = s.TranslatedText,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Speaker = null,
                    Confidence = s.Confidence
                }).ToList();

                filteredSegments = segments;
                hasTranscript = true;

                // Update translation status
                translationStatuses[languageCode] = "Completed";

                Console.WriteLine($"[VideoTranscriptViewer] Auto-switched to {GetLanguageName(languageCode)} translation");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VideoTranscriptViewer] Error in silent language switch: {ex.Message}");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }
}

/// <summary>
/// Language option for dropdown selector.
/// Phase 8.5: Multi-Language Subtitle Support.
/// </summary>
public class LanguageOption
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
}
