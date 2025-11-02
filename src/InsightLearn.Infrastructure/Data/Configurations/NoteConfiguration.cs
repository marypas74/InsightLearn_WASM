using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using InsightLearn.Core.Entities;

namespace InsightLearn.Infrastructure.Data.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("Notes");
        
        builder.HasKey(n => n.Id);
        
        // Properties
        builder.Property(n => n.Id)
            .ValueGeneratedNever();
            
        builder.Property(n => n.Content)
            .IsRequired();
            
        builder.Property(n => n.CreatedAt)
            .IsRequired();
        
        // Relationships
        builder.HasOne(n => n.User)
            .WithMany(u => u.Notes)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(n => n.Lesson)
            .WithMany(l => l.Notes)
            .HasForeignKey(n => n.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(n => n.UserId)
            .HasDatabaseName("IX_Notes_UserId");
            
        builder.HasIndex(n => n.LessonId)
            .HasDatabaseName("IX_Notes_LessonId");
            
        builder.HasIndex(n => new { n.UserId, n.LessonId })
            .HasDatabaseName("IX_Notes_User_Lesson");
            
        builder.HasIndex(n => n.CreatedAt)
            .HasDatabaseName("IX_Notes_CreatedAt");
    }
}