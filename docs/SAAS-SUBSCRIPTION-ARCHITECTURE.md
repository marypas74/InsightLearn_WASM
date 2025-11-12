# SaaS Subscription Model - Complete Architecture Design

**Project**: InsightLearn WASM
**Version**: 2.0.0 (Major business model change)
**Date**: 2025-01-10
**Author**: System Architect

---

## Executive Summary

This document outlines the complete architecture for transitioning InsightLearn from a **pay-per-course model** to a **SaaS subscription model** with engagement-based instructor revenue sharing.

### Business Model Comparison

| Aspect | Current (Pay-per-course) | New (SaaS Subscription) |
|--------|--------------------------|-------------------------|
| User Payment | €49.99 per course | €4.00/month unlimited access |
| Instructor Revenue | 80% of course price | Based on engagement time |
| Revenue Predictability | Variable, one-time | Recurring, predictable (MRR) |
| User Barrier to Entry | High (per-course cost) | Low (monthly subscription) |
| Platform Scalability | Limited by course sales | Unlimited course access drives engagement |

### Key Metrics

**User Subscription Tiers**:
- Basic: €4.00/month (unlimited courses)
- Pro: €8.00/month (unlimited + offline downloads + priority support)
- Premium: €12.00/month (unlimited + 1-on-1 mentoring + certificates)

**Instructor Payout Formula**:
```
instructor_monthly_payout = (total_platform_revenue * 0.80) * (instructor_engagement_minutes / total_platform_engagement_minutes)
```

**Platform Fee**: 20% of total revenue (covers infrastructure, support, operations)

---

## 1. Database Schema Design

### 1.1 New Tables

#### SubscriptionPlans
```sql
CREATE TABLE SubscriptionPlans (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL,                    -- "Basic", "Pro", "Premium"
    Slug NVARCHAR(100) NOT NULL UNIQUE,             -- "basic", "pro", "premium"
    Description NVARCHAR(500),                      -- Plan description
    PriceMonthly DECIMAL(10,2) NOT NULL,            -- Monthly price (e.g., 4.00)
    PriceYearly DECIMAL(10,2) NULL,                 -- Annual price (e.g., 40.00 = 2 months free)
    StripePriceIdMonthly NVARCHAR(255) NULL,        -- Stripe Price ID for monthly billing
    StripePriceIdYearly NVARCHAR(255) NULL,         -- Stripe Price ID for annual billing
    Features NVARCHAR(MAX),                         -- JSON array: ["unlimited_courses", "offline_downloads"]
    MaxConcurrentDevices INT DEFAULT 2,             -- Device limit
    HasOfflineAccess BIT DEFAULT 0,
    HasPrioritySupport BIT DEFAULT 0,
    HasMentorship BIT DEFAULT 0,
    HasCertificates BIT DEFAULT 1,
    OrderIndex INT DEFAULT 0,                       -- Display order
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_SubscriptionPlans_Slug (Slug),
    INDEX IX_SubscriptionPlans_IsActive (IsActive)
);
```

#### UserSubscriptions
```sql
CREATE TABLE UserSubscriptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    PlanId UNIQUEIDENTIFIER NOT NULL,
    Status NVARCHAR(50) NOT NULL,                   -- "active", "trialing", "cancelled", "past_due", "paused", "expired"
    BillingCycle NVARCHAR(20) NOT NULL,             -- "monthly", "yearly"

    -- Stripe Integration
    StripeSubscriptionId NVARCHAR(255) NULL UNIQUE,
    StripeCustomerId NVARCHAR(255) NULL,
    StripePaymentMethodId NVARCHAR(255) NULL,

    -- Subscription Periods
    CurrentPeriodStart DATETIME2 NOT NULL,
    CurrentPeriodEnd DATETIME2 NOT NULL,
    TrialStart DATETIME2 NULL,
    TrialEnd DATETIME2 NULL,

    -- Cancellation
    CancelAtPeriodEnd BIT DEFAULT 0,                -- Cancel at end of billing period
    CancelledAt DATETIME2 NULL,
    CancellationReason NVARCHAR(500) NULL,

    -- Pricing
    CurrentPrice DECIMAL(10,2) NOT NULL,            -- Price at subscription time (for price locks)
    Currency NVARCHAR(3) DEFAULT 'EUR',

    -- Metadata
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),

    CONSTRAINT FK_UserSubscriptions_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserSubscriptions_SubscriptionPlans FOREIGN KEY (PlanId) REFERENCES SubscriptionPlans(Id),

    INDEX IX_UserSubscriptions_UserId (UserId),
    INDEX IX_UserSubscriptions_Status (Status),
    INDEX IX_UserSubscriptions_StripeSubscriptionId (StripeSubscriptionId),
    INDEX IX_UserSubscriptions_CurrentPeriodEnd (CurrentPeriodEnd)  -- For renewal checks
);
```

#### CourseEngagement
```sql
CREATE TABLE CourseEngagement (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    CourseId UNIQUEIDENTIFIER NOT NULL,
    LessonId UNIQUEIDENTIFIER NULL,
    SectionId UNIQUEIDENTIFIER NULL,

    -- Engagement Type and Duration
    EngagementType NVARCHAR(50) NOT NULL,           -- "video_watch", "quiz_attempt", "assignment_submit", "reading", "discussion_post"
    DurationMinutes INT NOT NULL,                   -- Actual time spent (validated, capped)

    -- Session Tracking
    SessionId NVARCHAR(255) NULL,                   -- Track continuous sessions
    StartedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL,

    -- Validation & Anti-Fraud
    IsValidated BIT DEFAULT 0,                      -- Passed anti-fraud checks
    ValidationScore DECIMAL(3,2) NULL,              -- 0.00-1.00 confidence score
    DeviceFingerprint NVARCHAR(500) NULL,           -- Device ID for fraud detection
    IpAddress NVARCHAR(45) NULL,                    -- IPv4/IPv6
    UserAgent NVARCHAR(500) NULL,

    -- Metadata (JSON)
    MetaData NVARCHAR(MAX),                         -- { "video_progress": 95, "quiz_score": 85, "playback_speed": 1.0 }

    -- Timestamps
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    CONSTRAINT FK_CourseEngagement_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CourseEngagement_Courses FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CourseEngagement_Lessons FOREIGN KEY (LessonId) REFERENCES Lessons(Id),
    CONSTRAINT FK_CourseEngagement_Sections FOREIGN KEY (SectionId) REFERENCES Sections(Id),

    INDEX IX_CourseEngagement_UserId (UserId),
    INDEX IX_CourseEngagement_CourseId (CourseId),
    INDEX IX_CourseEngagement_LessonId (LessonId),
    INDEX IX_CourseEngagement_StartedAt (StartedAt),
    INDEX IX_CourseEngagement_InstructorRevenue (CourseId, DurationMinutes) INCLUDE (IsValidated),  -- Revenue calculations
    INDEX IX_CourseEngagement_SessionId (SessionId)
);
```

#### InstructorPayouts
```sql
CREATE TABLE InstructorPayouts (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    InstructorId UNIQUEIDENTIFIER NOT NULL,

    -- Payout Period
    Month INT NOT NULL,                             -- 1-12
    Year INT NOT NULL,                              -- 2024, 2025, etc.

    -- Engagement Metrics
    TotalEngagementMinutes BIGINT NOT NULL,         -- Instructor's total engagement
    PlatformTotalEngagementMinutes BIGINT NOT NULL, -- Platform-wide engagement
    EngagementPercentage DECIMAL(10,8) NOT NULL,    -- instructor_engagement / total_engagement

    -- Revenue Calculation
    TotalPlatformRevenue DECIMAL(12,2) NOT NULL,    -- Total subscription revenue for month
    InstructorShareRevenue DECIMAL(12,2) NOT NULL,  -- Platform revenue * 0.80 (80% to instructors)
    PayoutAmount DECIMAL(12,2) NOT NULL,            -- Instructor's calculated payout

    -- Platform Fee
    PlatformFeePercentage DECIMAL(5,2) DEFAULT 20.00,
    PlatformFeeAmount DECIMAL(12,2) NOT NULL,

    -- Status & Processing
    Status NVARCHAR(50) NOT NULL,                   -- "pending", "processing", "paid", "failed", "on_hold"

    -- Stripe Connect Integration
    StripeTransferId NVARCHAR(255) NULL,
    StripeConnectAccountId NVARCHAR(255) NULL,

    -- Payment Details
    Currency NVARCHAR(3) DEFAULT 'EUR',
    PaymentMethod NVARCHAR(50) NULL,                -- "stripe_connect", "bank_transfer", "paypal"

    -- Timestamps
    CalculatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ProcessedAt DATETIME2 NULL,
    PaidAt DATETIME2 NULL,

    -- Notes & Metadata
    Notes NVARCHAR(MAX),
    MetaData NVARCHAR(MAX),                         -- JSON with detailed breakdown

    CONSTRAINT FK_InstructorPayouts_Users FOREIGN KEY (InstructorId) REFERENCES Users(Id),

    INDEX IX_InstructorPayouts_InstructorId (InstructorId),
    INDEX IX_InstructorPayouts_Period (Year, Month),
    INDEX IX_InstructorPayouts_Status (Status),
    UNIQUE INDEX UX_InstructorPayouts_Period (InstructorId, Year, Month)  -- One payout per instructor per month
);
```

