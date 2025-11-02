using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for PaymentMethod entity
/// Ensures proper indexing and security constraints for PCI DSS compliance
/// </summary>
public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("PaymentMethods");

        // Primary key
        builder.HasKey(pm => pm.Id);

        // Required fields
        builder.Property(pm => pm.UserId)
            .IsRequired();

        builder.Property(pm => pm.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        // Encrypted fields - store as large text fields
        builder.Property(pm => pm.EncryptedCardNumber)
            .HasMaxLength(2000); // Large enough for encrypted data

        builder.Property(pm => pm.EncryptedCardholderName)
            .HasMaxLength(2000);

        builder.Property(pm => pm.EncryptedCvv)
            .HasMaxLength(1000);

        builder.Property(pm => pm.EncryptedBankAccountNumber)
            .HasMaxLength(2000);

        // Display fields
        builder.Property(pm => pm.DisplayName)
            .HasMaxLength(100);

        builder.Property(pm => pm.LastFourDigits)
            .HasMaxLength(4);

        builder.Property(pm => pm.CardType)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Billing address fields
        builder.Property(pm => pm.BillingAddress)
            .HasMaxLength(500);

        builder.Property(pm => pm.BillingCity)
            .HasMaxLength(100);

        builder.Property(pm => pm.BillingState)
            .HasMaxLength(100);

        builder.Property(pm => pm.BillingPostalCode)
            .HasMaxLength(20);

        builder.Property(pm => pm.BillingCountry)
            .HasMaxLength(2);

        // PayPal fields
        builder.Property(pm => pm.PayPalEmail)
            .HasMaxLength(255);

        // Bank transfer fields
        builder.Property(pm => pm.BankRoutingNumber)
            .HasMaxLength(20);

        builder.Property(pm => pm.BankName)
            .HasMaxLength(200);

        // External integration
        builder.Property(pm => pm.ExternalPaymentMethodId)
            .HasMaxLength(255);

        builder.Property(pm => pm.VerificationNotes)
            .HasMaxLength(500);

        // Security fields
        builder.Property(pm => pm.SecurityFingerprint)
            .HasMaxLength(128);

        builder.Property(pm => pm.EncryptionKeyId)
            .HasMaxLength(50);

        // Timestamps
        builder.Property(pm => pm.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(pm => pm.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(pm => pm.User)
            .WithMany() // No navigation property on User for security
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(pm => pm.Payments)
            .WithOne()
            .HasForeignKey("PaymentMethodId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(pm => pm.AuditLogs)
            .WithOne(al => al.PaymentMethod)
            .HasForeignKey(al => al.PaymentMethodId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance and security
        builder.HasIndex(pm => pm.UserId)
            .HasDatabaseName("IX_PaymentMethods_UserId");

        builder.HasIndex(pm => new { pm.UserId, pm.IsDefault })
            .HasDatabaseName("IX_PaymentMethods_UserId_IsDefault");

        builder.HasIndex(pm => pm.SecurityFingerprint)
            .HasDatabaseName("IX_PaymentMethods_SecurityFingerprint");

        builder.HasIndex(pm => pm.Type)
            .HasDatabaseName("IX_PaymentMethods_Type");

        builder.HasIndex(pm => pm.IsActive)
            .HasDatabaseName("IX_PaymentMethods_IsActive");

        builder.HasIndex(pm => pm.CreatedAt)
            .HasDatabaseName("IX_PaymentMethods_CreatedAt");

        // Composite index for common queries
        builder.HasIndex(pm => new { pm.UserId, pm.IsActive, pm.Type })
            .HasDatabaseName("IX_PaymentMethods_UserId_IsActive_Type");

        // Security constraint: Only one default payment method per user
        builder.HasIndex(pm => new { pm.UserId, pm.IsDefault })
            .HasDatabaseName("IX_PaymentMethods_UserId_IsDefault_Unique")
            .HasFilter("[IsDefault] = 1 AND [IsActive] = 1")
            .IsUnique();
    }
}

/// <summary>
/// Entity Framework configuration for PaymentMethodAuditLog entity
/// Provides comprehensive audit trail for compliance and security monitoring
/// </summary>
public class PaymentMethodAuditLogConfiguration : IEntityTypeConfiguration<PaymentMethodAuditLog>
{
    public void Configure(EntityTypeBuilder<PaymentMethodAuditLog> builder)
    {
        builder.ToTable("PaymentMethodAuditLogs");

        // Primary key
        builder.HasKey(al => al.Id);

        // Required fields
        builder.Property(al => al.PaymentMethodId)
            .IsRequired();

        builder.Property(al => al.UserId)
            .IsRequired();

        builder.Property(al => al.Action)
            .IsRequired()
            .HasMaxLength(50);

        // Optional fields
        builder.Property(al => al.Description)
            .HasMaxLength(1000);

        builder.Property(al => al.IpAddress)
            .HasMaxLength(45); // IPv6 compatible

        builder.Property(al => al.UserAgent)
            .HasMaxLength(500);

        builder.Property(al => al.Metadata)
            .HasColumnType("nvarchar(max)"); // JSON metadata

        // Timestamp
        builder.Property(al => al.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(al => al.PaymentMethod)
            .WithMany(pm => pm.AuditLogs)
            .HasForeignKey(al => al.PaymentMethodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for audit queries
        builder.HasIndex(al => al.PaymentMethodId)
            .HasDatabaseName("IX_PaymentMethodAuditLogs_PaymentMethodId");

        builder.HasIndex(al => al.UserId)
            .HasDatabaseName("IX_PaymentMethodAuditLogs_UserId");

        builder.HasIndex(al => al.Action)
            .HasDatabaseName("IX_PaymentMethodAuditLogs_Action");

        builder.HasIndex(al => al.CreatedAt)
            .HasDatabaseName("IX_PaymentMethodAuditLogs_CreatedAt");

        // Composite indexes for common audit queries
        builder.HasIndex(al => new { al.UserId, al.CreatedAt })
            .HasDatabaseName("IX_PaymentMethodAuditLogs_UserId_CreatedAt");

        builder.HasIndex(al => new { al.PaymentMethodId, al.CreatedAt })
            .HasDatabaseName("IX_PaymentMethodAuditLogs_PaymentMethodId_CreatedAt");
    }
}