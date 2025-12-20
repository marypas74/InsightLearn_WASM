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
    private int? copiedMessageId { get; set; }
    private bool shouldAutoScroll { get; set; } = true;
    private string? lastUserQuestion { get; set; }

    // Suggested questions for empty state - LinkedIn Learning style
    private readonly List<string> SuggestedQuestions = new()
    {
        "What are the key concepts covered so far?",
        "Can you summarize this section for me?",
        "Explain the main points in simpler terms",
        "What practical skills will I learn here?"
    };

    // Quick action prompts - contextual questions based on video timestamp
    private readonly Dictionary<string, string> QuickActionPrompts = new()
    {
        { "summarize", "Please summarize the key points covered up to this point in the video. Focus on the main takeaways I should remember." },
        { "explain", "Can you explain the concepts being discussed at this point in the video? Break them down in a way that's easy to understand." },
        { "quiz", "Based on what I've watched so far, create 3 quick quiz questions to test my understanding. Include the answers after I respond." },
        { "examples", "Can you provide practical examples or code snippets related to what's being explained at this point in the video?" }
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
            Console.WriteLine("[AIChatPanel] Checking Ollama service availability...");
            IsAvailable = await ChatService.IsAvailableAsync();

            if (IsAvailable)
            {
                Console.WriteLine("[AIChatPanel] ✅ Ollama service is available and ready");
            }
            else
            {
                Console.WriteLine("[AIChatPanel] ⚠️ Ollama service returned not available");
            }
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            Console.WriteLine($"[AIChatPanel] ❌ Ollama connection failed: {ex.Message}");
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

    /// <summary>
    /// Handle quick action button clicks - LinkedIn Learning style contextual prompts.
    /// </summary>
    private async Task AskQuickAction(string actionType)
    {
        if (!QuickActionPrompts.TryGetValue(actionType, out var prompt))
            return;

        CurrentMessage = prompt;
        await SendMessage();
    }

    /// <summary>
    /// Copy AI response to clipboard with visual feedback.
    /// </summary>
    private async Task CopyMessage(string content)
    {
        try
        {
            var success = await JS.InvokeAsync<bool>("copyToClipboard", content);
            if (success)
            {
                copiedMessageId = content.GetHashCode();
                StateHasChanged();

                // Reset the copied indicator after 2 seconds
                await Task.Delay(2000);
                copiedMessageId = null;
                StateHasChanged();
            }
        }
        catch
        {
            // Clipboard access failed, silently ignore
        }
    }

    /// <summary>
    /// Regenerate the last AI response by re-asking the previous question.
    /// </summary>
    private async Task RegenerateResponse(ChatMessage aiMessage)
    {
        // Find the user message that preceded this AI response
        var aiIndex = Messages.IndexOf(aiMessage);
        if (aiIndex <= 0) return;

        var userMessage = Messages.Take(aiIndex).LastOrDefault(m => m.IsUser);
        if (userMessage == null) return;

        // Remove the AI response we're regenerating
        Messages.Remove(aiMessage);
        StateHasChanged();

        // Re-send the user's question
        CurrentMessage = userMessage.Content;

        // Remove the original user message since SendMessage will add it again
        Messages.Remove(userMessage);

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
