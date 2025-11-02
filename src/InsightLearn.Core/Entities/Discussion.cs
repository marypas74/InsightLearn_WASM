using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class Discussion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid CourseId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    public Guid? LessonId { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DiscussionType Type { get; set; } = DiscussionType.Question;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsPinned { get; set; }
    
    public bool IsLocked { get; set; }
    
    public bool IsResolved { get; set; }
    
    public int ViewCount { get; set; }
    
    public int UpVotes { get; set; }
    
    public int DownVotes { get; set; }
    
    // Navigation properties
    public virtual Course Course { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual Lesson? Lesson { get; set; }
    public virtual ICollection<DiscussionComment> Comments { get; set; } = new List<DiscussionComment>();
    public virtual ICollection<DiscussionVote> Votes { get; set; } = new List<DiscussionVote>();
    
    public int NetVotes => UpVotes - DownVotes;
    
    public int CommentCount => Comments.Count(c => c.IsActive);
    
    public DateTime LastActivity => Comments.Any() 
        ? Comments.Where(c => c.IsActive).Max(c => c.CreatedAt) 
        : UpdatedAt ?? CreatedAt;
}

public class DiscussionComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid DiscussionId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    public Guid? ParentCommentId { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public bool IsBestAnswer { get; set; }
    
    public bool IsInstructorReply { get; set; }
    
    public int UpVotes { get; set; }
    
    public int DownVotes { get; set; }
    
    // Navigation properties
    public virtual Discussion Discussion { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual DiscussionComment? ParentComment { get; set; }
    public virtual ICollection<DiscussionComment> Replies { get; set; } = new List<DiscussionComment>();
    public virtual ICollection<DiscussionCommentVote> Votes { get; set; } = new List<DiscussionCommentVote>();
    
    public int NetVotes => UpVotes - DownVotes;
    
    public int ReplyCount => Replies.Count(r => r.IsActive);
}

public class DiscussionVote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid DiscussionId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    public bool IsUpVote { get; set; }
    
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Discussion Discussion { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

public class DiscussionCommentVote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid CommentId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    public bool IsUpVote { get; set; }
    
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual DiscussionComment Comment { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

public enum DiscussionType
{
    Question,
    Announcement,
    General,
    TechnicalIssue,
    CourseContent,
    Assignment
}