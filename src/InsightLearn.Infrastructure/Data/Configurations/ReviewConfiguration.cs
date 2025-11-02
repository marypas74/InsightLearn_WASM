using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating)
            .IsRequired()
            .HasColumnType("int");

        builder.Property(r => r.Comment)
            .HasMaxLength(1000);

        builder.Property(r => r.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(r => r.UpdatedAt)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(r => r.Rating);
        builder.HasIndex(r => r.CreatedAt);
        builder.HasIndex(r => new { r.CourseId, r.Rating });
        builder.HasIndex(r => new { r.UserId, r.CourseId }).IsUnique();

        // Relationships with controlled cascade behavior
        builder.HasOne(r => r.User)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete of user

        builder.HasOne(r => r.Course)
            .WithMany(c => c.Reviews)
            .HasForeignKey(r => r.CourseId)
            .OnDelete(DeleteBehavior.Cascade); // Allow cascade delete when course is deleted

        // Review votes relationship
        builder.HasMany(r => r.Votes)
            .WithOne(rv => rv.Review)
            .HasForeignKey(rv => rv.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}