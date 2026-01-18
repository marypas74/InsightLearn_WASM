using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InsightLearn.Core.DTOs.VideoTranscript;
using InsightLearn.Core.Interfaces;
using InsightLearn.Application.BackgroundJobs;

namespace InsightLearn.Application.Services
{
    /// <summary>
    /// Service for video transcript generation and management.
    /// Integrates with Whisper API (self-hosted) for ASR.
    /// Uses Redis for caching, Hangfire for background processing.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class VideoTranscriptService : IVideoTranscriptService
    {
        private readonly IVideoTranscriptRepository _repository;
        private readonly IDistributedCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<VideoTranscriptService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWhisperTranscriptionService _whisperService;

        private const string CacheKeyPrefix = "transcript:";
        private const int CacheDurationMinutes = 60; // 1 hour cache

        public VideoTranscriptService(
            IVideoTranscriptRepository repository,
            IDistributedCache cache,
            IHttpClientFactory httpClientFactory,
            ILogger<VideoTranscriptService> logger,
            IConfiguration configuration,
            IWhisperTranscriptionService whisperService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _whisperService = whisperService ?? throw new ArgumentNullException(nameof(whisperService));
        }

        public async Task<VideoTranscriptDto?> GetTranscriptAsync(Guid lessonId, CancellationToken ct = default)
        {
            // 1. Try Redis cache first
            var cacheKey = $"{CacheKeyPrefix}{lessonId}";
            var cachedJson = await _cache.GetStringAsync(cacheKey, ct);

            if (!string.IsNullOrEmpty(cachedJson))
            {
                _logger.LogDebug("Transcript cache HIT for lesson {LessonId}", lessonId);
                return JsonSerializer.Deserialize<VideoTranscriptDto>(cachedJson);
            }

            _logger.LogDebug("Transcript cache MISS for lesson {LessonId}", lessonId);

            // 2. Get from database
            var transcript = await _repository.GetTranscriptAsync(lessonId, ct);

            if (transcript == null)
                return null;

            // 3. Cache the result
            var json = JsonSerializer.Serialize(transcript);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheDurationMinutes)
            };
            await _cache.SetStringAsync(cacheKey, json, cacheOptions, ct);

