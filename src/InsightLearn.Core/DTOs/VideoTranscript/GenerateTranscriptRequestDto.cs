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

        /// <summary>
        /// Alias for ProcessingStatus (used by services).
        /// </summary>
        public string Status => ProcessingStatus;

        /// <summary>
        /// Alias for ProgressPercentage (used by services).
        /// </summary>
        public int? Progress => ProgressPercentage;

        /// <summary>
        /// Estimated time remaining in seconds (calculated from progress).
        /// </summary>
        public int? EstimatedTimeRemaining
        {
            get
            {
                if (!ProgressPercentage.HasValue || ProgressPercentage.Value == 0)
                    return null;

                // Estimate based on average processing speed (1 minute per 1% for transcripts)
                var remainingPercent = 100 - ProgressPercentage.Value;
                return remainingPercent * 60; // seconds
            }
        }
    }

    /// <summary>
    /// Request DTO for batch translation of transcripts.
    /// v2.3.99-dev: Queue translations to multiple languages.
    /// </summary>
    public class BatchTranslationRequest
    {
        /// <summary>
        /// Lesson ID to translate.
        /// </summary>
        [Required]
        public Guid LessonId { get; set; }

        /// <summary>
        /// Target languages (ISO 639-1 codes: es, fr, de, pt, it).
        /// If null/empty, uses config default: ["es", "fr", "de", "pt", "it"].
        /// </summary>
        public string[]? TargetLanguages { get; set; }

        /// <summary>
        /// Translator to use: "openai", "azure", or "ollama".
        /// If null, uses config default (typically "openai").
        /// </summary>
        public string? Translator { get; set; }
    }
}
