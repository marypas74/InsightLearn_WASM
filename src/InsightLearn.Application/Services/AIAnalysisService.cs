using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InsightLearn.Core.DTOs.AITakeaways;
using InsightLearn.Core.Interfaces;

namespace InsightLearn.Application.Services
{
    /// <summary>
    /// Service for AI-powered content analysis using Ollama (qwen2:0.5b).
    /// Extracts key takeaways, concepts, and best practices from video transcripts.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class AIAnalysisService : IAIAnalysisService
    {
        private readonly IAITakeawayRepository _repository;
        private readonly IDistributedCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AIAnalysisService> _logger;
        private readonly IConfiguration _configuration;

        private const string CacheKeyPrefix = "takeaways:";
        private const int CacheDurationMinutes = 120; // 2 hours cache (longer than transcripts)

        public AIAnalysisService(
            IAITakeawayRepository repository,
            IDistributedCache cache,
            IHttpClientFactory httpClientFactory,
            ILogger<AIAnalysisService> logger,
            IConfiguration configuration)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<VideoKeyTakeawaysDto?> GetTakeawaysAsync(Guid lessonId, CancellationToken ct = default)
        {
            // 1. Try Redis cache first
            var cacheKey = $"{CacheKeyPrefix}{lessonId}";
            var cachedJson = await _cache.GetStringAsync(cacheKey, ct);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                _logger.LogDebug("Takeaways cache HIT for lesson {LessonId}", lessonId);
                return JsonSerializer.Deserialize<VideoKeyTakeawaysDto>(cachedJson);
            }

            _logger.LogDebug("Takeaways cache MISS for lesson {LessonId}", lessonId);

            // 2. Get from database
            var takeaways = await _repository.GetTakeawaysAsync(lessonId, ct);

            if (takeaways == null)
                return null;

            // 3. Cache the result
            var json = JsonSerializer.Serialize(takeaways);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheDurationMinutes)
            };
            await _cache.SetStringAsync(cacheKey, json, cacheOptions, ct);

            return takeaways;
        }

        public async Task<string> QueueTakeawayGenerationAsync(Guid lessonId, string? transcriptText = null, CancellationToken ct = default)
        {
            _logger.LogInformation("Queueing takeaway generation for lesson {LessonId}", lessonId);

            // Check if already exists
            var existing = await _repository.GetMetadataAsync(lessonId, ct);
            if (existing != null)
            {
                _logger.LogWarning("Takeaways already exist for lesson {LessonId}, status: {Status}", lessonId, existing.ProcessingStatus);
                return existing.ProcessingStatus;
            }

            // TODO: Enqueue Hangfire background job
            // For now, set status to "Queued"
            await _repository.UpdateProcessingStatusAsync(lessonId, "Queued", null, ct);

            var jobId = Guid.NewGuid().ToString();
            _logger.LogInformation("Takeaway generation job queued: {JobId} for lesson {LessonId}", jobId, lessonId);

            return jobId;
        }

        public async Task<VideoKeyTakeawaysDto> GenerateTakeawaysAsync(Guid lessonId, string transcriptText, CancellationToken ct = default)
        {
            _logger.LogInformation("Starting AI takeaway generation for lesson {LessonId}", lessonId);

            try
            {
                // Update status to "Processing"
                await _repository.UpdateProcessingStatusAsync(lessonId, "Processing", null, ct);

                // 1. Call Ollama AI to extract takeaways
                var ollamaBaseUrl = _configuration["Ollama:BaseUrl"] ?? "http://ollama-service:11434";
                var rawTakeaways = await CallOllamaForTakeawaysAsync(transcriptText, ollamaBaseUrl, ct);

                // 2. Process and enrich takeaways
                var takeawayDtos = new List<TakeawayDto>();

                foreach (var rawTakeaway in rawTakeaways)
                {
                    var category = ClassifyTakeawayCategory(rawTakeaway.Text);
                    var relevanceScore = CalculateRelevanceScore(rawTakeaway.Text, transcriptText);

                    takeawayDtos.Add(new TakeawayDto
                    {
                        TakeawayId = Guid.NewGuid().ToString(),
                        Text = rawTakeaway.Text,
                        Category = category,
                        RelevanceScore = relevanceScore,
                        TimestampStart = rawTakeaway.TimestampStart,
                        TimestampEnd = rawTakeaway.TimestampEnd,
                        UserFeedback = null
                    });
                }

                // 3. Sort by relevance score
                takeawayDtos = takeawayDtos.OrderByDescending(t => t.RelevanceScore).ToList();

                // 4. Create DTO
                var takeawaysDto = new VideoKeyTakeawaysDto
                {
                    LessonId = lessonId,
                    Takeaways = takeawayDtos,
                    Metadata = new TakeawayMetadataDto
                    {
                        TotalTakeaways = takeawayDtos.Count,
                        ProcessingModel = "qwen2:0.5b",
                        ProcessedAt = DateTime.UtcNow
                    }
                };

                // 5. Save to database
                await _repository.CreateAsync(lessonId, takeawaysDto, ct);

                // 6. Invalidate cache
                await InvalidateCacheAsync(lessonId, ct);

                _logger.LogInformation("Takeaway generation completed for lesson {LessonId}, {Count} takeaways extracted", lessonId, takeawayDtos.Count);

                return takeawaysDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Takeaway generation failed for lesson {LessonId}", lessonId);
                await _repository.UpdateProcessingStatusAsync(lessonId, "Failed", ex.Message, ct);
                throw;
            }
        }

        public async Task SubmitFeedbackAsync(Guid lessonId, string takeawayId, int feedback, CancellationToken ct = default)
        {
            _logger.LogInformation("Submitting feedback {Feedback} for takeaway {TakeawayId} in lesson {LessonId}", feedback, takeawayId, lessonId);

            // Update in database
            await _repository.SubmitFeedbackAsync(lessonId, takeawayId, feedback, ct);

            // Invalidate cache
            await InvalidateCacheAsync(lessonId, ct);
        }

        public async Task<TakeawayProcessingStatusDto> GetProcessingStatusAsync(Guid lessonId, CancellationToken ct = default)
        {
            var metadata = await _repository.GetMetadataAsync(lessonId, ct);

            if (metadata == null)
            {
                return new TakeawayProcessingStatusDto
                {
                    LessonId = lessonId,
                    Status = "NotStarted",
                    Progress = 0,
                    TotalTakeaways = 0,
                    ErrorMessage = null
                };
            }

            return new TakeawayProcessingStatusDto
            {
                LessonId = lessonId,
                Status = metadata.ProcessingStatus,
                Progress = metadata.ProcessingStatus == "Completed" ? 100 : 0,
                TotalTakeaways = metadata.TakeawayCount,
                ErrorMessage = null
            };
        }

        public async Task DeleteTakeawaysAsync(Guid lessonId, CancellationToken ct = default)
        {
            _logger.LogInformation("Deleting takeaways for lesson {LessonId}", lessonId);

            // 1. Delete from database
            await _repository.DeleteAsync(lessonId, ct);

            // 2. Invalidate cache
            await InvalidateCacheAsync(lessonId, ct);
        }

        public async Task InvalidateCacheAsync(Guid lessonId, CancellationToken ct = default)
        {
            var cacheKey = $"{CacheKeyPrefix}{lessonId}";
            await _cache.RemoveAsync(cacheKey, ct);
            _logger.LogDebug("Cache invalidated for lesson {LessonId}", lessonId);
        }

        public async Task UpdateProcessingStatusAsync(Guid lessonId, string status, string? errorMessage = null, CancellationToken ct = default)
        {
            await _repository.UpdateProcessingStatusAsync(lessonId, status, errorMessage, ct);
        }

        public double CalculateRelevanceScore(string takeawayText, string fullTranscript)
        {
            // Heuristic-based relevance scoring
            double score = 0.5; // Base score

            // 1. Length factor (longer takeaways might be more detailed)
            var wordCount = takeawayText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount >= 15 && wordCount <= 50)
                score += 0.1; // Optimal length

            // 2. Keyword density (important technical terms)
            var keywords = new[] { "important", "key", "essential", "critical", "best practice", "should", "must", "always", "never" };
            var keywordMatches = keywords.Count(kw => takeawayText.Contains(kw, StringComparison.OrdinalIgnoreCase));
            score += keywordMatches * 0.05;

            // 3. Uniqueness (appears rarely in transcript = more specific)
            var occurrences = Regex.Matches(fullTranscript, Regex.Escape(takeawayText), RegexOptions.IgnoreCase).Count;
            if (occurrences == 1)
                score += 0.15; // Unique concept

            // 4. Position in transcript (concepts introduced early might be fundamental)
            var position = fullTranscript.IndexOf(takeawayText, StringComparison.OrdinalIgnoreCase);
            if (position >= 0 && position < fullTranscript.Length * 0.3)
                score += 0.1; // In first 30% of content

            // Clamp score between 0 and 1
            return Math.Clamp(score, 0.0, 1.0);
        }

        public string ClassifyTakeawayCategory(string takeawayText)
        {
            var lowerText = takeawayText.ToLowerInvariant();

            // Rule-based classification
            if (lowerText.Contains("best practice") || lowerText.Contains("should always") || lowerText.Contains("recommended"))
                return "BestPractice";

            if (lowerText.Contains("example") || lowerText.Contains("for instance") || lowerText.Contains("such as"))
                return "Example";

            if (lowerText.Contains("warning") || lowerText.Contains("avoid") || lowerText.Contains("don't") || lowerText.Contains("never"))
                return "Warning";

            if (lowerText.Contains("in summary") || lowerText.Contains("to conclude") || lowerText.Contains("overall"))
                return "Summary";

            if (lowerText.Contains("key") || lowerText.Contains("important") || lowerText.Contains("critical") || lowerText.Contains("essential"))
                return "KeyPoint";

            // Default to CoreConcept
            return "CoreConcept";
        }

        /// <summary>
        /// Call Ollama API to extract key takeaways from transcript.
        /// </summary>
        private async Task<List<RawTakeaway>> CallOllamaForTakeawaysAsync(string transcriptText, string ollamaBaseUrl, CancellationToken ct)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);

            // Construct prompt for Ollama
            var prompt = $@"You are an expert educational content analyzer. Extract 5-10 key takeaways from the following video transcript.
