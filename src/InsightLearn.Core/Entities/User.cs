using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InsightLearn.Core.Entities;

public class User : IdentityUser<Guid>
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
    
    public string? ProfileImageUrl { get; set; }
    
    public string? Bio { get; set; }
    
    public DateTime DateJoined { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastLoginDate { get; set; }
    
    public bool IsInstructor { get; set; }
    
    public bool IsVerified { get; set; }
    
    // Google OAuth Integration
    public string? GoogleId { get; set; }
    
    public string? GooglePictureUrl { get; set; }
    
    public bool IsGoogleUser { get; set; }
    
    public DateTime? GoogleTokenExpiry { get; set; }
    
    // Enhanced profile data from Google
    public string? GoogleLocale { get; set; }
    
    public string? GoogleGivenName { get; set; }
    
    public string? GoogleFamilyName { get; set; }
    
    public decimal WalletBalance { get; set; }

    // Registration completion tracking
    public bool RegistrationCompleted { get; set; } = false;
    public DateTime? RegistrationCompletedDate { get; set; }

    // Address information
    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }

    // Additional profile information
    public DateTime? DateOfBirth { get; set; }

    // Gender information (optional)
    public string? Gender { get; set; }

    // User type preference
    public string UserType { get; set; } = "Student"; // Student or Teacher

    // Payment preferences
    public string? PreferredPaymentMethod { get; set; } // CreditCard, PayPal, BankTransfer

    // Terms and privacy agreement tracking
    public bool HasAgreedToTerms { get; set; } = false;
    public DateTime? TermsAgreedDate { get; set; }
    public bool HasAgreedToPrivacyPolicy { get; set; } = false;
    public DateTime? PrivacyPolicyAgreedDate { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties (with [JsonIgnore] to prevent circular reference)
    [JsonIgnore]
    public virtual ICollection<Course> CreatedCourses { get; set; } = new List<Course>();

    [JsonIgnore]
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    [JsonIgnore]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [JsonIgnore]
    public virtual ICollection<ReviewVote> ReviewVotes { get; set; } = new List<ReviewVote>();

    [JsonIgnore]
    public virtual ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();

    [JsonIgnore]
    public virtual ICollection<DiscussionComment> DiscussionComments { get; set; } = new List<DiscussionComment>();

    [JsonIgnore]
    public virtual ICollection<DiscussionVote> DiscussionVotes { get; set; } = new List<DiscussionVote>();

    [JsonIgnore]
    public virtual ICollection<DiscussionCommentVote> DiscussionCommentVotes { get; set; } = new List<DiscussionCommentVote>();

    [JsonIgnore]
    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    [JsonIgnore]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [JsonIgnore]
    public virtual ICollection<LessonProgress> LessonProgress { get; set; } = new List<LessonProgress>();

    [JsonIgnore]
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

    [JsonIgnore]
    public virtual ICollection<Coupon> CreatedCoupons { get; set; } = new List<Coupon>();

    [JsonIgnore]
    public virtual ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();

    [JsonIgnore]
    public virtual ICollection<CourseEngagement> CourseEngagements { get; set; } = new List<CourseEngagement>();

    [JsonIgnore]
    public virtual ICollection<InstructorPayout> InstructorPayouts { get; set; } = new List<InstructorPayout>();

    [JsonIgnore]
    public virtual InstructorConnectAccount? ConnectAccount { get; set; }
    
    public string FullName => $"{FirstName} {LastName}";
    
    public string? ProfilePictureUrl => GooglePictureUrl ?? ProfileImageUrl;
}