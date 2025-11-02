using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class ReviewVoteConfiguration : IEntityTypeConfiguration<ReviewVote>
{
    public void Configure(EntityTypeBuilder<ReviewVote> builder)
    {
        builder.ToTable("ReviewVotes");
        
        builder.HasKey(rv => rv.Id);
        
        // Properties
        builder.Property(rv => rv.Id)
            .ValueGeneratedNever(); // GUID is generated in the entity
            
        builder.Property(rv => rv.UserId)
            .IsRequired();
            
        builder.Property(rv => rv.ReviewId)
            .IsRequired();
            
        builder.Property(rv => rv.IsHelpful)
            .IsRequired();
            
        builder.Property(rv => rv.VotedAt)
            .IsRequired();
        
        // Relationships
        builder.HasOne(rv => rv.User)
            .WithMany(u => u.ReviewVotes)
            .HasForeignKey(rv => rv.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(rv => rv.Review)
            .WithMany(r => r.Votes)
            .HasForeignKey(rv => rv.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(rv => new { rv.UserId, rv.ReviewId })
            .IsUnique()
            .HasDatabaseName("IX_ReviewVotes_User_Review");
            
        builder.HasIndex(rv => rv.ReviewId)
            .HasDatabaseName("IX_ReviewVotes_ReviewId");
            
        builder.HasIndex(rv => rv.VotedAt)
            .HasDatabaseName("IX_ReviewVotes_VotedAt");
    }
}