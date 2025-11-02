using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        
        builder.HasKey(p => p.Id);
        
        // Properties
        builder.Property(p => p.Id)
            .ValueGeneratedNever();
            
        builder.Property(p => p.Amount)
            .IsRequired()
            .HasColumnType("decimal(10,2)");
            
        builder.Property(p => p.OriginalAmount)
            .IsRequired()
            .HasColumnType("decimal(10,2)");
            
        builder.Property(p => p.DiscountAmount)
            .IsRequired()
            .HasColumnType("decimal(10,2)");
            
        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");
            
        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(p => p.PaymentMethod)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(p => p.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(p => p.RefundAmount)
            .HasColumnType("decimal(10,2)");
        
        // Relationships
        builder.HasOne(p => p.User)
            .WithMany(u => u.Payments)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(p => p.Course)
            .WithMany()
            .HasForeignKey(p => p.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(p => p.Coupon)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.CouponId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        
        // Indexes
        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_Payments_UserId");
            
        builder.HasIndex(p => p.CourseId)
            .HasDatabaseName("IX_Payments_CourseId");
            
        builder.HasIndex(p => p.TransactionId)
            .HasDatabaseName("IX_Payments_TransactionId");
            
        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Payments_CreatedAt");
    }
}

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons");
        
        builder.HasKey(c => c.Id);
        
        // Properties
        builder.Property(c => c.Id)
            .ValueGeneratedNever();
            
        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(c => c.Description)
            .HasMaxLength(200);
            
        builder.Property(c => c.Type)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(c => c.Value)
            .IsRequired()
            .HasColumnType("decimal(10,2)");
            
        builder.Property(c => c.MinimumAmount)
            .HasColumnType("decimal(10,2)");
            
        builder.Property(c => c.MaximumDiscount)
            .HasColumnType("decimal(10,2)");
            
        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        // Relationships
        builder.HasOne(c => c.Course)
            .WithMany(course => course.Coupons)
            .HasForeignKey(c => c.CourseId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
            
        builder.HasOne(c => c.Instructor)
            .WithMany(u => u.CreatedCoupons)
            .HasForeignKey(c => c.InstructorId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        
        // Indexes
        builder.HasIndex(c => c.Code)
            .IsUnique()
            .HasDatabaseName("IX_Coupons_Code");
            
        builder.HasIndex(c => c.CourseId)
            .HasDatabaseName("IX_Coupons_CourseId");
            
        builder.HasIndex(c => c.ValidFrom)
            .HasDatabaseName("IX_Coupons_ValidFrom");
            
        builder.HasIndex(c => c.ValidUntil)
            .HasDatabaseName("IX_Coupons_ValidUntil");
    }
}