using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.DTOs.Course;

/// <summary>
/// DTO for creating a new course
/// </summary>
public class CreateCourseDto
{
    [Required(ErrorMessage = "Course title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(5000, MinimumLength = 50, ErrorMessage = "Description must be between 50 and 5000 characters")]
    public string Description { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Short description cannot exceed 500 characters")]
    public string? ShortDescription { get; set; }

    [Required(ErrorMessage = "Instructor ID is required")]
    public Guid InstructorId { get; set; }

    [Required(ErrorMessage = "Category is required")]
    public Guid CategoryId { get; set; }

    [Range(0, 10000, ErrorMessage = "Price must be between 0 and 10000")]
    public decimal Price { get; set; }

    [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100")]
    public decimal DiscountPercentage { get; set; } = 0;

    [Url(ErrorMessage = "Invalid thumbnail URL")]
    public string? ThumbnailUrl { get; set; }

    [Url(ErrorMessage = "Invalid preview video URL")]
    public string? PreviewVideoUrl { get; set; }

    [Required(ErrorMessage = "Level is required")]
    public string Level { get; set; } = "Beginner"; // Beginner, Intermediate, Advanced, AllLevels

    [Range(1, 10000, ErrorMessage = "Duration must be at least 1 minute")]
    public int EstimatedDurationMinutes { get; set; }

    [StringLength(500, ErrorMessage = "Requirements cannot exceed 500 characters")]
    public string? Requirements { get; set; }

    [StringLength(500, ErrorMessage = "What you will learn cannot exceed 500 characters")]
    public string? WhatYouWillLearn { get; set; }

    [StringLength(100, ErrorMessage = "Language cannot exceed 100 characters")]
    public string Language { get; set; } = "English";

    public bool HasCertificate { get; set; } = true;

    [StringLength(160, ErrorMessage = "Meta description cannot exceed 160 characters")]
    public string? MetaDescription { get; set; }
}
