using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        builder.ToTable("LoginAttempts");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWID()");
            
        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);
            
        builder.Property(x => x.LoginMethod)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("EmailPassword");
            
        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);
            
        builder.Property(x => x.IpAddress)
            .IsRequired()
            .HasMaxLength(45);
            
        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);
            
        builder.Property(x => x.DeviceFingerprint)
            .HasMaxLength(500);
            
        builder.Property(x => x.SessionId)
            .HasMaxLength(100);
            
        builder.Property(x => x.AttemptedAt)
            .HasDefaultValueSql("GETUTCDATE()");
            
        builder.Property(x => x.GeolocationData)
            .HasMaxLength(1000);
            
        builder.Property(x => x.RiskScore)
            .HasColumnType("decimal(3,2)");
            
        builder.Property(x => x.BrowserInfo)
            .HasMaxLength(500);
            
        builder.Property(x => x.AuthProvider)
            .HasMaxLength(100);
            
        builder.Property(x => x.ProviderUserId)
            .HasMaxLength(200);
            
        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);
        
        // Foreign key relationships removed to prevent cascade conflicts
        // UserId maintained as simple nullable Guid field
        
        // Indexes for performance
        builder.HasIndex(x => new { x.Email, x.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_Email_AttemptedAt");
            
        builder.HasIndex(x => new { x.UserId, x.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_UserId_AttemptedAt");
            
        builder.HasIndex(x => new { x.IpAddress, x.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_IpAddress_AttemptedAt");
            
        builder.HasIndex(x => new { x.IsSuccess, x.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_IsSuccess_AttemptedAt");
            
        builder.HasIndex(x => x.LoginMethod)
            .HasDatabaseName("IX_LoginAttempts_LoginMethod");
            
        builder.HasIndex(x => x.RiskScore)
            .HasDatabaseName("IX_LoginAttempts_RiskScore");
            
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_LoginAttempts_CorrelationId");
    }
}