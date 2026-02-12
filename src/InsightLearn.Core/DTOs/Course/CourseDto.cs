namespace InsightLearn.Core.DTOs.Course;

/// <summary>
/// Full course data transfer object
/// </summary>
public class CourseDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// URL-safe encoded ID for public URLs (v2.3.113-dev)
    /// </summary>
    public string EncodedId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }

    public Guid InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public string? InstructorProfilePictureUrl { get; set; }

    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? CategorySlug { get; set; }

    public decimal Price { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal CurrentPrice { get; set; }

    public string? ThumbnailUrl { get; set; }
    public string? PreviewVideoUrl { get; set; }

    public string Level { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    public int EstimatedDurationMinutes { get; set; }
    public string? Requirements { get; set; }
    public string? WhatYouWillLearn { get; set; }
    public string Language { get; set; } = "English";
    public bool HasCertificate { get; set; }

    public string Slug { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int EnrollmentCount { get; set; }

    public List<SectionDto>? Sections { get; set; }
    public bool IsFree => Price == 0;

    // v2.3.34-dev FIX: Computed properties for course detail page display
    public int SectionCount => Sections?.Count ?? 0;
    public int LessonCount => Sections?.Sum(s => s.Lessons?.Count ?? 0) ?? 0;

    // Additional property for instructor image (used in Detail.razor line 86)
    public string? InstructorImageUrl => InstructorProfilePictureUrl;
}
