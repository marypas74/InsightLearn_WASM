using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using InsightLearn.WebAssembly.Services.LearningSpace;
using InsightLearn.WebAssembly.Services.Auth;

namespace InsightLearn.WebAssembly.Components.LearningSpace;

/// <summary>
/// SmartVideoPlayer - Advanced HTML5 video player with AI features.
/// Part of SmartVideoPlayer component stack (v2.2.0-dev)
/// </summary>
public partial class SmartVideoPlayer : IAsyncDisposable
{
    #region Parameters

    [Parameter] public string VideoSource { get; set; } = string.Empty;
    [Parameter] public string VideoId { get; set; } = string.Empty;
    [Parameter] public string LessonId { get; set; } = string.Empty;
    [Parameter] public List<SubtitleTrack>? SubtitleTracks { get; set; }
    [Parameter] public List<VideoBookmark>? Bookmarks { get; set; }
    [Parameter] public bool AutoPlay { get; set; } = false;
    [Parameter] public bool ShowBigPlayButton { get; set; } = true;
    [Parameter] public double StartTime { get; set; } = 0;

    [Parameter] public EventCallback<double> OnTimeChange { get; set; }
    [Parameter] public EventCallback<double> OnProgressUpdate { get; set; }
    [Parameter] public EventCallback OnVideoEnded { get; set; }
    [Parameter] public EventCallback<string> OnErrorOccurred { get; set; }

    #endregion

    #region State

    private string VideoElementId => $"video-{VideoId}";
    private ElementReference _containerRef;
    private ElementReference _progressRef;
    private ElementReference _transcriptRef;
    private DotNetObjectReference<SmartVideoPlayer>? _dotNetRef;

    private double CurrentTime { get; set; }
    private double Duration { get; set; }
    private double Volume { get; set; } = 1.0;
    private double PlaybackRate { get; set; } = 1.0;
    private double BufferedPercentage { get; set; }

    private bool IsPaused { get; set; } = true;
    private bool IsMuted { get; set; } = false;
    private bool IsFullscreen { get; set; } = false;
    private bool IsBuffering { get; set; } = false;
    private bool ShowControls { get; set; } = true;
    private bool ShowSpeedMenu { get; set; } = false;
    private bool ShowSubtitleMenu { get; set; } = false;
    private bool ShowSettingsMenu { get; set; } = false;
    private bool ShowTranscript { get; set; } = false;

    private string ActiveSubtitle { get; set; } = "off";
    private string? ErrorMessage { get; set; }
    private string? DebugInfo { get; set; }
    private bool IsLoadingTimedOut { get; set; } = false;
    private System.Timers.Timer? _loadingTimeoutTimer;
    private const int LoadingTimeoutSeconds = 15;

    private TranscriptData? TranscriptData { get; set; }
    private IEnumerable<TranslationLanguage> TranslationLanguages { get; set; } = [];
    private System.Timers.Timer? _controlsTimer;
    private TranscriptSegment? _activeSegment;

    private static readonly double[] PlaybackRates = [0.5, 0.75, 1.0, 1.25, 1.5, 1.75, 2.0];

    #endregion

