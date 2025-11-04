using System.ComponentModel.DataAnnotations;
using InsightLearn.Core.Entities;

namespace InsightLearn.Shared.DTOs;

public class CourseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public Guid InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public string InstructorImageUrl { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal CurrentPrice { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? PreviewVideoUrl { get; set; }
    public CourseLevel Level { get; set; }
    public CourseStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public string? Requirements { get; set; }
    public string? WhatYouWillLearn { get; set; }
    public string? Language { get; set; }
    public bool HasCertificate { get; set; }
    public string Slug { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public int EnrollmentCount { get; set; }
    public int ViewCount { get; set; }
    public bool IsFree { get; set; }
    public bool IsEnrolledByCurrentUser { get; set; }
    public List<SectionDto> Sections { get; set; } = new();
    public List<ReviewDto> Reviews { get; set; } = new();
    public int SectionCount { get; set; }
    public int LessonCount { get; set; }
}

public class CreateCourseDto
{
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(5000, MinimumLength = 50)]
    public string Description { get; set; } = string.Empty;

    [StringLength(300)]
    public string? ShortDescription { get; set; }

    [Required]
    public Guid CategoryId { get; set; }
    
    [Required]
    public Guid InstructorId { get; set; }

    [Range(0, 9999.99)]
    public decimal Price { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;

    public string? ThumbnailUrl { get; set; }

    public string? PreviewVideoUrl { get; set; }

    [Required]
    public CourseLevel Level { get; set; }

    [Range(1, 10000)]
    public int EstimatedDurationMinutes { get; set; } = 60;

    [StringLength(500)]
    public string? Requirements { get; set; }

    [StringLength(500)]
    public string? WhatYouWillLearn { get; set; }

    [StringLength(50)]
    public string? Language { get; set; } = "English";

    public bool HasCertificate { get; set; } = true;
}

public class UpdateCourseDto
{
    [Required]
    [StringLength(200, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(5000, MinimumLength = 50)]
    public string Description { get; set; } = string.Empty;

    [StringLength(300)]
    public string? ShortDescription { get; set; }

    [Required]
    public Guid CategoryId { get; set; }
    
    public Guid? InstructorId { get; set; }

    [Range(0, 9999.99)]
    public decimal Price { get; set; }

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; } = 0;

    public string? ThumbnailUrl { get; set; }

    public string? PreviewVideoUrl { get; set; }

    [Required]
    public CourseLevel Level { get; set; }
    
    public CourseStatus Status { get; set; }

    [Range(1, 10000)]
    public int EstimatedDurationMinutes { get; set; } = 60;

    [StringLength(500)]
    public string? Requirements { get; set; }

    [StringLength(500)]
    public string? WhatYouWillLearn { get; set; }

    [StringLength(50)]
    public string? Language { get; set; } = "English";

    public bool HasCertificate { get; set; } = true;
}

public class SectionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public List<LessonDto> Lessons { get; set; } = new();
    public int TotalLessons { get; set; }
    public int TotalDurationMinutes { get; set; }
}

public class CreateSectionDto
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public int OrderIndex { get; set; }
}

public class LessonDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LessonType Type { get; set; }
    public int OrderIndex { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsFree { get; set; }
    public string? VideoUrl { get; set; }
    public string? VideoThumbnailUrl { get; set; }
    public string? ContentText { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public bool IsCompleted { get; set; }
    public double ProgressPercentage { get; set; }
}

public class CreateLessonDto
{
    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    public LessonType Type { get; set; }

    public int OrderIndex { get; set; }

    [Range(0, 600)]
    public int DurationMinutes { get; set; }

    public bool IsFree { get; set; }

    public string? VideoUrl { get; set; }
    public string? ContentText { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
}

public class CourseSearchDto
{
    public string? Query { get; set; }
    public Guid? CategoryId { get; set; }
    public CourseLevel? Level { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? HasCertificate { get; set; }
    public string? Language { get; set; }
    public double? MinRating { get; set; }
    public string? SortBy { get; set; } = "Relevance"; // Relevance, Newest, Price, Rating, Popular
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class CourseSearchResultDto
{
    public List<CourseDto> Courses { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageCount { get; set; }
    public int CurrentPage { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? ColorCode { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public List<CategoryDto> SubCategories { get; set; } = new();
    public int CourseCount { get; set; }
}

public class ReviewDto
{
    public Guid Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CourseStatsDto
{
    public int TotalCourses { get; set; }
    public int PublishedCourses { get; set; }
    public int DraftCourses { get; set; }
    public int UnderReviewCourses { get; set; }
    public int TotalEnrollments { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class EnrollmentDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double Progress { get; set; }
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime LastAccessedAt { get; set; }
}

public class CourseAnalyticsDto
{
    public Guid CourseId { get; set; }
    public string CourseTitle { get; set; } = string.Empty;
    public int TotalEnrollments { get; set; }
    public int ActiveStudents { get; set; }
    public int CompletedEnrollments { get; set; }
    public int CompletedStudents { get; set; }
    public double AverageProgress { get; set; }
    public decimal TotalRevenue { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public double CompletionRate { get; set; }
    public int TotalLessons { get; set; }
    public int TotalSections { get; set; }
    public List<EnrollmentTrendDto> EnrollmentTrends { get; set; } = new();
}

public class EnrollmentTrendDto
{
    public DateTime Date { get; set; }
    public int Enrollments { get; set; }
}