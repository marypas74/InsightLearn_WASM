using System;
using System.Collections.Generic;

namespace InsightLearn.Core.DTOs.VideoTranscript
{
    /// <summary>
    /// Transcript search result DTO.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class TranscriptSearchResultDto
    {
        public Guid LessonId { get; set; }
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Total number of matches found.
        /// </summary>
        public int TotalMatches { get; set; }

        /// <summary>
        /// List of matching segments with context.
        /// </summary>
        public List<TranscriptSearchMatchDto> Matches { get; set; } = new();
    }

    /// <summary>
    /// Single search match with highlighted text.
    /// </summary>
    public class TranscriptSearchMatchDto
    {
        public double Timestamp { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? Speaker { get; set; }

        /// <summary>
        /// Relevance score (0.0 - 1.0).
        /// </summary>
        public double RelevanceScore { get; set; }
    }
}
