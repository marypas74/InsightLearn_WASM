using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

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

    // Navigation properties
    public virtual ICollection<Course> CreatedCourses { get; set; } = new List<Course>();
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<ReviewVote> ReviewVotes { get; set; } = new List<ReviewVote>();
    public virtual ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
    public virtual ICollection<DiscussionComment> DiscussionComments { get; set; } = new List<DiscussionComment>();
    public virtual ICollection<DiscussionVote> DiscussionVotes { get; set; } = new List<DiscussionVote>();
    public virtual ICollection<DiscussionCommentVote> DiscussionCommentVotes { get; set; } = new List<DiscussionCommentVote>();
    public virtual ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<LessonProgress> LessonProgress { get; set; } = new List<LessonProgress>();
    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();
    public virtual ICollection<Coupon> CreatedCoupons { get; set; } = new List<Coupon>();
    
    public string FullName => $"{FirstName} {LastName}";
    
    public string? ProfilePictureUrl => GooglePictureUrl ?? ProfileImageUrl;
}