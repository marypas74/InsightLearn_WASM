using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace InsightLearn.WebAssembly.Components;

public partial class VideoUpload : ComponentBase
{
    [Parameter] public Guid LessonId { get; set; }
    [Parameter] public Guid UserId { get; set; }
    [Parameter] public string Title { get; set; } = "Upload Video";
    [Parameter] public long MaxFileSize { get; set; } = 500 * 1024 * 1024; // 500MB
    [Parameter] public EventCallback<VideoUploadResult> OnUploadComplete { get; set; }
    [Parameter] public EventCallback<string> OnUploadError { get; set; }

    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private ILogger<VideoUpload> Logger { get; set; } = default!;

    private string errorMessage = string.Empty;
    private string successMessage = string.Empty;
    private bool isUploading = false;
    private double uploadProgress = 0;
    private IBrowserFile? selectedFile;
    private string? selectedFileName;
    private long selectedFileSize;

    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        try
        {
            errorMessage = string.Empty;
            successMessage = string.Empty;
            selectedFile = e.File;
            selectedFileName = selectedFile.Name;
            selectedFileSize = selectedFile.Size;

            // Validate file size
            if (selectedFileSize > MaxFileSize)
            {
                errorMessage = $"File too large. Maximum size: {MaxFileSize / 1024 / 1024}MB";
                selectedFile = null;
                return;
            }

            // Validate file type
            var allowedTypes = new[] { "video/mp4", "video/webm", "video/ogg", "video/quicktime" };
            if (!allowedTypes.Contains(selectedFile.ContentType))
            {
                errorMessage = "Invalid file type. Allowed: MP4, WebM, OGG, MOV";
                selectedFile = null;
                return;
            }

            Logger.LogInformation("File selected: {FileName} ({Size} bytes)", selectedFileName, selectedFileSize);
        }
        catch (Exception ex)
        {
            errorMessage = $"Error selecting file: {ex.Message}";
            Logger.LogError(ex, "Error selecting file");
        }
    }

    private async Task UploadVideo()
    {
        if (selectedFile == null)
        {
            errorMessage = "Please select a video file";
            return;
        }

        if (LessonId == Guid.Empty)
        {
            errorMessage = "Invalid lesson ID";
            return;
        }

        try
        {
            isUploading = true;
            uploadProgress = 0;
            errorMessage = string.Empty;
            successMessage = string.Empty;

            using var content = new MultipartFormDataContent();

            // Add video file
            var fileContent = new StreamContent(selectedFile.OpenReadStream(MaxFileSize));
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(selectedFile.ContentType);
            content.Add(fileContent, "video", selectedFile.Name);

            // Add metadata
            content.Add(new StringContent(LessonId.ToString()), "lessonId");
            content.Add(new StringContent(UserId.ToString()), "userId");

            Logger.LogInformation("Uploading video: {FileName} for lesson {LessonId}", selectedFileName, LessonId);

            // Upload to API
            var response = await Http.PostAsync("/api/video/upload", content);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<VideoUploadResult>();
                if (result != null)
                {
                    successMessage = $"Video uploaded successfully! ID: {result.VideoId}";
                    Logger.LogInformation("Video uploaded: {VideoId}", result.VideoId);

                    // Notify parent component
                    await OnUploadComplete.InvokeAsync(result);

                    // Clear selection
                    selectedFile = null;
                    selectedFileName = null;
                }
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                errorMessage = $"Upload failed: {error}";
                Logger.LogError("Upload failed with status {StatusCode}: {Error}", response.StatusCode, error);

                await OnUploadError.InvokeAsync(errorMessage);
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Upload error: {ex.Message}";
            Logger.LogError(ex, "Error uploading video");
            await OnUploadError.InvokeAsync(errorMessage);
        }
        finally
        {
            isUploading = false;
            uploadProgress = 0;
        }
    }

    private void CancelUpload()
    {
        selectedFile = null;
        selectedFileName = null;
        errorMessage = string.Empty;
        successMessage = string.Empty;
    }
}

public class VideoUploadResult
{
    public string VideoId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public long CompressedSize { get; set; }
    public double CompressionRatio { get; set; }
    public string Message { get; set; } = string.Empty;
}
