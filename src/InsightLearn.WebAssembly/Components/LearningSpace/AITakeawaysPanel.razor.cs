using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using InsightLearn.WebAssembly.Services.LearningSpace;
using Blazored.Toast.Services;
using InsightLearn.Core.DTOs.AITakeaways;

namespace InsightLearn.WebAssembly.Components.LearningSpace;

/// <summary>
/// AI-powered key takeaways panel for video lessons.
/// Part of Student Learning Space v2.1.0.
/// </summary>
public partial class AITakeawaysPanel : ComponentBase
{
    [Inject] private IAITakeawayClientService TakeawayService { get; set; } = default!;
    [Inject] private IToastService ToastService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    /// <summary>
    /// Lesson ID to display takeaways for.
    /// </summary>
    [Parameter] public Guid LessonId { get; set; }

    /// <summary>
    /// Callback when user clicks a takeaway timestamp (to seek video).
    /// </summary>
    [Parameter] public EventCallback<double> OnTimestampClick { get; set; }

    private VideoKeyTakeawaysDto? takeaways = null;
    private List<TakeawayDto> filteredTakeaways = new();
    private bool isLoading = false;
    private bool hasTakeaways = false;
    private bool isAuthenticated = false;
    private string processingStatus = "Unknown";
    private string? errorMessage = null;
    private string selectedCategory = "All";
    private Dictionary<string, int> categoryCounts = new();

    private readonly List<string> categories = new()
    {
        "All",
        "CoreConcept",
        "BestPractice",
        "Example",
        "Warning",
        "Summary"
    };

    private readonly Dictionary<string, string> categoryIcons = new()
    {
        { "CoreConcept", "💡" },
        { "BestPractice", "⭐" },
        { "Example", "📌" },
        { "Warning", "⚠️" },
        { "Summary", "📋" }
    };

    private readonly Dictionary<string, string> categoryColors = new()
    {
        { "CoreConcept", "#4a90e2" },
        { "BestPractice", "#f39c12" },
        { "Example", "#9b59b6" },
        { "Warning", "#e74c3c" },
        { "Summary", "#2ecc71" }
    };

    protected override async Task OnParametersSetAsync()
    {
        // Check authentication state first
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        isAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

        if (LessonId != Guid.Empty && !hasTakeaways && isAuthenticated)
        {
            await LoadTakeawaysAsync();
        }
    }

    private async Task LoadTakeawaysAsync()
    {
        isLoading = true;
        errorMessage = null;
        hasTakeaways = false;

        try
        {
            var response = await TakeawayService.GetTakeawaysAsync(LessonId);
            if (response.Success && response.Data != null)
            {
                takeaways = response.Data;

                // v2.1.0-dev: Check ProcessingStatus in Metadata for better UX
                var status = takeaways.Metadata?.ProcessingStatus ?? "NotGenerated";

                if (status == "NotGenerated" || (takeaways.Takeaways?.Count ?? 0) == 0)
                {
                    // No takeaways yet - show friendly message instead of error
                    processingStatus = "NotGenerated";
                    errorMessage = null; // No error - just not generated yet
                    hasTakeaways = false;
                }
                else if (status == "Processing" || status == "Queued")
                {
                    processingStatus = status;
                    errorMessage = "AI is analyzing this video. This may take a few minutes.";
                    hasTakeaways = false;
                }
                else if (status == "Failed")
                {
                    processingStatus = "Failed";
                    errorMessage = "AI analysis failed. Please try again later.";
                    hasTakeaways = false;
                }
                else
                {
                    // Takeaways exist and are completed
                    filteredTakeaways = takeaways.Takeaways ?? new();
                    hasTakeaways = filteredTakeaways.Count > 0;
                    processingStatus = "Completed";
                    CalculateCategoryCounts();
                    FilterByCategory(selectedCategory);
                }
            }
            else
            {
                // API call failed - show error but don't toast (it's expected for new lessons)
                errorMessage = response.Message ?? "Failed to load takeaways";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading takeaways: {ex.Message}";
            ToastService.ShowError(errorMessage);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void CalculateCategoryCounts()
    {
        categoryCounts.Clear();
        if (takeaways?.Takeaways == null)
            return;

        foreach (var category in categories.Where(c => c != "All"))
        {
            categoryCounts[category] = takeaways.Takeaways.Count(t => t.Category == category);
        }
    }

    private void FilterByCategory(string category)
    {
        selectedCategory = category;

        if (takeaways?.Takeaways == null)
        {
            filteredTakeaways = new();
            return;
        }

        if (category == "All")
        {
            filteredTakeaways = takeaways.Takeaways;
        }
        else
        {
            filteredTakeaways = takeaways.Takeaways
                .Where(t => t.Category == category)
                .ToList();
        }
    }

    private async Task OnTakeawayClickAsync(TakeawayDto takeaway)
    {
        if (takeaway.TimestampStart.HasValue && OnTimestampClick.HasDelegate)
        {
            await OnTimestampClick.InvokeAsync(takeaway.TimestampStart.Value);
        }
    }

    private async Task SubmitFeedbackAsync(string takeawayId, int feedback)
    {
        try
        {
            var dto = new SubmitFeedbackDto
            {
                TakeawayId = takeawayId,
                Feedback = feedback
            };

            var response = await TakeawayService.SubmitFeedbackAsync(LessonId, dto);
            if (response.Success)
            {
                // Update local feedback state
                var takeaway = filteredTakeaways.FirstOrDefault(t => t.TakeawayId == takeawayId);
                if (takeaway != null)
                {
                    takeaway.UserFeedback = feedback;
                    StateHasChanged();
                }

                ToastService.ShowSuccess("Feedback submitted");
            }
            else
            {
                ToastService.ShowError(response.Message ?? "Failed to submit feedback");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Error submitting feedback: {ex.Message}");
        }
    }

    private async Task RequestGenerationAsync()
    {
        // This would typically require admin/instructor role
        ToastService.ShowInfo("Contact instructor to generate AI takeaways for this lesson");
        await Task.CompletedTask;
    }

    private void NavigateToLogin()
    {
        var returnUrl = Navigation.Uri;
        Navigation.NavigateTo($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    private string GetCategoryIcon(string category)
    {
        return categoryIcons.TryGetValue(category, out var icon) ? icon : "📄";
    }

    private string GetCategoryColor(string category)
    {
        return categoryColors.TryGetValue(category, out var color) ? color : "#999";
    }

    private string GetRelevanceClass(double score)
    {
        if (score >= 0.8)
            return "relevance-high";
        if (score >= 0.6)
            return "relevance-medium";
        return "relevance-low";
    }

    private string GetRelevanceLabel(double score)
    {
        if (score >= 0.8)
            return "High";
        if (score >= 0.6)
            return "Medium";
        return "Low";
    }

    private string FormatTimestamp(double seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.Hours > 0)
            return ts.ToString(@"h\:mm\:ss");
        return ts.ToString(@"mm\:ss");
    }
}
