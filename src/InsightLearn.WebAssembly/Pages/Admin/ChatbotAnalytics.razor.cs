using Microsoft.AspNetCore.Components;
using InsightLearn.WebAssembly.Services.Http;
using Blazored.Toast.Services;

namespace InsightLearn.WebAssembly.Pages.Admin;

public partial class ChatbotAnalytics
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IApiClient ApiClient { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;

    private bool isLoading = true;
    private string? error;
    private string selectedPeriod = "7days";
    private string chartView = "conversations";

    // Data models
    private ChatbotStats? stats;
    private List<UsageDataPoint> usageData = new();
    private List<TopQuestion> topQuestions = new();
    private List<ResponseTimeBucket> responseTimeBuckets = new();
    private List<PopularTopic> popularTopics = new();
    private List<PeakHour> peakHours = new();
    private List<RecentConversation> recentConversations = new();

    // Modal state
    private bool showConversationModal = false;
    private ConversationDetail? selectedConversation;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task RefreshData()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        try
        {
            isLoading = true;
            error = null;
            StateHasChanged();

            await Task.Delay(300); // Simulated API call

            // Load mock data
            stats = new ChatbotStats
            {
                TotalConversations = 12847,
                ConversationsTrend = 15.3,
                TotalMessages = 89234,
                MessagesTrend = 22.1,
                AvgResponseTime = 1.73,
                ResponseTimeTrend = -8.5,
                SatisfactionRate = 87.2,
                SatisfactionTrend = 3.2,
                UniqueUsers = 4523,
                AvgMessagesPerSession = 6.9,
                AvgSessionDuration = 245,
                ResolutionRate = 82.5,
                ErrorCount = 127,
                P50ResponseTime = 1.42,
                P90ResponseTime = 2.85,
                P99ResponseTime = 4.21,
                PositiveFeedback = 8234,
                NegativeFeedback = 1205,
                NoFeedback = 3408,
                PositiveFeedbackPercent = 87.2,
                ModelName = "qwen2:0.5b",
                TotalTokensGenerated = 15678923,
                AvgTokensPerResponse = 175,
                ContextWindowUsage = 42.3,
                ModelUptime = 99.87
            };

            usageData = GenerateMockUsageData();
            topQuestions = GenerateMockTopQuestions();
            responseTimeBuckets = GenerateMockResponseBuckets();
            popularTopics = GenerateMockTopics();
            peakHours = GenerateMockPeakHours();
            recentConversations = GenerateMockConversations();

            isLoading = false;
        }
        catch (Exception ex)
        {
            error = "An error occurred while loading analytics";
            Toast.ShowError(error);
            Console.WriteLine($"ChatbotAnalytics error: {ex}");
            isLoading = false;
        }
    }

    private async Task OnPeriodChanged()
    {
        await LoadData();
    }

    private async Task ExportReport()
    {
        Toast.ShowInfo("Generating analytics report...");
        await Task.Delay(500);
        Toast.ShowSuccess("Report exported successfully");
    }

    private void ViewAllConversations()
    {
        Navigation.NavigateTo("/admin/chatbot/conversations");
    }

    private async Task ViewConversation(string sessionId)
    {
        selectedConversation = new ConversationDetail
        {
            SessionId = sessionId,
            UserName = "John Doe",
            UserEmail = "john.doe@example.com",
            StartedAt = DateTime.Now.AddHours(-2),
            Duration = 245,
            Messages = new List<ChatMessage>
            {
                new ChatMessage { IsUser = true, Text = "How do I access my purchased courses?", Timestamp = DateTime.Now.AddHours(-2), ResponseTime = 0, Tokens = 0 },
                new ChatMessage { IsUser = false, Text = "Great question! To access your purchased courses, please follow these steps:\n\n1. Log into your InsightLearn account\n2. Click on 'My Learning' in the top navigation\n3. You'll see all your enrolled courses listed there\n\nIs there anything else I can help you with?", Timestamp = DateTime.Now.AddHours(-2).AddSeconds(2), ResponseTime = 1.82, Tokens = 156 },
                new ChatMessage { IsUser = true, Text = "Where can I find my certificates?", Timestamp = DateTime.Now.AddHours(-1).AddMinutes(50), ResponseTime = 0, Tokens = 0 },
                new ChatMessage { IsUser = false, Text = "Certificates can be found in the 'Achievements' section of your profile. Once you complete a course, your certificate will automatically appear there. You can download it as a PDF or share it directly to LinkedIn!", Timestamp = DateTime.Now.AddHours(-1).AddMinutes(50).AddSeconds(2), ResponseTime = 1.65, Tokens = 142 },
            }
        };
        showConversationModal = true;
        await Task.CompletedTask;
    }

    private async Task DeleteConversation(string sessionId)
    {
        try
        {
            Toast.ShowInfo("Deleting conversation...");
            await Task.Delay(500);
            recentConversations.RemoveAll(c => c.SessionId == sessionId);
            CloseModals();
            Toast.ShowSuccess("Conversation deleted successfully");
        }
        catch (Exception ex)
        {
            Toast.ShowError("Failed to delete conversation");
            Console.WriteLine($"Delete error: {ex}");
        }
    }

    private void CloseModals()
    {
        showConversationModal = false;
        selectedConversation = null;
    }

    private double GetBarHeight(int value)
    {
        if (!usageData.Any()) return 0;
        var max = usageData.Max(d => d.Value);
        return max > 0 ? (double)value / max * 100 : 0;
    }

    private string FormatDuration(int seconds)
    {
        if (seconds < 60) return $"{seconds}s";
        if (seconds < 3600) return $"{seconds / 60}m {seconds % 60}s";
        return $"{seconds / 3600}h {(seconds % 3600) / 60}m";
    }

    private string FormatNumber(long number)
    {
        if (number >= 1000000) return $"{number / 1000000.0:F1}M";
        if (number >= 1000) return $"{number / 1000.0:F1}K";
        return number.ToString("N0");
    }

    private string GetCategoryClass(string category) => category.ToLower() switch
    {
        "course" => "course",
        "account" => "account",
        "technical" => "technical",
        "billing" => "billing",
        _ => "general"
    };

    private int GetTopicFontSize(int count)
    {
        var min = popularTopics.Min(t => t.Count);
        var max = popularTopics.Max(t => t.Count);
        var range = max - min;
        if (range == 0) return 14;
        var normalized = (double)(count - min) / range;
        return (int)(12 + normalized * 10);
    }

    private string GetIntensityClass(int intensity) => intensity switch
    {
        < 20 => "low",
        < 40 => "medium-low",
        < 60 => "medium",
        < 80 => "medium-high",
        _ => "high"
    };

    // Mock data generators
    private List<UsageDataPoint> GenerateMockUsageData()
    {
        var data = new List<UsageDataPoint>();
        var labels = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        var random = new Random(42);
        foreach (var label in labels)
        {
            data.Add(new UsageDataPoint { Label = label, Value = random.Next(1200, 2500) });
        }
        return data;
    }

    private List<TopQuestion> GenerateMockTopQuestions()
    {
        return new List<TopQuestion>
        {
            new TopQuestion { Rank = 1, Text = "How do I access my purchased courses?", Count = 1247, Category = "Course", Trend = 12 },
            new TopQuestion { Rank = 2, Text = "How can I get a refund?", Count = 892, Category = "Billing", Trend = -5 },
            new TopQuestion { Rank = 3, Text = "Why isn't my video playing?", Count = 756, Category = "Technical", Trend = 8 },
            new TopQuestion { Rank = 4, Text = "How do I change my password?", Count = 623, Category = "Account", Trend = 3 },
            new TopQuestion { Rank = 5, Text = "Where can I find my certificates?", Count = 512, Category = "Course", Trend = 15 },
        };
    }

    private List<ResponseTimeBucket> GenerateMockResponseBuckets()
    {
        return new List<ResponseTimeBucket>
        {
            new ResponseTimeBucket { Label = "< 1s", Percentage = 32.5, ColorClass = "excellent" },
            new ResponseTimeBucket { Label = "1-2s", Percentage = 45.2, ColorClass = "good" },
            new ResponseTimeBucket { Label = "2-3s", Percentage = 15.8, ColorClass = "average" },
            new ResponseTimeBucket { Label = "3-5s", Percentage = 5.2, ColorClass = "slow" },
            new ResponseTimeBucket { Label = "> 5s", Percentage = 1.3, ColorClass = "critical" },
        };
    }

    private List<PopularTopic> GenerateMockTopics()
    {
        return new List<PopularTopic>
        {
            new PopularTopic { Name = "Course Access", Count = 245 },
            new PopularTopic { Name = "Payments", Count = 189 },
            new PopularTopic { Name = "Certificates", Count = 156 },
            new PopularTopic { Name = "Account", Count = 134 },
            new PopularTopic { Name = "Video Issues", Count = 112 },
            new PopularTopic { Name = "Refunds", Count = 98 },
            new PopularTopic { Name = "Password Reset", Count = 87 },
            new PopularTopic { Name = "Enrollment", Count = 76 },
            new PopularTopic { Name = "Mobile App", Count = 65 },
            new PopularTopic { Name = "Progress", Count = 54 },
        };
    }

    private List<PeakHour> GenerateMockPeakHours()
    {
        var hours = new List<PeakHour>();
        var random = new Random(123);
        for (int i = 0; i < 24; i++)
        {
            var baseIntensity = (i >= 9 && i <= 21) ? 50 : 20;
            var peakBonus = (i >= 19 && i <= 21) ? 30 : 0;
            hours.Add(new PeakHour
            {
                Hour = i,
                Intensity = Math.Min(100, baseIntensity + peakBonus + random.Next(-15, 15)),
                Count = random.Next(50, 500)
            });
        }
        return hours;
    }

    private List<RecentConversation> GenerateMockConversations()
    {
        return new List<RecentConversation>
        {
            new RecentConversation { SessionId = "abc123def456", UserName = "John Doe", UserEmail = "john@example.com", StartedAt = DateTime.Now.AddHours(-1), Duration = 245, MessageCount = 8, Feedback = "positive" },
            new RecentConversation { SessionId = "xyz789ghi012", UserName = "Jane Smith", UserEmail = "jane@example.com", StartedAt = DateTime.Now.AddHours(-2), Duration = 180, MessageCount = 5, Feedback = "positive" },
            new RecentConversation { SessionId = "mno345pqr678", UserName = "Bob Wilson", UserEmail = "bob@example.com", StartedAt = DateTime.Now.AddHours(-3), Duration = 420, MessageCount = 12, Feedback = "negative" },
            new RecentConversation { SessionId = "stu901vwx234", UserName = "Alice Brown", UserEmail = "alice@example.com", StartedAt = DateTime.Now.AddHours(-4), Duration = 90, MessageCount = 3, Feedback = "none" },
            new RecentConversation { SessionId = "yza567bcd890", UserName = "Charlie Davis", UserEmail = "charlie@example.com", StartedAt = DateTime.Now.AddHours(-5), Duration = 310, MessageCount = 9, Feedback = "positive" },
        };
    }

    // Internal Models
    private class ChatbotStats
    {
        public int TotalConversations { get; set; }
        public double ConversationsTrend { get; set; }
        public int TotalMessages { get; set; }
        public double MessagesTrend { get; set; }
        public double AvgResponseTime { get; set; }
        public double ResponseTimeTrend { get; set; }
        public double SatisfactionRate { get; set; }
        public double SatisfactionTrend { get; set; }
        public int UniqueUsers { get; set; }
        public double AvgMessagesPerSession { get; set; }
        public int AvgSessionDuration { get; set; }
        public double ResolutionRate { get; set; }
        public int ErrorCount { get; set; }
        public double P50ResponseTime { get; set; }
        public double P90ResponseTime { get; set; }
        public double P99ResponseTime { get; set; }
        public int PositiveFeedback { get; set; }
        public int NegativeFeedback { get; set; }
        public int NoFeedback { get; set; }
        public double PositiveFeedbackPercent { get; set; }
        public string ModelName { get; set; } = "";
        public long TotalTokensGenerated { get; set; }
        public double AvgTokensPerResponse { get; set; }
        public double ContextWindowUsage { get; set; }
        public double ModelUptime { get; set; }
    }

    private class UsageDataPoint
    {
        public string Label { get; set; } = "";
        public int Value { get; set; }
    }

    private class TopQuestion
    {
        public int Rank { get; set; }
        public string Text { get; set; } = "";
        public int Count { get; set; }
        public string Category { get; set; } = "";
        public int Trend { get; set; }
    }

    private class ResponseTimeBucket
    {
        public string Label { get; set; } = "";
        public double Percentage { get; set; }
        public string ColorClass { get; set; } = "";
    }

    private class PopularTopic
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    private class PeakHour
    {
        public int Hour { get; set; }
        public int Intensity { get; set; }
        public int Count { get; set; }
    }

    private class RecentConversation
    {
        public string SessionId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public DateTime StartedAt { get; set; }
        public int Duration { get; set; }
        public int MessageCount { get; set; }
        public string Feedback { get; set; } = "";
    }

    private class ConversationDetail
    {
        public string SessionId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public DateTime StartedAt { get; set; }
        public int Duration { get; set; }
        public List<ChatMessage> Messages { get; set; } = new();
    }

    private class ChatMessage
    {
        public bool IsUser { get; set; }
        public string Text { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public double ResponseTime { get; set; }
        public int Tokens { get; set; }
    }
}
