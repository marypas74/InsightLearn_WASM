namespace InsightLearn.WebAssembly.Models.Courses;

public class CourseListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public Guid InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal CurrentPrice { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int EnrollmentCount { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int ViewCount { get; set; }
}

public class CourseDetail : CourseListItem
{
    public string? PreviewVideoUrl { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public string? Requirements { get; set; }
    public string? WhatYouWillLearn { get; set; }
    public string? Language { get; set; }
    public bool HasCertificate { get; set; }
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CourseCreateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public Guid InstructorId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Price { get; set; }
    public decimal DiscountPercentage { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? PreviewVideoUrl { get; set; }
    public string Level { get; set; } = "Beginner";
    public int EstimatedDurationMinutes { get; set; } = 60;
    public string? Requirements { get; set; }
    public string? WhatYouWillLearn { get; set; }
    public string? Language { get; set; } = "English";
    public bool HasCertificate { get; set; } = true;
}

public class CourseUpdateRequest : CourseCreateRequest
{
    public string Status { get; set; } = "Draft";
    public bool IsActive { get; set; } = true;
}
