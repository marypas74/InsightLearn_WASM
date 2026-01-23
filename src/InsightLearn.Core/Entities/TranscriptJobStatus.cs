using System;
using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities
{
    /// <summary>
    /// Tracks real-time status of transcript generation jobs.
    /// Provides visibility into long-running transcription processes.
    /// v2.3.78-dev: Added for real-time monitoring of FasterWhisper transcriptions.
    /// </summary>
    public class TranscriptJobStatus
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Lesson ID being transcribed
        /// </summary>
        public Guid LessonId { get; set; }

        /// <summary>
        /// Hangfire Job ID (for correlation)
        /// </summary>
        [MaxLength(50)]
        public string? HangfireJobId { get; set; }

        /// <summary>
        /// Current processing phase
        /// </summary>
        [MaxLength(50)]
        public string Phase { get; set; } = "Queued";

        /// <summary>
        /// Job status: Queued, Downloading, ExtractingAudio, Transcribing, Saving, Completed, Failed, Timeout
        /// </summary>
        [MaxLength(50)]
        public string Status { get; set; } = "Queued";

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage { get; set; } = 0;

        /// <summary>
        /// Current status message for UI display
        /// </summary>
        [MaxLength(500)]
        public string? StatusMessage { get; set; }

        /// <summary>
        /// Video duration in seconds (for progress calculation)
        /// </summary>
        public double? VideoDurationSeconds { get; set; }

        /// <summary>
        /// Audio file size in KB (for progress calculation)
        /// </summary>
        public long? AudioFileSizeKB { get; set; }

        /// <summary>
        /// Transcription service used (FasterWhisper, OpenAI, etc.)
        /// </summary>
        [MaxLength(50)]
        public string? ServiceUsed { get; set; }

        /// <summary>
        /// Language code (en-US, it-IT, etc.)
        /// </summary>
        [MaxLength(10)]
        public string? Language { get; set; }

        /// <summary>
        /// Error message if failed
        /// </summary>
        [MaxLength(2000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Number of retry attempts
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// When the job was queued
        /// </summary>
        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When processing started
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// When the job completed (success or failure)
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Last update timestamp (for stale job detection)
        /// </summary>
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Number of segments generated (populated on completion)
        /// </summary>
        public int? SegmentCount { get; set; }

        /// <summary>
        /// Total processing time in milliseconds
        /// </summary>
        public long? ProcessingTimeMs { get; set; }

        // ═══════════════════════════════════════════════════════════════════════════════
        // CHUNK TRACKING (v2.3.97-dev): Real-time chunk-by-chunk progress
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Total number of audio chunks to process (30s each)
        /// </summary>
        public int? ChunkCount { get; set; }

        /// <summary>
        /// Number of chunks completed
        /// </summary>
        public int? CompletedChunks { get; set; }

        /// <summary>
        /// Current chunk being processed (1-based)
        /// </summary>
        public int? CurrentChunk { get; set; }

        /// <summary>
        /// Size of each chunk in seconds (default: 30)
        /// </summary>
        public int ChunkDurationSeconds { get; set; } = 30;

        // Calculated properties
        public TimeSpan? ElapsedTime => StartedAt.HasValue
            ? (CompletedAt ?? DateTime.UtcNow) - StartedAt.Value
            : null;

        public bool IsStale => LastUpdatedAt < DateTime.UtcNow.AddMinutes(-5) &&
                               Status != "Completed" && Status != "Failed";

        public bool IsTerminal => Status == "Completed" || Status == "Failed" || Status == "Timeout";
    }
}
