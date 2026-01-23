using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using InsightLearn.WebAssembly.Components;
using static InsightLearn.WebAssembly.Components.TranscriptJobCard;

namespace InsightLearn.WebAssembly.Pages.Admin;

/// <summary>
/// Code-behind for TranscriptMonitoring page.
/// Handles real-time polling and job management.
/// v2.3.83-dev: Real-time transcript monitoring with chunk visualization.
/// </summary>
public partial class TranscriptMonitoring : IDisposable
{
    private bool isLoading = true;
    private string? error;
    private bool autoRefresh = true;
    private DateTime lastChecked = DateTime.Now;
    private System.Threading.Timer? refreshTimer;
    private const int pollingIntervalSec = 2;

    private List<TranscriptJobDto>? jobs;
    private List<TranscriptJobDto> activeJobs = new();
    private List<TranscriptJobDto> queuedJobs = new();
    private List<TranscriptJobDto> completedJobs = new();
    private List<TranscriptJobDto> failedJobs = new();

    private List<LessonSummary>? lessons;
    private string selectedLessonId = "";
    private string selectedLanguage = "en-US";
    private bool isStartingJob;

    private bool showHistory = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
        await LoadLessons();
        StartAutoRefresh();
    }

    private async Task RefreshData()
    {
        await LoadData(silent: false);
    }

    private void StartAutoRefresh()
    {
        if (autoRefresh)
        {
            refreshTimer = new System.Threading.Timer(async _ =>
            {
                await InvokeAsync(async () =>
                {
                    await LoadData(silent: true);
                    StateHasChanged();
                });
            }, null, TimeSpan.FromSeconds(pollingIntervalSec), TimeSpan.FromSeconds(pollingIntervalSec));
        }
    }

    private void ToggleAutoRefresh()
    {
        if (autoRefresh)
        {
            StartAutoRefresh();
        }
        else
        {
            refreshTimer?.Dispose();
            refreshTimer = null;
        }
    }

    private async Task LoadData(bool silent = false)
    {
        try
        {
            if (!silent) isLoading = true;
            error = null;

            // Call the monitor endpoint
            var response = await ApiClient.GetAsync<List<TranscriptJobDto>>("/api/jobs/transcripts/monitor");

            if (response.Success && response.Data != null)
            {
                jobs = response.Data;
                CategorizeJobs();
            }
            else
            {
                // If endpoint doesn't exist yet, use empty list
                jobs = new List<TranscriptJobDto>();
                CategorizeJobs();
            }

            lastChecked = DateTime.Now;
            if (!silent) isLoading = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TranscriptMonitoring] Load error: {ex.Message}");

            // Fallback: try diagnostics endpoint for compatibility
            try
            {
                var diagResponse = await ApiClient.GetAsync<DiagnosticsResponse>("/api/jobs/transcripts/diagnostics");
                if (diagResponse.Success && diagResponse.Data?.RecentJobs != null)
                {
                    jobs = diagResponse.Data.RecentJobs.Select(j => new TranscriptJobDto
                    {
                        Id = Guid.TryParse(j.Id, out var id) ? id : Guid.NewGuid(),
                        LessonId = j.LessonId,
                        Phase = j.Phase ?? "Unknown",
                        Status = j.Status ?? "Unknown",
                        ProgressPercentage = j.ProgressPercentage,
                        StatusMessage = j.StatusMessage,
                        QueuedAt = j.QueuedAt,
                        StartedAt = j.StartedAt,
                        CompletedAt = j.CompletedAt,
                        ErrorMessage = j.ErrorMessage,
                        ChunkCount = 10,
                        CompletedChunks = j.ProgressPercentage / 10,
                        CurrentChunk = (j.ProgressPercentage / 10) + 1
                    }).ToList();
                    CategorizeJobs();
                    lastChecked = DateTime.Now;
                }
            }
            catch
            {
                if (!silent)
                {
                    error = "Failed to load transcript jobs. Please try again.";
                    Toast.ShowError(error);
                }
            }

            if (!silent) isLoading = false;
        }
    }

    private void CategorizeJobs()
    {
        if (jobs == null) return;

        activeJobs = jobs
            .Where(j => j.Status != "Completed" && j.Status != "Failed" && j.Status != "Timeout" && j.Status != "Queued")
            .OrderByDescending(j => j.QueuedAt)
            .ToList();

        queuedJobs = jobs
            .Where(j => j.Status == "Queued")
            .OrderBy(j => j.QueuedAt)
            .ToList();

        completedJobs = jobs
            .Where(j => j.Status == "Completed")
            .OrderByDescending(j => j.CompletedAt ?? j.QueuedAt)
            .ToList();

        failedJobs = jobs
            .Where(j => j.Status == "Failed" || j.Status == "Timeout")
            .OrderByDescending(j => j.CompletedAt ?? j.QueuedAt)
            .ToList();
    }

    private async Task LoadLessons()
    {
        try
        {
            // Load lessons that have videos (for transcription)
            var response = await ApiClient.GetAsync<List<LessonSummary>>("/api/admin/lessons/with-videos");

            if (response.Success && response.Data != null)
            {
                lessons = response.Data;
            }
            else
            {
                // Fallback: empty list if endpoint doesn't exist
                lessons = new List<LessonSummary>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TranscriptMonitoring] LoadLessons error: {ex.Message}");
            lessons = new List<LessonSummary>();
        }
    }

    private async Task StartTranscription()
    {
        if (string.IsNullOrEmpty(selectedLessonId))
        {
            Toast.ShowWarning("Please select a lesson first.");
            return;
        }

        try
        {
            isStartingJob = true;
            var lessonGuid = Guid.Parse(selectedLessonId);

            // Get lesson title for the request
            var lessonTitle = lessons?.FirstOrDefault(l => l.Id == lessonGuid)?.Title ?? "Unknown";

            var request = new
            {
                LessonTitle = lessonTitle,
                Language = selectedLanguage
            };

            // Use the async Hangfire endpoint: /api/transcripts/{lessonId}/generate
            // This queues a background job and returns immediately with HTTP 202
            var response = await ApiClient.PostAsync<QueuedJobResponse>(
                $"/api/transcripts/{lessonGuid}/generate",
                request);

            if (response.Success && response.Data != null)
            {
                Toast.ShowSuccess($"Transcription job queued! Job ID: {response.Data.JobId}");
                selectedLessonId = "";

                // Refresh immediately to see the new job
                await LoadData(silent: true);
                StateHasChanged();
            }
            else
            {
                Toast.ShowError(response.Message ?? "Failed to queue transcription job.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TranscriptMonitoring] StartTranscription error: {ex.Message}");
            Toast.ShowError($"Error: {ex.Message}");
        }
        finally
        {
            isStartingJob = false;
        }
    }

    private void ToggleHistory()
    {
        showHistory = !showHistory;
    }

    private string GetLessonTitle(Guid lessonId)
    {
        return lessons?.FirstOrDefault(l => l.Id == lessonId)?.Title ?? $"Lesson {lessonId.ToString().Substring(0, 8)}...";
    }

    // Delete transcript functionality (v2.3.93-dev)
    private Guid? lessonToDelete;
    private bool showDeleteConfirm;
    private bool isDeleting;

    private void RequestDelete(Guid lessonId)
    {
        lessonToDelete = lessonId;
        showDeleteConfirm = true;
    }

    private void CancelDelete()
    {
        lessonToDelete = null;
        showDeleteConfirm = false;
    }

    private async Task ConfirmDelete()
    {
        if (lessonToDelete == null) return;

        try
        {
            isDeleting = true;
            var response = await ApiClient.DeleteAsync($"/api/transcripts/{lessonToDelete}");

            if (response.Success)
            {
                Toast.ShowSuccess("Transcript deleted successfully!");
                await LoadData(silent: true);
            }
            else
            {
                Toast.ShowError(response.Message ?? "Failed to delete transcript.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TranscriptMonitoring] Delete error: {ex.Message}");
            Toast.ShowError($"Error deleting transcript: {ex.Message}");
        }
        finally
        {
            isDeleting = false;
            showDeleteConfirm = false;
            lessonToDelete = null;
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        refreshTimer?.Dispose();
    }

    // DTOs
    private class DiagnosticsResponse
    {
        public string? Status { get; set; }
        public List<DiagnosticJob>? RecentJobs { get; set; }
    }

    private class DiagnosticJob
    {
        public string? Id { get; set; }
        public Guid LessonId { get; set; }
        public string? Phase { get; set; }
        public string? Status { get; set; }
        public int ProgressPercentage { get; set; }
        public string? StatusMessage { get; set; }
        public DateTime QueuedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private class LessonSummary
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string? VideoUrl { get; set; }
    }

    // Response from /api/transcripts/{lessonId}/generate (HTTP 202 Accepted)
    private class QueuedJobResponse
    {
        public Guid LessonId { get; set; }
        public string? JobId { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public int EstimatedCompletionSeconds { get; set; }
    }
}
