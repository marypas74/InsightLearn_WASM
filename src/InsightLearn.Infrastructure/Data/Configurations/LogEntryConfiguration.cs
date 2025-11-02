using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class LogEntryConfiguration : IEntityTypeConfiguration<LogEntry>
{
    public void Configure(EntityTypeBuilder<LogEntry> builder)
    {
        builder.ToTable("LogEntries");
        
        builder.HasKey(le => le.Id);
        
        builder.Property(le => le.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(le => le.Level)
            .IsRequired()
            .HasMaxLength(20);
            
        builder.Property(le => le.Message)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        // Configure large text fields to support unlimited length
        builder.Property(le => le.Exception)
            .HasColumnType("nvarchar(max)");

        builder.Property(le => le.Properties)
            .HasColumnType("nvarchar(max)");

        builder.Property(le => le.Logger)
            .HasMaxLength(200);
            
        builder.Property(le => le.Application)
            .HasMaxLength(100);
            
        builder.Property(le => le.MachineName)
            .HasMaxLength(100);
            
        builder.Property(le => le.RequestPath)
            .HasMaxLength(500);
            
        builder.Property(le => le.HttpMethod)
            .HasMaxLength(20);
            
        builder.Property(le => le.IpAddress)
            .HasMaxLength(15);
            
        builder.Property(le => le.UserAgent)
            .HasMaxLength(500);
            
        builder.Property(le => le.UserName)
            .HasMaxLength(100);
            
        builder.Property(le => le.SessionId)
            .HasMaxLength(50);
            
        builder.Property(le => le.CorrelationId)
            .HasMaxLength(100);
            
        builder.Property(le => le.Timestamp)
            .IsRequired();
            
        builder.Property(le => le.ThreadId)
            .HasMaxLength(100);
            
        builder.Property(le => le.ProcessId)
            .HasMaxLength(50);
            
        // Indexes for performance
        builder.HasIndex(le => le.Level);
        builder.HasIndex(le => le.Timestamp);
        builder.HasIndex(le => le.UserId);
        builder.HasIndex(le => le.Application);
        builder.HasIndex(le => le.Logger);
        builder.HasIndex(le => new { le.Level, le.Timestamp });
        builder.HasIndex(le => new { le.Application, le.Timestamp });
        builder.HasIndex(le => new { le.UserId, le.Timestamp });
        
        // Foreign key relationship
        builder.HasOne(le => le.User)
            .WithMany()
            .HasForeignKey(le => le.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}