using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("Certificates");
        
        builder.HasKey(c => c.Id);
        
        // Properties
        builder.Property(c => c.Id)
            .ValueGeneratedNever();
            
        builder.Property(c => c.CertificateNumber)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(c => c.IssuedAt)
            .IsRequired();
            
        builder.Property(c => c.TemplateUrl)
            .HasMaxLength(500);
            
        builder.Property(c => c.PdfUrl)
            .HasMaxLength(500);
            
        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(c => c.IsVerified)
            .IsRequired()
            .HasDefaultValue(true);
        
        // Relationships
        builder.HasOne(c => c.User)
            .WithMany(u => u.Certificates)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(c => c.Course)
            .WithMany()
            .HasForeignKey(c => c.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(c => c.Enrollment)
            .WithMany()
            .HasForeignKey(c => c.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        builder.HasIndex(c => c.CertificateNumber)
            .IsUnique()
            .HasDatabaseName("IX_Certificates_CertificateNumber");
            
        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("IX_Certificates_UserId");
            
        builder.HasIndex(c => c.CourseId)
            .HasDatabaseName("IX_Certificates_CourseId");
            
        builder.HasIndex(c => c.EnrollmentId)
            .HasDatabaseName("IX_Certificates_EnrollmentId");
            
        builder.HasIndex(c => c.IssuedAt)
            .HasDatabaseName("IX_Certificates_IssuedAt");
    }
}