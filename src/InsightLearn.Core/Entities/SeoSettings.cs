using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class SeoSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [StringLength(200)]
    public string PageUrl { get; set; } = string.Empty;
    
    [Required]
    [StringLength(60)]
    public string MetaTitle { get; set; } = string.Empty;
    
    [Required]
    [StringLength(160)]
    public string MetaDescription { get; set; } = string.Empty;
    
    [StringLength(255)]
    public string MetaKeywords { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string CanonicalUrl { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string OgTitle { get; set; } = string.Empty;
    
    [StringLength(300)]
    public string OgDescription { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string OgImage { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string OgType { get; set; } = "website";
    
    [StringLength(200)]
    public string TwitterTitle { get; set; } = string.Empty;
    
    [StringLength(300)]
    public string TwitterDescription { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string TwitterImage { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string TwitterCard { get; set; } = "summary_large_image";
    
    public string? StructuredData { get; set; }
    
    public bool NoIndex { get; set; } = false;
    
    public bool NoFollow { get; set; } = false;
    
    public int Priority { get; set; } = 5;
    
    public string ChangeFrequency { get; set; } = "weekly";
    
    public string RobotsDirective { get; set; } = "index,follow";
    
    public bool IsActive { get; set; } = true;
    
    public string? CustomCode { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid? CourseId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? UserId { get; set; }
    
    // Navigation properties
    public virtual Course? Course { get; set; }
    public virtual Category? Category { get; set; }
    public virtual User? User { get; set; }
}

public class GoogleIntegration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [StringLength(100)]
    public string ServiceName { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string ApiKey { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string PropertyId { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string TrackingCode { get; set; } = string.Empty;
    
    public string? Configuration { get; set; }
    
    public bool IsEnabled { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum GoogleService
{
    Analytics,
    SearchConsole,
    TagManager,
    AdSense,
    Ads,
    PageSpeedInsights,
    Lighthouse,
    ReCaptcha
}

public class SeoAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [StringLength(500)]
    public string Url { get; set; } = string.Empty;
    
    public string PageUrl { get; set; } = string.Empty;
    
    public int SeoScore { get; set; }
    
    public string? Issues { get; set; }
    
    public string? Suggestions { get; set; }
    
    public string Recommendations { get; set; } = string.Empty;
    
    public DateTime AuditDate { get; set; }
    
    public string? TechnicalData { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid? CourseId { get; set; }
    
    // Navigation properties
    public virtual Course? Course { get; set; }
}

public class Sitemap
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public string Content { get; set; } = string.Empty;
    
    public DateTime LastGenerated { get; set; } = DateTime.UtcNow;
    
    public int UrlCount { get; set; }
    
    public bool IsActive { get; set; } = true;
}