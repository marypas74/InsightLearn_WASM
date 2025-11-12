using InsightLearn.Core.Entities;
using InsightLearn.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InsightLearn.Application.Services;

public class EngagementTrackingService : IEngagementTrackingService
{
    private readonly ICourseEngagementRepository _engagementRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly ILogger<EngagementTrackingService> _logger;

    // Anti-fraud validation weights
    private const decimal TAB_VISIBILITY_WEIGHT = 0.40m;
    private const decimal PLAYBACK_SPEED_WEIGHT = 0.30m;
    private const decimal SESSION_CONTINUITY_WEIGHT = 0.20m;
    private const decimal DEVICE_FINGERPRINT_WEIGHT = 0.10m;

    // Validation thresholds
    private const decimal VALIDATION_THRESHOLD = 0.70m;
    private const int MAX_REASONABLE_DURATION_MINUTES = 180; // 3 hours

    public EngagementTrackingService(
        ICourseEngagementRepository engagementRepository,
        ICourseRepository courseRepository,
        ILogger<EngagementTrackingService> logger)
    {
        _engagementRepository = engagementRepository;
        _courseRepository = courseRepository;
        _logger = logger;
    }

    public async Task<CourseEngagement?> RecordEngagementAsync(
        Guid userId,
        Guid courseId,
        string engagementType,
        int durationMinutes,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceFingerprint = null,
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            // Validate course exists
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course == null)
            {
                _logger.LogWarning($"Course {courseId} not found for engagement recording");
                return null;
            }

            // Calculate validation score
            var validationScore = await CalculateValidationScoreAsync(
                userId,
                courseId,
                durationMinutes,
                deviceFingerprint,
                metadata);

            // Determine if engagement counts for payout
            var countsForPayout = validationScore >= VALIDATION_THRESHOLD;