For each takeaway:
1. Make it concise (1-2 sentences)
2. Focus on actionable insights, concepts, or best practices
3. Output as JSON array with 'text' field

Transcript:
{transcriptText.Substring(0, Math.Min(transcriptText.Length, 4000))}

Output format:
[
  {{""text"": ""First key takeaway...""}},
  {{""text"": ""Second key takeaway...""}}
]";

            var requestBody = new
            {
                model = "qwen2:0.5b",
                prompt = prompt,
                stream = false,
                options = new
                {
                    temperature = 0.3, // Low temperature for focused extraction
                    num_predict = 800 // Limit response length
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Calling Ollama API: {Url}", ollamaBaseUrl);

            var response = await httpClient.PostAsync($"{ollamaBaseUrl}/api/generate", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"Ollama API failed: {response.StatusCode} - {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

            if (string.IsNullOrEmpty(ollamaResponse?.Response))
                throw new InvalidOperationException("Ollama API returned empty response");

            // Parse JSON array from response
            var takeaways = new List<RawTakeaway>();

            try
            {
                // Extract JSON array from response (Ollama might add extra text)
                var jsonMatch = Regex.Match(ollamaResponse.Response, @"\[.*\]", RegexOptions.Singleline);
                if (jsonMatch.Success)
                {
                    var parsedTakeaways = JsonSerializer.Deserialize<List<RawTakeaway>>(jsonMatch.Value);
                    if (parsedTakeaways != null)
                        takeaways.AddRange(parsedTakeaways);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Ollama response as JSON, falling back to text extraction");
                // Fallback: extract sentences as takeaways
                var sentences = ollamaResponse.Response.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                takeaways.AddRange(sentences.Take(5).Select(s => new RawTakeaway { Text = s }));
            }

            return takeaways;
        }

        private class OllamaResponse
        {
            public string? Response { get; set; }
        }

        private class RawTakeaway
        {
            public string Text { get; set; } = string.Empty;
            public double? TimestampStart { get; set; }
            public double? TimestampEnd { get; set; }
        }
    }
}
