using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class SeoSettingsConfiguration : IEntityTypeConfiguration<SeoSettings>
{
    public void Configure(EntityTypeBuilder<SeoSettings> builder)
    {
        builder.ToTable("SeoSettings");
        
        builder.HasKey(s => s.Id);
        
        // Properties
        builder.Property(s => s.Id)
            .ValueGeneratedNever();
            
        builder.Property(s => s.PageUrl)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(s => s.MetaTitle)
            .IsRequired()
            .HasMaxLength(60);
            
        builder.Property(s => s.MetaDescription)
            .IsRequired()
            .HasMaxLength(160);
            
        builder.Property(s => s.MetaKeywords)
            .HasMaxLength(255);
            
        builder.Property(s => s.CanonicalUrl)
            .HasMaxLength(200);
            
        builder.Property(s => s.OgTitle)
            .HasMaxLength(200);
            
        builder.Property(s => s.OgDescription)
            .HasMaxLength(300);
            
        builder.Property(s => s.OgImage)
            .HasMaxLength(500);
            
        builder.Property(s => s.OgType)
            .HasMaxLength(50)
            .HasDefaultValue("website");
        
        // Relationships
        builder.HasOne(s => s.Course)
            .WithMany()
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
            
        builder.HasOne(s => s.Category)
            .WithMany()
            .HasForeignKey(s => s.CategoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
            
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
        
        // Indexes
        builder.HasIndex(s => s.PageUrl)
            .IsUnique()
            .HasDatabaseName("IX_SeoSettings_PageUrl");
            
        builder.HasIndex(s => s.CourseId)
            .HasDatabaseName("IX_SeoSettings_CourseId");
            
        builder.HasIndex(s => s.CategoryId)
            .HasDatabaseName("IX_SeoSettings_CategoryId");
    }
}

public class GoogleIntegrationConfiguration : IEntityTypeConfiguration<GoogleIntegration>
{
    public void Configure(EntityTypeBuilder<GoogleIntegration> builder)
    {
        builder.ToTable("GoogleIntegrations");
        
        builder.HasKey(g => g.Id);
        
        // Properties
        builder.Property(g => g.Id)
            .ValueGeneratedNever();
            
        builder.Property(g => g.ServiceName)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(g => g.ApiKey)
            .HasMaxLength(500);
            
        builder.Property(g => g.PropertyId)
            .HasMaxLength(100);
            
        builder.Property(g => g.TrackingCode)
            .HasMaxLength(500);
            
        builder.Property(g => g.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);
        
        // Indexes
        builder.HasIndex(g => g.ServiceName)
            .IsUnique()
            .HasDatabaseName("IX_GoogleIntegrations_ServiceName");
    }
}

public class SeoAuditConfiguration : IEntityTypeConfiguration<SeoAudit>
{
    public void Configure(EntityTypeBuilder<SeoAudit> builder)
    {
        builder.ToTable("SeoAudits");
        
        builder.HasKey(s => s.Id);
        
        // Properties
        builder.Property(s => s.Id)
            .ValueGeneratedNever();
            
        builder.Property(s => s.Url)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(s => s.SeoScore)
            .IsRequired();
            
        builder.Property(s => s.CreatedAt)
            .IsRequired();
        
        // Relationships
        builder.HasOne(s => s.Course)
            .WithMany()
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        
        // Indexes
        builder.HasIndex(s => s.Url)
            .HasDatabaseName("IX_SeoAudits_Url");
            
        builder.HasIndex(s => s.CourseId)
            .HasDatabaseName("IX_SeoAudits_CourseId");
            
        builder.HasIndex(s => s.CreatedAt)
            .HasDatabaseName("IX_SeoAudits_CreatedAt");
    }
}

public class SitemapConfiguration : IEntityTypeConfiguration<Sitemap>
{
    public void Configure(EntityTypeBuilder<Sitemap> builder)
    {
        builder.ToTable("Sitemaps");
        
        builder.HasKey(s => s.Id);
        
        // Properties
        builder.Property(s => s.Id)
            .ValueGeneratedNever();
            
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(s => s.Content)
            .IsRequired();
            
        builder.Property(s => s.LastGenerated)
            .IsRequired();
            
        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        // Indexes
        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("IX_Sitemaps_Name");
            
        builder.HasIndex(s => s.LastGenerated)
            .HasDatabaseName("IX_Sitemaps_LastGenerated");
    }
}