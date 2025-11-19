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

        private const string CacheKeyPrefix = "transcript:";
        private const int CacheDurationMinutes = 60; // 1 hour cache

        public VideoTranscriptService(
            IVideoTranscriptRepository repository,
            IDistributedCache cache,
            IHttpClientFactory httpClientFactory,
            ILogger<VideoTranscriptService> logger,
            IConfiguration configuration)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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

            // Create metadata with "Pending" status
            var metadata = await _repository.GetMetadataAsync(lessonId, ct);
            if (metadata != null)
            {
                _logger.LogWarning("Transcript already exists for lesson {LessonId}, status: {Status}", lessonId, metadata.ProcessingStatus);
                return metadata.ProcessingStatus;
            }

            // TODO: Enqueue Hangfire background job
            // For now, set status to "Queued"
            await _repository.UpdateProcessingStatusAsync(lessonId, "Queued", null, ct);

            // Return job ID (placeholder for now)
            var jobId = Guid.NewGuid().ToString();
            _logger.LogInformation("Transcript generation job queued: {JobId} for lesson {LessonId}", jobId, lessonId);

            return jobId;
        }

        public async Task<VideoTranscriptDto> GenerateTranscriptAsync(Guid lessonId, string videoUrl, string language = "en-US", CancellationToken ct = default)
        {
            _logger.LogInformation("Starting synchronous transcript generation for lesson {LessonId}", lessonId);

            try
            {
                // Update status to "Processing"
                await _repository.UpdateProcessingStatusAsync(lessonId, "Processing", null, ct);

                // 1. Call Whisper API
                var whisperBaseUrl = _configuration["Whisper:BaseUrl"] ?? "http://whisper-service:9000";
                var segments = await CallWhisperApiAsync(videoUrl, language, whisperBaseUrl, ct);

                // 2. Calculate metadata
                var wordCount = segments.Sum(s => s.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
                var averageConfidence = segments.Where(s => s.Confidence.HasValue).Average(s => s.Confidence!.Value);

                // 3. Create DTO
                var transcriptDto = new VideoTranscriptDto
                {
                    LessonId = lessonId,
                    Language = language,
                    ProcessingStatus = "Completed",
                    Transcript = segments,
                    Metadata = new TranscriptMetadataDto
                    {
                        WordCount = wordCount,
                        AverageConfidence = averageConfidence,
                        ProcessingModel = "whisper-large-v3",
                        ProcessedAt = DateTime.UtcNow
                    }
                };

                // 4. Save to database
                await _repository.CreateAsync(lessonId, transcriptDto, ct);

                // 5. Invalidate cache (if any)
                await InvalidateCacheAsync(lessonId, ct);

                _logger.LogInformation("Transcript generation completed for lesson {LessonId}, {WordCount} words", lessonId, wordCount);

                return transcriptDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Transcript generation failed for lesson {LessonId}", lessonId);
                await _repository.UpdateProcessingStatusAsync(lessonId, "Failed", ex.Message, ct);
                throw;
            }
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
                    Status = "NotStarted",
                    Progress = 0,
                    EstimatedTimeRemaining = null,
                    ErrorMessage = null
                };
            }

            return new TranscriptProcessingStatusDto
            {
                LessonId = lessonId,
                Status = metadata.ProcessingStatus,
                Progress = metadata.ProcessingStatus == "Completed" ? 100 : 0,
                EstimatedTimeRemaining = null,
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

            return new TranscriptMetadataDto
            {
                WordCount = metadata.WordCount ?? 0,
                AverageConfidence = metadata.AverageConfidence ?? 0,
                ProcessingModel = "whisper-large-v3",
                ProcessedAt = metadata.ProcessedAt ?? DateTime.UtcNow
            };
        }

        public async Task UpdateProcessingStatusAsync(Guid lessonId, string status, string? errorMessage = null, CancellationToken ct = default)
        {
            await _repository.UpdateProcessingStatusAsync(lessonId, status, errorMessage, ct);
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