    #region Lifecycle

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Start loading timeout timer (shows error if video doesn't load in time)
            StartLoadingTimeout();

            // Log video source for debugging
            Console.WriteLine($"[SmartVideoPlayer] Loading video: {VideoSource}");
            DebugInfo = $"Source: {VideoSource}\nLessonId: {LessonId}\nVideoId: {VideoId}";

            // CRITICAL: Initialize video JS interop FIRST (required for video playback)
            try
            {
                _dotNetRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("VideoInterop.initialize", VideoElementId, _dotNetRef);
                Console.WriteLine($"[SmartVideoPlayer] Video JS interop initialized: {VideoElementId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartVideoPlayer] CRITICAL: Video init failed: {ex.Message}");
                StopLoadingTimeout();
                ErrorMessage = "Failed to initialize video player";
                DebugInfo += $"\nInit Error: {ex.Message}";
                StateHasChanged();
                return; // Cannot continue without video initialization
            }

            // AUTHENTICATED VIDEO LOADING: Fetch video with JWT token and create Blob URL
            // This is required because HTML <video src="..."> tag doesn't send Authorization header
            try
            {
                if (!string.IsNullOrEmpty(VideoSource) && VideoSource.Contains("/api/video/"))
                {
                    Console.WriteLine($"[SmartVideoPlayer] Attempting authenticated video load: {VideoSource}");
                    var token = await TokenService.GetTokenAsync();

                    if (!string.IsNullOrEmpty(token))
                    {
                        Console.WriteLine($"[SmartVideoPlayer] JWT token obtained, loading video with authentication...");
                        var result = await JS.InvokeAsync<VideoLoadResult>("VideoInterop.loadAuthenticatedVideo",
                            VideoElementId, VideoSource, token);

                        Console.WriteLine($"[SmartVideoPlayer] Auth video load result: Success={result.Success}, Status={result.StatusCode}, Message={result.Message}");
                        DebugInfo += $"\nAuth Load: {result.Success} (HTTP {result.StatusCode})";

                        if (!result.Success)
                        {
                            if (result.IsAuthError)
                            {
                                ErrorMessage = "Authentication required. Please log in again.";
                                DebugInfo += $"\nAuth Error: Token may be expired";
                            }
                            else
                            {
                                ErrorMessage = result.Message ?? "Failed to load video";
                            }
                            StopLoadingTimeout();
                            StateHasChanged();
                            return;
                        }

                        if (result.Size > 0)
                        {
                            DebugInfo += $"\nBlob Size: {result.Size / 1024 / 1024:F2} MB";
                            DebugInfo += $"\nContent-Type: {result.ContentType}";
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[SmartVideoPlayer] No JWT token available, trying direct load...");
                        DebugInfo += $"\nNo auth token - trying direct load";
                        // Fall through to let HTML5 video element try direct loading
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartVideoPlayer] Authenticated video load failed: {ex.Message}");
                DebugInfo += $"\nAuth Load Error: {ex.Message}";
                // Continue - video might still work without authentication
            }

            // Load transcript data (non-blocking - video works without this)
            try
            {
                await LoadTranscriptAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartVideoPlayer] Transcript loading skipped: {ex.Message}");
                // Continue - video playback works without transcripts
            }

            // Load translation languages (non-blocking - video works without this)
            try
            {
                TranslationLanguages = TranscriptionService.GetSupportedLanguages();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartVideoPlayer] Translation languages skipped: {ex.Message}");
                TranslationLanguages = [];
            }

            // Start at specified time (optional - video works without this)
            try
            {
                if (StartTime > 0)
                {
                    await JS.InvokeVoidAsync("VideoInterop.seek", VideoElementId, StartTime);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartVideoPlayer] Seek to start time failed: {ex.Message}");
            }

            // Auto-play if requested (optional - video works without this)
            try
            {
                if (AutoPlay)
                {
                    await Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SmartVideoPlayer] Auto-play failed: {ex.Message}");
            }

            // Setup controls auto-hide timer
            _controlsTimer = new System.Timers.Timer(3000);
            _controlsTimer.Elapsed += (s, e) =>
            {
                if (!IsPaused)
                {
                    ShowControls = false;
                    InvokeAsync(StateHasChanged);
                }
            };

            StateHasChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _controlsTimer?.Dispose();
        _loadingTimeoutTimer?.Dispose();
        _dotNetRef?.Dispose();

        // Cleanup Blob URL to free memory (from authenticated video loading)
        try
        {
            await JS.InvokeVoidAsync("VideoInterop.revokeBlobUrl", VideoElementId);
        }
        catch { /* Ignore cleanup errors */ }

        await JS.InvokeVoidAsync("VideoInterop.dispose", VideoElementId);
    }

    #endregion

    #region JS Callbacks

    [JSInvokable]
    public void OnPlay()
    {
        IsPaused = false;
        StartControlsHideTimer();
        StateHasChanged();
    }

    [JSInvokable]
    public void OnPause()
    {
        IsPaused = true;
        ShowControls = true;
        _controlsTimer?.Stop();
        StateHasChanged();
    }

