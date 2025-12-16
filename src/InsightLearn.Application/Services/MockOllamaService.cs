using InsightLearn.Application.Interfaces;
using Microsoft.Extensions.Logging;

// Using directive for the mock service interface

namespace InsightLearn.Application.Services;

/// <summary>
/// Mock implementation of IOllamaService for testing and development
/// Simulates AI responses with 1.5s async delay and provides dummy transcript data
/// </summary>
public class MockOllamaService : IOllamaService, IMockOllamaService
{
    private readonly ILogger<MockOllamaService> _logger;
    private readonly TimeSpan _simulatedDelay = TimeSpan.FromMilliseconds(1500);

    // Dummy transcript data for testing
    private static readonly List<TranscriptSegment> _dummyTranscript = new()
    {
        new TranscriptSegment(0.0, 4.5, "Welcome to this comprehensive course on web development."),
        new TranscriptSegment(4.5, 9.2, "In this lesson, we'll explore the fundamentals of modern JavaScript."),
        new TranscriptSegment(9.2, 14.8, "Understanding these core concepts is essential for building robust applications."),
        new TranscriptSegment(14.8, 20.1, "Let's start by examining how variables work in ES6 and beyond."),
        new TranscriptSegment(20.1, 26.5, "The 'const' and 'let' keywords replaced 'var' for better scoping control."),
        new TranscriptSegment(26.5, 32.0, "Arrow functions provide a more concise syntax for writing functions."),
        new TranscriptSegment(32.0, 38.2, "They also lexically bind the 'this' value, which is incredibly useful."),
        new TranscriptSegment(38.2, 44.5, "Destructuring allows you to extract values from arrays and objects easily."),
        new TranscriptSegment(44.5, 50.8, "Template literals make string concatenation much more readable."),
        new TranscriptSegment(50.8, 57.0, "The spread operator can copy arrays and merge objects efficiently."),
        new TranscriptSegment(57.0, 63.5, "Promises and async/await revolutionized asynchronous programming."),
        new TranscriptSegment(63.5, 70.0, "Classes provide a cleaner syntax for object-oriented programming."),
        new TranscriptSegment(70.0, 76.5, "Modules help organize code into reusable, maintainable pieces."),
        new TranscriptSegment(76.5, 83.0, "Let's now look at some practical examples of these features."),
        new TranscriptSegment(83.0, 90.0, "In the next section, we'll build a complete project using these concepts.")
    };

    // Dummy AI responses for different query types
    private static readonly Dictionary<string, string[]> _responseTemplates = new()
    {
        ["question"] = new[]
        {
            "Great question! Based on my analysis, the key concept here is about understanding the fundamentals. I recommend reviewing the section on core principles.",
            "That's an interesting query. The answer involves understanding the relationship between components. Let me explain the key points.",
            "I can help with that! The main idea is to break down the problem into smaller, manageable parts. Here's what you should focus on."
        },
        ["summary"] = new[]
        {
            "Here's a summary of the key points: 1) Understanding core concepts is fundamental. 2) Practice with real examples. 3) Apply knowledge incrementally.",
            "The main takeaways from this section are: Strong foundations lead to better understanding. Consistent practice improves retention.",
            "To summarize: This lesson covers essential building blocks that form the basis for more advanced topics ahead."
        },
        ["translation"] = new[]
        {
            "Translation complete. The text has been converted while maintaining the original meaning and context.",
            "Here is the translated content, preserving the educational tone and technical accuracy.",
            "The translation is ready. I've ensured that technical terms are appropriately localized."
        },
        ["default"] = new[]
        {
            "I understand your request. Let me help you with that. The most important thing to remember is to approach this step by step.",
            "Thanks for reaching out! Based on the context, I can provide guidance on this topic. Feel free to ask follow-up questions.",
            "I'm here to help! This topic has several interesting aspects we can explore together."
        }
    };

    public MockOllamaService(ILogger<MockOllamaService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("[MockOllamaService] Initialized - Using simulated responses with {Delay}ms delay", _simulatedDelay.TotalMilliseconds);
    }

    public async Task<string> GenerateResponseAsync(string prompt, string? model = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
        }

        _logger.LogInformation("[MockOllamaService] Generating mock response for prompt: {Prompt}",
            prompt.Length > 50 ? prompt.Substring(0, 50) + "..." : prompt);

        // Simulate network delay (1.5 seconds as requested)
        await Task.Delay(_simulatedDelay, cancellationToken);

        // Determine response type based on prompt content
        var responseCategory = DetermineResponseCategory(prompt);
        var responses = _responseTemplates.GetValueOrDefault(responseCategory, _responseTemplates["default"]);

        // Select a random response from the appropriate category
        var random = new Random();
        var selectedResponse = responses[random.Next(responses.Length)];

        _logger.LogInformation("[MockOllamaService] Generated {Category} response with {Length} characters",
            responseCategory, selectedResponse.Length);

