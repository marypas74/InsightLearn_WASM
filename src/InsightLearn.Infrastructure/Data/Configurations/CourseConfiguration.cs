using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);
        
        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(c => c.Description)
            .IsRequired();
            
        builder.Property(c => c.Price)
            .HasColumnType("decimal(10,2)");
            
        builder.Property(c => c.DiscountPercentage)
            .HasColumnType("decimal(3,2)");
            
        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.HasIndex(c => c.Slug)
            .IsUnique();
            
        builder.HasIndex(c => c.Title);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => new { c.CategoryId, c.Status, c.IsActive });
        
        // Relationships
        builder.HasOne(c => c.Instructor)
            .WithMany(u => u.CreatedCourses)
            .HasForeignKey(c => c.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(c => c.Category)
            .WithMany(cat => cat.Courses)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasMany(c => c.Sections)
            .WithOne(s => s.Course)
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(c => c.Enrollments)
            .WithOne(e => e.Course)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasMany(c => c.Reviews)
            .WithOne(r => r.Course)
            .HasForeignKey(r => r.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(c => c.Discussions)
            .WithOne(d => d.Course)
            .HasForeignKey(d => d.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}