using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> builder)
    {
        builder.HasKey(lp => lp.Id);

        builder.Property(lp => lp.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(lp => lp.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(lp => new { lp.UserId, lp.LessonId }).IsUnique();
        builder.HasIndex(lp => lp.IsCompleted);
        builder.HasIndex(lp => lp.CompletedAt);

        // Relationships
        builder.HasOne(lp => lp.User)
            .WithMany(u => u.LessonProgress)
            .HasForeignKey(lp => lp.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Progress should be deleted when user is deleted

        builder.HasOne(lp => lp.Lesson)
            .WithMany(l => l.LessonProgress)
            .HasForeignKey(lp => lp.LessonId)
            .OnDelete(DeleteBehavior.Cascade); // Progress should be deleted when lesson is deleted
    }
}