using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

public class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Description { get; set; } = string.Empty;
    
    public string? ShortDescription { get; set; }
    
    [Required]
    public Guid InstructorId { get; set; }
    
    [Required]
    public Guid CategoryId { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }
    
    [Column(TypeName = "decimal(3,2)")]
    public decimal DiscountPercentage { get; set; }
    
    public string? ThumbnailUrl { get; set; }
    
    public string? PreviewVideoUrl { get; set; }
    
    public CourseLevel Level { get; set; }
    
    public CourseStatus Status { get; set; } = CourseStatus.Draft;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public DateTime? PublishedAt { get; set; }
    
    public int EstimatedDurationMinutes { get; set; }
    
    [StringLength(500)]
    public string? Requirements { get; set; }
    
    [StringLength(500)]
    public string? WhatYouWillLearn { get; set; }
    
    [StringLength(100)]
    public string? Language { get; set; } = "English";
    
    public bool HasCertificate { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
    
    // SEO and Analytics
    [StringLength(160)]
    public string? MetaDescription { get; set; }
    
    [StringLength(200)]
    public string Slug { get; set; } = string.Empty;
    
    public int ViewCount { get; set; }
    
    public double AverageRating { get; set; }
    
    public int ReviewCount { get; set; }
    
    public int EnrollmentCount { get; set; }
    
    // Navigation properties
    public virtual User Instructor { get; set; } = null!;
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<Section> Sections { get; set; } = new List<Section>();
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
    public virtual ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();
    
    public decimal CurrentPrice => Price * (1 - DiscountPercentage / 100);
    
    public bool IsFree => Price == 0;
    
    public TimeSpan EstimatedDuration => TimeSpan.FromMinutes(EstimatedDurationMinutes);
}

public enum CourseLevel
{
    Beginner,
    Intermediate,
    Advanced,
    AllLevels
}

public enum CourseStatus
{
    Draft,
    UnderReview,
    Published,
    Archived
}