#### SubscriptionRevenue
```sql
CREATE TABLE SubscriptionRevenue (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SubscriptionId UNIQUEIDENTIFIER NOT NULL,

    -- Billing Details
    Amount DECIMAL(10,2) NOT NULL,
    Currency NVARCHAR(3) DEFAULT 'EUR',
    BillingPeriodStart DATETIME2 NOT NULL,
    BillingPeriodEnd DATETIME2 NOT NULL,

    -- Stripe Integration
    StripeInvoiceId NVARCHAR(255) NULL UNIQUE,
    StripePaymentIntentId NVARCHAR(255) NULL,
    StripeChargeId NVARCHAR(255) NULL,

    -- Status
    Status NVARCHAR(50) NOT NULL,                   -- "paid", "pending", "failed", "refunded", "partially_refunded"

    -- Refunds
    RefundAmount DECIMAL(10,2) NULL,
    RefundedAt DATETIME2 NULL,
    RefundReason NVARCHAR(500) NULL,

    -- Timestamps
    PaidAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    -- Metadata
    MetaData NVARCHAR(MAX),                         -- JSON with invoice details

    CONSTRAINT FK_SubscriptionRevenue_UserSubscriptions FOREIGN KEY (SubscriptionId) REFERENCES UserSubscriptions(Id) ON DELETE CASCADE,

    INDEX IX_SubscriptionRevenue_SubscriptionId (SubscriptionId),
    INDEX IX_SubscriptionRevenue_BillingPeriod (BillingPeriodStart),
    INDEX IX_SubscriptionRevenue_Status (Status),
    INDEX IX_SubscriptionRevenue_PaidAt (PaidAt)   -- For revenue reporting
);
```

#### SubscriptionEvents (Audit Log)
```sql
CREATE TABLE SubscriptionEvents (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SubscriptionId UNIQUEIDENTIFIER NOT NULL,
    EventType NVARCHAR(100) NOT NULL,               -- "created", "updated", "cancelled", "renewed", "payment_failed", "trial_ended"
    EventData NVARCHAR(MAX),                        -- JSON with event details
    StripeEventId NVARCHAR(255) NULL,               -- Stripe webhook event ID
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    CONSTRAINT FK_SubscriptionEvents_UserSubscriptions FOREIGN KEY (SubscriptionId) REFERENCES UserSubscriptions(Id) ON DELETE CASCADE,

    INDEX IX_SubscriptionEvents_SubscriptionId (SubscriptionId),
    INDEX IX_SubscriptionEvents_EventType (EventType),
    INDEX IX_SubscriptionEvents_CreatedAt (CreatedAt)
);
```

#### InstructorConnectAccounts
```sql
CREATE TABLE InstructorConnectAccounts (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    InstructorId UNIQUEIDENTIFIER NOT NULL UNIQUE,

    -- Stripe Connect Details
    StripeConnectAccountId NVARCHAR(255) NOT NULL UNIQUE,
    AccountType NVARCHAR(50) DEFAULT 'express',    -- "express", "standard"

    -- Status
    ChargesEnabled BIT DEFAULT 0,
    PayoutsEnabled BIT DEFAULT 0,
    DetailsSubmitted BIT DEFAULT 0,

    -- Country & Currency
    Country NVARCHAR(2) NOT NULL,                   -- ISO 3166-1 alpha-2 (e.g., "IT", "US")
    DefaultCurrency NVARCHAR(3) DEFAULT 'EUR',

    -- Onboarding
    OnboardingLink NVARCHAR(500) NULL,
    OnboardingCompletedAt DATETIME2 NULL,

    -- Timestamps
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),

    CONSTRAINT FK_InstructorConnectAccounts_Users FOREIGN KEY (InstructorId) REFERENCES Users(Id) ON DELETE CASCADE,

    INDEX IX_InstructorConnectAccounts_InstructorId (InstructorId),
    INDEX IX_InstructorConnectAccounts_StripeConnectAccountId (StripeConnectAccountId)
);
```

### 1.2 Modifications to Existing Tables

#### Courses Table
```sql
-- Add subscription-only flag
ALTER TABLE Courses ADD IsSubscriptionOnly BIT DEFAULT 1;
ALTER TABLE Courses ADD LegacyPrice DECIMAL(10,2) NULL;  -- Preserve old price for historical data

-- Update existing data
UPDATE Courses SET IsSubscriptionOnly = 1, LegacyPrice = Price;

-- Price is now deprecated but kept for backward compatibility
```

#### Enrollments Table
```sql
-- Add subscription tracking
ALTER TABLE Enrollments ADD SubscriptionId UNIQUEIDENTIFIER NULL;
ALTER TABLE Enrollments ADD EnrolledViaSubscription BIT DEFAULT 1;
ALTER TABLE Enrollments ADD AutoEnrolled BIT DEFAULT 1;  -- Auto-enrolled via active subscription

-- Add foreign key
ALTER TABLE Enrollments ADD CONSTRAINT FK_Enrollments_UserSubscriptions
    FOREIGN KEY (SubscriptionId) REFERENCES UserSubscriptions(Id);

-- Add index
CREATE INDEX IX_Enrollments_SubscriptionId ON Enrollments(SubscriptionId);
```

#### Users Table
```sql
-- Add Stripe customer tracking
ALTER TABLE Users ADD StripeCustomerId NVARCHAR(255) NULL;
ALTER TABLE Users ADD SubscriptionStatus NVARCHAR(50) DEFAULT 'none';  -- "none", "active", "trialing", "cancelled", "past_due"
ALTER TABLE Users ADD CurrentSubscriptionId UNIQUEIDENTIFIER NULL;

-- Add indexes
CREATE INDEX IX_Users_StripeCustomerId ON Users(StripeCustomerId);
CREATE INDEX IX_Users_SubscriptionStatus ON Users(SubscriptionStatus);

-- Add foreign key
ALTER TABLE Users ADD CONSTRAINT FK_Users_CurrentSubscription
    FOREIGN KEY (CurrentSubscriptionId) REFERENCES UserSubscriptions(Id);
```

---

## 2. Entity Models (.NET)

### 2.1 SubscriptionPlan.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

public class SubscriptionPlan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PriceMonthly { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PriceYearly { get; set; }

    [StringLength(255)]
    public string? StripePriceIdMonthly { get; set; }

    [StringLength(255)]
    public string? StripePriceIdYearly { get; set; }

    public string? Features { get; set; }  // JSON array

    public int MaxConcurrentDevices { get; set; } = 2;

    public bool HasOfflineAccess { get; set; }

    public bool HasPrioritySupport { get; set; }

    public bool HasMentorship { get; set; }

    public bool HasCertificates { get; set; } = true;

