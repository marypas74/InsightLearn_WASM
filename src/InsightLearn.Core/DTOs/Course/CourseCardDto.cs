namespace InsightLearn.Core.DTOs.Course;

/// <summary>
/// Minimal DTO for course cards in listing pages
/// </summary>
public class CourseCardDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal DiscountPercentage { get; set; }

    public string? ThumbnailUrl { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int EnrollmentCount { get; set; }
    public int EstimatedDurationMinutes { get; set; }

    public bool IsFree => Price == 0;
    public bool HasDiscount => DiscountPercentage > 0;
}
