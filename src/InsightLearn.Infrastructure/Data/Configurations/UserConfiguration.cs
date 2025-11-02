using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Configure User-specific properties beyond what Identity provides
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.WalletBalance)
            .HasColumnType("decimal(10,2)")
            .HasDefaultValue(0.00);

        builder.Property(u => u.Bio)
            .HasMaxLength(1000);

        builder.Property(u => u.ProfileImageUrl)
            .HasMaxLength(500);

        builder.Property(u => u.DateJoined)
            .HasDefaultValueSql("GETUTCDATE()");

        // Google OAuth Properties
        builder.Property(u => u.GoogleId)
            .HasMaxLength(50);

        builder.Property(u => u.GooglePictureUrl)
            .HasMaxLength(500);

        builder.Property(u => u.GoogleLocale)
            .HasMaxLength(10);

        builder.Property(u => u.GoogleGivenName)
            .HasMaxLength(100);

        builder.Property(u => u.GoogleFamilyName)
            .HasMaxLength(100);

        // Indexes for better performance
        builder.HasIndex(u => u.DateJoined);
        builder.HasIndex(u => u.IsInstructor);
        builder.HasIndex(u => u.IsVerified);
        builder.HasIndex(u => u.GoogleId)
            .IsUnique()
            .HasFilter("[GoogleId] IS NOT NULL");
        builder.HasIndex(u => u.IsGoogleUser);

        // Navigation properties relationships are configured in their respective entity configurations
        // to avoid circular dependencies and better control cascade behaviors
    }
}