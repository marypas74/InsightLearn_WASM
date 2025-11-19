using System;
using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.VideoTranscripts
{
    /// <summary>
    /// DTO for queueing video transcript generation.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class QueueTranscriptDto
    {
        [Required(ErrorMessage = "Lesson ID is required")]
        public Guid LessonId { get; set; }

        [Required(ErrorMessage = "Video URL is required")]
        [StringLength(2000, ErrorMessage = "Video URL must be 2000 characters or less")]
        public string VideoUrl { get; set; } = string.Empty;

        /// <summary>
        /// Language code for ASR (e.g., "en-US", "it-IT", "es-ES").
        /// Default: "en-US".
        /// </summary>
        [StringLength(10, ErrorMessage = "Language code must be 10 characters or less")]
        [RegularExpression(@"^[a-z]{2}-[A-Z]{2}$", ErrorMessage = "Language code must be in format: 'en-US'")]
        public string? Language { get; set; } = "en-US";
    }
}
