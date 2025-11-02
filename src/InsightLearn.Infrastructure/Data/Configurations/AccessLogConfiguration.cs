using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class AccessLogConfiguration : IEntityTypeConfiguration<AccessLog>
{
    public void Configure(EntityTypeBuilder<AccessLog> builder)
    {
        builder.ToTable("AccessLogs");
        
        builder.HasKey(al => al.Id);
        
        builder.Property(al => al.Id)
            .ValueGeneratedOnAdd();
            
        builder.Property(al => al.IpAddress)
            .IsRequired()
            .HasMaxLength(15);
            
        builder.Property(al => al.RequestPath)
            .IsRequired()
            .HasMaxLength(2000);
            
        builder.Property(al => al.HttpMethod)
            .IsRequired()
            .HasMaxLength(20);
            
        builder.Property(al => al.UserAgent)
            .HasMaxLength(500);
            
        builder.Property(al => al.Referer)
            .HasMaxLength(500);
            
        builder.Property(al => al.SessionId)
            .HasMaxLength(50);
            
        builder.Property(al => al.AdditionalData)
            .HasMaxLength(1000);
            
        builder.Property(al => al.AccessedAt)
            .IsRequired();
            
        // Indexes for performance
        builder.HasIndex(al => al.UserId);
        builder.HasIndex(al => al.AccessedAt);
        builder.HasIndex(al => al.IpAddress);
        builder.HasIndex(al => new { al.UserId, al.AccessedAt });
        
        // Foreign key relationship
        builder.HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}