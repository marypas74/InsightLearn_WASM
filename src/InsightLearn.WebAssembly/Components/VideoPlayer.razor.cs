using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Timers;
using InsightLearn.WebAssembly.Services.LearningSpace;

namespace InsightLearn.WebAssembly.Components;

/// <summary>
/// Enhanced video player component with progress tracking and timestamp navigation.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public partial class VideoPlayer : ComponentBase, IAsyncDisposable
{
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILogger<VideoPlayer> Logger { get; set; } = default!;
    [Inject] private IVideoProgressClientService ProgressService { get; set; } = default!;

    /// <summary>
    /// MongoDB GridFS file ID for video streaming.
    /// </summary>
    [Parameter] public string? VideoFileId { get; set; }

    /// <summary>
    /// Direct video URL (alternative to VideoFileId).
    /// </summary>
    [Parameter] public string? VideoUrl { get; set; }

    /// <summary>
    /// Lesson ID for progress tracking.
    /// </summary>
    [Parameter] public Guid? LessonId { get; set; }

    /// <summary>
    /// Show native HTML5 controls.
    /// </summary>
    [Parameter] public bool ShowControls { get; set; } = true;

    /// <summary>
    /// Show video metadata below player.
    /// </summary>
    [Parameter] public bool ShowMetadata { get; set; } = false;

    /// <summary>
    /// Auto-play on load.
    /// </summary>
    [Parameter] public bool AutoPlay { get; set; } = false;

    /// <summary>
    /// Callback when video time updates (every second).
    /// </summary>
    [Parameter] public EventCallback<VideoTimeUpdate> OnTimeUpdate { get; set; }

    /// <summary>
    /// Callback when video playback state changes (play/pause).
    /// </summary>
    [Parameter] public EventCallback<bool> OnPlayStateChanged { get; set; }

    /// <summary>
    /// Callback when video ends.
    /// </summary>
    [Parameter] public EventCallback OnVideoEnded { get; set; }

    /// <summary>
    /// List of subtitle tracks for the video.
    /// </summary>
    [Parameter] public List<SubtitleTrack>? SubtitleTracks { get; set; }

    /// <summary>
    /// Default subtitle language code (e.g., "en", "it", "es").
    /// </summary>
    [Parameter] public string? DefaultSubtitleLanguage { get; set; }

    private string videoId = $"video_{Guid.NewGuid():N}";
    private string? selectedSubtitleLanguage;
    private bool showSubtitleMenu = false;
    private string videoUrl = "";
    private string contentType = "video/mp4";
    private bool isLoading = true;
    private string errorMessage = "";
    private VideoMetadata? metadata;
    private DotNetObjectReference<VideoPlayer>? dotNetReference;
    private System.Timers.Timer? progressTimer;

    // Current video state
    private double currentTime = 0;
    private double duration = 0;
    private bool isPlaying = false;

    // LinkedIn Learning custom controls state
    private bool controlsVisible = true;
    private bool showVolumeSlider = false;
    private bool showSpeedMenu = false;
    private bool showSettingsMenu = false;
    private double volume = 1.0;
    private double previousVolume = 1.0;
    private bool isMuted = false;
    private double playbackSpeed = 1.0;
    private double progressPercent = 0;
    private double bufferedPercent = 0;
    private bool isFullscreen = false;
    private bool isBuffering = false;
    private double previewTime = 0;
    private double previewPosition = 0;
    private bool showTimePreview = false;
    private System.Threading.Timer? hideControlsTimer;
    private string selectedQuality = "auto";

    // Playback speed options
    public static readonly double[] PlaybackSpeeds = { 0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0 };

    // Translation state
    private bool isTranslating = false;
    private bool isTranslatedSubtitle = false;
    private Dictionary<string, string> translationCache = new();

    /// <summary>
    /// Available languages for AI auto-translation via Ollama
    /// </summary>
    public static readonly List<TranslationLanguage> AvailableTranslationLanguages = new()
    {
        new("it", "🇮🇹 Italiano"),
        new("en", "🇬🇧 English"),
        new("es", "🇪🇸 Español"),
        new("fr", "🇫🇷 Français"),
        new("de", "🇩🇪 Deutsch"),
        new("pt", "🇵🇹 Português"),
        new("ru", "🇷🇺 Русский"),
        new("zh", "🇨🇳 中文"),
        new("ja", "🇯🇵 日本語"),
        new("ko", "🇰🇷 한국어"),
        new("ar", "🇸🇦 العربية"),
        new("hi", "🇮🇳 हिन्दी"),
    };

    public record TranslationLanguage(string Code, string Name);

    protected override async Task OnInitializedAsync()
    {
        dotNetReference = DotNetObjectReference.Create(this);
        await LoadVideo();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !string.IsNullOrEmpty(videoUrl))
        {
            try
            {
                // Initialize enhanced JS interop for LinkedIn Learning style video controls
                await JSRuntime.InvokeVoidAsync("videoPlayer.initializeEnhanced", videoId, dotNetReference);

                // Start progress tracking timer (every 5 seconds)
                progressTimer = new System.Timers.Timer(5000);
                progressTimer.Elapsed += async (sender, e) => await TrackProgressAsync();
                progressTimer.AutoReset = true;
                progressTimer.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error initializing video player JS interop");
            }
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrEmpty(VideoFileId) || !string.IsNullOrEmpty(VideoUrl))
        {
            await LoadVideo();
        }
    }

    private async Task LoadVideo()
    {
        try
        {
            isLoading = true;
            errorMessage = "";

            if (!string.IsNullOrEmpty(VideoUrl))
            {
                videoUrl = VideoUrl;
            }
            else if (!string.IsNullOrEmpty(VideoFileId))
            {
                videoUrl = $"/api/video/stream/{VideoFileId}";

                if (ShowMetadata)
                {
                    var metadataResponse = await Http.GetAsync($"/api/video/metadata/{VideoFileId}");
                    if (metadataResponse.IsSuccessStatusCode)
                    {
                        metadata = await metadataResponse.Content.ReadFromJsonAsync<VideoMetadata>();
                        if (metadata != null)
                        {
                            contentType = metadata.ContentType;
                        }
                    }
                }
            }
            else
            {
                errorMessage = "No video source specified";
                return;
            }

            Logger.LogInformation("Video loaded: {VideoUrl}", videoUrl);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load video: {ex.Message}";
            Logger.LogError(ex, "Error loading video");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task RetryLoad()
    {
        await LoadVideo();
    }

    /// <summary>
    /// Toggle subtitle menu visibility.
    /// </summary>
    private void ToggleSubtitleMenu()
    {
        showSubtitleMenu = !showSubtitleMenu;
    }

    /// <summary>
    /// Select a subtitle track by language code, or null to turn off subtitles.
    /// </summary>
    private async Task SelectSubtitle(string? language)
    {
        selectedSubtitleLanguage = language;
        showSubtitleMenu = false;

        try
        {
            // Use JavaScript to enable/disable subtitle tracks
            await JSRuntime.InvokeVoidAsync("videoPlayer.setSubtitle", videoId, language);
            Logger.LogDebug("Subtitle changed to: {Language}", language ?? "Off");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error setting subtitle: {Language}", language);
        }

        isTranslatedSubtitle = false;
    }

    /// <summary>
    /// Request AI translation of subtitles to target language via Ollama.
    /// </summary>
    private async Task RequestTranslation(string targetLanguage)
    {
        if (LessonId == Guid.Empty)
        {
            Logger.LogWarning("Cannot translate: LessonId is empty");
            return;
        }

        isTranslating = true;
        showSubtitleMenu = false;
        StateHasChanged();

        try
        {
            // Check if we have cached translation
            if (translationCache.TryGetValue(targetLanguage, out var cachedVttUrl))
            {
                await ApplyTranslatedSubtitle(cachedVttUrl, targetLanguage);
                return;
            }

            // Call backend API to get translated subtitles
            var translationUrl = $"api/subtitles/{LessonId}/translate/{targetLanguage}";
            var response = await Http.GetAsync(translationUrl);

            if (response.IsSuccessStatusCode)
            {
                var vttContent = await response.Content.ReadAsStringAsync();

                // Create blob URL for the translated VTT
                var blobUrl = await JSRuntime.InvokeAsync<string>("videoPlayer.createSubtitleBlobUrl", vttContent);

                // Cache and apply
                translationCache[targetLanguage] = blobUrl;
                await ApplyTranslatedSubtitle(blobUrl, targetLanguage);

                Logger.LogInformation("Successfully translated subtitles to {Language}", targetLanguage);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Logger.LogError("Translation failed: {StatusCode} - {Error}", response.StatusCode, error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error requesting translation to {Language}", targetLanguage);
        }
        finally
        {
            isTranslating = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Apply translated subtitle track to video element.
    /// </summary>
    private async Task ApplyTranslatedSubtitle(string vttUrl, string language)
    {
        try
        {
            // Add translated track to video element and enable it
            await JSRuntime.InvokeVoidAsync("videoPlayer.addTranslatedSubtitle", videoId, vttUrl, language,
                AvailableTranslationLanguages.FirstOrDefault(l => l.Code == language)?.Name ?? language);

            selectedSubtitleLanguage = language;
            isTranslatedSubtitle = true;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error applying translated subtitle");
        }
    }

    /// <summary>
    /// Seek to specific timestamp (called from external components like StudentNotesPanel).
    /// </summary>
    public async Task SeekToAsync(double timestampSeconds)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("videoPlayer.seekTo", videoId, timestampSeconds);
            Logger.LogDebug("Seeked to {Timestamp}s", timestampSeconds);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error seeking video to {Timestamp}s", timestampSeconds);
        }
    }

    /// <summary>
    /// Get current video time.
    /// </summary>
    public async Task<double> GetCurrentTimeAsync()
    {
        try
        {
            return await JSRuntime.InvokeAsync<double>("videoPlayer.getCurrentTime", videoId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting current time");
            return 0;
        }
    }

    /// <summary>
    /// Play video.
    /// </summary>
    public async Task PlayAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("videoPlayer.play", videoId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error playing video");
        }
    }

    /// <summary>
    /// Pause video.
    /// </summary>
    public async Task PauseAsync()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("videoPlayer.pause", videoId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error pausing video");
        }
    }

    /// <summary>
    /// Called from JavaScript when video time updates.
    /// </summary>
    [JSInvokable]
    public async Task OnTimeUpdateFromJS(double currentTimeSeconds, double durationSeconds)
    {
        currentTime = currentTimeSeconds;
        duration = durationSeconds;

        // Update progress percentage for custom controls
        progressPercent = durationSeconds > 0 ? (currentTimeSeconds / durationSeconds) * 100 : 0;

        await OnTimeUpdate.InvokeAsync(new VideoTimeUpdate
        {
            CurrentTime = currentTimeSeconds,
            Duration = durationSeconds,
            Progress = progressPercent
        });
    }

    /// <summary>
    /// Called from JavaScript when video play state changes.
    /// </summary>
    [JSInvokable]
    public async Task OnPlayStateChangedFromJS(bool playing)
    {
        isPlaying = playing;
        await OnPlayStateChanged.InvokeAsync(playing);
    }

    /// <summary>
    /// Called from JavaScript when video ends.
    /// </summary>
    [JSInvokable]
    public async Task OnVideoEndedFromJS()
    {
        await OnVideoEnded.InvokeAsync();
    }

    /// <summary>
    /// Called from JavaScript when video error occurs (e.g., codec not supported).
    /// </summary>
    [JSInvokable]
    public Task OnVideoErrorFromJS(string message, int errorCode)
    {
        Logger.LogError("Video playback error: {Message} (Code: {ErrorCode})", message, errorCode);
        errorMessage = message;
        StateHasChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Track progress automatically (called by timer every 5 seconds).
    /// </summary>
    private async Task TrackProgressAsync()
    {
        if (!LessonId.HasValue || !isPlaying)
            return;

        try
        {
            var currentTimeValue = await GetCurrentTimeAsync();

            // Call the video progress service to persist progress
            var progressDto = new TrackVideoProgressDto
            {
                LessonId = LessonId.Value,
                CurrentTimestampSeconds = (int)currentTimeValue,
                TotalDurationSeconds = (int)duration,
                TabActive = true,
                PlaybackSpeed = 1.0m
            };

            var response = await ProgressService.TrackProgressAsync(progressDto);

            if (response.Success)
            {
                Logger.LogDebug("Progress tracked: {Time}s / {Duration}s ({Progress}%)",
                    currentTimeValue, duration, response.Data?.CompletionPercentage ?? 0);
            }
            else
            {
                Logger.LogWarning("Failed to track progress: {Error}", response.Message);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error tracking video progress");
        }
    }

    /// <summary>
    /// Format file size in human-readable format (B, KB, MB, GB).
    /// </summary>
    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:F2} {sizes[order]}";
    }

    #region LinkedIn Learning Custom Controls

    /// <summary>
    /// Format time in MM:SS or HH:MM:SS format.
    /// </summary>
    private string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds))
            return "0:00";

        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    /// <summary>
    /// Show video controls overlay on mouse move.
    /// </summary>
    private void ShowControlsOverlay()
    {
        controlsVisible = true;
        ResetHideControlsTimer();
    }

    /// <summary>
    /// Hide controls after delay when mouse leaves.
    /// </summary>
    private void HideControlsDelayed()
    {
        hideControlsTimer?.Dispose();
        hideControlsTimer = new System.Threading.Timer(_ =>
        {
            if (isPlaying)
            {
                controlsVisible = false;
                InvokeAsync(StateHasChanged);
            }
        }, null, 3000, Timeout.Infinite);
    }

    /// <summary>
    /// Reset the hide controls timer.
    /// </summary>
    private void ResetHideControlsTimer()
    {
        hideControlsTimer?.Dispose();
        hideControlsTimer = new System.Threading.Timer(_ =>
        {
            if (isPlaying)
            {
                controlsVisible = false;
                InvokeAsync(StateHasChanged);
            }
        }, null, 3000, Timeout.Infinite);
    }

    /// <summary>
    /// Toggle play/pause state.
    /// </summary>
    private async Task TogglePlayPause()
    {
        try
        {
            if (isPlaying)
            {
                await PauseAsync();
            }
            else
            {
                await PlayAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error toggling play/pause");
        }
    }

    /// <summary>
    /// Rewind video by 10 seconds.
    /// </summary>
    private async Task Rewind10()
    {
        try
        {
            var newTime = Math.Max(0, currentTime - 10);
            await SeekToAsync(newTime);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error rewinding video");
        }
    }

    /// <summary>
    /// Forward video by 10 seconds.
    /// </summary>
    private async Task Forward10()
    {
        try
        {
            var newTime = Math.Min(duration, currentTime + 10);
            await SeekToAsync(newTime);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error forwarding video");
        }
    }

    /// <summary>
    /// Toggle mute state.
    /// </summary>
    private async Task ToggleMute()
    {
        try
        {
            if (isMuted)
            {
                volume = previousVolume;
                isMuted = false;
            }
            else
            {
                previousVolume = volume;
                volume = 0;
                isMuted = true;
            }
            await JSRuntime.InvokeVoidAsync("videoPlayer.setVolume", videoId, volume);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error toggling mute");
        }
    }

    /// <summary>
    /// Handle volume slider change.
    /// </summary>
    private async Task OnVolumeChange(ChangeEventArgs e)
    {
        try
        {
            if (double.TryParse(e.Value?.ToString(), out var newVolume))
            {
                volume = newVolume / 100;
                isMuted = volume == 0;
                await JSRuntime.InvokeVoidAsync("videoPlayer.setVolume", videoId, volume);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error changing volume");
        }
    }

    /// <summary>
    /// Show volume slider on hover.
    /// </summary>
    private void ShowVolumeSlider() => showVolumeSlider = true;

    /// <summary>
    /// Hide volume slider when mouse leaves.
    /// </summary>
    private void HideVolumeSlider() => showVolumeSlider = false;

    /// <summary>
    /// Get appropriate volume icon based on current state.
    /// </summary>
    private string GetVolumeIcon()
    {
        if (isMuted || volume == 0) return "fa-volume-mute";
        if (volume < 0.3) return "fa-volume-off";
        if (volume < 0.7) return "fa-volume-down";
        return "fa-volume-up";
    }

    /// <summary>
    /// Toggle playback speed menu.
    /// </summary>
    private void ToggleSpeedMenu()
    {
        showSpeedMenu = !showSpeedMenu;
        showSettingsMenu = false;
        showSubtitleMenu = false;
    }

    /// <summary>
    /// Set playback speed.
    /// </summary>
    private async Task SetPlaybackSpeed(double speed)
    {
        try
        {
            playbackSpeed = speed;
            showSpeedMenu = false;
            await JSRuntime.InvokeVoidAsync("videoPlayer.setPlaybackRate", videoId, speed);
            Logger.LogDebug("Playback speed set to {Speed}x", speed);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error setting playback speed");
        }
    }

    /// <summary>
    /// Toggle settings menu.
    /// </summary>
    private void ToggleSettingsMenu()
    {
        showSettingsMenu = !showSettingsMenu;
        showSpeedMenu = false;
        showSubtitleMenu = false;
    }

    /// <summary>
    /// Set video quality (placeholder - requires HLS/DASH implementation).
    /// </summary>
    private void SetQuality(string quality)
    {
        selectedQuality = quality;
        showSettingsMenu = false;
        Logger.LogDebug("Quality set to {Quality}", quality);
        // Note: Actual quality switching requires HLS/DASH implementation
    }

    /// <summary>
    /// Toggle fullscreen mode.
    /// </summary>
    private async Task ToggleFullscreenMode()
    {
        try
        {
            isFullscreen = !isFullscreen;
            await JSRuntime.InvokeVoidAsync("videoPlayer.toggleFullscreen", videoId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error toggling fullscreen");
            isFullscreen = false;
        }
    }

    /// <summary>
    /// Seek to position on progress bar click.
    /// </summary>
    private async Task SeekToPosition(MouseEventArgs e)
    {
        try
        {
            // Calculate position based on click
            // This requires JS interop to get element dimensions
            var percent = await JSRuntime.InvokeAsync<double>("videoPlayer.getClickPosition", videoId, e.ClientX);
            var seekTime = duration * (percent / 100);
            await SeekToAsync(seekTime);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error seeking to position");
        }
    }

    /// <summary>
    /// Show time preview on progress bar hover.
    /// </summary>
    private void ShowPreviewTime(MouseEventArgs e)
    {
        showTimePreview = true;
        // Calculate preview time based on mouse position
        // This would need JS interop for accurate calculation
    }

    /// <summary>
    /// Called from JavaScript to update buffered progress.
    /// </summary>
    [JSInvokable]
    public Task OnBufferedUpdateFromJS(double bufferedPercentage)
    {
        bufferedPercent = bufferedPercentage;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called from JavaScript when buffering state changes.
    /// </summary>
    [JSInvokable]
    public Task OnBufferingStateChangedFromJS(bool buffering)
    {
        isBuffering = buffering;
        InvokeAsync(StateHasChanged);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called from JavaScript when fullscreen state changes.
    /// </summary>
    [JSInvokable]
    public Task OnFullscreenChangedFromJS(bool fullscreen)
    {
        isFullscreen = fullscreen;
        InvokeAsync(StateHasChanged);
        return Task.CompletedTask;
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        try
        {
            progressTimer?.Stop();
            progressTimer?.Dispose();
            hideControlsTimer?.Dispose();

            if (!string.IsNullOrEmpty(videoId))
            {
                await JSRuntime.InvokeVoidAsync("videoPlayer.dispose", videoId);
            }

            dotNetReference?.Dispose();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing VideoPlayer");
        }
    }

    public class VideoMetadata
    {
        public string FileId { get; set; } = "";
        public string FileName { get; set; } = "";
        public long FileSize { get; set; }
        public long CompressedSize { get; set; }
        public string ContentType { get; set; } = "";
        public DateTime UploadDate { get; set; }
        public Guid LessonId { get; set; }
        public string Format { get; set; } = "";
        public double CompressionRatio { get; set; }
    }

    public class VideoTimeUpdate
    {
        public double CurrentTime { get; set; }
        public double Duration { get; set; }
        public double Progress { get; set; }
    }

    /// <summary>
    /// Represents a subtitle/caption track for the video.
    /// </summary>
    public class SubtitleTrack
    {
        /// <summary>
        /// URL to the subtitle file (WebVTT format recommended).
        /// </summary>
        public string Url { get; set; } = "";

        /// <summary>
        /// ISO 639-1 language code (e.g., "en", "it", "es", "fr").
        /// </summary>
        public string Language { get; set; } = "";

        /// <summary>
        /// Human-readable label (e.g., "English", "Italiano", "Español").
        /// </summary>
        public string Label { get; set; } = "";

        /// <summary>
        /// Kind of track: "subtitles", "captions", or "descriptions".
        /// Default is "subtitles".
        /// </summary>
        public string Kind { get; set; } = "subtitles";

        /// <summary>
        /// Whether this track should be the default.
        /// </summary>
        public bool IsDefault { get; set; } = false;
    }
}
