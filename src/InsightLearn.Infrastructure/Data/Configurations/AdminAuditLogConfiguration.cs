using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> builder)
    {
        builder.ToTable("AdminAuditLogs");
        
        builder.HasKey(aal => aal.Id);
        
        builder.Property(aal => aal.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(aal => aal.AdminUserId)
            .IsRequired();
            
        builder.Property(aal => aal.Action)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(aal => aal.EntityType)
            .HasMaxLength(100);
            
        builder.Property(aal => aal.Description)
            .HasMaxLength(1000);
            
        builder.Property(aal => aal.IpAddress)
            .HasMaxLength(15);
            
        builder.Property(aal => aal.UserAgent)
            .HasMaxLength(500);
            
        builder.Property(aal => aal.PerformedAt)
            .IsRequired();
            
        builder.Property(aal => aal.Severity)
            .IsRequired()
            .HasMaxLength(20);
            
        // Indexes for performance
        builder.HasIndex(aal => aal.AdminUserId);
        builder.HasIndex(aal => aal.PerformedAt);
        builder.HasIndex(aal => aal.Action);
        builder.HasIndex(aal => aal.EntityType);
        builder.HasIndex(aal => new { aal.AdminUserId, aal.PerformedAt });
        builder.HasIndex(aal => new { aal.EntityType, aal.EntityId });
        
        // Foreign key relationship
        builder.HasOne(aal => aal.AdminUser)
            .WithMany()
            .HasForeignKey(aal => aal.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}