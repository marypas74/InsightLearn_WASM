using System;
using System.Collections.Generic;

namespace InsightLearn.Core.DTOs.VideoTranscript
{
    /// <summary>
    /// Complete video transcript response DTO.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class VideoTranscriptDto
    {
        public Guid LessonId { get; set; }
        public string Language { get; set; } = "en-US";
        public string ProcessingStatus { get; set; } = "Completed";

        /// <summary>
        /// List of transcript segments with timestamps.
        /// </summary>
        public List<TranscriptSegmentDto> Transcript { get; set; } = new();

        /// <summary>
        /// Metadata about the transcript.
        /// </summary>
        public TranscriptMetadataDto? Metadata { get; set; }
    }

    /// <summary>
    /// Single transcript segment (timestamped text).
    /// </summary>
    public class TranscriptSegmentDto
    {
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public string? Speaker { get; set; }
        public string Text { get; set; } = string.Empty;
        public double? Confidence { get; set; }
    }

    /// <summary>
    /// Transcript metadata (processing info).
    /// </summary>
    public class TranscriptMetadataDto
    {
        public int DurationSeconds { get; set; }
        public int WordCount { get; set; }
        public double AverageConfidence { get; set; }
        public string ProcessingEngine { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
    }
}
