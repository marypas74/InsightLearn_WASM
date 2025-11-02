using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class ReviewVote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid ReviewId { get; set; }
    
    public bool IsHelpful { get; set; }
    
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual User User { get; set; } = null!;
    public virtual Review Review { get; set; } = null!;
}