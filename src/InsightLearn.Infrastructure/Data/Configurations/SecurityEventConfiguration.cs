using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("SecurityEvents");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");
            
        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(x => x.Severity)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Medium");
            
        builder.Property(x => x.Email)
            .HasMaxLength(256);
            
        builder.Property(x => x.IpAddress)
            .IsRequired()
            .HasMaxLength(45);
            
        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);
            
        builder.Property(x => x.EventDetails)
            .IsRequired();
            
        builder.Property(x => x.RiskScore)
            .HasColumnType("decimal(3,2)")
            .HasDefaultValue(0.00m);
            
        builder.Property(x => x.IsResolved)
            .HasDefaultValue(false);
            
        builder.Property(x => x.ResolutionNotes)
            .HasMaxLength(1000);
            
        builder.Property(x => x.DetectedAt)
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(x => x.GeolocationData)
            .HasMaxLength(1000);
            
        builder.Property(x => x.RelatedSessionId)
            .HasMaxLength(100);
            
        builder.Property(x => x.AutoBlocked)
            .HasDefaultValue(false);
            
        builder.Property(x => x.NotificationSent)
            .HasDefaultValue(false);
            
        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);
        
        // Foreign key relationships removed to prevent cascade conflicts
        // UserId, ResolvedByUserId, and RelatedLoginAttemptId maintained as simple nullable Guid fields
        
        // Indexes for performance
        builder.HasIndex(x => new { x.EventType, x.DetectedAt })
            .HasDatabaseName("IX_SecurityEvents_EventType_DetectedAt");
            
        builder.HasIndex(x => new { x.Severity, x.DetectedAt })
            .HasDatabaseName("IX_SecurityEvents_Severity_DetectedAt");
            
        builder.HasIndex(x => new { x.UserId, x.DetectedAt })
            .HasDatabaseName("IX_SecurityEvents_UserId_DetectedAt");
            
        builder.HasIndex(x => new { x.IpAddress, x.DetectedAt })
            .HasDatabaseName("IX_SecurityEvents_IpAddress_DetectedAt");
            
        builder.HasIndex(x => x.IsResolved)
            .HasDatabaseName("IX_SecurityEvents_IsResolved");
            
        builder.HasIndex(x => x.RiskScore)
            .HasDatabaseName("IX_SecurityEvents_RiskScore");
            
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_SecurityEvents_CorrelationId");
    }
}