    [JSInvokable]
    public void OnEnded()
    {
        IsPaused = true;
        ShowControls = true;
        OnVideoEnded.InvokeAsync();
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnTimeUpdate(double time)
    {
        CurrentTime = time;
        await OnTimeChange.InvokeAsync(time);

        // Update active transcript segment
        if (TranscriptData != null)
        {
            var newActiveSegment = TranscriptionService.GetActiveSegment(TranscriptData, time);
            if (newActiveSegment != _activeSegment)
            {
                _activeSegment = newActiveSegment;
                await ScrollToActiveSegment();
            }
        }

        StateHasChanged();
    }

    [JSInvokable]
    public void OnMetadataLoaded(VideoMetadata metadata)
    {
        // Video loaded successfully - cancel timeout
        StopLoadingTimeout();
        IsLoadingTimedOut = false;
        Duration = metadata.Duration;
        Console.WriteLine($"[SmartVideoPlayer] Metadata loaded: Duration={metadata.Duration}s, Size={metadata.VideoWidth}x{metadata.VideoHeight}");
        DebugInfo += $"\nDuration: {metadata.Duration:F2}s\nResolution: {metadata.VideoWidth}x{metadata.VideoHeight}";
        StateHasChanged();
    }

    [JSInvokable]
    public void OnVolumeChange(VolumeState state)
    {
        Volume = state.Volume;
        IsMuted = state.Muted;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnRateChange(double rate)
    {
        PlaybackRate = rate;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnBuffering(bool isBuffering)
    {
        IsBuffering = isBuffering;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnBufferProgress(double percentage)
    {
        BufferedPercentage = percentage;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnFullscreenChange(bool isFullscreen)
    {
        IsFullscreen = isFullscreen;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnError(string message)
    {
        StopLoadingTimeout();
        IsBuffering = false;
        ErrorMessage = message;
        Console.WriteLine($"[SmartVideoPlayer] ERROR: {message}");
        Console.WriteLine($"[SmartVideoPlayer] VideoSource: {VideoSource}");
        DebugInfo += $"\nError: {message}";
        OnErrorOccurred.InvokeAsync(message);
        StateHasChanged();
    }

    #endregion

    #region Video Controls

    private async Task Play()
    {
        await JS.InvokeVoidAsync("VideoInterop.play", VideoElementId);
    }

    private async Task Pause()
    {
        await JS.InvokeVoidAsync("VideoInterop.pause", VideoElementId);
    }

    private async Task TogglePlayPause()
    {
        if (IsPaused)
            await Play();
        else
            await Pause();
    }

    private async Task Skip(int seconds)
    {
        await JS.InvokeVoidAsync("VideoInterop.skip", VideoElementId, seconds);
    }

    private async Task Seek(double time)
    {
        await JS.InvokeVoidAsync("VideoInterop.seek", VideoElementId, time);
    }

    /// <summary>
    /// Public method to seek to a specific time in the video.
    /// Called from parent components (e.g., Learn.razor for transcript/note navigation).
    /// </summary>
    public async Task SeekToAsync(double timeInSeconds)
    {
        await Seek(timeInSeconds);
    }

    private async Task SeekToPosition(MouseEventArgs e)
    {
        // Calculate click position relative to progress bar
        var jsArgs = new { elementId = VideoElementId, clientX = e.ClientX };
        var percentage = await JS.InvokeAsync<double>("eval", $@"
            (function() {{
                var el = document.querySelector('.progress-container');
                var rect = el.getBoundingClientRect();
                return ({e.ClientX} - rect.left) / rect.width;
            }})()
        ");
        var seekTime = percentage * Duration;
        await Seek(seekTime);
    }

    private async Task ToggleMute()
    {
        await JS.InvokeVoidAsync("VideoInterop.toggleMute", VideoElementId);
    }

    private async Task OnVolumeInput(ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), out var volume))
        {
            await JS.InvokeVoidAsync("VideoInterop.setVolume", VideoElementId, volume);
        }
    }

    private async Task SetPlaybackRate(double rate)
    {
        await JS.InvokeVoidAsync("VideoInterop.setPlaybackRate", VideoElementId, rate);
        ShowSpeedMenu = false;
    }

    private async Task ToggleFullscreen()
    {
        await JS.InvokeVoidAsync("VideoInterop.toggleFullscreen", VideoElementId);
    }

    private async Task RetryLoad()
    {
        // Reset all error states
        ErrorMessage = null;
        DebugInfo = $"Source: {VideoSource}\nRetrying...";
        IsLoadingTimedOut = false;
        IsBuffering = true;

        // Restart loading timeout
        StartLoadingTimeout();

        StateHasChanged();
        await Task.Delay(100);

        Console.WriteLine($"[SmartVideoPlayer] Retrying video load: {VideoSource}");
        await JS.InvokeVoidAsync("eval", $"document.getElementById('{VideoElementId}').load()");
    }

    #endregion

    #region Subtitles & Translation

    private async Task SetSubtitle(string trackId)
    {
        ActiveSubtitle = trackId;
        await JS.InvokeVoidAsync("VideoInterop.setSubtitleTrack", VideoElementId, trackId);
        ShowSubtitleMenu = false;
    }

    private async Task SetSubtitleOff()
    {
        await SetSubtitle("off");
    }

    private async Task RequestAiTranslation(string languageCode)
    {
        if (TranscriptData == null) return;

        ShowSubtitleMenu = false;

        // Get current segment for translation
        var segment = _activeSegment ?? TranscriptData.Segments.FirstOrDefault();
        if (segment == null) return;

        // Stream translation from AI
        try
        {
            var translatedText = string.Empty;
            await foreach (var chunk in TranscriptionService.StreamTranslationAsync(
                segment.Text, languageCode, CancellationToken.None))
            {
                translatedText += chunk;
                // Could update UI in real-time here
            }

            // Show translated subtitle
            // In production, this would update the subtitle track
            Console.WriteLine($"[SmartVideoPlayer] AI Translation ({languageCode}): {translatedText}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SmartVideoPlayer] Translation error: {ex.Message}");
        }
    }

    #endregion

    #region Transcript

    private async Task LoadTranscriptAsync()
    {
        if (string.IsNullOrEmpty(LessonId)) return;

        try
        {
            var isAvailable = await TranscriptionService.IsAvailableAsync();
            if (isAvailable)
            {
                TranscriptData = await TranscriptionService.GetTranscriptAsync(LessonId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SmartVideoPlayer] Failed to load transcript: {ex.Message}");
        }
    }

    private bool IsSegmentActive(TranscriptSegment segment)
    {
        return _activeSegment?.Index == segment.Index;
    }

    private async Task SeekToSegment(TranscriptSegment segment)
    {
        await Seek(segment.StartTime);
    }

    private async Task ScrollToActiveSegment()
    {
        if (_activeSegment == null) return;

        try
        {
            await JS.InvokeVoidAsync("eval", $@"
                (function() {{
                    var container = document.querySelector('.transcript-content');
                    var active = container?.querySelector('.transcript-segment.active');
                    if (active && container) {{
                        var containerRect = container.getBoundingClientRect();
                        var activeRect = active.getBoundingClientRect();
                        if (activeRect.top < containerRect.top || activeRect.bottom > containerRect.bottom) {{
                            active.scrollIntoView({{ behavior: 'smooth', block: 'center' }});
                        }}
                    }}
                }})()
            ");
        }
        catch { /* Ignore scroll errors */ }
    }

    private async Task CopyTranscript()
    {
        if (TranscriptData == null) return;

        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", TranscriptData.FullText);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SmartVideoPlayer] Failed to copy transcript: {ex.Message}");
        }
    }

    #endregion

    #region Menu Toggles

    private void ToggleSpeedMenu()
    {
        ShowSpeedMenu = !ShowSpeedMenu;
        ShowSubtitleMenu = false;
        ShowSettingsMenu = false;
    }

    private void ToggleSubtitleMenu()
    {
        ShowSubtitleMenu = !ShowSubtitleMenu;
        ShowSpeedMenu = false;
        ShowSettingsMenu = false;
    }

    private void ToggleSettingsMenu()
    {
        ShowSettingsMenu = !ShowSettingsMenu;
        ShowSpeedMenu = false;
        ShowSubtitleMenu = false;
    }

    #endregion

    #region Helpers

    private double ProgressPercentage => Duration > 0 ? (CurrentTime / Duration) * 100 : 0;

    /// <summary>
    /// Start loading timeout timer - if video doesn't load in LoadingTimeoutSeconds, show error
    /// </summary>
    private void StartLoadingTimeout()
    {
        StopLoadingTimeout();
        _loadingTimeoutTimer = new System.Timers.Timer(LoadingTimeoutSeconds * 1000);
        _loadingTimeoutTimer.AutoReset = false;
        _loadingTimeoutTimer.Elapsed += (s, e) =>
        {
            Console.WriteLine($"[SmartVideoPlayer] Loading timeout ({LoadingTimeoutSeconds}s) - video failed to load");
            IsLoadingTimedOut = true;
            IsBuffering = false;
            DebugInfo += $"\nTimeout: Video did not load within {LoadingTimeoutSeconds}s";
            InvokeAsync(StateHasChanged);
        };
        _loadingTimeoutTimer.Start();
        Console.WriteLine($"[SmartVideoPlayer] Loading timeout started: {LoadingTimeoutSeconds}s");
    }

    /// <summary>
    /// Stop loading timeout timer (video loaded successfully or error occurred)
    /// </summary>
    private void StopLoadingTimeout()
    {
        if (_loadingTimeoutTimer != null)
        {
            _loadingTimeoutTimer.Stop();
            _loadingTimeoutTimer.Dispose();
            _loadingTimeoutTimer = null;
        }
    }

    private void StartControlsHideTimer()
    {
        ShowControls = true;
        _controlsTimer?.Stop();
        _controlsTimer?.Start();
    }

    private static string FormatTime(double seconds)
    {
        var time = TimeSpan.FromSeconds(seconds);
        return time.Hours > 0
            ? time.ToString(@"h\:mm\:ss")
            : time.ToString(@"m\:ss");
    }

    #endregion

    #region Models

    public record VideoMetadata(double Duration, int VideoWidth, int VideoHeight);
    public record VolumeState(double Volume, bool Muted);

    /// <summary>
    /// Result from authenticated video loading via Blob URL.
    /// Returned by VideoInterop.loadAuthenticatedVideo JS function.
    /// </summary>
    public class VideoLoadResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int StatusCode { get; set; }
        public bool IsAuthError { get; set; }
        public bool IsNetworkError { get; set; }
        public string? BlobUrl { get; set; }
        public long Size { get; set; }
        public string? ContentType { get; set; }
    }

    public class SubtitleTrack
    {
        public string Src { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class VideoBookmark
    {
        public double Time { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Color { get; set; } = "#ffd700";
    }

    #endregion
}
