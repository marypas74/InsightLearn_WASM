using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class ApiRequestLogConfiguration : IEntityTypeConfiguration<ApiRequestLog>
{
    public void Configure(EntityTypeBuilder<ApiRequestLog> builder)
    {
        builder.ToTable("ApiRequestLogs");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");
            
        builder.Property(x => x.RequestId)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);
            
        builder.Property(x => x.SessionId)
            .HasMaxLength(100);
            
        builder.Property(x => x.Method)
            .IsRequired()
            .HasMaxLength(10);
            
        builder.Property(x => x.Path)
            .IsRequired()
            .HasMaxLength(2000);
            
        builder.Property(x => x.QueryString)
            .HasMaxLength(2000);
            
        builder.Property(x => x.RequestSize)
            .HasDefaultValue(0);
            
        builder.Property(x => x.ResponseSize)
            .HasDefaultValue(0);
            
        builder.Property(x => x.IpAddress)
            .HasMaxLength(45);
            
        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);
            
        builder.Property(x => x.Referer)
            .HasMaxLength(1000);
            
        builder.Property(x => x.RequestedAt)
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(x => x.CompletedAt)
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(x => x.DatabaseQueries)
            .HasDefaultValue(0);
            
        builder.Property(x => x.DatabaseDurationMs)
            .HasDefaultValue(0);
            
        builder.Property(x => x.MemoryUsageMB)
            .HasColumnType("decimal(10,2)");
            
        builder.Property(x => x.ApiVersion)
            .HasMaxLength(20);
            
        builder.Property(x => x.ClientApp)
            .HasMaxLength(100);
            
        builder.Property(x => x.Feature)
            .HasMaxLength(100);
        
        // Foreign key relationships
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Indexes for performance
        builder.HasIndex(x => x.RequestId)
            .IsUnique()
            .HasDatabaseName("IX_ApiRequestLogs_RequestId");
            
        builder.HasIndex(x => new { x.Path, x.RequestedAt })
            .HasDatabaseName("IX_ApiRequestLogs_Path_RequestedAt");
            
        builder.HasIndex(x => new { x.UserId, x.RequestedAt })
            .HasDatabaseName("IX_ApiRequestLogs_UserId_RequestedAt");
            
        builder.HasIndex(x => x.ResponseStatusCode)
            .HasDatabaseName("IX_ApiRequestLogs_ResponseStatusCode");
            
        builder.HasIndex(x => x.DurationMs)
            .HasDatabaseName("IX_ApiRequestLogs_DurationMs");
            
        builder.HasIndex(x => x.RequestedAt)
            .HasDatabaseName("IX_ApiRequestLogs_RequestedAt");
            
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_ApiRequestLogs_CorrelationId");
            
        builder.HasIndex(x => x.Feature)
            .HasDatabaseName("IX_ApiRequestLogs_Feature");
    }
}