    public int OrderIndex { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();

    // Computed properties
    [NotMapped]
    public decimal? YearlySavings => PriceYearly.HasValue
        ? (PriceMonthly * 12) - PriceYearly.Value
        : null;

    [NotMapped]
    public int? YearlySavingsPercentage => PriceYearly.HasValue
        ? (int)((YearlySavings / (PriceMonthly * 12)) * 100)
        : null;
}
```

### 2.2 UserSubscription.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

public class UserSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid PlanId { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = SubscriptionStatus.Active;

    [Required]
    [StringLength(20)]
    public string BillingCycle { get; set; } = "monthly";

    // Stripe Integration
    [StringLength(255)]
    public string? StripeSubscriptionId { get; set; }

    [StringLength(255)]
    public string? StripeCustomerId { get; set; }

    [StringLength(255)]
    public string? StripePaymentMethodId { get; set; }

    // Subscription Periods
    public DateTime CurrentPeriodStart { get; set; }

    public DateTime CurrentPeriodEnd { get; set; }

    public DateTime? TrialStart { get; set; }

    public DateTime? TrialEnd { get; set; }

    // Cancellation
    public bool CancelAtPeriodEnd { get; set; }

    public DateTime? CancelledAt { get; set; }

    [StringLength(500)]
    public string? CancellationReason { get; set; }

    // Pricing
    [Column(TypeName = "decimal(10,2)")]
    public decimal CurrentPrice { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "EUR";

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;

    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual ICollection<SubscriptionRevenue> SubscriptionRevenues { get; set; } = new List<SubscriptionRevenue>();

    public virtual ICollection<SubscriptionEvent> Events { get; set; } = new List<SubscriptionEvent>();

    // Computed properties
    [NotMapped]
    public bool IsActive => Status == SubscriptionStatus.Active || Status == SubscriptionStatus.Trialing;

    [NotMapped]
    public bool IsTrialing => Status == SubscriptionStatus.Trialing && DateTime.UtcNow <= TrialEnd;

    [NotMapped]
    public int DaysUntilRenewal => IsActive
        ? Math.Max(0, (CurrentPeriodEnd - DateTime.UtcNow).Days)
        : 0;

    [NotMapped]
    public bool RequiresPaymentMethod => string.IsNullOrEmpty(StripePaymentMethodId);
}

public static class SubscriptionStatus
{
    public const string Active = "active";
    public const string Trialing = "trialing";
    public const string Cancelled = "cancelled";
    public const string PastDue = "past_due";
    public const string Paused = "paused";
    public const string Expired = "expired";
}
```

### 2.3 CourseEngagement.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

public class CourseEngagement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public Guid CourseId { get; set; }

    public Guid? LessonId { get; set; }

    public Guid? SectionId { get; set; }

