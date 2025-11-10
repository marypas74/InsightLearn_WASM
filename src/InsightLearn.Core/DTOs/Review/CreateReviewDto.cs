using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Review;

public class CreateReviewDto
{
    [Required] public Guid UserId { get; set; }
    [Required] public Guid CourseId { get; set; }
    [Required][Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")] public int Rating { get; set; }
    [StringLength(2000, ErrorMessage = "Comment cannot exceed 2000 characters")] public string? Comment { get; set; }
}
