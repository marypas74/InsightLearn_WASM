using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using InsightLearn.WebAssembly.Services.LearningSpace;
using InsightLearn.Core.DTOs.AIChat;
using System.Text.RegularExpressions;

namespace InsightLearn.WebAssembly.Components.LearningSpace;

/// <summary>
/// AI Chat Panel - LinkedIn Learning Style AI Assistant
/// Part of Student Learning Space v2.2.0
/// Provides contextual AI assistance for video lessons.
/// </summary>
public partial class AIChatPanel : ComponentBase, IDisposable
{
    [Inject] private IAIConversationClientService ChatService { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Current lesson ID for context.
    /// </summary>
    [Parameter, EditorRequired]
    public Guid LessonId { get; set; }

    /// <summary>
    /// Current video timestamp in seconds for contextual responses.
    /// </summary>
    [Parameter]
    public int CurrentVideoTimestamp { get; set; }

    /// <summary>
    /// Callback when user clicks a timestamp in AI response.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnTimestampClick { get; set; }

    // Component state
    private ElementReference messagesContainer;
    private List<ChatMessage> Messages { get; set; } = new();
    private string CurrentMessage { get; set; } = string.Empty;
    private bool IsLoading { get; set; } = true;
    private bool IsAvailable { get; set; } = false;
    private bool IsTyping { get; set; } = false;
    private string? ErrorMessage { get; set; }
    private Guid? CurrentSessionId { get; set; }

    // Suggested questions for empty state
    private readonly List<string> SuggestedQuestions = new()
    {
        "What are the key concepts in this video?",
        "Can you summarize what I just watched?",
        "Explain the main points in simple terms",
        "What should I learn from this section?"
    };

    protected override async Task OnInitializedAsync()
    {
        await CheckServiceAvailability();
        IsLoading = false;
    }

    protected override void OnParametersSet()
    {
        // If lesson changed, keep existing session for same lesson
        // Future: could load conversation history here
    }

    private async Task CheckServiceAvailability()
    {
        try
        {
            IsAvailable = await ChatService.IsAvailableAsync();
        }
        catch
        {
            IsAvailable = false;
        }
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(CurrentMessage) || !IsAvailable || IsTyping)
            return;

        var userMessage = CurrentMessage.Trim();
        CurrentMessage = string.Empty;

        // Add user message to chat
        Messages.Add(new ChatMessage
        {
            Content = userMessage,
            IsUser = true,
            Timestamp = DateTime.UtcNow,
            VideoTimestamp = CurrentVideoTimestamp > 0 ? CurrentVideoTimestamp : null
        });

        await ScrollToBottom();

        // Show typing indicator
        IsTyping = true;
        StateHasChanged();

        try
        {
            var messageDto = new AIChatMessageDto
            {
                Message = userMessage,
                SessionId = CurrentSessionId,
                LessonId = LessonId,
                VideoTimestamp = CurrentVideoTimestamp > 0 ? CurrentVideoTimestamp : null
            };

            var response = await ChatService.SendMessageAsync(messageDto);

            IsTyping = false;

            if (response.Success && response.Data != null)
            {
                CurrentSessionId = response.Data.SessionId;

                Messages.Add(new ChatMessage
                {
                    Content = response.Data.Response,
                    IsUser = false,
                    Timestamp = response.Data.Timestamp,
                    VideoTimestamp = response.Data.Context?.VideoTimestamp
                });
            }
            else
            {
                ErrorMessage = response.Message ?? "Failed to get AI response. Please try again.";
            }
        }
        catch (Exception ex)
        {
            IsTyping = false;
            ErrorMessage = $"Error: {ex.Message}";
        }

        await ScrollToBottom();
        StateHasChanged();
    }

    private async Task AskSuggestedQuestion(string question)
    {
        CurrentMessage = question;
        await SendMessage();
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendMessage();
        }
    }

    private async Task ClearConversation()
    {
        if (CurrentSessionId.HasValue)
        {
            await ChatService.DeleteConversationAsync(CurrentSessionId.Value.ToString());
        }

        Messages.Clear();
        CurrentSessionId = null;
        ErrorMessage = null;
        StateHasChanged();
    }

    private async Task SeekToTimestamp(int timestamp)
    {
        if (OnTimestampClick.HasDelegate)
        {
            await OnTimestampClick.InvokeAsync(timestamp);
        }
    }

    private async Task ScrollToBottom()
    {
        try
        {
            await Task.Delay(50); // Small delay for DOM update
            await JS.InvokeVoidAsync("scrollToBottom", messagesContainer);
        }
        catch
        {
            // Ignore scroll errors
        }
    }

    private void DismissError()
    {
        ErrorMessage = null;
        StateHasChanged();
    }

    private static string FormatTimestamp(int seconds)
    {
        var timespan = TimeSpan.FromSeconds(seconds);
        return timespan.Hours > 0
            ? timespan.ToString(@"h\:mm\:ss")
            : timespan.ToString(@"m\:ss");
    }

    private static string FormatMessage(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        // Basic markdown-like formatting
        var formatted = System.Web.HttpUtility.HtmlEncode(content);

        // Bold: **text**
        formatted = Regex.Replace(formatted, @"\*\*(.+?)\*\*", "<strong>$1</strong>");

        // Italic: *text*
        formatted = Regex.Replace(formatted, @"\*(.+?)\*", "<em>$1</em>");

        // Code: `text`
        formatted = Regex.Replace(formatted, @"`(.+?)`", "<code>$1</code>");

        // Line breaks
        formatted = formatted.Replace("\n", "<br/>");

        return formatted;
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    /// <summary>
    /// Internal chat message model.
    /// </summary>
    private class ChatMessage
    {
        public string Content { get; set; } = string.Empty;
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; }
        public int? VideoTimestamp { get; set; }
    }
}
