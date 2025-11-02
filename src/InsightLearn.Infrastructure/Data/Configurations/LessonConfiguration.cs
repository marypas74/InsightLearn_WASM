using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.Description)
            .HasMaxLength(1000);

        builder.Property(l => l.VideoUrl)
            .HasMaxLength(500);

        builder.Property(l => l.VideoThumbnailUrl)
            .HasMaxLength(500);

        builder.Property(l => l.AttachmentUrl)
            .HasMaxLength(500);

        builder.Property(l => l.AttachmentName)
            .HasMaxLength(200);

        builder.Property(l => l.VideoQuality)
            .HasMaxLength(10);

        builder.Property(l => l.VideoFormat)
            .HasMaxLength(10);

        builder.Property(l => l.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(l => l.OrderIndex);
        builder.HasIndex(l => new { l.SectionId, l.OrderIndex });
        builder.HasIndex(l => l.IsActive);
        builder.HasIndex(l => l.Type);

        // Relationships
        builder.HasOne(l => l.Section)
            .WithMany(s => s.Lessons)
            .HasForeignKey(l => l.SectionId)
            .OnDelete(DeleteBehavior.Cascade); // Lessons should be deleted when section is deleted

        builder.HasMany(l => l.LessonProgress)
            .WithOne(lp => lp.Lesson)
            .HasForeignKey(lp => lp.LessonId)
            .OnDelete(DeleteBehavior.Cascade); // Progress should be deleted when lesson is deleted

        builder.HasMany(l => l.Notes)
            .WithOne(n => n.Lesson)
            .HasForeignKey(n => n.LessonId)
            .OnDelete(DeleteBehavior.Cascade); // Notes should be deleted when lesson is deleted
    }
}