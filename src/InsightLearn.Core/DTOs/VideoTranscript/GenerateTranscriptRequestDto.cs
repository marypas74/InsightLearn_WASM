using System;
using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.VideoTranscript
{
    /// <summary>
    /// Request DTO for queuing transcript generation.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class GenerateTranscriptRequestDto
    {
        /// <summary>
        /// Language code for transcript (e.g., "en-US", "it-IT").
        /// Default: en-US.
        /// </summary>
        [RegularExpression(@"^[a-z]{2}-[A-Z]{2}$", ErrorMessage = "Language must be in format: xx-XX (e.g., en-US)")]
        public string Language { get; set; } = "en-US";

        /// <summary>
        /// Optional: Force regeneration even if transcript already exists.
        /// </summary>
        public bool ForceRegenerate { get; set; } = false;
    }

    /// <summary>
    /// Transcript processing status response.
    /// </summary>
    public class TranscriptProcessingStatusDto
    {
        public Guid LessonId { get; set; }
        public string ProcessingStatus { get; set; } = "Pending";
        public int? ProgressPercentage { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