    [Required]
    [StringLength(50)]
    public string EngagementType { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    [StringLength(255)]
    public string? SessionId { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Validation & Anti-Fraud
    public bool IsValidated { get; set; }

    [Column(TypeName = "decimal(3,2)")]
    public decimal? ValidationScore { get; set; }

    [StringLength(500)]
    public string? DeviceFingerprint { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    [StringLength(500)]
    public string? UserAgent { get; set; }

    public string? MetaData { get; set; }  // JSON

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User User { get; set; } = null!;

    public virtual Course Course { get; set; } = null!;

    public virtual Lesson? Lesson { get; set; }

    public virtual Section? Section { get; set; }
}

public static class EngagementType
{
    public const string VideoWatch = "video_watch";
    public const string QuizAttempt = "quiz_attempt";
    public const string AssignmentSubmit = "assignment_submit";
    public const string Reading = "reading";
    public const string DiscussionPost = "discussion_post";
}
```

### 2.4 InstructorPayout.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

public class InstructorPayout
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid InstructorId { get; set; }

    public int Month { get; set; }  // 1-12

    public int Year { get; set; }

    public long TotalEngagementMinutes { get; set; }

    public long PlatformTotalEngagementMinutes { get; set; }

    [Column(TypeName = "decimal(10,8)")]
    public decimal EngagementPercentage { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal TotalPlatformRevenue { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal InstructorShareRevenue { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal PayoutAmount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal PlatformFeePercentage { get; set; } = 20.00m;

    [Column(TypeName = "decimal(12,2)")]
    public decimal PlatformFeeAmount { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = PayoutStatus.Pending;

    [StringLength(255)]
    public string? StripeTransferId { get; set; }

    [StringLength(255)]
    public string? StripeConnectAccountId { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "EUR";

    [StringLength(50)]
    public string? PaymentMethod { get; set; }

    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? Notes { get; set; }

    public string? MetaData { get; set; }  // JSON

    // Navigation properties
    public virtual User Instructor { get; set; } = null!;

    // Computed properties
    [NotMapped]
    public string Period => $"{Year}-{Month:D2}";

    [NotMapped]
    public bool IsPaid => Status == PayoutStatus.Paid && PaidAt.HasValue;

    [NotMapped]
    public decimal EffectiveRate => PlatformTotalEngagementMinutes > 0
        ? (decimal)TotalEngagementMinutes / PlatformTotalEngagementMinutes
        : 0;
}

public static class PayoutStatus
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Paid = "paid";
    public const string Failed = "failed";
    public const string OnHold = "on_hold";
}
```

### 2.5 SubscriptionRevenue.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InsightLearn.Core.Entities;

public class SubscriptionRevenue
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SubscriptionId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [StringLength(3)]
    public string Currency { get; set; } = "EUR";

    public DateTime BillingPeriodStart { get; set; }

    public DateTime BillingPeriodEnd { get; set; }

    [StringLength(255)]
    public string? StripeInvoiceId { get; set; }

    [StringLength(255)]
    public string? StripePaymentIntentId { get; set; }

    [StringLength(255)]
    public string? StripeChargeId { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = RevenueStatus.Pending;

    [Column(TypeName = "decimal(10,2)")]
    public decimal? RefundAmount { get; set; }

    public DateTime? RefundedAt { get; set; }

    [StringLength(500)]
    public string? RefundReason { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? MetaData { get; set; }  // JSON

    // Navigation properties
    public virtual UserSubscription Subscription { get; set; } = null!;

    // Computed properties
    [NotMapped]
    public decimal NetAmount => Amount - (RefundAmount ?? 0);

    [NotMapped]
    public bool IsPaid => Status == RevenueStatus.Paid && PaidAt.HasValue;
}

public static class RevenueStatus
{
    public const string Paid = "paid";
    public const string Pending = "pending";
    public const string Failed = "failed";
    public const string Refunded = "refunded";
    public const string PartiallyRefunded = "partially_refunded";
}
```

### 2.6 SubscriptionEvent.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class SubscriptionEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SubscriptionId { get; set; }

    [Required]
    [StringLength(100)]
    public string EventType { get; set; } = string.Empty;

    public string? EventData { get; set; }  // JSON

    [StringLength(255)]
    public string? StripeEventId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual UserSubscription Subscription { get; set; } = null!;
}

public static class SubscriptionEventType
{
    public const string Created = "created";
    public const string Updated = "updated";
    public const string Cancelled = "cancelled";
    public const string Renewed = "renewed";
    public const string PaymentFailed = "payment_failed";
    public const string PaymentSucceeded = "payment_succeeded";
    public const string TrialEnded = "trial_ended";
    public const string PlanChanged = "plan_changed";
}
```

### 2.7 InstructorConnectAccount.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace InsightLearn.Core.Entities;

public class InstructorConnectAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid InstructorId { get; set; }

    [Required]
    [StringLength(255)]
    public string StripeConnectAccountId { get; set; } = string.Empty;

    [StringLength(50)]
    public string AccountType { get; set; } = "express";

    public bool ChargesEnabled { get; set; }

    public bool PayoutsEnabled { get; set; }

    public bool DetailsSubmitted { get; set; }

    [Required]
    [StringLength(2)]
    public string Country { get; set; } = "IT";

    [StringLength(3)]
    public string DefaultCurrency { get; set; } = "EUR";

    [StringLength(500)]
    public string? OnboardingLink { get; set; }

    public DateTime? OnboardingCompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual User Instructor { get; set; } = null!;
}
```

---

## 3. Service Interfaces

### 3.1 ISubscriptionService.cs
```csharp
using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

public interface ISubscriptionService
{
    // Subscription Management
    Task<UserSubscription> CreateSubscriptionAsync(Guid userId, Guid planId, string billingCycle, string? couponCode = null);
    Task<UserSubscription?> GetUserSubscriptionAsync(Guid userId);
    Task<UserSubscription?> GetSubscriptionByIdAsync(Guid subscriptionId);
    Task<bool> CancelSubscriptionAsync(Guid subscriptionId, string? cancellationReason = null, bool cancelImmediately = false);
    Task<bool> ResumeSubscriptionAsync(Guid subscriptionId);
    Task<UserSubscription> UpgradePlanAsync(Guid subscriptionId, Guid newPlanId);
    Task<UserSubscription> DowngradePlanAsync(Guid subscriptionId, Guid newPlanId);

    // Subscription Status
    Task<bool> IsUserSubscribedAsync(Guid userId);
    Task<bool> HasActiveSubscriptionAsync(Guid userId);
    Task<SubscriptionPlan?> GetUserCurrentPlanAsync(Guid userId);

    // Plans
    Task<List<SubscriptionPlan>> GetAllPlansAsync();
    Task<SubscriptionPlan?> GetPlanBySlugAsync(string slug);

    // Stripe Integration
    Task<string> CreateCheckoutSessionAsync(Guid userId, Guid planId, string billingCycle, string successUrl, string cancelUrl);
    Task<string> CreateCustomerPortalSessionAsync(Guid userId, string returnUrl);
    Task HandleStripeWebhookAsync(string json, string stripeSignature);
}
```

### 3.2 IEngagementTrackingService.cs
```csharp
using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

public interface IEngagementTrackingService
{
    // Track Engagement
    Task<CourseEngagement> TrackVideoWatchAsync(Guid userId, Guid lessonId, int durationMinutes, string? sessionId = null, Dictionary<string, object>? metadata = null);
    Task<CourseEngagement> TrackQuizAttemptAsync(Guid userId, Guid quizId, int durationMinutes, int score, string? sessionId = null);
    Task<CourseEngagement> TrackAssignmentSubmitAsync(Guid userId, Guid assignmentId, int durationMinutes, string? sessionId = null);
    Task<CourseEngagement> TrackReadingAsync(Guid userId, Guid lessonId, int durationMinutes, string? sessionId = null);
    Task<CourseEngagement> TrackDiscussionPostAsync(Guid userId, Guid discussionId, int durationMinutes);

    // Engagement Stats
    Task<UserEngagementStats> GetUserEngagementStatsAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<CourseEngagementStats> GetCourseEngagementStatsAsync(Guid courseId, DateTime? startDate = null, DateTime? endDate = null);
    Task<InstructorEngagementStats> GetInstructorEngagementStatsAsync(Guid instructorId, DateTime? startDate = null, DateTime? endDate = null);

    // Platform-wide Stats
    Task<long> GetPlatformTotalEngagementAsync(int year, int month);
    Task<Dictionary<Guid, long>> GetInstructorsEngagementMapAsync(int year, int month);

    // Validation & Anti-Fraud
    Task<bool> ValidateEngagementAsync(CourseEngagement engagement);
    Task<decimal> CalculateValidationScoreAsync(CourseEngagement engagement);
}

public class UserEngagementStats
{
    public Guid UserId { get; set; }
    public long TotalMinutes { get; set; }
    public int CoursesWatched { get; set; }
    public int LessonsCompleted { get; set; }
    public int QuizzesTaken { get; set; }
    public int AssignmentsSubmitted { get; set; }
    public DateTime? LastActivity { get; set; }
    public Dictionary<string, long> EngagementByType { get; set; } = new();
}

public class CourseEngagementStats
{
    public Guid CourseId { get; set; }
    public long TotalMinutes { get; set; }
    public int UniqueUsers { get; set; }
    public int TotalEngagements { get; set; }
    public double AverageEngagementMinutes { get; set; }
    public DateTime? LastEngagement { get; set; }
}

public class InstructorEngagementStats
{
    public Guid InstructorId { get; set; }
    public long TotalMinutes { get; set; }
    public int CoursesCount { get; set; }
    public int UniqueStudents { get; set; }
    public Dictionary<Guid, long> EngagementByCourse { get; set; } = new();
    public int Month { get; set; }
    public int Year { get; set; }
}
```

### 3.3 IPayoutCalculationService.cs
```csharp
using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

public interface IPayoutCalculationService
{
    // Calculate Payouts
    Task<List<InstructorPayout>> CalculateMonthlyPayoutsAsync(int year, int month);
    Task<InstructorPayout?> CalculateInstructorPayoutAsync(Guid instructorId, int year, int month);

    // Process Payouts
    Task<bool> ProcessPayoutAsync(Guid payoutId);
    Task<int> ProcessAllPendingPayoutsAsync(int year, int month);

    // Preview & Reporting
    Task<InstructorPayoutPreview> GetInstructorPayoutPreviewAsync(Guid instructorId);
    Task<List<InstructorPayout>> GetInstructorPayoutsAsync(Guid instructorId, int? year = null, int? month = null);
    Task<List<InstructorPayout>> GetPendingPayoutsAsync(int? year = null, int? month = null);

    // Platform Revenue
    Task<decimal> GetPlatformRevenueAsync(int year, int month);
    Task<decimal> GetInstructorShareRevenueAsync(int year, int month);
    Task<PlatformRevenueBreakdown> GetRevenueBreakdownAsync(int year, int month);
}

public class InstructorPayoutPreview
{
    public Guid InstructorId { get; set; }
    public int CurrentMonth { get; set; }
    public int CurrentYear { get; set; }
    public long TotalEngagementMinutes { get; set; }
    public long PlatformTotalEngagementMinutes { get; set; }
    public decimal EngagementPercentage { get; set; }
    public decimal EstimatedPayout { get; set; }
    public decimal CurrentPlatformRevenue { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class PlatformRevenueBreakdown
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal InstructorShare { get; set; }  // 80%
    public decimal PlatformFee { get; set; }      // 20%
    public int TotalSubscribers { get; set; }
    public int NewSubscribers { get; set; }
    public int CancelledSubscriptions { get; set; }
    public decimal MRR { get; set; }  // Monthly Recurring Revenue
    public decimal ChurnRate { get; set; }
}
```

### 3.4 IStripeConnectService.cs
```csharp
using InsightLearn.Core.Entities;

namespace InsightLearn.Core.Interfaces;

public interface IStripeConnectService
{
    // Account Management
    Task<InstructorConnectAccount> CreateConnectAccountAsync(Guid instructorId, string country = "IT");
    Task<string> GetOnboardingLinkAsync(Guid instructorId, string returnUrl, string refreshUrl);
    Task<InstructorConnectAccount?> GetConnectAccountAsync(Guid instructorId);
    Task<bool> IsConnectAccountActiveAsync(Guid instructorId);

    // Transfers
    Task<string> CreateTransferAsync(Guid instructorId, decimal amount, string currency, string description);
    Task<bool> VerifyTransferStatusAsync(string transferId);

    // Account Status
    Task UpdateConnectAccountStatusAsync(Guid instructorId);
}
```

---

## 4. API Endpoints Specification

### 4.1 Subscription Management

#### GET /api/subscriptions/plans
**Description**: Get all available subscription plans
**Auth**: Public
**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "name": "Basic",
      "slug": "basic",
      "description": "Unlimited access to all courses",
      "priceMonthly": 4.00,
      "priceYearly": 40.00,
      "yearlySavings": 8.00,
      "yearlySavingsPercentage": 16,
      "features": [
        "unlimited_courses",
        "certificates",
        "mobile_access"
      ],
      "maxConcurrentDevices": 2,
      "hasOfflineAccess": false,
      "hasPrioritySupport": false,
      "hasMentorship": false,
      "orderIndex": 0,
      "isActive": true
    }
  ]
}
```

#### POST /api/subscriptions/subscribe
**Description**: Create new subscription
**Auth**: Required
**Request**:
```json
{
  "planId": "guid",
  "billingCycle": "monthly",  // "monthly" or "yearly"
  "paymentMethodId": "pm_xxx",  // Stripe payment method ID
  "couponCode": "WELCOME20"  // Optional
}
```
**Response**:
```json
{
  "success": true,
  "data": {
    "subscriptionId": "guid",
    "status": "active",
    "currentPeriodEnd": "2025-02-10T00:00:00Z",
    "trialEnd": null,
    "plan": {
      "name": "Basic",
      "price": 4.00
    }
  }
}
```

#### GET /api/subscriptions/my-subscription
**Description**: Get current user's subscription
**Auth**: Required
**Response**:
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "userId": "guid",
    "plan": {
      "name": "Basic",
      "slug": "basic"
    },
    "status": "active",
    "billingCycle": "monthly",
    "currentPeriodStart": "2025-01-10T00:00:00Z",
    "currentPeriodEnd": "2025-02-10T00:00:00Z",
    "daysUntilRenewal": 30,
    "cancelAtPeriodEnd": false,
    "currentPrice": 4.00,
    "currency": "EUR"
  }
}
```

#### POST /api/subscriptions/cancel
**Description**: Cancel subscription
**Auth**: Required
**Request**:
```json
{
  "subscriptionId": "guid",
  "cancellationReason": "Too expensive",
  "cancelImmediately": false  // If false, cancels at period end
}
```
**Response**:
```json
{
  "success": true,
  "message": "Subscription will be cancelled on 2025-02-10"
}
```

#### POST /api/subscriptions/resume
**Description**: Resume cancelled subscription
**Auth**: Required
**Request**:
```json
{
  "subscriptionId": "guid"
}
```
**Response**:
```json
{
  "success": true,
  "message": "Subscription resumed successfully"
}
```

#### POST /api/subscriptions/upgrade
**Description**: Upgrade subscription plan
**Auth**: Required
**Request**:
```json
{
  "subscriptionId": "guid",
  "newPlanId": "guid"
}
```
**Response**:
```json
{
  "success": true,
  "data": {
    "subscriptionId": "guid",
    "newPlan": "Pro",
    "priceChange": 4.00,
    "effectiveDate": "2025-01-10T12:00:00Z"
  }
}
```

#### POST /api/subscriptions/create-checkout-session
**Description**: Create Stripe checkout session
**Auth**: Required
**Request**:
```json
{
  "planId": "guid",
  "billingCycle": "monthly",
  "successUrl": "https://app.insightlearn.cloud/subscription/success",
  "cancelUrl": "https://app.insightlearn.cloud/pricing"
}
```
**Response**:
```json
{
  "success": true,
  "data": {
    "sessionId": "cs_xxx",
    "url": "https://checkout.stripe.com/pay/cs_xxx"
  }
}
```

#### POST /api/subscriptions/create-portal-session
**Description**: Create Stripe customer portal session
**Auth**: Required
**Request**:
```json
{
  "returnUrl": "https://app.insightlearn.cloud/settings/subscription"
}
```
**Response**:
```json
{
  "success": true,
  "data": {
    "url": "https://billing.stripe.com/session/xxx"
  }
}
```

### 4.2 Engagement Tracking

#### POST /api/engagement/track
**Description**: Track engagement event
**Auth**: Required
**Request**:
```json
{
  "lessonId": "guid",
  "engagementType": "video_watch",
  "durationMinutes": 15,
  "sessionId": "sess_xxx",
  "metadata": {
    "video_progress": 95,
    "playback_speed": 1.5
  }
}
```
**Response**:
```json
{
  "success": true,
  "data": {
    "engagementId": "guid",
    "validated": true,
    "validationScore": 0.98
  }
}
```

#### POST /api/engagement/video-progress
**Description**: Update video watch progress
**Auth**: Required
**Request**:
```json
{
  "lessonId": "guid",
  "currentTime": 450,  // seconds
  "duration": 600,     // seconds
  "sessionId": "sess_xxx"
}
```
**Response**:
```json
{
  "success": true,
  "message": "Progress updated"
}
```

#### GET /api/engagement/my-stats
**Description**: Get user engagement statistics
**Auth**: Required
**Query Params**: `?startDate=2025-01-01&endDate=2025-01-31`
**Response**:
```json
{
  "success": true,
  "data": {
    "totalMinutes": 1250,
    "coursesWatched": 5,
    "lessonsCompleted": 42,
    "quizzesTaken": 8,
    "lastActivity": "2025-01-10T14:30:00Z",
    "engagementByType": {
      "video_watch": 1000,
      "quiz_attempt": 150,
      "reading": 100
    }
  }
}
```

### 4.3 Instructor Earnings

#### GET /api/instructor/earnings/preview
**Description**: Preview current month earnings
**Auth**: Required (Instructor role)
**Response**:
```json
{
  "success": true,
  "data": {
    "currentMonth": 1,
    "currentYear": 2025,
    "totalEngagementMinutes": 125000,
    "platformTotalEngagementMinutes": 1000000,
    "engagementPercentage": 0.125,
    "estimatedPayout": 5000.00,
    "currentPlatformRevenue": 50000.00,
    "calculatedAt": "2025-01-10T15:00:00Z"
  }
}
```

#### GET /api/instructor/payouts
**Description**: Get instructor payout history
**Auth**: Required (Instructor role)
**Query Params**: `?year=2024&month=12`
**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "period": "2024-12",
      "totalEngagementMinutes": 120000,
      "engagementPercentage": 0.12,
      "payoutAmount": 4800.00,
      "status": "paid",
      "paidAt": "2025-01-15T10:00:00Z",
      "currency": "EUR"
    }
  ]
}
```

#### GET /api/instructor/payouts/{id}
**Description**: Get payout details
**Auth**: Required (Instructor role)
**Response**:
```json
{
  "success": true,
  "data": {
    "id": "guid",
    "period": "2024-12",
    "totalEngagementMinutes": 120000,
    "platformTotalEngagementMinutes": 1000000,
    "engagementPercentage": 0.12,
    "totalPlatformRevenue": 50000.00,
    "instructorShareRevenue": 40000.00,
    "payoutAmount": 4800.00,
    "platformFeePercentage": 20.00,
    "platformFeeAmount": 800.00,
    "status": "paid",
    "stripeTransferId": "tr_xxx",
    "paidAt": "2025-01-15T10:00:00Z"
  }
}
```

#### POST /api/instructor/connect/onboard
**Description**: Create Stripe Connect onboarding link
**Auth**: Required (Instructor role)
**Request**:
```json
{
  "country": "IT",
  "returnUrl": "https://app.insightlearn.cloud/instructor/earnings",
  "refreshUrl": "https://app.insightlearn.cloud/instructor/connect"
}
```
**Response**:
```json
{
  "success": true,
  "data": {
    "onboardingUrl": "https://connect.stripe.com/express/onboarding/xxx"
  }
}
```

### 4.4 Admin Endpoints

#### POST /api/admin/payouts/calculate/{year}/{month}
**Description**: Calculate monthly payouts for all instructors
**Auth**: Required (Admin role)
**Response**:
```json
{
  "success": true,
  "data": {
    "totalPayouts": 45,
    "totalAmount": 32000.00,
    "calculatedAt": "2025-01-15T00:00:00Z"
  }
}
```

#### POST /api/admin/payouts/process/{id}
**Description**: Process a specific payout
**Auth**: Required (Admin role)
**Response**:
```json
{
  "success": true,
  "data": {
    "payoutId": "guid",
    "stripeTransferId": "tr_xxx",
    "status": "paid",
    "paidAt": "2025-01-15T10:30:00Z"
  }
}
```

#### GET /api/admin/payouts/pending
**Description**: Get all pending payouts
**Auth**: Required (Admin role)
**Query Params**: `?year=2024&month=12`
**Response**:
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "instructorId": "guid",
      "instructorName": "Jane Smith",
      "period": "2024-12",
      "payoutAmount": 4800.00,
      "status": "pending",
      "calculatedAt": "2025-01-01T00:00:00Z"
    }
  ]
}
```

#### GET /api/admin/engagement/course/{courseId}
**Description**: Get engagement stats for a course
**Auth**: Required (Admin role)
**Response**:
```json
{
  "success": true,
  "data": {
    "courseId": "guid",
    "courseName": "Python 101",
    "totalMinutes": 25000,
    "uniqueUsers": 350,
    "totalEngagements": 1250,
    "averageEngagementMinutes": 71.4
  }
}
```

#### GET /api/admin/engagement/monthly-summary
**Description**: Get monthly engagement summary
**Auth**: Required (Admin role)
**Query Params**: `?year=2024&month=12`
**Response**:
```json
{
  "success": true,
  "data": {
    "month": 12,
    "year": 2024,
    "platformTotalMinutes": 1000000,
    "totalUsers": 12500,
    "totalCourses": 450,
    "topInstructors": [
      {
        "instructorId": "guid",
        "name": "Jane Smith",
        "engagementMinutes": 125000,
        "percentage": 12.5
      }
    ]
  }
}
```

#### GET /api/admin/subscriptions/metrics
**Description**: Get subscription metrics
**Auth**: Required (Admin role)
**Query Params**: `?year=2024&month=12`
**Response**:
```json
{
  "success": true,
  "data": {
    "totalSubscribers": 12500,
    "activeSubscribers": 11800,
    "trialingSubscribers": 500,
    "cancelledSubscribers": 200,
    "mrr": 50000.00,
    "arr": 600000.00,
    "churnRate": 1.6,
    "newSubscribers": 850,
    "averageRevenuePerUser": 4.24,
    "planBreakdown": {
      "basic": 10000,
      "pro": 1500,
      "premium": 300
    }
  }
}
```

### 4.5 Webhook Endpoint

#### POST /api/webhooks/stripe
**Description**: Handle Stripe webhook events
**Auth**: Stripe signature verification
**Headers**: `Stripe-Signature`
**Events Handled**:
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `invoice.payment_succeeded`
- `invoice.payment_failed`
- `customer.subscription.trial_will_end`

**Response**:
```json
{
  "received": true
}
```

---

## 5. Database Migration Script

See file: `/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/docs/SAAS-MIGRATION-SCRIPT.sql`

---

## 6. Stripe Integration Guide

### 6.1 Stripe Setup

**Required Stripe Products**:

1. Create Products in Stripe Dashboard:
```bash
# Basic Plan
Product Name: InsightLearn Basic
Product ID: prod_basic_xxx

