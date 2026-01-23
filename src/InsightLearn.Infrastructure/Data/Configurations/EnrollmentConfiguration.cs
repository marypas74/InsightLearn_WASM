using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.AmountPaid)
            .HasColumnType("decimal(10,2)");
            
        builder.HasIndex(e => new { e.UserId, e.CourseId })
            .IsUnique();
            
        builder.HasIndex(e => e.EnrolledAt);
        builder.HasIndex(e => e.Status);

        // Fix v2.3.63: Ignore computed/non-mapped properties that don't have columns in DB
        builder.Ignore(e => e.Progress);
        builder.Ignore(e => e.ProgressPercentage);
        builder.Ignore(e => e.IsCompleted);
        builder.Ignore(e => e.TotalWatchedTime);
        
        // Relationships
        builder.HasOne(e => e.User)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(e => e.CurrentLesson)
            .WithMany()
            .HasForeignKey(e => e.CurrentLessonId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasOne(e => e.Certificate)
            .WithOne(cert => cert.Enrollment)
            .HasForeignKey<Certificate>(cert => cert.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}