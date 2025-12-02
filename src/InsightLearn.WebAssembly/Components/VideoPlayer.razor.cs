using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Timers;
using System.Net.Http.Json;
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

    private string videoId = $"video_{Guid.NewGuid():N}";
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
                // Initialize JS interop for video events
                await JSRuntime.InvokeVoidAsync("videoPlayer.initialize", videoId, dotNetReference);

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

        await OnTimeUpdate.InvokeAsync(new VideoTimeUpdate
        {
            CurrentTime = currentTimeSeconds,
            Duration = durationSeconds,
            Progress = durationSeconds > 0 ? (currentTimeSeconds / durationSeconds) * 100 : 0
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

    public async ValueTask DisposeAsync()
    {
        try
        {
            progressTimer?.Stop();
            progressTimer?.Dispose();

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
}
