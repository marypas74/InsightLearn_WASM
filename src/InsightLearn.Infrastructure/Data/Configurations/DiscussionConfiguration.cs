using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class DiscussionConfiguration : IEntityTypeConfiguration<Discussion>
{
    public void Configure(EntityTypeBuilder<Discussion> builder)
    {
        builder.ToTable("Discussions");
        
        builder.HasKey(d => d.Id);
        
        // Properties
        builder.Property(d => d.Id)
            .ValueGeneratedNever();
            
        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(d => d.Content)
            .IsRequired();
            
        builder.Property(d => d.Type)
            .IsRequired()
            .HasConversion<string>();
            
        builder.Property(d => d.CreatedAt)
            .IsRequired();
        
        // Relationships
        builder.HasOne(d => d.Course)
            .WithMany(c => c.Discussions)
            .HasForeignKey(d => d.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(d => d.User)
            .WithMany(u => u.Discussions)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(d => d.Lesson)
            .WithMany()
            .HasForeignKey(d => d.LessonId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);
        
        // Indexes
        builder.HasIndex(d => d.CourseId)
            .HasDatabaseName("IX_Discussions_CourseId");
            
        builder.HasIndex(d => d.UserId)
            .HasDatabaseName("IX_Discussions_UserId");
            
        builder.HasIndex(d => d.LessonId)
            .HasDatabaseName("IX_Discussions_LessonId");
            
        builder.HasIndex(d => d.CreatedAt)
            .HasDatabaseName("IX_Discussions_CreatedAt");
    }
}

public class DiscussionCommentConfiguration : IEntityTypeConfiguration<DiscussionComment>
{
    public void Configure(EntityTypeBuilder<DiscussionComment> builder)
    {
        builder.ToTable("DiscussionComments");
        
        builder.HasKey(dc => dc.Id);
        
        // Properties
        builder.Property(dc => dc.Id)
            .ValueGeneratedNever();
            
        builder.Property(dc => dc.Content)
            .IsRequired();
            
        builder.Property(dc => dc.CreatedAt)
            .IsRequired();
            
        builder.Property(dc => dc.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        // Relationships
        builder.HasOne(dc => dc.Discussion)
            .WithMany(d => d.Comments)
            .HasForeignKey(dc => dc.DiscussionId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(dc => dc.User)
            .WithMany(u => u.DiscussionComments)
            .HasForeignKey(dc => dc.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(dc => dc.ParentComment)
            .WithMany(dc => dc.Replies)
            .HasForeignKey(dc => dc.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
        
        // Indexes
        builder.HasIndex(dc => dc.DiscussionId)
            .HasDatabaseName("IX_DiscussionComments_DiscussionId");
            
        builder.HasIndex(dc => dc.UserId)
            .HasDatabaseName("IX_DiscussionComments_UserId");
            
        builder.HasIndex(dc => dc.ParentCommentId)
            .HasDatabaseName("IX_DiscussionComments_ParentCommentId");
            
        builder.HasIndex(dc => dc.CreatedAt)
            .HasDatabaseName("IX_DiscussionComments_CreatedAt");
    }
}

public class DiscussionVoteConfiguration : IEntityTypeConfiguration<DiscussionVote>
{
    public void Configure(EntityTypeBuilder<DiscussionVote> builder)
    {
        builder.ToTable("DiscussionVotes");
        
        builder.HasKey(dv => dv.Id);
        
        // Properties
        builder.Property(dv => dv.Id)
            .ValueGeneratedNever();
            
        builder.Property(dv => dv.IsUpVote)
            .IsRequired();
            
        builder.Property(dv => dv.VotedAt)
            .IsRequired();
        
        // Relationships
        builder.HasOne(dv => dv.Discussion)
            .WithMany(d => d.Votes)
            .HasForeignKey(dv => dv.DiscussionId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(dv => dv.User)
            .WithMany(u => u.DiscussionVotes)
            .HasForeignKey(dv => dv.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        builder.HasIndex(dv => new { dv.UserId, dv.DiscussionId })
            .IsUnique()
            .HasDatabaseName("IX_DiscussionVotes_User_Discussion");
    }
}

public class DiscussionCommentVoteConfiguration : IEntityTypeConfiguration<DiscussionCommentVote>
{
    public void Configure(EntityTypeBuilder<DiscussionCommentVote> builder)
    {
        builder.ToTable("DiscussionCommentVotes");
        
        builder.HasKey(dcv => dcv.Id);
        
        // Properties
        builder.Property(dcv => dcv.Id)
            .ValueGeneratedNever();
            
        builder.Property(dcv => dcv.IsUpVote)
            .IsRequired();
            
        builder.Property(dcv => dcv.VotedAt)
            .IsRequired();
        
        // Relationships
        builder.HasOne(dcv => dcv.Comment)
            .WithMany(dc => dc.Votes)
            .HasForeignKey(dcv => dcv.CommentId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(dcv => dcv.User)
            .WithMany(u => u.DiscussionCommentVotes)
            .HasForeignKey(dcv => dcv.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Indexes
        builder.HasIndex(dcv => new { dcv.UserId, dcv.CommentId })
            .IsUnique()
            .HasDatabaseName("IX_DiscussionCommentVotes_User_Comment");
    }
}