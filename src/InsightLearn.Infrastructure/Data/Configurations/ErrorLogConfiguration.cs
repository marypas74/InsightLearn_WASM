using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> builder)
    {
        builder.ToTable("ErrorLogs");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");
            
        builder.Property(x => x.ExceptionType)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(x => x.ExceptionMessage)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        // Configure large text fields to support unlimited length
        builder.Property(x => x.StackTrace)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.InnerException)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.RequestData)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.AdditionalData)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.RequestPath)
            .HasMaxLength(1000);
            
        builder.Property(x => x.HttpMethod)
            .HasMaxLength(20);
            
        builder.Property(x => x.IpAddress)
            .HasMaxLength(45);
            
        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);
            
        builder.Property(x => x.Severity)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Error");
            
        builder.Property(x => x.Source)
            .HasMaxLength(100);
            
        builder.Property(x => x.LoggedAt)
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(x => x.IsResolved)
            .HasDefaultValue(false);
            
        builder.Property(x => x.ResolutionNotes)
            .HasMaxLength(1000);
        
        // Foreign key relationships removed to prevent cascade conflicts
        // UserId and ResolvedByUserId maintained as simple nullable Guid fields
        
        // Indexes for performance
        builder.HasIndex(x => new { x.ExceptionType, x.LoggedAt })
            .HasDatabaseName("IX_ErrorLogs_ExceptionType_LoggedAt");
            
        builder.HasIndex(x => new { x.Severity, x.LoggedAt })
            .HasDatabaseName("IX_ErrorLogs_Severity_LoggedAt");
            
        builder.HasIndex(x => new { x.UserId, x.LoggedAt })
            .HasDatabaseName("IX_ErrorLogs_UserId_LoggedAt");
            
        builder.HasIndex(x => x.IsResolved)
            .HasDatabaseName("IX_ErrorLogs_IsResolved");
            
        builder.HasIndex(x => new { x.IpAddress, x.LoggedAt })
            .HasDatabaseName("IX_ErrorLogs_IpAddress_LoggedAt");
    }
}