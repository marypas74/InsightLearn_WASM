using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("UserSessions");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");
            
        builder.Property(x => x.SessionId)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(x => x.StartedAt)
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(x => x.LastActivityAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.EndReason)
            .HasMaxLength(100);
            
        builder.Property(x => x.IpAddress)
            .IsRequired()
            .HasMaxLength(45);
            
        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);
            
        builder.Property(x => x.DeviceType)
            .HasMaxLength(50);
            
        builder.Property(x => x.Platform)
            .HasMaxLength(50);
            
        builder.Property(x => x.Browser)
            .HasMaxLength(100);
            
        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);
            
        builder.Property(x => x.JwtTokenId)
            .HasMaxLength(100);
            
        builder.Property(x => x.GeolocationData)
            .HasMaxLength(1000);
            
        builder.Property(x => x.ActivityCount)
            .HasDefaultValue(0);
            
        builder.Property(x => x.DataTransferred)
            .HasDefaultValue(0);
            
        builder.Property(x => x.LastPageVisited)
            .HasMaxLength(500);
            
        builder.Property(x => x.TimeZone)
            .HasMaxLength(100);
        
        // Foreign key relationships
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // LoginAttempt FK relationship removed to prevent cascade conflicts
        // LoginAttemptId maintained as simple nullable Guid field
        
        // Indexes for performance
        builder.HasIndex(x => x.SessionId)
            .IsUnique()
            .HasDatabaseName("IX_UserSessions_SessionId");
            
        builder.HasIndex(x => new { x.UserId, x.StartedAt })
            .HasDatabaseName("IX_UserSessions_UserId_StartedAt");
            
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_UserSessions_IsActive");
            
        builder.HasIndex(x => x.LastActivityAt)
            .HasDatabaseName("IX_UserSessions_LastActivityAt");
            
        builder.HasIndex(x => x.IpAddress)
            .HasDatabaseName("IX_UserSessions_IpAddress");
            
        builder.HasIndex(x => x.DeviceType)
            .HasDatabaseName("IX_UserSessions_DeviceType");
    }
}