            return transcript;
        }

        public async Task<string> QueueTranscriptGenerationAsync(Guid lessonId, string videoUrl, string language = "en-US", CancellationToken ct = default)
        {
            _logger.LogInformation("Queueing transcript generation for lesson {LessonId}, language {Language}", lessonId, language);

            // Check if transcript already exists
            var metadata = await _repository.GetMetadataAsync(lessonId, ct);
            if (metadata != null)
            {
                _logger.LogWarning("Transcript already exists for lesson {LessonId}, status: {Status}", lessonId, metadata.Status);
                return metadata.Status;
            }

            // ✅ FIXED (v2.3.50-dev-fix3): Implement proper Hangfire job queueing
            // Enqueue Hangfire background job using TranscriptGenerationJob.Enqueue()
            // This creates InvocationData with proper type serialization matching UseSimpleAssemblyNameTypeSerializer()
            var jobId = TranscriptGenerationJob.Enqueue(lessonId, videoUrl, language);

            // Update metadata status to "Queued" (will be changed to "Processing" by the job)
            await _repository.UpdateProcessingStatusAsync(lessonId, "Queued", null, ct);

            _logger.LogInformation("Transcript generation job queued: {JobId} for lesson {LessonId}", jobId, lessonId);

            return jobId;
        }

        public async Task<VideoTranscriptDto> GenerateTranscriptAsync(Guid lessonId, string videoUrl, string language = "en-US", CancellationToken ct = default)
        {
            _logger.LogInformation("Starting synchronous transcript generation for lesson {LessonId} using Whisper.net", lessonId);

            try
            {
                // Update status to "Processing"
                await _repository.UpdateProcessingStatusAsync(lessonId, "Processing", null, ct);

                // 1. Download video stream
                _logger.LogDebug("Downloading video from URL: {VideoUrl}", videoUrl);
                var videoStream = await DownloadVideoStreamAsync(videoUrl, ct);

                // 2. Transcribe using Whisper.net
                // Extract language code (convert "en-US" to "en" for Whisper)
                var whisperLanguage = language.Contains('-') ? language.Split('-')[0] : language;
                var transcriptionResult = await _whisperService.TranscribeVideoAsync(videoStream, whisperLanguage, lessonId);

                // 3. Map TranscriptionResult to TranscriptSegmentDto
                var segments = transcriptionResult.Segments.Select(s => new TranscriptSegmentDto
                {
                    StartTime = s.StartSeconds,
                    EndTime = s.EndSeconds,
                    Text = s.Text,
                    Speaker = null, // Whisper doesn't provide speaker diarization
                    Confidence = s.Confidence
                }).ToList();

                // 4. Calculate metadata
                var wordCount = segments.Sum(s => s.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
                var averageConfidence = segments.Where(s => s.Confidence.HasValue).Average(s => s.Confidence!.Value);

                // 5. Create DTO
                var transcriptDto = new VideoTranscriptDto
                {
                    LessonId = lessonId,
                    Language = language,
                    ProcessingStatus = "Completed",
                    Transcript = segments,
                    Metadata = new TranscriptMetadataDto
                    {
                        DurationSeconds = (int)transcriptionResult.DurationSeconds,
                        WordCount = wordCount,
                        AverageConfidence = averageConfidence,
                        ProcessingEngine = transcriptionResult.ModelUsed, // "whisper-base"
                        ProcessedAt = DateTime.UtcNow
                    }
                };

                // 6. Save to database
                await _repository.CreateAsync(lessonId, transcriptDto, ct);

                // 7. Invalidate cache (if any)
                await InvalidateCacheAsync(lessonId, ct);

                _logger.LogInformation("Transcript generation completed for lesson {LessonId}, {WordCount} words, {SegmentCount} segments",
                    lessonId, wordCount, segments.Count);

                return transcriptDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transcript generation failed for lesson {LessonId}", lessonId);
                await _repository.UpdateProcessingStatusAsync(lessonId, "Failed", ex.Message, ct);
                throw;
            }
        }

        /// <summary>
        /// Download video stream from the provided video URL.
        /// Handles both relative URLs (e.g., /api/video/stream/{id}) and absolute URLs.
        /// </summary>
        private async Task<Stream> DownloadVideoStreamAsync(string videoUrl, CancellationToken ct)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5); // 5-minute timeout for large videos

            // If videoUrl is relative, construct full URL using API base URL
            string fullUrl;
            if (videoUrl.StartsWith("/"))
            {
                // Relative URL - use localhost or configured base URL
                var apiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:80";
                fullUrl = $"{apiBaseUrl}{videoUrl}";
            }
            else
            {
                fullUrl = videoUrl;
            }

            _logger.LogDebug("Downloading video from: {Url}", fullUrl);

            var response = await httpClient.GetAsync(fullUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStreamAsync(ct);
        }

        public async Task<TranscriptSearchResultDto> SearchTranscriptAsync(Guid lessonId, string query, int limit = 10, CancellationToken ct = default)
        {
            _logger.LogDebug("Searching transcript for lesson {LessonId}, query: {Query}", lessonId, query);

            // Delegate to repository (MongoDB full-text search)
            return await _repository.SearchTranscriptAsync(lessonId, query, limit, ct);
        }

        public async Task<TranscriptProcessingStatusDto> GetProcessingStatusAsync(Guid lessonId, CancellationToken ct = default)
        {
            var metadata = await _repository.GetMetadataAsync(lessonId, ct);

            if (metadata == null)
            {
                return new TranscriptProcessingStatusDto
                {
                    LessonId = lessonId,
                    ProcessingStatus = "NotStarted",
                    ProgressPercentage = 0,
                    ErrorMessage = null
                };
            }

            return new TranscriptProcessingStatusDto
            {
                LessonId = lessonId,
                ProcessingStatus = metadata.Status,
                ProgressPercentage = metadata.Status == "Completed" ? 100 : 0,
                ErrorMessage = null
            };
        }

        public async Task DeleteTranscriptAsync(Guid lessonId, CancellationToken ct = default)
        {
            _logger.LogInformation("Deleting transcript for lesson {LessonId}", lessonId);

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

        public async Task<TranscriptMetadataDto?> GetMetadataAsync(Guid lessonId, CancellationToken ct = default)
        {
            var metadata = await _repository.GetMetadataAsync(lessonId, ct);

            if (metadata == null)
                return null;

            // Note: WordCount and AverageConfidence are stored in MongoDB only
            // Use defaults here - full data available via GetTranscriptAsync
            return new TranscriptMetadataDto
            {
                WordCount = 0, // Available in MongoDB full transcript
                AverageConfidence = 0.0, // Available in MongoDB full transcript
                ProcessingModel = "whisper-large-v3",
                ProcessedAt = metadata.GeneratedAt ?? DateTime.UtcNow
            };
        }

        public async Task UpdateProcessingStatusAsync(Guid lessonId, string status, string? errorMessage = null, CancellationToken ct = default)
        {
            await _repository.UpdateProcessingStatusAsync(lessonId, status, errorMessage, ct);
        }

        /// <summary>
        /// Generate demo transcript using Ollama LLM.
        /// Creates sample educational content segments when no real ASR is available.
        /// </summary>
        public async Task<VideoTranscriptDto> GenerateDemoTranscriptAsync(Guid lessonId, string lessonTitle, int durationSeconds = 300, string language = "en-US", CancellationToken ct = default)
        {
            _logger.LogInformation("[DEMO_TRANSCRIPT] Generating demo transcript for lesson {LessonId}, title: {Title}, duration: {Duration}s",
                lessonId, lessonTitle, durationSeconds);

            try
            {
                // Check if transcript already exists
                var existing = await _repository.GetMetadataAsync(lessonId, ct);
                if (existing != null)
                {
                    _logger.LogWarning("[DEMO_TRANSCRIPT] Transcript already exists for lesson {LessonId}", lessonId);
                    var existingTranscript = await _repository.GetTranscriptAsync(lessonId, ct);
                    if (existingTranscript != null)
                        return existingTranscript;
                }

                // Generate segments using Ollama
                var segments = await GenerateTranscriptSegmentsWithOllamaAsync(lessonTitle, durationSeconds, language, ct);

                // Calculate metadata
                var wordCount = segments.Sum(s => s.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
                var avgConfidence = segments.Where(s => s.Confidence.HasValue).DefaultIfEmpty().Average(s => s?.Confidence ?? 0.95);

                // Create DTO
                var transcriptDto = new VideoTranscriptDto
                {
                    LessonId = lessonId,
                    Language = language,
                    ProcessingStatus = "Completed",
                    Transcript = segments,
                    Metadata = new TranscriptMetadataDto
                    {
                        DurationSeconds = durationSeconds,
                        WordCount = wordCount,
                        AverageConfidence = avgConfidence,
                        ProcessingEngine = "Ollama/qwen2:0.5b",
                        ProcessedAt = DateTime.UtcNow
                    }
                };

                // Save to database (MongoDB + SQL Server)
                await _repository.CreateAsync(lessonId, transcriptDto, ct);

                // Invalidate cache
                await InvalidateCacheAsync(lessonId, ct);

                _logger.LogInformation("[DEMO_TRANSCRIPT] Demo transcript created for lesson {LessonId}, {SegmentCount} segments, {WordCount} words",
                    lessonId, segments.Count, wordCount);

                return transcriptDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DEMO_TRANSCRIPT] Failed to generate demo transcript for lesson {LessonId}", lessonId);
                throw;
            }
        }

        /// <summary>
        /// Generate transcript segments using Ollama LLM.
        /// </summary>
        private async Task<List<TranscriptSegmentDto>> GenerateTranscriptSegmentsWithOllamaAsync(string lessonTitle, int durationSeconds, string language, CancellationToken ct)
        {
            var ollamaBaseUrl = _configuration["Ollama:BaseUrl"] ?? _configuration["Ollama:Url"] ?? "http://ollama-service:11434";
            var ollamaModel = _configuration["Ollama:Model"] ?? "qwen2:0.5b";

            // Calculate number of segments (roughly 10-15 seconds per segment)
            var segmentCount = Math.Max(5, Math.Min(30, durationSeconds / 12));
            var segmentDuration = (double)durationSeconds / segmentCount;

            var prompt = $@"Generate {segmentCount} educational transcript segments for a lesson titled ""{lessonTitle}"".
Each segment should be 1-2 sentences of educational content.
Format: Just provide the text content, one segment per line.
Language: {language}
Be educational, professional, and engaging.";

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromMinutes(2);

                var requestBody = new
                {
                    model = ollamaModel,
                    prompt = prompt,
                    stream = false,
                    options = new { temperature = 0.7, num_predict = 500 }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogDebug("[DEMO_TRANSCRIPT] Calling Ollama API: {Url}/api/generate", ollamaBaseUrl);

                var response = await httpClient.PostAsync($"{ollamaBaseUrl}/api/generate", content, ct);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync(ct);
                    var ollamaResponse = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseJson);

                    if (!string.IsNullOrEmpty(ollamaResponse?.Response))
                    {
                        // Parse response into segments
                        var lines = ollamaResponse.Response
                            .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length > 10)
                            .Take(segmentCount)
                            .ToList();

                        return CreateSegmentsFromLines(lines, durationSeconds);
                    }
                }
                else
                {
                    _logger.LogWarning("[DEMO_TRANSCRIPT] Ollama API failed: {StatusCode}, using fallback", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DEMO_TRANSCRIPT] Ollama call failed, using fallback demo content");
            }

            // Fallback: Generate static demo content
            return GenerateFallbackSegments(lessonTitle, durationSeconds, segmentCount);
        }

        /// <summary>
        /// Create transcript segments from text lines.
        /// </summary>
        private List<TranscriptSegmentDto> CreateSegmentsFromLines(List<string> lines, int durationSeconds)
        {
            var segments = new List<TranscriptSegmentDto>();
            var segmentDuration = (double)durationSeconds / lines.Count;
            var random = new Random();

            for (int i = 0; i < lines.Count; i++)
            {
                var text = lines[i].Trim();
                // Remove common formatting like "1." or "-"
                if (text.Length > 2 && (char.IsDigit(text[0]) || text[0] == '-'))
                {
                    var colonIndex = text.IndexOf(':');
                    var dotIndex = text.IndexOf('.');
                    var dashIndex = text.IndexOf('-');
                    var skipIndex = new[] { colonIndex, dotIndex, dashIndex }.Where(x => x > 0 && x < 5).DefaultIfEmpty(-1).Min();
                    if (skipIndex > 0)
                        text = text.Substring(skipIndex + 1).Trim();
                }

                segments.Add(new TranscriptSegmentDto
                {
                    StartTime = i * segmentDuration,
                    EndTime = (i + 1) * segmentDuration,
                    Text = text,
                    Speaker = "Instructor",
                    Confidence = 0.90 + (random.NextDouble() * 0.09) // 0.90-0.99
                });
            }

            return segments;
        }

        /// <summary>
        /// Generate fallback demo segments when Ollama is unavailable.
        /// </summary>
        private List<TranscriptSegmentDto> GenerateFallbackSegments(string lessonTitle, int durationSeconds, int segmentCount)
        {
            var segments = new List<TranscriptSegmentDto>();
            var segmentDuration = (double)durationSeconds / segmentCount;
            var random = new Random();

            var demoTexts = new[]
            {
                $"Welcome to this lesson on {lessonTitle}. Let's explore the key concepts together.",
                "In this section, we'll cover the fundamental principles that form the foundation of this topic.",
                "Understanding these concepts is crucial for your professional development in this field.",
                "Let me demonstrate a practical example that illustrates this point clearly.",
                "This technique is widely used in industry and will serve you well in real-world scenarios.",
                "Pay attention to this important detail, as it often comes up in practical applications.",
                "Now let's look at how this connects to what we learned earlier in the course.",
                "Here's a best practice that experienced professionals always follow.",
                "Let's review the key takeaways from this section before moving on.",
                $"Excellent work! You've now completed the core concepts of {lessonTitle}."
            };

            for (int i = 0; i < segmentCount; i++)
            {
                var textIndex = Math.Min(i, demoTexts.Length - 1);
                segments.Add(new TranscriptSegmentDto
                {
                    StartTime = i * segmentDuration,
                    EndTime = (i + 1) * segmentDuration,
                    Text = demoTexts[textIndex],
                    Speaker = "Instructor",
                    Confidence = 0.92 + (random.NextDouble() * 0.07)
                });
            }

            return segments;
        }

        /// <summary>
        /// Ollama API response model.
        /// </summary>
        private class OllamaGenerateResponse
        {
            public string? Response { get; set; }
            public bool Done { get; set; }
        }

        /// <summary>
        /// Call Whisper API to generate transcript segments.
        /// </summary>
        private async Task<List<TranscriptSegmentDto>> CallWhisperApiAsync(string videoUrl, string language, string whisperBaseUrl, CancellationToken ct)
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10); // Long timeout for large videos

            var requestBody = new
            {
                audio_url = videoUrl,
                language = language,
                task = "transcribe",
                word_timestamps = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Calling Whisper API: {Url}", whisperBaseUrl);

            var response = await httpClient.PostAsync($"{whisperBaseUrl}/asr", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"Whisper API failed: {response.StatusCode} - {error}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var whisperResponse = JsonSerializer.Deserialize<WhisperApiResponse>(responseJson);

            if (whisperResponse?.Segments == null)
                throw new InvalidOperationException("Whisper API returned null segments");

            // Map Whisper segments to our DTOs
            return whisperResponse.Segments.Select(s => new TranscriptSegmentDto
            {
                StartTime = s.Start,
                EndTime = s.End,
                Text = s.Text.Trim(),
                Confidence = s.Confidence,
                Speaker = null // Whisper doesn't provide speaker diarization by default
            }).ToList();
        }

        /// <summary>
        /// Whisper API response model.
        /// </summary>
        private class WhisperApiResponse
        {
            public string? Text { get; set; }
            public List<WhisperSegment>? Segments { get; set; }
            public string? Language { get; set; }
        }

        private class WhisperSegment
        {
            public double Start { get; set; }
            public double End { get; set; }
            public string Text { get; set; } = string.Empty;
            public double? Confidence { get; set; }
        }
    }
}
