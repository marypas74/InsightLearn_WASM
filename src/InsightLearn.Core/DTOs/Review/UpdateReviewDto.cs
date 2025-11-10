using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Review;

public class UpdateReviewDto
{
    [Required] public Guid Id { get; set; }
    [Range(1, 5)] public int? Rating { get; set; }
    [StringLength(2000)] public string? Comment { get; set; }
}
