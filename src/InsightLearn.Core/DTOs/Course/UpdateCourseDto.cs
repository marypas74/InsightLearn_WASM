using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Course;

/// <summary>
/// DTO for updating an existing course
/// </summary>
public class UpdateCourseDto
{
    [Required(ErrorMessage = "Course ID is required")]
    public Guid Id { get; set; }

    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string? Title { get; set; }

    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Short description cannot exceed 500 characters")]
    public string? ShortDescription { get; set; }

    public Guid? CategoryId { get; set; }

    [Range(0, 10000, ErrorMessage = "Price must be between 0 and 10000")]
    public decimal? Price { get; set; }

    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100")]
    public decimal? DiscountPercentage { get; set; }

    [Url(ErrorMessage = "Invalid thumbnail URL")]
    public string? ThumbnailUrl { get; set; }

    [Url(ErrorMessage = "Invalid preview video URL")]
    public string? PreviewVideoUrl { get; set; }

    public string? Level { get; set; }

    public string? Status { get; set; } // Draft, UnderReview, Published, Archived

    public int? EstimatedDurationMinutes { get; set; }

    [StringLength(500)]
    public string? Requirements { get; set; }

    [StringLength(500)]
    public string? WhatYouWillLearn { get; set; }

    [StringLength(100)]
    public string? Language { get; set; }

    public bool? HasCertificate { get; set; }

    public bool? IsActive { get; set; }

    [StringLength(160)]
    public string? MetaDescription { get; set; }
}