            // Create engagement record
            var engagement = new CourseEngagement
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CourseId = courseId,
                EngagementType = engagementType,
                DurationMinutes = durationMinutes,
                ValidationScore = validationScore,
                CountsForPayout = countsForPayout,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceFingerprint = deviceFingerprint,
                MetaData = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow.AddMinutes(durationMinutes),
                CreatedAt = DateTime.UtcNow
            };

            var created = await _engagementRepository.CreateAsync(engagement);

            _logger.LogInformation(
                $"Recorded engagement: User={userId}, Course={courseId}, Type={engagementType}, " +
                $"Duration={durationMinutes}min, Score={validationScore:F2}, CountsForPayout={countsForPayout}");

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error recording engagement for user {userId} on course {courseId}");
            return null;
        }
    }

    public async Task<decimal> CalculateValidationScoreAsync(
        Guid userId,
        Guid courseId,
        int durationMinutes,
        string? deviceFingerprint = null,
        Dictionary<string, object>? metadata = null)
    {
        try
        {
            decimal totalScore = 0m;

            // Factor 1: Tab Visibility (40%)
            var tabVisibilityScore = CalculateTabVisibilityScore(metadata);
            totalScore += tabVisibilityScore * TAB_VISIBILITY_WEIGHT;

            // Factor 2: Playback Speed (30%)
            var playbackSpeedScore = CalculatePlaybackSpeedScore(durationMinutes, metadata);
            totalScore += playbackSpeedScore * PLAYBACK_SPEED_WEIGHT;

            // Factor 3: Session Continuity (20%)
            var sessionContinuityScore = await CalculateSessionContinuityScoreAsync(userId, courseId, metadata);
            totalScore += sessionContinuityScore * SESSION_CONTINUITY_WEIGHT;

            // Factor 4: Device Fingerprint Consistency (10%)
            var deviceFingerprintScore = await CalculateDeviceFingerprintScoreAsync(userId, deviceFingerprint);
            totalScore += deviceFingerprintScore * DEVICE_FINGERPRINT_WEIGHT;

            // Ensure score is between 0 and 1
            totalScore = Math.Max(0m, Math.Min(1m, totalScore));

            _logger.LogDebug(
                $"Validation score for User={userId}, Course={courseId}: " +
                $"Total={totalScore:F2}, TabVis={tabVisibilityScore:F2}, " +
                $"PlaybackSpeed={playbackSpeedScore:F2}, SessionCont={sessionContinuityScore:F2}, " +
                $"DeviceFingerprint={deviceFingerprintScore:F2}");

            return totalScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating validation score for user {userId} on course {courseId}");
            // Return default score of 0.5 (neutral) on error
            return 0.5m;
        }
    }

    public async Task<int> ValidatePendingEngagementsAsync(DateTime? since = null, int batchSize = 100)
    {
        try
        {
            var sinceDate = since ?? DateTime.UtcNow.AddHours(-24);
            var pendingEngagements = await _engagementRepository.GetPendingValidationAsync(sinceDate, batchSize);

            int validatedCount = 0;

            foreach (var engagement in pendingEngagements)
            {
                // Recalculate validation score
                var metadata = engagement.MetaData != null
                    ? JsonSerializer.Deserialize<Dictionary<string, object>>(engagement.MetaData)
                    : null;

                var newScore = await CalculateValidationScoreAsync(
                    engagement.UserId,
                    engagement.CourseId,
                    engagement.DurationMinutes,
                    engagement.DeviceFingerprint,
                    metadata);

                var countsForPayout = newScore >= VALIDATION_THRESHOLD;

                // Update engagement
                var updated = await _engagementRepository.ValidateEngagementAsync(
                    engagement.Id,
                    newScore,
                    countsForPayout);

                if (updated)
                    validatedCount++;
            }

            _logger.LogInformation($"Validated {validatedCount} engagements in batch");
            return validatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating pending engagements");
            return 0;
        }
    }

    public async Task<UserEngagementAnalytics> GetUserEngagementAnalyticsAsync(Guid userId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var engagements = await _engagementRepository.GetByUserIdAsync(userId, startDate, endDate);

            var analytics = new UserEngagementAnalytics
            {
                UserId = userId,
                TotalEngagementMinutes = engagements.Sum(e => e.DurationMinutes),
                ValidatedEngagementMinutes = engagements.Where(e => e.CountsForPayout).Sum(e => e.DurationMinutes),
                TotalEngagements = engagements.Count,
                ValidatedEngagements = engagements.Count(e => e.CountsForPayout),
                AverageValidationScore = engagements.Any() ? engagements.Average(e => e.ValidationScore) : 0m,
                UniqueCoursesEngaged = engagements.Select(e => e.CourseId).Distinct().Count(),
                EngagementByType = engagements
                    .GroupBy(e => e.EngagementType)
                    .ToDictionary(g => g.Key, g => (long)g.Sum(e => e.DurationMinutes))
            };

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting user engagement analytics for user {userId}");
            return new UserEngagementAnalytics { UserId = userId };
        }
    }

    public async Task<CourseEngagementAnalytics> GetCourseEngagementAnalyticsAsync(Guid courseId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var engagements = await _engagementRepository.GetByCourseIdAsync(courseId, startDate, endDate);

            var analytics = new CourseEngagementAnalytics
            {
                CourseId = courseId,
                TotalEngagementMinutes = engagements.Sum(e => e.DurationMinutes),
                ValidatedEngagementMinutes = engagements.Where(e => e.CountsForPayout).Sum(e => e.DurationMinutes),
                TotalEngagements = engagements.Count,
                ValidatedEngagements = engagements.Count(e => e.CountsForPayout),
                UniqueUsers = engagements.Select(e => e.UserId).Distinct().Count(),
                AverageValidationScore = engagements.Any() ? engagements.Average(e => e.ValidationScore) : 0m,
                EngagementByType = engagements
                    .GroupBy(e => e.EngagementType)
                    .ToDictionary(g => g.Key, g => (long)g.Sum(e => e.DurationMinutes))
            };

            return analytics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting course engagement analytics for course {courseId}");
            return new CourseEngagementAnalytics { CourseId = courseId };
        }
    }

    public async Task<InstructorEngagementBreakdown> GetInstructorEngagementBreakdownAsync(Guid instructorId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var engagementBreakdown = await _engagementRepository.GetCourseEngagementBreakdownAsync(instructorId, startDate, endDate);
            var totalMinutes = await _engagementRepository.GetInstructorEngagementMinutesAsync(instructorId, startDate, endDate);

            var breakdown = new InstructorEngagementBreakdown
            {
                InstructorId = instructorId,
                TotalValidatedEngagementMinutes = totalMinutes
            };

            // Build course breakdown
            foreach (var courseEntry in engagementBreakdown)
            {
                var course = await _courseRepository.GetByIdAsync(courseEntry.Key);
                if (course == null)
                    continue;

                var courseEngagements = await _engagementRepository.GetByCourseIdAsync(courseEntry.Key, startDate, endDate);
                var validatedEngagements = courseEngagements.Where(e => e.CountsForPayout).ToList();

                breakdown.CourseBreakdown[courseEntry.Key] = new CourseEngagementDetail
                {
                    CourseId = courseEntry.Key,
                    CourseName = course.Title,
                    ValidatedEngagementMinutes = courseEntry.Value,
                    UniqueStudents = validatedEngagements.Select(e => e.UserId).Distinct().Count(),
                    TotalEngagements = validatedEngagements.Count
                };
            }

            breakdown.UniqueStudents = breakdown.CourseBreakdown.Values.Sum(c => c.UniqueStudents);
            breakdown.TotalEngagements = breakdown.CourseBreakdown.Values.Sum(c => c.TotalEngagements);

            return breakdown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting instructor engagement breakdown for instructor {instructorId}");
            return new InstructorEngagementBreakdown { InstructorId = instructorId };
        }
    }

    public async Task<bool> MarkEngagementAsFraudulentAsync(Guid engagementId, string reason)
    {
        try
        {
            var engagement = await _engagementRepository.GetByIdAsync(engagementId);
            if (engagement == null)
            {
                _logger.LogWarning($"Engagement {engagementId} not found");
                return false;
            }

            // Set validation score to 0 and mark as not counting for payout
            var updated = await _engagementRepository.ValidateEngagementAsync(engagementId, 0m, false);

            if (updated)
            {
                _logger.LogWarning($"Engagement {engagementId} marked as fraudulent: {reason}");
            }

            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error marking engagement {engagementId} as fraudulent");
            return false;
        }
    }

    public async Task<bool> RecalculateValidationScoreAsync(Guid engagementId)
    {
        try
        {
            var engagement = await _engagementRepository.GetByIdAsync(engagementId);
            if (engagement == null)
            {
                _logger.LogWarning($"Engagement {engagementId} not found");
                return false;
            }

            var metadata = engagement.MetaData != null
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(engagement.MetaData)
                : null;

            var newScore = await CalculateValidationScoreAsync(
                engagement.UserId,
                engagement.CourseId,
                engagement.DurationMinutes,
                engagement.DeviceFingerprint,
                metadata);

            var countsForPayout = newScore >= VALIDATION_THRESHOLD;

            return await _engagementRepository.ValidateEngagementAsync(engagementId, newScore, countsForPayout);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error recalculating validation score for engagement {engagementId}");
            return false;
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Calculates tab visibility score based on metadata
    /// Checks how much time the tab was actually visible/active
    /// </summary>
    private decimal CalculateTabVisibilityScore(Dictionary<string, object>? metadata)
    {
        if (metadata == null)
            return 0.5m; // Neutral score if no data

        try
        {
            // Check for tab visibility percentage in metadata
            if (metadata.TryGetValue("tabVisibilityPercentage", out var visibilityObj))
            {
                if (visibilityObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
                {
                    var visibilityPercentage = jsonElement.GetDecimal();
                    // Convert percentage (0-100) to score (0-1)
                    return Math.Max(0m, Math.Min(1m, visibilityPercentage / 100m));
                }
            }

            // Fallback: check for visibility events
            if (metadata.TryGetValue("visibilityEvents", out var eventsObj))
            {
                // If we have visibility events, assume moderate engagement
                return 0.7m;
            }

            return 0.5m; // Default neutral
        }
        catch
        {
            return 0.5m;
        }
    }

    /// <summary>
    /// Calculates playback speed score
    /// Penalizes extremely fast or slow playback speeds
    /// </summary>
    private decimal CalculatePlaybackSpeedScore(int durationMinutes, Dictionary<string, object>? metadata)
    {
        // Check for unreasonably long sessions
        if (durationMinutes > MAX_REASONABLE_DURATION_MINUTES)
        {
            return 0.3m; // Penalize very long sessions
        }

        if (metadata == null)
            return 0.7m; // Default good score if no metadata

        try
        {
            if (metadata.TryGetValue("playbackSpeed", out var speedObj))
            {
                if (speedObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Number)
                {
                    var playbackSpeed = jsonElement.GetDecimal();

                    // Normal speed (0.8 - 1.5x) = good score
                    if (playbackSpeed >= 0.8m && playbackSpeed <= 1.5m)
                        return 1.0m;

                    // Slightly fast (1.5 - 2.0x) = moderate score
                    if (playbackSpeed > 1.5m && playbackSpeed <= 2.0m)
                        return 0.7m;

                    // Very fast (>2.0x) or slow (<0.8x) = low score
                    return 0.4m;
                }
            }

            return 0.7m; // Default if no playback speed data
        }
        catch
        {
            return 0.7m;
        }
    }

    /// <summary>
    /// Calculates session continuity score
    /// Checks if user has consistent engagement patterns
    /// </summary>
    private async Task<decimal> CalculateSessionContinuityScoreAsync(Guid userId, Guid courseId, Dictionary<string, object>? metadata)
    {
        try
        {
            // Get recent engagements for this user on this course (last 7 days)
            var recentEngagements = await _engagementRepository.GetByUserAndCourseAsync(
                userId,
                courseId,
                DateTime.UtcNow.AddDays(-7),
                DateTime.UtcNow);

            if (!recentEngagements.Any())
                return 0.8m; // First engagement, assume good

            // Check for consistent engagement patterns
            var avgDuration = recentEngagements.Average(e => e.DurationMinutes);
            var avgScore = recentEngagements.Average(e => e.ValidationScore);

            // If user has good historical scores, give higher continuity score
            if (avgScore >= 0.7m)
                return 1.0m;

            if (avgScore >= 0.5m)
                return 0.7m;

            return 0.5m; // Questionable historical pattern
        }
        catch
        {
            return 0.6m; // Default moderate score
        }
    }

    /// <summary>
    /// Calculates device fingerprint consistency score
    /// Checks if user consistently uses same device
    /// </summary>
    private async Task<decimal> CalculateDeviceFingerprintScoreAsync(Guid userId, string? deviceFingerprint)
    {
        if (string.IsNullOrEmpty(deviceFingerprint))
            return 0.5m; // Neutral if no fingerprint

        try
        {
            // Get recent engagements for this user (last 30 days)
            var recentEngagements = await _engagementRepository.GetByUserIdAsync(
                userId,
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow);

            if (!recentEngagements.Any())
                return 0.8m; // First engagement, assume good

            // Check how many unique device fingerprints user has used
            var uniqueFingerprints = recentEngagements
                .Where(e => !string.IsNullOrEmpty(e.DeviceFingerprint))
                .Select(e => e.DeviceFingerprint)
                .Distinct()
                .Count();

            // 1-2 devices = very good
            if (uniqueFingerprints <= 2)
                return 1.0m;

            // 3-4 devices = moderate (could be legitimate: phone, tablet, laptop, desktop)
            if (uniqueFingerprints <= 4)
                return 0.7m;

            // 5+ devices = suspicious
            return 0.4m;
        }
        catch
        {
            return 0.6m; // Default moderate score
        }
    }

    #endregion
}
