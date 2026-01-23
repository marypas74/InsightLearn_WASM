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
    /// Uses IAIServiceFactory to dynamically select transcription provider (OpenAI Whisper or faster-whisper).
    /// Supports parallel transcription using both providers simultaneously for real-time results.
    /// Uses Redis for caching, Hangfire for background processing.
    /// Part of Student Learning Space v2.3.67-dev.
    /// </summary>
    public class VideoTranscriptService : IVideoTranscriptService
    {
        private readonly IVideoTranscriptRepository _repository;
        private readonly IDistributedCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<VideoTranscriptService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IAIServiceFactory _serviceFactory;
        private readonly IWhisperTranscriptionService _whisperServiceFallback; // Direct fallback
        private readonly IParallelTranscriptionService? _parallelService; // Optional parallel service

        private const string CacheKeyPrefix = "transcript:";
        private const int CacheDurationMinutes = 60; // 1 hour cache

        public VideoTranscriptService(
            IVideoTranscriptRepository repository,
            IDistributedCache cache,
            IHttpClientFactory httpClientFactory,
            ILogger<VideoTranscriptService> logger,
            IConfiguration configuration,
            IAIServiceFactory serviceFactory,
            IWhisperTranscriptionService whisperService,
            IParallelTranscriptionService? parallelService = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));
            _whisperServiceFallback = whisperService ?? throw new ArgumentNullException(nameof(whisperService));
            _parallelService = parallelService; // Optional - null if not configured
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
            _logger.LogInformation("Starting transcript generation for lesson {LessonId}", lessonId);

            try
            {
                // Update status to "Processing"
                await _repository.UpdateProcessingStatusAsync(lessonId, "Processing", null, ct);

                // 1. Download video stream
                _logger.LogDebug("Downloading video from URL: {VideoUrl}", videoUrl);
                var videoStream = await DownloadVideoStreamAsync(videoUrl, ct);

                // 2. Get transcription service from factory (supports OpenAI Whisper or faster-whisper based on config)
                var whisperService = await _serviceFactory.GetTranscriptionServiceAsync();
                _logger.LogInformation("Using transcription service: {ServiceType}", whisperService.GetType().Name);

                // Extract language code (convert "en-US" to "en" for Whisper)
                var whisperLanguage = language.Contains('-') ? language.Split('-')[0] : language;
                var transcriptionResult = await whisperService.TranscribeVideoAsync(videoStream, whisperLanguage, lessonId);

                // ═══════════════════════════════════════════════════════════════════════════════
                // PHASE 90-100%: POST-TRANSCRIPTION PROCESSING (Detailed Logging v2.3.91-dev)
                // ═══════════════════════════════════════════════════════════════════════════════

                // STEP 90-91%: Map TranscriptionResult to TranscriptSegmentDto
                _logger.LogInformation("[TRANSCRIPT:90%] ═══ FasterWhisper COMPLETED for lesson {LessonId} ═══", lessonId);
                _logger.LogInformation("[TRANSCRIPT:90%] Received {SegmentCount} raw segments, Duration: {Duration}s, Model: {Model}",
                    transcriptionResult.Segments.Count, transcriptionResult.DurationSeconds, transcriptionResult.ModelUsed);

                await _repository.UpdateProcessingStatusAsync(lessonId, "Mapping", null, ct);
                _logger.LogInformation("[TRANSCRIPT:91%] Starting segment mapping...");

                var segments = transcriptionResult.Segments.Select(s => new TranscriptSegmentDto
                {
                    StartTime = s.StartSeconds,
                    EndTime = s.EndSeconds,
                    Text = s.Text,
                    Speaker = null, // Whisper doesn't provide speaker diarization
                    Confidence = s.Confidence
                }).ToList();

                _logger.LogInformation("[TRANSCRIPT:92%] Mapped {SegmentCount} segments successfully", segments.Count);

                // STEP 92-93%: Calculate metadata
                _logger.LogInformation("[TRANSCRIPT:92%] Calculating metadata (word count, confidence)...");
                var wordCount = segments.Sum(s => s.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
                var averageConfidence = segments.Any(s => s.Confidence.HasValue)
                    ? segments.Where(s => s.Confidence.HasValue).Average(s => s.Confidence!.Value)
                    : 0.0;
                _logger.LogInformation("[TRANSCRIPT:93%] Metadata calculated: {WordCount} words, {AvgConfidence:F2}% avg confidence",
                    wordCount, averageConfidence * 100);

                // STEP 93-94%: Create DTO
                _logger.LogInformation("[TRANSCRIPT:93%] Creating transcript DTO...");
                // Normalize language to xx-XX format for MongoDB schema validation
                var normalizedLanguage = NormalizeLanguageCode(language);
                _logger.LogInformation("[TRANSCRIPT:94%] Language normalized: {Original} → {Normalized}", language, normalizedLanguage);

                var transcriptDto = new VideoTranscriptDto
                {
                    LessonId = lessonId,
                    Language = normalizedLanguage,
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
                _logger.LogInformation("[TRANSCRIPT:94%] DTO created: {SegmentCount} segments, {Duration}s duration",
                    segments.Count, transcriptionResult.DurationSeconds);

                // STEP 95-98%: Save to database (MongoDB + SQL Server)
                _logger.LogInformation("[TRANSCRIPT:95%] ═══ SAVING TO DATABASE ═══");
                _logger.LogInformation("[TRANSCRIPT:95%] Step 1: Saving transcript to MongoDB...");
                await _repository.UpdateProcessingStatusAsync(lessonId, "SavingMongoDB", null, ct);

                var saveStartTime = DateTime.UtcNow;
                await _repository.CreateAsync(lessonId, transcriptDto, ct);
                var saveEndTime = DateTime.UtcNow;
                var saveDuration = (saveEndTime - saveStartTime).TotalMilliseconds;

                _logger.LogInformation("[TRANSCRIPT:98%] Database save COMPLETED in {Duration}ms", saveDuration);

                // STEP 98-99%: Invalidate cache (if any)
                _logger.LogInformation("[TRANSCRIPT:98%] Invalidating Redis cache...");
                await _repository.UpdateProcessingStatusAsync(lessonId, "CacheInvalidation", null, ct);
                await InvalidateCacheAsync(lessonId, ct);
                _logger.LogInformation("[TRANSCRIPT:99%] Cache invalidated successfully");

                // STEP 99-100%: Final status update
                _logger.LogInformation("[TRANSCRIPT:99%] Updating final status to 'Completed'...");
                await _repository.UpdateProcessingStatusAsync(lessonId, "Completed", null, ct);
                _logger.LogInformation("[TRANSCRIPT:100%] ═══ TRANSCRIPT GENERATION COMPLETE ═══");

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
        /// Generate transcript using parallel processing with both OpenAI Whisper and faster-whisper.
        /// Splits video into chunks and distributes between providers for real-time transcription.
        /// </summary>
        /// <param name="lessonId">Lesson ID</param>
        /// <param name="videoUrl">Video URL to transcribe</param>
        /// <param name="language">Language code (e.g., "en-US", "it-IT")</param>
        /// <param name="options">Parallel transcription options (chunk size, strategy, etc.)</param>
        /// <param name="progress">Optional progress callback for real-time updates</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Generated transcript DTO</returns>
        public async Task<VideoTranscriptDto> GenerateTranscriptParallelAsync(
            Guid lessonId,
            string videoUrl,
            string language = "en-US",
            ParallelTranscriptionOptions? options = null,
            IProgress<ParallelTranscriptionProgress>? progress = null,
            CancellationToken ct = default)
        {
            if (_parallelService == null)
            {
                _logger.LogWarning("[VideoTranscript] Parallel transcription service not available, falling back to single provider");
                return await GenerateTranscriptAsync(lessonId, videoUrl, language, ct);
            }

            _logger.LogInformation("[VideoTranscript] Starting PARALLEL transcript generation for lesson {LessonId}", lessonId);

            try
            {
                // Update status to "Processing"
                await _repository.UpdateProcessingStatusAsync(lessonId, "Processing", null, ct);

                // 1. Check parallel service availability
                var availability = await _parallelService.CheckAvailabilityAsync(ct);
                if (!availability.IsFullyParallel)
                {
                    _logger.LogWarning("[VideoTranscript] Only one provider available ({Status}), using single provider mode",
                        availability.RecommendedStrategy);

                    if (!availability.IsAvailable)
                    {
                        throw new InvalidOperationException("No transcription providers available");
                    }
                }

                // 2. Download video stream
                _logger.LogDebug("[VideoTranscript] Downloading video from URL: {VideoUrl}", videoUrl);
                var videoStream = await DownloadVideoStreamAsync(videoUrl, ct);

                // 3. Use default options if not provided
                options ??= new ParallelTranscriptionOptions
                {
                    ChunkDurationSeconds = 30,
                    ChunkOverlapSeconds = 2,
                    DistributionStrategy = ChunkDistributionStrategy.RoundRobin,
                    MaxParallelChunksPerProvider = 2,
                    EnableRealTimeStreaming = true,
                    EnableFallbackOnFailure = true
                };

                // 4. Extract language code (convert "en-US" to "en" for Whisper)
                var whisperLanguage = language.Contains('-') ? language.Split('-')[0] : language;

                // 5. Process with parallel transcription
                var transcriptionResult = await _parallelService.TranscribeVideoParallelAsync(
                    videoStream, whisperLanguage, lessonId, options, progress, ct);

                // 6. Map TranscriptionResult to TranscriptSegmentDto
                var segments = transcriptionResult.Segments.Select(s => new TranscriptSegmentDto
                {
                    StartTime = s.StartSeconds,
                    EndTime = s.EndSeconds,
                    Text = s.Text,
                    Speaker = null,
                    Confidence = s.Confidence
                }).ToList();

                // 7. Calculate metadata
                var wordCount = segments.Sum(s => s.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
                var averageConfidence = segments.Where(s => s.Confidence.HasValue).DefaultIfEmpty()
                    .Average(s => s?.Confidence ?? 0.9);

                // 8. Normalize language and create DTO
                var normalizedLanguage = NormalizeLanguageCode(language);

                var transcriptDto = new VideoTranscriptDto
                {
                    LessonId = lessonId,
                    Language = normalizedLanguage,
                    ProcessingStatus = "Completed",
                    Transcript = segments,
                    Metadata = new TranscriptMetadataDto
                    {
                        DurationSeconds = (int)transcriptionResult.DurationSeconds,
                        WordCount = wordCount,
                        AverageConfidence = averageConfidence,
                        ProcessingEngine = transcriptionResult.ModelUsed, // "parallel-openai+fasterwhisper"
                        ProcessedAt = DateTime.UtcNow
                    }
                };

                // 9. Save to database
                await _repository.CreateAsync(lessonId, transcriptDto, ct);

                // 10. Invalidate cache
                await InvalidateCacheAsync(lessonId, ct);

                _logger.LogInformation("[VideoTranscript] PARALLEL transcript generation completed for lesson {LessonId}: {WordCount} words, {SegmentCount} segments, engine: {Engine}",
                    lessonId, wordCount, segments.Count, transcriptionResult.ModelUsed);

                return transcriptDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[VideoTranscript] PARALLEL transcript generation failed for lesson {LessonId}", lessonId);
                await _repository.UpdateProcessingStatusAsync(lessonId, "Failed", ex.Message, ct);
                throw;
            }
        }

        /// <summary>
        /// Check if parallel transcription is available and return availability status.
        /// </summary>
        public async Task<ParallelTranscriptionAvailability?> CheckParallelAvailabilityAsync(CancellationToken ct = default)
        {
            if (_parallelService == null)
            {
                return null;
            }

            return await _parallelService.CheckAvailabilityAsync(ct);
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

        /// <summary>
        /// Normalize language code to xx-XX format for MongoDB schema validation.
        /// MongoDB schema requires pattern: ^[a-z]{2}-[A-Z]{2}$
        /// Examples: "en" -> "en-US", "it" -> "it-IT", "en-US" -> "en-US"
        /// </summary>
        private static string NormalizeLanguageCode(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return "en-US";

            // If already in xx-XX format, return as-is
            if (language.Contains('-') && language.Length >= 5)
                return language;

            // Map common 2-letter codes to full locale codes
            return language.ToLowerInvariant() switch
            {
                "en" => "en-US",
                "it" => "it-IT",
                "es" => "es-ES",
                "fr" => "fr-FR",
                "de" => "de-DE",
                "pt" => "pt-BR",
                "zh" => "zh-CN",
                "ja" => "ja-JP",
                "ko" => "ko-KR",
                "ru" => "ru-RU",
                "ar" => "ar-SA",
                "hi" => "hi-IN",
                "nl" => "nl-NL",
                "pl" => "pl-PL",
                "tr" => "tr-TR",
                "vi" => "vi-VN",
                "th" => "th-TH",
                "id" => "id-ID",
                "cs" => "cs-CZ",
                "sv" => "sv-SE",
                "da" => "da-DK",
                "fi" => "fi-FI",
                "no" => "nb-NO",
                "el" => "el-GR",
                "he" => "he-IL",
                "uk" => "uk-UA",
                "ro" => "ro-RO",
                "hu" => "hu-HU",
                "sk" => "sk-SK",
                "bg" => "bg-BG",
                "hr" => "hr-HR",
                "sl" => "sl-SI",
                "sr" => "sr-RS",
                "lt" => "lt-LT",
                "lv" => "lv-LV",
                "et" => "et-EE",
                _ => $"{language.ToLowerInvariant()}-{language.ToUpperInvariant()}" // Fallback: xx -> xx-XX
            };
        }
    }
}
