using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using InsightLearn.Core.DTOs.Engagement;
using InsightLearn.Core.DTOs.Enrollment;

namespace InsightLearn.Core.Interfaces
{
    /// <summary>
    /// Service interface for video progress tracking.
    /// Uses CourseEngagement backend for persistence and validation.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public interface IVideoProgressService
    {
        /// <summary>
        /// Track video watch progress (current timestamp, duration).
        /// Updates existing engagement session or creates new one.
        /// Calculates validation score based on tab activity, playback speed, etc.
        /// </summary>
        Task<VideoProgressResponseDto> TrackProgressAsync(Guid userId, TrackVideoProgressDto dto, CancellationToken ct = default);

        /// <summary>
        /// Get last watched position for a lesson to resume playback.
        /// Returns null if no progress exists.
        /// </summary>
        Task<int?> GetLastPositionAsync(Guid userId, Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Get detailed progress summary for a specific lesson.
        /// Includes: watched minutes, completion percentage, last position, validation score.
        /// </summary>
        Task<LessonProgressDto?> GetLessonProgressAsync(Guid userId, Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Get progress summary for all lessons in a course.
        /// Useful for course dashboard/analytics.
        /// </summary>
        Task<List<LessonProgressDto>> GetCourseProgressAsync(Guid userId, Guid courseId, CancellationToken ct = default);

        /// <summary>
        /// Mark a lesson as complete when user reaches >= 90% watched.
        /// Also triggers certificate generation if course complete.
        /// </summary>
        Task MarkLessonCompleteAsync(Guid userId, Guid lessonId, CancellationToken ct = default);

        /// <summary>
        /// Calculate watch completion percentage (0-100).
        /// Based on total watch time vs lesson duration.
        /// </summary>
        double CalculateCompletionPercentage(int watchedSeconds, int totalDurationSeconds);

        /// <summary>
        /// Calculate engagement validation score (0.0 - 1.0).
        /// Anti-fraud: checks tab visibility, playback speed, session continuity.
        /// Score >= 0.7 counts toward instructor payout.
        /// </summary>
        decimal CalculateValidationScore(bool tabActive, decimal? playbackSpeed, int sessionDurationSeconds);
    }

    /// <summary>
    /// Response DTO for track progress operation.
    /// </summary>
    public class VideoProgressResponseDto
    {
        public Guid EngagementId { get; set; }
        public int CurrentTimestampSeconds { get; set; }
        public double CompletionPercentage { get; set; }
        public bool IsCompleted { get; set; }
        public decimal ValidationScore { get; set; }
        public bool CountsForPayout { get; set; }
    }
}