Price (Monthly): €4.00/month
Price ID: price_basic_monthly_xxx

Price (Yearly): €40.00/year
Price ID: price_basic_yearly_xxx
```

2. Enable Stripe Connect:
```bash
# In Stripe Dashboard:
Settings → Connect → Enable Express accounts
Webhook endpoint: https://api.insightlearn.cloud/api/webhooks/stripe
```

### 6.2 Backend Configuration

**appsettings.json**:
```json
{
  "Stripe": {
    "SecretKey": "sk_live_xxx",
    "PublishableKey": "pk_live_xxx",
    "WebhookSecret": "whsec_xxx",
    "Connect": {
      "ClientId": "ca_xxx"
    }
  },
  "Subscription": {
    "TrialDays": 7,
    "GracePeriodDays": 3,
    "PlatformFeePercentage": 20.0,
    "InstructorSharePercentage": 80.0,
    "MinimumPayoutAmount": 10.00
  }
}
```

### 6.3 Subscription Creation Flow

```csharp
// 1. Create Stripe Customer
var customer = await stripeCustomerService.CreateAsync(new CustomerCreateOptions
{
    Email = user.Email,
    Name = user.FullName,
    Metadata = new Dictionary<string, string>
    {
        { "userId", user.Id.ToString() }
    }
});

