using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.IconUrl)
            .HasMaxLength(200);

        builder.Property(c => c.ColorCode)
            .HasMaxLength(10);

        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.OrderIndex);
        builder.HasIndex(c => c.IsActive);

        // Relationships
        builder.HasMany(c => c.Courses)
            .WithOne(course => course.Category)
            .HasForeignKey(course => course.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deletion of category if courses exist
    }
}