        return selectedResponse;
    }

    public async Task<bool> IsAvailableAsync()
    {
        _logger.LogDebug("[MockOllamaService] Checking availability (always returns true for mock)");

        // Simulate minimal delay for health check
        await Task.Delay(100);

        return true;
    }

    public async Task<List<string>> GetAvailableModelsAsync()
    {
        _logger.LogDebug("[MockOllamaService] Returning mock model list");

        // Simulate minimal delay
        await Task.Delay(100);

        return new List<string>
        {
            "qwen2:0.5b",
            "qwen2:1.5b",
            "llama2:7b",
            "mistral:7b",
            "mock-model:test"
        };
    }

    /// <summary>
    /// Gets dummy transcript data for testing video transcription features
    /// </summary>
    /// <param name="videoId">The video ID (used for logging)</param>
    /// <returns>List of transcript segments with timing information</returns>
    public async Task<List<TranscriptSegment>> GetDummyTranscriptAsync(string videoId)
    {
        _logger.LogInformation("[MockOllamaService] Generating dummy transcript for video: {VideoId}", videoId);

        // Simulate transcription delay (1.5 seconds)
        await Task.Delay(_simulatedDelay);

        return _dummyTranscript.ToList();
    }

    /// <summary>
    /// Translates dummy transcript to target language (simulated)
    /// </summary>
    /// <param name="segments">Original transcript segments</param>
    /// <param name="targetLanguage">Target language code (e.g., "es", "fr", "de")</param>
    /// <returns>Translated transcript segments</returns>
    public async Task<List<TranscriptSegment>> TranslateDummyTranscriptAsync(
        List<TranscriptSegment> segments,
        string targetLanguage)
    {
        _logger.LogInformation("[MockOllamaService] Translating {Count} segments to {Language}",
            segments.Count, targetLanguage);

        // Simulate translation delay (1.5 seconds)
        await Task.Delay(_simulatedDelay);

        // Return "translated" segments with language prefix for testing
        var translatedSegments = segments.Select(s => new TranscriptSegment(
            s.StartTime,
            s.EndTime,
            $"[{targetLanguage.ToUpper()}] {s.Text}"
        )).ToList();

        return translatedSegments;
    }

    /// <summary>
    /// Generates AI key takeaways from transcript (simulated)
    /// </summary>
    /// <param name="transcript">Full transcript text</param>
    /// <returns>List of key takeaways</returns>
    public async Task<List<KeyTakeaway>> GenerateDummyTakeawaysAsync(string transcript)
    {
        _logger.LogInformation("[MockOllamaService] Generating dummy takeaways");

        // Simulate AI processing delay
        await Task.Delay(_simulatedDelay);

        return new List<KeyTakeaway>
        {
            new KeyTakeaway("CoreConcept", "ES6+ features like const, let, and arrow functions are essential for modern JavaScript development.", 0.95),
            new KeyTakeaway("BestPractice", "Use destructuring and spread operators to write cleaner, more maintainable code.", 0.88),
            new KeyTakeaway("Example", "Template literals provide a more readable way to construct strings with embedded expressions.", 0.82),
            new KeyTakeaway("Warning", "Be aware of the differences in 'this' binding between regular functions and arrow functions.", 0.91),
            new KeyTakeaway("Summary", "Modern JavaScript provides powerful features that simplify common programming patterns.", 0.85)
        };
    }

    private static string DetermineResponseCategory(string prompt)
    {
        var lowerPrompt = prompt.ToLowerInvariant();

        if (lowerPrompt.Contains("question") || lowerPrompt.Contains("what") ||
            lowerPrompt.Contains("how") || lowerPrompt.Contains("why") || lowerPrompt.Contains("?"))
        {
            return "question";
        }

        if (lowerPrompt.Contains("summary") || lowerPrompt.Contains("summarize") ||
            lowerPrompt.Contains("key points") || lowerPrompt.Contains("takeaway"))
        {
            return "summary";
        }

        if (lowerPrompt.Contains("translate") || lowerPrompt.Contains("translation") ||
            lowerPrompt.Contains("convert to"))
        {
            return "translation";
        }

        return "default";
    }

    /// <summary>
    /// Represents a transcript segment with timing information
    /// </summary>
    public class TranscriptSegment
    {
        public double StartTime { get; }
        public double EndTime { get; }
        public string Text { get; }
        public double? Confidence { get; }

        public TranscriptSegment(double startTime, double endTime, string text, double? confidence = 0.95)
        {
            StartTime = startTime;
            EndTime = endTime;
            Text = text;
            Confidence = confidence;
        }
    }

    /// <summary>
    /// Represents an AI-generated key takeaway
    /// </summary>
    public class KeyTakeaway
    {
        public string Category { get; }
        public string Content { get; }
        public double Relevance { get; }
        public DateTime GeneratedAt { get; }

        public KeyTakeaway(string category, string content, double relevance)
        {
            Category = category;
            Content = content;
            Relevance = relevance;
            GeneratedAt = DateTime.UtcNow;
        }
    }
}