// 2. Attach Payment Method
await stripePaymentMethodService.AttachAsync(paymentMethodId, new PaymentMethodAttachOptions
{
    Customer = customer.Id
});

// 3. Set Default Payment Method
await stripeCustomerService.UpdateAsync(customer.Id, new CustomerUpdateOptions
{
    InvoiceSettings = new CustomerInvoiceSettingsOptions
    {
        DefaultPaymentMethod = paymentMethodId
    }
});

// 4. Create Subscription
var subscription = await stripeSubscriptionService.CreateAsync(new SubscriptionCreateOptions
{
    Customer = customer.Id,
    Items = new List<SubscriptionItemOptions>
    {
        new SubscriptionItemOptions
        {
            Price = plan.StripePriceIdMonthly  // or StripePriceIdYearly
        }
    },
    TrialPeriodDays = 7,
    Metadata = new Dictionary<string, string>
    {
        { "userId", user.Id.ToString() },
        { "planId", plan.Id.ToString() }
    }
});

// 5. Save to Database
var userSubscription = new UserSubscription
{
    UserId = user.Id,
    PlanId = plan.Id,
    Status = SubscriptionStatus.Trialing,
    BillingCycle = billingCycle,
    StripeSubscriptionId = subscription.Id,
    StripeCustomerId = customer.Id,
    CurrentPeriodStart = subscription.CurrentPeriodStart,
    CurrentPeriodEnd = subscription.CurrentPeriodEnd,
    TrialStart = subscription.TrialStart,
    TrialEnd = subscription.TrialEnd,
    CurrentPrice = plan.PriceMonthly
};

await dbContext.UserSubscriptions.AddAsync(userSubscription);
await dbContext.SaveChangesAsync();
```

### 6.4 Webhook Handling

```csharp
[HttpPost("api/webhooks/stripe")]
public async Task<IActionResult> HandleStripeWebhook()
{
    var json = await new StreamReader(Request.Body).ReadToEndAsync();
    var stripeSignature = Request.Headers["Stripe-Signature"];

    try
    {
        var stripeEvent = EventUtility.ConstructEvent(
            json,
            stripeSignature,
            webhookSecret
        );

        switch (stripeEvent.Type)
        {
            case Events.CustomerSubscriptionCreated:
                var createdSub = stripeEvent.Data.Object as Subscription;
                await HandleSubscriptionCreatedAsync(createdSub);
                break;

            case Events.CustomerSubscriptionUpdated:
                var updatedSub = stripeEvent.Data.Object as Subscription;
                await HandleSubscriptionUpdatedAsync(updatedSub);
                break;

            case Events.CustomerSubscriptionDeleted:
                var deletedSub = stripeEvent.Data.Object as Subscription;
                await HandleSubscriptionDeletedAsync(deletedSub);
                break;

            case Events.InvoicePaymentSucceeded:
                var invoice = stripeEvent.Data.Object as Invoice;
                await HandleInvoicePaymentSucceededAsync(invoice);
                break;

            case Events.InvoicePaymentFailed:
                var failedInvoice = stripeEvent.Data.Object as Invoice;
                await HandleInvoicePaymentFailedAsync(failedInvoice);
                break;
        }

        return Ok(new { received = true });
    }
    catch (StripeException ex)
    {
        logger.LogError(ex, "Stripe webhook error");
        return BadRequest();
    }
}
```

### 6.5 Instructor Payout Flow

```csharp
// 1. Create Stripe Connect Account
var account = await stripeAccountService.CreateAsync(new AccountCreateOptions
{
    Type = "express",
    Country = "IT",
    Email = instructor.Email,
    Capabilities = new AccountCapabilitiesOptions
    {
        Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
    },
    Metadata = new Dictionary<string, string>
    {
        { "instructorId", instructor.Id.ToString() }
    }
});

// 2. Create Onboarding Link
var accountLink = await stripeAccountLinkService.CreateAsync(new AccountLinkCreateOptions
{
    Account = account.Id,
    RefreshUrl = "https://app.insightlearn.cloud/instructor/connect",
    ReturnUrl = "https://app.insightlearn.cloud/instructor/earnings",
    Type = "account_onboarding"
});

// 3. Process Monthly Payout
var transfer = await stripeTransferService.CreateAsync(new TransferCreateOptions
{
    Amount = (long)(payout.PayoutAmount * 100),  // Convert to cents
    Currency = "eur",
    Destination = instructor.StripeConnectAccountId,
    Description = $"Payout for {payout.Period}",
    Metadata = new Dictionary<string, string>
    {
        { "payoutId", payout.Id.ToString() },
        { "period", payout.Period }
    }
});

