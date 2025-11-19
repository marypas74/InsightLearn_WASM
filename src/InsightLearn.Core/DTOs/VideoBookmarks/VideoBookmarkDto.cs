using System;
using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.VideoBookmarks
{
    /// <summary>
    /// Video bookmark response DTO.
    /// Part of Student Learning Space v2.1.0.
    /// </summary>
    public class VideoBookmarkDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid LessonId { get; set; }

        /// <summary>
        /// Video timestamp in seconds.
        /// </summary>
        public int VideoTimestamp { get; set; }

        /// <summary>
        /// Optional custom label.
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// Manual (user-created) or Auto (AI-generated from key takeaways).
        /// </summary>
        public string BookmarkType { get; set; } = "Manual";

        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating a video bookmark.
    /// </summary>
    public class CreateVideoBookmarkDto
    {
        [Required(ErrorMessage = "Lesson ID is required")]
        public Guid LessonId { get; set; }

        [Required(ErrorMessage = "Video timestamp is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Video timestamp must be non-negative")]
        public int VideoTimestamp { get; set; }

        [StringLength(200, ErrorMessage = "Label must be 200 characters or less")]
        public string? Label { get; set; }

        /// <summary>
        /// Bookmark type: Manual or Auto.
        /// Default: Manual.
        /// </summary>
        [RegularExpression("^(Manual|Auto)$", ErrorMessage = "Bookmark type must be Manual or Auto")]
        public string BookmarkType { get; set; } = "Manual";
    }

    /// <summary>
    /// DTO for updating a video bookmark.
    /// </summary>
    public class UpdateVideoBookmarkDto
    {
        [StringLength(200, ErrorMessage = "Label must be 200 characters or less")]
        public string? Label { get; set; }
    }
}
