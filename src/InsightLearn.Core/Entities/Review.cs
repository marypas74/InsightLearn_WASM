using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InsightLearn.Core.Entities;

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public Guid CourseId { get; set; }
    
    [Range(1, 5)]
    public int Rating { get; set; }
    
    [StringLength(1000)]
    public string? Comment { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    public bool IsApproved { get; set; } = true;
    
    public int HelpfulVotes { get; set; }
    
    public int UnhelpfulVotes { get; set; }
    
    // Navigation properties (with [JsonIgnore] to prevent circular reference)
    [JsonIgnore]
    public virtual User User { get; set; } = null!;

    [JsonIgnore]
    public virtual Course Course { get; set; } = null!;

    [JsonIgnore]
    public virtual ICollection<ReviewVote> Votes { get; set; } = new List<ReviewVote>();
    
    public int NetVotes => HelpfulVotes - UnhelpfulVotes;
    
    public double HelpfulnessRatio => (HelpfulVotes + UnhelpfulVotes) > 0 
        ? (double)HelpfulVotes / (HelpfulVotes + UnhelpfulVotes) 
        : 0;
}