// 4. Update Payout Status
payout.Status = PayoutStatus.Paid;
payout.StripeTransferId = transfer.Id;
payout.PaidAt = DateTime.UtcNow;
await dbContext.SaveChangesAsync();
```

---

## 7. Migration Strategy

### 7.1 Pre-Migration Checklist

- [ ] Backup production database
- [ ] Test migration script on staging environment
- [ ] Configure Stripe products and prices
- [ ] Set up Stripe Connect
- [ ] Update frontend pricing pages
- [ ] Prepare user communication (email templates)
- [ ] Set up monitoring and alerts

### 7.2 Migration Timeline

**Week 1: Preparation**
- Deploy new database schema
- Seed subscription plans
- Configure Stripe integration
- Deploy new API endpoints (disabled)

**Week 2: Beta Testing**
- Enable subscriptions for internal team
- Test end-to-end subscription flow
- Test engagement tracking
- Validate payout calculations

**Week 3: Soft Launch**
- Enable subscriptions for new users
- Grandfather existing users (see below)
- Monitor engagement tracking
- Daily payout calculation tests

**Week 4: Full Migration**
- Force migration for all users
- Disable pay-per-course option
- Monitor system performance
- Process first real instructor payouts

### 7.3 Existing User Migration

**Option 1: Free Trial for All Existing Users**
```sql
-- Give 30 days free trial to all existing users
DECLARE @BasicPlanId UNIQUEIDENTIFIER = (SELECT Id FROM SubscriptionPlans WHERE Slug = 'basic');

INSERT INTO UserSubscriptions (UserId, PlanId, Status, BillingCycle, CurrentPeriodStart, CurrentPeriodEnd, TrialStart, TrialEnd, CurrentPrice, Currency)
SELECT
    u.Id AS UserId,
    @BasicPlanId AS PlanId,
    'trialing' AS Status,
    'monthly' AS BillingCycle,
    GETUTCDATE() AS CurrentPeriodStart,
    DATEADD(MONTH, 1, GETUTCDATE()) AS CurrentPeriodEnd,
    GETUTCDATE() AS TrialStart,
    DATEADD(DAY, 30, GETUTCDATE()) AS TrialEnd,
    4.00 AS CurrentPrice,
    'EUR' AS Currency
FROM Users u
WHERE u.DateJoined < '2025-02-01'  -- Cutoff date
  AND NOT EXISTS (SELECT 1 FROM UserSubscriptions WHERE UserId = u.Id);

-- Update User table
UPDATE Users
SET SubscriptionStatus = 'trialing',
    CurrentSubscriptionId = (SELECT TOP 1 Id FROM UserSubscriptions WHERE UserId = Users.Id)
WHERE DateJoined < '2025-02-01';
```

**Option 2: Grandfather Based on Purchase History**
```sql
-- Users with 1+ courses: 3 months free
-- Users with 3+ courses: 6 months free
-- Users with 5+ courses: 1 year free

DECLARE @BasicPlanId UNIQUEIDENTIFIER = (SELECT Id FROM SubscriptionPlans WHERE Slug = 'basic');

-- 1+ courses: 3 months
INSERT INTO UserSubscriptions (UserId, PlanId, Status, BillingCycle, CurrentPeriodStart, CurrentPeriodEnd, TrialStart, TrialEnd, CurrentPrice, Currency)
SELECT
    e.UserId,
    @BasicPlanId,
    'trialing',
    'monthly',
    GETUTCDATE(),
    DATEADD(MONTH, 3, GETUTCDATE()),
    GETUTCDATE(),
    DATEADD(MONTH, 3, GETUTCDATE()),
    4.00,
    'EUR'
FROM (
    SELECT UserId, COUNT(DISTINCT CourseId) AS CourseCount
    FROM Enrollments
    WHERE AmountPaid > 0
    GROUP BY UserId
    HAVING COUNT(DISTINCT CourseId) = 1 OR COUNT(DISTINCT CourseId) = 2
) e
WHERE NOT EXISTS (SELECT 1 FROM UserSubscriptions WHERE UserId = e.UserId);

-- 3-4 courses: 6 months
INSERT INTO UserSubscriptions (UserId, PlanId, Status, BillingCycle, CurrentPeriodStart, CurrentPeriodEnd, TrialStart, TrialEnd, CurrentPrice, Currency)
SELECT
    e.UserId,
    @BasicPlanId,
    'trialing',
    'monthly',
    GETUTCDATE(),
    DATEADD(MONTH, 6, GETUTCDATE()),
    GETUTCDATE(),
    DATEADD(MONTH, 6, GETUTCDATE()),
    4.00,
    'EUR'
FROM (
    SELECT UserId, COUNT(DISTINCT CourseId) AS CourseCount
    FROM Enrollments
    WHERE AmountPaid > 0
    GROUP BY UserId
    HAVING COUNT(DISTINCT CourseId) BETWEEN 3 AND 4
) e
WHERE NOT EXISTS (SELECT 1 FROM UserSubscriptions WHERE UserId = e.UserId);

-- 5+ courses: 1 year free
INSERT INTO UserSubscriptions (UserId, PlanId, Status, BillingCycle, CurrentPeriodStart, CurrentPeriodEnd, TrialStart, TrialEnd, CurrentPrice, Currency)
SELECT
    e.UserId,
    @BasicPlanId,
    'trialing',
    'monthly',
    GETUTCDATE(),
    DATEADD(YEAR, 1, GETUTCDATE()),
    GETUTCDATE(),
    DATEADD(YEAR, 1, GETUTCDATE()),
    4.00,
    'EUR'
FROM (
    SELECT UserId, COUNT(DISTINCT CourseId) AS CourseCount
    FROM Enrollments
    WHERE AmountPaid > 0
    GROUP BY UserId
    HAVING COUNT(DISTINCT CourseId) >= 5
) e
WHERE NOT EXISTS (SELECT 1 FROM UserSubscriptions WHERE UserId = e.UserId);
```

### 7.4 Auto-Enrollment for Subscription Users

```sql
-- Create trigger to auto-enroll users with active subscriptions to ALL courses
CREATE TRIGGER TR_AutoEnrollSubscribers
ON Courses
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    -- Auto-enroll all active subscribers to new course
    INSERT INTO Enrollments (UserId, CourseId, EnrolledAt, Status, SubscriptionId, EnrolledViaSubscription, AutoEnrolled)
    SELECT
        us.UserId,
        i.Id AS CourseId,
        GETUTCDATE(),
        'Active',
        us.Id AS SubscriptionId,
        1 AS EnrolledViaSubscription,
        1 AS AutoEnrolled
    FROM inserted i
    CROSS JOIN UserSubscriptions us
    WHERE us.Status IN ('active', 'trialing')
      AND NOT EXISTS (
          SELECT 1 FROM Enrollments e
          WHERE e.UserId = us.UserId AND e.CourseId = i.Id
      );
END;
GO
```

---

## 8. Edge Cases & Considerations

### 8.1 Engagement Tracking Anti-Fraud

**Rules**:
1. **Max Daily Cap**: 8 hours (480 minutes) per user per day
2. **Tab Visibility**: Only count time when tab is active (Page Visibility API)
3. **Playback Speed Cap**: Max 2x speed, adjust engagement time accordingly
4. **Session Timeout**: Max 4 hours continuous session
5. **Quiz Time Cap**: Max 2x expected quiz duration
6. **Device Fingerprinting**: Detect bot/automated playback

**Implementation**:
```csharp
public async Task<decimal> CalculateValidationScoreAsync(CourseEngagement engagement)
{
    decimal score = 1.0m;

    // Check daily cap (8 hours)
    var dailyTotal = await GetUserDailyEngagementAsync(engagement.UserId, engagement.StartedAt.Date);
    if (dailyTotal > 480) score -= 0.5m;

    // Check session duration (max 4 hours)
    var sessionDuration = (engagement.CompletedAt - engagement.StartedAt)?.TotalMinutes ?? 0;
    if (sessionDuration > 240) score -= 0.3m;

    // Check playback speed
    var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(engagement.MetaData ?? "{}");
    if (metadata.ContainsKey("playback_speed") && double.Parse(metadata["playback_speed"].ToString()) > 2.0)
        score -= 0.2m;

    // Check device fingerprint (detect bots)
    if (string.IsNullOrEmpty(engagement.DeviceFingerprint)) score -= 0.1m;

    return Math.Max(0, score);
}
```

### 8.2 Payout Fairness

**Minimum Payout Threshold**: €10/month
- Instructors below threshold carry balance to next month
- Prevents excessive transaction fees

**Dispute Resolution**:
1. Instructor can view detailed engagement breakdown
2. Admin review process for disputed payouts
3. Manual adjustment capability with audit log

**Platform Sustainability**:
```
Platform Fee: 20%
Instructor Share: 80%

