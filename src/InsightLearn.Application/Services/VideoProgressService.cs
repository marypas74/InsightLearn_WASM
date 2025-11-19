using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using InsightLearn.Core.DTOs.Engagement;
using InsightLearn.Core.DTOs.Enrollment;
using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;

namespace InsightLearn.Application.Services
{
    /// <summary>
    /// Service for video progress tracking using CourseEngagement backend.
    /// Provides simplified interface for video watch tracking with anti-fraud validation.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class VideoProgressService : IVideoProgressService
    {
        private readonly ICourseEngagementRepository _engagementRepository;
        private readonly ILessonRepository _lessonRepository;
        private readonly ILogger<VideoProgressService> _logger;

        private const decimal MinValidationScore = 0.7m; // Minimum score to count for payout
        private const double CompletionThreshold = 0.9; // 90% watched = complete

        public VideoProgressService(
            ICourseEngagementRepository engagementRepository,
            ILessonRepository lessonRepository,
            ILogger<VideoProgressService> logger)
        {
            _engagementRepository = engagementRepository ?? throw new ArgumentNullException(nameof(engagementRepository));
            _lessonRepository = lessonRepository ?? throw new ArgumentNullException(nameof(lessonRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VideoProgressResponseDto> TrackProgressAsync(Guid userId, TrackVideoProgressDto dto, CancellationToken ct = default)
        {
            _logger.LogDebug("Tracking video progress for user {UserId} in lesson {LessonId} at {Timestamp}s",
                userId, dto.LessonId, dto.CurrentTimestampSeconds);

            // Get lesson to determine course ID
            var lesson = await _lessonRepository.GetByIdAsync(dto.LessonId, ct);
            if (lesson == null)
                throw new KeyNotFoundException($"Lesson {dto.LessonId} not found");

            // Calculate engagement duration (time since last update)
            var sessionDurationSeconds = dto.CurrentTimestampSeconds; // For now, use current timestamp as total watched

            // Calculate validation score
            var validationScore = CalculateValidationScore(
                dto.TabActive ?? true,
                dto.PlaybackSpeed,
                sessionDurationSeconds);

            var countsForPayout = validationScore >= MinValidationScore;

            // Build metadata JSON
            var metadata = new
            {
                currentTimestamp = dto.CurrentTimestampSeconds,
                totalDuration = dto.TotalDurationSeconds,
                playbackSpeed = dto.PlaybackSpeed ?? 1.0m,
                tabActive = dto.TabActive ?? true,
                sessionId = dto.SessionId
            };

            CourseEngagement engagement;

            // Check if we should update existing engagement or create new
            if (!string.IsNullOrEmpty(dto.SessionId) && Guid.TryParse(dto.SessionId, out var sessionGuid))
            {
                // Update existing engagement
                engagement = await _engagementRepository.GetByIdAsync(sessionGuid, ct);
                if (engagement != null && engagement.UserId == userId && engagement.LessonId == dto.LessonId)
                {
                    engagement.DurationMinutes = sessionDurationSeconds / 60;
                    engagement.ValidationScore = validationScore;
                    engagement.CountsForPayout = countsForPayout;
                    engagement.MetaData = JsonSerializer.Serialize(metadata);

                    // Mark as completed if reached threshold
                    if (CalculateCompletionPercentage(dto.CurrentTimestampSeconds, dto.TotalDurationSeconds) >= CompletionThreshold * 100)
                    {
                        engagement.CompletedAt = DateTime.UtcNow;
                    }

                    engagement = await _engagementRepository.UpdateAsync(engagement);
                    _logger.LogDebug("Updated engagement {EngagementId} for user {UserId}", engagement.Id, userId);
                }
                else
                {
                    // Session ID invalid or doesn't match user/lesson, create new
                    engagement = await CreateNewEngagement(userId, lesson.CourseId, dto.LessonId, sessionDurationSeconds, validationScore, countsForPayout, metadata, ct);
                }
            }
            else
            {
                // Create new engagement session
                engagement = await CreateNewEngagement(userId, lesson.CourseId, dto.LessonId, sessionDurationSeconds, validationScore, countsForPayout, metadata, ct);
            }

            var completionPercentage = CalculateCompletionPercentage(dto.CurrentTimestampSeconds, dto.TotalDurationSeconds);

            return new VideoProgressResponseDto
            {
                EngagementId = engagement.Id,
                CurrentTimestampSeconds = dto.CurrentTimestampSeconds,
                CompletionPercentage = completionPercentage,
                IsCompleted = engagement.CompletedAt.HasValue,
                ValidationScore = validationScore,
                CountsForPayout = countsForPayout
            };
        }

        public async Task<int?> GetLastPositionAsync(Guid userId, Guid lessonId, CancellationToken ct = default)
        {
            _logger.LogDebug("Getting last position for user {UserId} in lesson {LessonId}", userId, lessonId);

            // Get most recent engagement for this user/lesson
            var engagements = await _engagementRepository.GetByUserIdAsync(userId, 1, 100);
            var lessonEngagement = engagements
                .Where(e => e.LessonId == lessonId && e.EngagementType == "video_watch")
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefault();

            if (lessonEngagement == null || string.IsNullOrEmpty(lessonEngagement.MetaData))
                return null;

            try
            {
                var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(lessonEngagement.MetaData);
                if (metadata != null && metadata.TryGetValue("currentTimestamp", out var timestamp))
                {
                    return timestamp.GetInt32();
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse engagement metadata for engagement {Id}", lessonEngagement.Id);
            }

            return null;
        }

        public async Task<LessonProgressDto?> GetLessonProgressAsync(Guid userId, Guid lessonId, CancellationToken ct = default)
        {
            _logger.LogDebug("Getting lesson progress for user {UserId} in lesson {LessonId}", userId, lessonId);

            var lesson = await _lessonRepository.GetByIdAsync(lessonId, ct);
            if (lesson == null)
                return null;

            var engagements = await _engagementRepository.GetByUserIdAsync(userId, 1, 100);
            var lessonEngagements = engagements
                .Where(e => e.LessonId == lessonId && e.EngagementType == "video_watch")
                .ToList();

            if (!lessonEngagements.Any())
                return null;

            var totalWatchedMinutes = lessonEngagements.Sum(e => e.DurationMinutes);
            var latestEngagement = lessonEngagements.OrderByDescending(e => e.CreatedAt).First();
            var isCompleted = latestEngagement.CompletedAt.HasValue;

            // Get total duration from metadata
            int totalDurationSeconds = 0;
            if (!string.IsNullOrEmpty(latestEngagement.MetaData))
            {
                try
                {
                    var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(latestEngagement.MetaData);
                    if (metadata != null && metadata.TryGetValue("totalDuration", out var duration))
                    {
                        totalDurationSeconds = duration.GetInt32();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse engagement metadata");
                }
            }

            var progressPercentage = totalDurationSeconds > 0
                ? CalculateCompletionPercentage(totalWatchedMinutes * 60, totalDurationSeconds)
                : 0;

            return new LessonProgressDto
            {
                Id = latestEngagement.Id,
                LessonId = lessonId,
                LessonTitle = lesson.Title,
                UserId = userId,
                IsCompleted = isCompleted,
                WatchedMinutes = totalWatchedMinutes,
                LessonDurationMinutes = totalDurationSeconds / 60,
                ProgressPercentage = progressPercentage,
                CompletedAt = latestEngagement.CompletedAt,
                CreatedAt = lessonEngagements.Min(e => e.CreatedAt),
                UpdatedAt = latestEngagement.CreatedAt
            };
        }

        public async Task<List<LessonProgressDto>> GetCourseProgressAsync(Guid userId, Guid courseId, CancellationToken ct = default)
        {
            _logger.LogDebug("Getting course progress for user {UserId} in course {CourseId}", userId, courseId);

            var engagements = await _engagementRepository.GetByUserAndCourseAsync(
                userId,
                courseId,
                DateTime.UtcNow.AddYears(-1), // Get last year of data
                DateTime.UtcNow);

            var lessonGroups = engagements
                .Where(e => e.LessonId.HasValue && e.EngagementType == "video_watch")
                .GroupBy(e => e.LessonId!.Value);

            var progressList = new List<LessonProgressDto>();

            foreach (var group in lessonGroups)
            {
                var lessonId = group.Key;
                var lessonEngagements = group.ToList();

                var lesson = await _lessonRepository.GetByIdAsync(lessonId, ct);
                if (lesson == null)
                    continue;

                var totalWatchedMinutes = lessonEngagements.Sum(e => e.DurationMinutes);
                var latestEngagement = lessonEngagements.OrderByDescending(e => e.CreatedAt).First();
                var isCompleted = latestEngagement.CompletedAt.HasValue;

                // Get total duration from metadata
                int totalDurationSeconds = 0;
                if (!string.IsNullOrEmpty(latestEngagement.MetaData))
                {
                    try
                    {
                        var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(latestEngagement.MetaData);
                        if (metadata != null && metadata.TryGetValue("totalDuration", out var duration))
                        {
                            totalDurationSeconds = duration.GetInt32();
                        }
                    }
                    catch (JsonException) { }
                }

                var progressPercentage = totalDurationSeconds > 0
                    ? CalculateCompletionPercentage(totalWatchedMinutes * 60, totalDurationSeconds)
                    : 0;

                progressList.Add(new LessonProgressDto
                {
                    Id = latestEngagement.Id,
                    LessonId = lessonId,
                    LessonTitle = lesson.Title,
                    UserId = userId,
                    IsCompleted = isCompleted,
                    WatchedMinutes = totalWatchedMinutes,
                    LessonDurationMinutes = totalDurationSeconds / 60,
                    ProgressPercentage = progressPercentage,
                    CompletedAt = latestEngagement.CompletedAt,
                    CreatedAt = lessonEngagements.Min(e => e.CreatedAt),
                    UpdatedAt = latestEngagement.CreatedAt
                });
            }

            return progressList.OrderBy(p => p.LessonTitle).ToList();
        }

        public async Task MarkLessonCompleteAsync(Guid userId, Guid lessonId, CancellationToken ct = default)
        {
            _logger.LogInformation("Marking lesson {LessonId} as complete for user {UserId}", lessonId, userId);

            var engagements = await _engagementRepository.GetByUserIdAsync(userId, 1, 100);
            var lessonEngagement = engagements
                .Where(e => e.LessonId == lessonId && e.EngagementType == "video_watch")
                .OrderByDescending(e => e.CreatedAt)
                .FirstOrDefault();

            if (lessonEngagement == null)
                throw new KeyNotFoundException($"No engagement found for user {userId} in lesson {lessonId}");

            if (!lessonEngagement.CompletedAt.HasValue)
            {
                lessonEngagement.CompletedAt = DateTime.UtcNow;
                await _engagementRepository.UpdateAsync(lessonEngagement);
            }

            // TODO: Check if all lessons in course are complete → trigger certificate generation
        }

        public double CalculateCompletionPercentage(int watchedSeconds, int totalDurationSeconds)
        {
            if (totalDurationSeconds == 0)
                return 0;

            var percentage = (double)watchedSeconds / totalDurationSeconds * 100.0;
            return Math.Min(Math.Round(percentage, 2), 100.0); // Cap at 100%
        }

        public decimal CalculateValidationScore(bool tabActive, decimal? playbackSpeed, int sessionDurationSeconds)
        {
            decimal score = 1.0m; // Start with perfect score

            // Tab visibility penalty
            if (!tabActive)
                score -= 0.3m; // 30% penalty if tab was inactive

            // Playback speed penalty (too fast = less learning)
            if (playbackSpeed.HasValue)
            {
                if (playbackSpeed.Value > 2.0m)
                    score -= 0.5m; // 50% penalty for > 2x speed (likely fraud)
                else if (playbackSpeed.Value > 1.5m)
                    score -= 0.2m; // 20% penalty for > 1.5x speed
                else if (playbackSpeed.Value < 0.5m)
                    score -= 0.3m; // 30% penalty for < 0.5x speed (suspicious)
            }

            // Session duration penalty (too short sessions = suspicious)
            if (sessionDurationSeconds < 10)
                score -= 0.2m; // 20% penalty for < 10 second sessions

            // Clamp score between 0 and 1
            return Math.Clamp(score, 0.0m, 1.0m);
        }

        private async Task<CourseEngagement> CreateNewEngagement(
            Guid userId,
            Guid courseId,
            Guid lessonId,
            int sessionDurationSeconds,
            decimal validationScore,
            bool countsForPayout,
            object metadata,
            CancellationToken ct)
        {
            var engagement = new CourseEngagement
            {
                UserId = userId,
                CourseId = courseId,
                LessonId = lessonId,
                EngagementType = "video_watch",
                DurationMinutes = sessionDurationSeconds / 60,
                StartedAt = DateTime.UtcNow,
                ValidationScore = validationScore,
                CountsForPayout = countsForPayout,
                MetaData = JsonSerializer.Serialize(metadata)
            };

            engagement = await _engagementRepository.CreateAsync(engagement);
            _logger.LogDebug("Created new engagement {EngagementId} for user {UserId}", engagement.Id, userId);

            return engagement;
        }
    }
}