Example:
- Total Revenue: €50,000
- Platform Fee: €10,000 (infrastructure, support, marketing)
- Instructor Pool: €40,000 (distributed by engagement)
```

### 8.3 Subscription Lifecycle Edge Cases

**Trial Period**:
- 7 days free trial for new users
- Requires payment method upfront
- Auto-converts to paid subscription after trial

**Grace Period**:
- 3 days after failed payment
- User retains access during grace period
- Email notifications on days 1, 2, 3

**Paused Subscriptions**:
- Users can pause up to 3 months
- Retains enrollment data
- No charges during pause

**Refund Policy**:
- Pro-rata refund within 14 days
- Automatic calculation: `refund = (days_remaining / days_in_period) * price`

### 8.4 Data Retention

**Cancelled Subscriptions**:
- Retain subscription data for 2 years (GDPR compliance)
- Retain engagement data indefinitely (instructor payout verification)
- Anonymize user data after account deletion

**Enrollment Data**:
- Keep enrollments for cancelled subscriptions
- Mark as "inactive" but preserve progress
- Re-activate if user re-subscribes within 60 days

---

## 9. Performance Considerations

### 9.1 Database Indexes

**Critical indexes** (already included in schema):
```sql
-- Subscription lookups
CREATE INDEX IX_UserSubscriptions_UserId ON UserSubscriptions(UserId);
CREATE INDEX IX_UserSubscriptions_Status ON UserSubscriptions(Status);
CREATE INDEX IX_UserSubscriptions_CurrentPeriodEnd ON UserSubscriptions(CurrentPeriodEnd);

-- Engagement aggregation
CREATE INDEX IX_CourseEngagement_InstructorRevenue
ON CourseEngagement(CourseId, DurationMinutes) INCLUDE (IsValidated);

-- Payout calculations
CREATE INDEX IX_InstructorPayouts_Period ON InstructorPayouts(Year, Month);
```

### 9.2 Caching Strategy

**Redis Cache**:
```csharp
// Cache user subscription status (5 min TTL)
var cacheKey = $"subscription:user:{userId}";
var subscription = await cache.GetOrSetAsync(cacheKey,
    async () => await GetUserSubscriptionAsync(userId),
    TimeSpan.FromMinutes(5)
);

// Cache monthly engagement stats (1 hour TTL)
var engagementKey = $"engagement:instructor:{instructorId}:{year}-{month}";
var stats = await cache.GetOrSetAsync(engagementKey,
    async () => await CalculateInstructorEngagementAsync(instructorId, year, month),
    TimeSpan.FromHours(1)
);
```

### 9.3 Scalability

**Horizontal Scaling**:
- API layer: Stateless, scale with load balancer
- Background jobs: Separate worker pods for payout calculations
- Database: Read replicas for reporting queries

**Background Jobs**:
```csharp
// Daily job: Calculate engagement stats
services.AddHostedService<DailyEngagementAggregationService>();

// Monthly job: Calculate instructor payouts (1st of month)
services.AddHostedService<MonthlyPayoutCalculationService>();

// Hourly job: Validate engagement data
services.AddHostedService<EngagementValidationService>();
```

---

## 10. Monitoring & Metrics

### 10.1 Key Metrics to Track

**Business Metrics**:
- MRR (Monthly Recurring Revenue)
- ARR (Annual Recurring Revenue)
- Churn Rate
- Customer Lifetime Value (CLV)
- Subscriber Growth Rate
- Trial-to-Paid Conversion Rate

**Engagement Metrics**:
- Platform-wide engagement minutes/month
- Average engagement per user
- Top 10 courses by engagement
- Engagement by day of week/hour

**Instructor Metrics**:
- Average payout per instructor
- Engagement distribution (top 10% vs bottom 10%)
- Instructor retention rate
- New instructor onboarding rate

### 10.2 Grafana Dashboards

**Subscription Dashboard**:
```json
{
  "panels": [
    {
      "title": "MRR Trend",
      "query": "SELECT DATE_TRUNC('month', PaidAt) AS month, SUM(Amount) AS mrr FROM SubscriptionRevenue WHERE Status = 'paid' GROUP BY month"
    },
    {
      "title": "Active Subscribers",
      "query": "SELECT COUNT(*) FROM UserSubscriptions WHERE Status IN ('active', 'trialing')"
    },
    {
      "title": "Churn Rate",
      "query": "SELECT (COUNT(CASE WHEN Status = 'cancelled' THEN 1 END) * 100.0 / COUNT(*)) AS churn_rate FROM UserSubscriptions"
    }
  ]
}
```

---

## 11. Testing Strategy

### 11.1 Unit Tests

```csharp
[Fact]
public async Task CalculateInstructorPayout_ShouldReturnCorrectAmount()
{
    // Arrange
    var instructorEngagement = 120000; // 120k minutes
    var platformEngagement = 1000000; // 1M minutes
    var platformRevenue = 50000m;

    // Act
    var payout = await payoutService.CalculateInstructorPayoutAsync(instructorId, 2024, 12);

    // Assert
    var expectedPayout = (platformRevenue * 0.80m) * (120000m / 1000000m);
    Assert.Equal(4800m, payout.PayoutAmount);
}

[Fact]
public async Task ValidateEngagement_ShouldRejectExcessive DailyUsage()
{
    // Arrange
    var engagement = new CourseEngagement
    {
        UserId = userId,
        DurationMinutes = 600 // 10 hours
    };

    // Act
    var isValid = await engagementService.ValidateEngagementAsync(engagement);

    // Assert
    Assert.False(isValid);
}
```

### 11.2 Integration Tests

```csharp
[Fact]
public async Task SubscriptionFlow_EndToEnd_ShouldWork()
{
    // 1. Create subscription
    var subscription = await subscriptionService.CreateSubscriptionAsync(userId, basicPlanId, "monthly");
    Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);

    // 2. Track engagement
    var engagement = await engagementService.TrackVideoWatchAsync(userId, lessonId, 30);
    Assert.True(engagement.IsValidated);

    // 3. Calculate payout
    var payout = await payoutService.CalculateInstructorPayoutAsync(instructorId, 2025, 1);
    Assert.True(payout.PayoutAmount > 0);

    // 4. Process payout
    var processed = await payoutService.ProcessPayoutAsync(payout.Id);
    Assert.True(processed);
    Assert.Equal(PayoutStatus.Paid, payout.Status);
}
```

---

## 12. Rollback Plan

### 12.1 Emergency Rollback Steps

If critical issues arise during migration:

1. **Disable new subscriptions**:
```sql
UPDATE SubscriptionPlans SET IsActive = 0;
```

2. **Revert to pay-per-course**:
```sql
UPDATE Courses SET IsSubscriptionOnly = 0, Price = LegacyPrice;
```

3. **Preserve user access**:
```sql
-- Keep existing enrollments active
-- No need to revert enrollments
```

4. **Database rollback** (if needed):
```bash
# Restore from backup
pg_restore -d insightlearn_db backup_20250110.dump
```

### 12.2 Partial Rollback

If only specific components fail:

- **Engagement tracking fails**: Continue subscriptions, manual payout calculation
- **Stripe Connect fails**: Process payouts via manual bank transfer
- **Webhook failures**: Manually sync subscription status from Stripe dashboard

---

## Summary

This architecture provides a complete, scalable solution for transitioning InsightLearn to a SaaS subscription model with:

1. **Fair instructor compensation** based on actual engagement time
2. **Low barrier to entry** for users (€4/month vs €49.99 per course)
3. **Predictable recurring revenue** for platform sustainability
4. **Anti-fraud mechanisms** to ensure accurate engagement tracking
5. **Seamless Stripe integration** for payments and payouts
6. **Comprehensive admin tools** for monitoring and management

**Next Steps**:
1. Review and approve architecture
2. Create database migration scripts
3. Implement service layer
4. Build API endpoints
5. Update frontend UI
6. Configure Stripe
7. Test on staging
8. Execute migration plan

**Files Created**:
- `/docs/SAAS-SUBSCRIPTION-ARCHITECTURE.md` (this file)
- `/docs/SAAS-MIGRATION-SCRIPT.sql` (next file)
- `/docs/SAAS-FRONTEND-MOCKUPS.md` (next file)
