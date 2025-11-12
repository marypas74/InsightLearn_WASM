-- =============================================================================
-- InsightLearn SaaS Subscription Model - Database Migration Script
-- Version: 2.0.0
-- Date: 2025-01-10
-- Description: Complete migration from pay-per-course to subscription model
-- =============================================================================

-- =============================================================================
-- SECTION 1: BACKUP EXISTING DATA
-- =============================================================================

-- Create backup tables (optional, for safety)
SELECT * INTO Courses_Backup_20250110 FROM Courses;
SELECT * INTO Enrollments_Backup_20250110 FROM Enrollments;
SELECT * INTO Users_Backup_20250110 FROM Users;
SELECT * INTO Payments_Backup_20250110 FROM Payments;

PRINT 'Backup tables created successfully';

-- =============================================================================
-- SECTION 2: CREATE NEW TABLES
-- =============================================================================

-- SubscriptionPlans table
CREATE TABLE SubscriptionPlans (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL,
    Slug NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(500),
    PriceMonthly DECIMAL(10,2) NOT NULL,
    PriceYearly DECIMAL(10,2) NULL,
    StripePriceIdMonthly NVARCHAR(255) NULL,
    StripePriceIdYearly NVARCHAR(255) NULL,
    Features NVARCHAR(MAX),
    MaxConcurrentDevices INT DEFAULT 2,
    HasOfflineAccess BIT DEFAULT 0,
    HasPrioritySupport BIT DEFAULT 0,
    HasMentorship BIT DEFAULT 0,
    HasCertificates BIT DEFAULT 1,
    OrderIndex INT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),

    INDEX IX_SubscriptionPlans_Slug (Slug),
    INDEX IX_SubscriptionPlans_IsActive (IsActive)
);

PRINT 'SubscriptionPlans table created';

-- UserSubscriptions table
CREATE TABLE UserSubscriptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    PlanId UNIQUEIDENTIFIER NOT NULL,
    Status NVARCHAR(50) NOT NULL,
    BillingCycle NVARCHAR(20) NOT NULL,

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
    CancelAtPeriodEnd BIT DEFAULT 0,
    CancelledAt DATETIME2 NULL,
    CancellationReason NVARCHAR(500) NULL,

    -- Pricing
    CurrentPrice DECIMAL(10,2) NOT NULL,
    Currency NVARCHAR(3) DEFAULT 'EUR',

    -- Metadata
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),

    CONSTRAINT FK_UserSubscriptions_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserSubscriptions_SubscriptionPlans FOREIGN KEY (PlanId) REFERENCES SubscriptionPlans(Id),

    INDEX IX_UserSubscriptions_UserId (UserId),
    INDEX IX_UserSubscriptions_Status (Status),
    INDEX IX_UserSubscriptions_StripeSubscriptionId (StripeSubscriptionId),
    INDEX IX_UserSubscriptions_CurrentPeriodEnd (CurrentPeriodEnd)
);

PRINT 'UserSubscriptions table created';

-- CourseEngagement table
CREATE TABLE CourseEngagement (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    CourseId UNIQUEIDENTIFIER NOT NULL,
    LessonId UNIQUEIDENTIFIER NULL,
    SectionId UNIQUEIDENTIFIER NULL,

    -- Engagement Type and Duration
    EngagementType NVARCHAR(50) NOT NULL,
    DurationMinutes INT NOT NULL,

    -- Session Tracking
    SessionId NVARCHAR(255) NULL,
    StartedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2 NULL,

    -- Validation & Anti-Fraud
    IsValidated BIT DEFAULT 0,
    ValidationScore DECIMAL(3,2) NULL,
    DeviceFingerprint NVARCHAR(500) NULL,
    IpAddress NVARCHAR(45) NULL,
    UserAgent NVARCHAR(500) NULL,

    -- Metadata (JSON)
    MetaData NVARCHAR(MAX),

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
    INDEX IX_CourseEngagement_InstructorRevenue (CourseId, DurationMinutes) INCLUDE (IsValidated),
    INDEX IX_CourseEngagement_SessionId (SessionId)
);

PRINT 'CourseEngagement table created';

-- InstructorPayouts table
CREATE TABLE InstructorPayouts (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    InstructorId UNIQUEIDENTIFIER NOT NULL,

    -- Payout Period
    Month INT NOT NULL,
    Year INT NOT NULL,

    -- Engagement Metrics
    TotalEngagementMinutes BIGINT NOT NULL,
    PlatformTotalEngagementMinutes BIGINT NOT NULL,
    EngagementPercentage DECIMAL(10,8) NOT NULL,

    -- Revenue Calculation
    TotalPlatformRevenue DECIMAL(12,2) NOT NULL,
    InstructorShareRevenue DECIMAL(12,2) NOT NULL,
    PayoutAmount DECIMAL(12,2) NOT NULL,

    -- Platform Fee
    PlatformFeePercentage DECIMAL(5,2) DEFAULT 20.00,
    PlatformFeeAmount DECIMAL(12,2) NOT NULL,

    -- Status & Processing
    Status NVARCHAR(50) NOT NULL,

    -- Stripe Connect Integration
    StripeTransferId NVARCHAR(255) NULL,
    StripeConnectAccountId NVARCHAR(255) NULL,

    -- Payment Details
    Currency NVARCHAR(3) DEFAULT 'EUR',
    PaymentMethod NVARCHAR(50) NULL,

    -- Timestamps
    CalculatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ProcessedAt DATETIME2 NULL,
    PaidAt DATETIME2 NULL,

    -- Notes & Metadata
    Notes NVARCHAR(MAX),
    MetaData NVARCHAR(MAX),

    CONSTRAINT FK_InstructorPayouts_Users FOREIGN KEY (InstructorId) REFERENCES Users(Id),

    INDEX IX_InstructorPayouts_InstructorId (InstructorId),
    INDEX IX_InstructorPayouts_Period (Year, Month),
    INDEX IX_InstructorPayouts_Status (Status),
    UNIQUE INDEX UX_InstructorPayouts_Period (InstructorId, Year, Month)
);

PRINT 'InstructorPayouts table created';

-- SubscriptionRevenue table
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
    Status NVARCHAR(50) NOT NULL,

    -- Refunds
    RefundAmount DECIMAL(10,2) NULL,
    RefundedAt DATETIME2 NULL,
    RefundReason NVARCHAR(500) NULL,

    -- Timestamps
    PaidAt DATETIME2 NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    -- Metadata
    MetaData NVARCHAR(MAX),

    CONSTRAINT FK_SubscriptionRevenue_UserSubscriptions FOREIGN KEY (SubscriptionId) REFERENCES UserSubscriptions(Id) ON DELETE CASCADE,

    INDEX IX_SubscriptionRevenue_SubscriptionId (SubscriptionId),
    INDEX IX_SubscriptionRevenue_BillingPeriod (BillingPeriodStart),
    INDEX IX_SubscriptionRevenue_Status (Status),
    INDEX IX_SubscriptionRevenue_PaidAt (PaidAt)
);

PRINT 'SubscriptionRevenue table created';

-- SubscriptionEvents table
CREATE TABLE SubscriptionEvents (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SubscriptionId UNIQUEIDENTIFIER NOT NULL,
    EventType NVARCHAR(100) NOT NULL,
    EventData NVARCHAR(MAX),
    StripeEventId NVARCHAR(255) NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),

    CONSTRAINT FK_SubscriptionEvents_UserSubscriptions FOREIGN KEY (SubscriptionId) REFERENCES UserSubscriptions(Id) ON DELETE CASCADE,

    INDEX IX_SubscriptionEvents_SubscriptionId (SubscriptionId),
    INDEX IX_SubscriptionEvents_EventType (EventType),
    INDEX IX_SubscriptionEvents_CreatedAt (CreatedAt)
);

PRINT 'SubscriptionEvents table created';

-- InstructorConnectAccounts table
CREATE TABLE InstructorConnectAccounts (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    InstructorId UNIQUEIDENTIFIER NOT NULL UNIQUE,

    -- Stripe Connect Details
    StripeConnectAccountId NVARCHAR(255) NOT NULL UNIQUE,
    AccountType NVARCHAR(50) DEFAULT 'express',

    -- Status
    ChargesEnabled BIT DEFAULT 0,
    PayoutsEnabled BIT DEFAULT 0,
    DetailsSubmitted BIT DEFAULT 0,

    -- Country & Currency
    Country NVARCHAR(2) NOT NULL,
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

PRINT 'InstructorConnectAccounts table created';

-- =============================================================================
-- SECTION 3: MODIFY EXISTING TABLES
-- =============================================================================

-- Courses: Add subscription-only flag and preserve legacy price
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Courses') AND name = 'IsSubscriptionOnly')
BEGIN
    ALTER TABLE Courses ADD IsSubscriptionOnly BIT DEFAULT 1;
    PRINT 'Added IsSubscriptionOnly to Courses';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Courses') AND name = 'LegacyPrice')
BEGIN
    ALTER TABLE Courses ADD LegacyPrice DECIMAL(10,2) NULL;
    PRINT 'Added LegacyPrice to Courses';
END

-- Update existing courses
UPDATE Courses SET IsSubscriptionOnly = 1, LegacyPrice = Price;
PRINT 'Updated existing courses with legacy prices';

-- Enrollments: Add subscription tracking
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Enrollments') AND name = 'SubscriptionId')
BEGIN
    ALTER TABLE Enrollments ADD SubscriptionId UNIQUEIDENTIFIER NULL;
    PRINT 'Added SubscriptionId to Enrollments';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Enrollments') AND name = 'EnrolledViaSubscription')
BEGIN
    ALTER TABLE Enrollments ADD EnrolledViaSubscription BIT DEFAULT 0;
    PRINT 'Added EnrolledViaSubscription to Enrollments';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Enrollments') AND name = 'AutoEnrolled')
BEGIN
    ALTER TABLE Enrollments ADD AutoEnrolled BIT DEFAULT 0;
    PRINT 'Added AutoEnrolled to Enrollments';
END

-- Add foreign key for SubscriptionId
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Enrollments_UserSubscriptions')
BEGIN
    ALTER TABLE Enrollments ADD CONSTRAINT FK_Enrollments_UserSubscriptions
        FOREIGN KEY (SubscriptionId) REFERENCES UserSubscriptions(Id);
    PRINT 'Added FK_Enrollments_UserSubscriptions';
END

-- Add index
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Enrollments_SubscriptionId')
BEGIN
    CREATE INDEX IX_Enrollments_SubscriptionId ON Enrollments(SubscriptionId);
    PRINT 'Added IX_Enrollments_SubscriptionId index';
END

-- Users: Add Stripe customer tracking
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'StripeCustomerId')
BEGIN
    ALTER TABLE Users ADD StripeCustomerId NVARCHAR(255) NULL;
    PRINT 'Added StripeCustomerId to Users';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'SubscriptionStatus')
BEGIN
    ALTER TABLE Users ADD SubscriptionStatus NVARCHAR(50) DEFAULT 'none';
    PRINT 'Added SubscriptionStatus to Users';
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'CurrentSubscriptionId')
BEGIN
    ALTER TABLE Users ADD CurrentSubscriptionId UNIQUEIDENTIFIER NULL;
    PRINT 'Added CurrentSubscriptionId to Users';
END

-- Add indexes
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_StripeCustomerId')
BEGIN
    CREATE INDEX IX_Users_StripeCustomerId ON Users(StripeCustomerId);
    PRINT 'Added IX_Users_StripeCustomerId index';
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_SubscriptionStatus')
BEGIN
    CREATE INDEX IX_Users_SubscriptionStatus ON Users(SubscriptionStatus);
    PRINT 'Added IX_Users_SubscriptionStatus index';
END

-- Add foreign key for CurrentSubscriptionId (nullable)
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_CurrentSubscription')
BEGIN
    ALTER TABLE Users ADD CONSTRAINT FK_Users_CurrentSubscription
        FOREIGN KEY (CurrentSubscriptionId) REFERENCES UserSubscriptions(Id);
    PRINT 'Added FK_Users_CurrentSubscription';
END

-- =============================================================================
-- SECTION 4: SEED SUBSCRIPTION PLANS
-- =============================================================================

-- Insert default subscription plans
DECLARE @BasicPlanId UNIQUEIDENTIFIER = NEWID();
DECLARE @ProPlanId UNIQUEIDENTIFIER = NEWID();
DECLARE @PremiumPlanId UNIQUEIDENTIFIER = NEWID();

INSERT INTO SubscriptionPlans (Id, Name, Slug, Description, PriceMonthly, PriceYearly, Features, MaxConcurrentDevices, HasOfflineAccess, HasPrioritySupport, HasMentorship, HasCertificates, OrderIndex, IsActive)
VALUES
(
    @BasicPlanId,
    'Basic',
    'basic',
    'Unlimited access to all courses with basic features',
    4.00,
    40.00,  -- 16% savings (2 months free)
    '["unlimited_courses", "certificates", "mobile_access", "community_access"]',
    2,      -- Max 2 devices
    0,      -- No offline access
    0,      -- No priority support
    0,      -- No mentorship
    1,      -- Has certificates
    0,      -- Order index
    1       -- Active
),
(
    @ProPlanId,
    'Pro',
    'pro',
    'All Basic features plus offline downloads and priority support',
    8.00,
    80.00,  -- 16% savings (2 months free)
    '["unlimited_courses", "certificates", "mobile_access", "community_access", "offline_downloads", "priority_support", "early_access"]',
    5,      -- Max 5 devices
    1,      -- Offline access
    1,      -- Priority support
    0,      -- No mentorship
    1,      -- Has certificates
    1,      -- Order index
    1       -- Active
),
(
    @PremiumPlanId,
    'Premium',
    'premium',
    'All Pro features plus 1-on-1 mentorship and exclusive content',
    12.00,
    120.00, -- 16% savings (2 months free)
    '["unlimited_courses", "certificates", "mobile_access", "community_access", "offline_downloads", "priority_support", "early_access", "one_on_one_mentorship", "exclusive_content", "career_coaching"]',
    10,     -- Max 10 devices
    1,      -- Offline access
    1,      -- Priority support
    1,      -- Mentorship
    1,      -- Has certificates
    2,      -- Order index
    1       -- Active
);

PRINT 'Subscription plans seeded successfully';

-- =============================================================================
-- SECTION 5: MIGRATE EXISTING USERS (GRANDFATHER STRATEGY)
-- =============================================================================

-- Strategy: Give existing users free trial based on purchase history
-- 1+ courses: 3 months free
-- 3+ courses: 6 months free
-- 5+ courses: 1 year free

-- Users with 5+ courses: 1 year free Basic
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

PRINT 'Created 1-year free subscriptions for users with 5+ courses';

-- Users with 3-4 courses: 6 months free Basic
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

PRINT 'Created 6-month free subscriptions for users with 3-4 courses';

-- Users with 1-2 courses: 3 months free Basic
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
    HAVING COUNT(DISTINCT CourseId) BETWEEN 1 AND 2
) e
WHERE NOT EXISTS (SELECT 1 FROM UserSubscriptions WHERE UserId = e.UserId);

PRINT 'Created 3-month free subscriptions for users with 1-2 courses';

-- Update Users table with subscription status
UPDATE u
SET u.SubscriptionStatus = us.Status,
    u.CurrentSubscriptionId = us.Id
FROM Users u
INNER JOIN UserSubscriptions us ON u.Id = us.UserId
WHERE us.Status IN ('active', 'trialing');

PRINT 'Updated Users table with subscription status';

-- Update existing enrollments to link to subscriptions
UPDATE e
SET e.SubscriptionId = us.Id,
    e.EnrolledViaSubscription = 1,
    e.AutoEnrolled = 0  -- These are existing enrollments, not auto-enrolled
FROM Enrollments e
INNER JOIN UserSubscriptions us ON e.UserId = us.UserId
WHERE e.SubscriptionId IS NULL
  AND us.Status IN ('active', 'trialing');

PRINT 'Linked existing enrollments to subscriptions';

-- =============================================================================
-- SECTION 6: CREATE TRIGGERS
-- =============================================================================

-- Trigger: Auto-enroll users with active subscriptions to new courses
IF OBJECT_ID('TR_AutoEnrollSubscribers', 'TR') IS NOT NULL
    DROP TRIGGER TR_AutoEnrollSubscribers;
GO

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
      AND i.IsActive = 1
      AND i.IsSubscriptionOnly = 1
      AND NOT EXISTS (
          SELECT 1 FROM Enrollments e
          WHERE e.UserId = us.UserId AND e.CourseId = i.Id
      );

    PRINT 'Auto-enrolled active subscribers to new courses';
END;
GO

PRINT 'Trigger TR_AutoEnrollSubscribers created';

-- Trigger: Update Enrollments when subscription status changes
IF OBJECT_ID('TR_UpdateEnrollmentsOnSubscriptionChange', 'TR') IS NOT NULL
    DROP TRIGGER TR_UpdateEnrollmentsOnSubscriptionChange;
GO

CREATE TRIGGER TR_UpdateEnrollmentsOnSubscriptionChange
ON UserSubscriptions
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Suspend enrollments if subscription is cancelled/expired
    UPDATE e
    SET e.Status = 'Suspended'
    FROM Enrollments e
    INNER JOIN deleted d ON e.SubscriptionId = d.Id
    INNER JOIN inserted i ON e.SubscriptionId = i.Id
    WHERE i.Status IN ('cancelled', 'expired', 'past_due')
      AND d.Status IN ('active', 'trialing')
      AND e.EnrolledViaSubscription = 1;

    -- Reactivate enrollments if subscription is resumed
    UPDATE e
    SET e.Status = 'Active'
    FROM Enrollments e
    INNER JOIN deleted d ON e.SubscriptionId = d.Id
    INNER JOIN inserted i ON e.SubscriptionId = i.Id
    WHERE i.Status IN ('active', 'trialing')
      AND d.Status IN ('cancelled', 'expired', 'past_due')
      AND e.EnrolledViaSubscription = 1;

    PRINT 'Updated enrollments based on subscription status changes';
END;
GO

PRINT 'Trigger TR_UpdateEnrollmentsOnSubscriptionChange created';

-- =============================================================================
-- SECTION 7: CREATE VIEWS (REPORTING)
-- =============================================================================

-- View: Active Subscriptions Summary
IF OBJECT_ID('vw_ActiveSubscriptionsSummary', 'V') IS NOT NULL
    DROP VIEW vw_ActiveSubscriptionsSummary;
GO

CREATE VIEW vw_ActiveSubscriptionsSummary
AS
SELECT
    sp.Name AS PlanName,
    sp.Slug AS PlanSlug,
    COUNT(*) AS ActiveSubscribers,
    SUM(us.CurrentPrice) AS MonthlyRevenue,
    AVG(us.CurrentPrice) AS AveragePrice
FROM UserSubscriptions us
INNER JOIN SubscriptionPlans sp ON us.PlanId = sp.Id
WHERE us.Status IN ('active', 'trialing')
GROUP BY sp.Name, sp.Slug;
GO

PRINT 'View vw_ActiveSubscriptionsSummary created';

-- View: Instructor Engagement Summary
IF OBJECT_ID('vw_InstructorEngagementSummary', 'V') IS NOT NULL
    DROP VIEW vw_InstructorEngagementSummary;
GO

CREATE VIEW vw_InstructorEngagementSummary
AS
SELECT
    c.InstructorId,
    u.FirstName + ' ' + u.LastName AS InstructorName,
    COUNT(DISTINCT ce.UserId) AS UniqueStudents,
    COUNT(DISTINCT ce.CourseId) AS CoursesWithEngagement,
    SUM(ce.DurationMinutes) AS TotalEngagementMinutes,
    AVG(ce.DurationMinutes) AS AverageEngagementMinutes,
    MAX(ce.StartedAt) AS LastEngagement
FROM CourseEngagement ce
INNER JOIN Courses c ON ce.CourseId = c.Id
INNER JOIN Users u ON c.InstructorId = u.Id
WHERE ce.IsValidated = 1
GROUP BY c.InstructorId, u.FirstName, u.LastName;
GO

PRINT 'View vw_InstructorEngagementSummary created';

-- View: Monthly Revenue Breakdown
IF OBJECT_ID('vw_MonthlyRevenueBreakdown', 'V') IS NOT NULL
    DROP VIEW vw_MonthlyRevenueBreakdown;
GO

CREATE VIEW vw_MonthlyRevenueBreakdown
AS
SELECT
    YEAR(sr.PaidAt) AS Year,
    MONTH(sr.PaidAt) AS Month,
    COUNT(DISTINCT sr.SubscriptionId) AS PayingSubscribers,
    SUM(sr.Amount) AS TotalRevenue,
    SUM(sr.Amount) * 0.80 AS InstructorShareRevenue,
    SUM(sr.Amount) * 0.20 AS PlatformFeeRevenue,
    AVG(sr.Amount) AS AverageRevenuePerSubscription
FROM SubscriptionRevenue sr
WHERE sr.Status = 'paid'
  AND sr.PaidAt IS NOT NULL
GROUP BY YEAR(sr.PaidAt), MONTH(sr.PaidAt);
GO

PRINT 'View vw_MonthlyRevenueBreakdown created';

-- =============================================================================
-- SECTION 8: CREATE STORED PROCEDURES
-- =============================================================================

-- Stored Procedure: Calculate Monthly Instructor Payouts
IF OBJECT_ID('sp_CalculateMonthlyPayouts', 'P') IS NOT NULL
    DROP PROCEDURE sp_CalculateMonthlyPayouts;
GO

CREATE PROCEDURE sp_CalculateMonthlyPayouts
    @Year INT,
    @Month INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Calculate platform total engagement for the month
    DECLARE @PlatformTotalEngagement BIGINT;

    SELECT @PlatformTotalEngagement = ISNULL(SUM(DurationMinutes), 0)
    FROM CourseEngagement
    WHERE IsValidated = 1
      AND YEAR(StartedAt) = @Year
      AND MONTH(StartedAt) = @Month;

    -- Calculate platform total revenue for the month
    DECLARE @PlatformTotalRevenue DECIMAL(12,2);

    SELECT @PlatformTotalRevenue = ISNULL(SUM(Amount), 0)
    FROM SubscriptionRevenue
    WHERE Status = 'paid'
      AND YEAR(BillingPeriodStart) = @Year
      AND MONTH(BillingPeriodStart) = @Month;

    -- Calculate instructor share (80%)
    DECLARE @InstructorShareRevenue DECIMAL(12,2) = @PlatformTotalRevenue * 0.80;

    -- Delete existing pending payouts for this period
    DELETE FROM InstructorPayouts
    WHERE Year = @Year
      AND Month = @Month
      AND Status = 'pending';

    -- Calculate and insert payouts for each instructor
    INSERT INTO InstructorPayouts (
        InstructorId,
        Month,
        Year,
        TotalEngagementMinutes,
        PlatformTotalEngagementMinutes,
        EngagementPercentage,
        TotalPlatformRevenue,
        InstructorShareRevenue,
        PayoutAmount,
        PlatformFeePercentage,
        PlatformFeeAmount,
        Status,
        Currency,
        CalculatedAt
    )
    SELECT
        c.InstructorId,
        @Month,
        @Year,
        SUM(ce.DurationMinutes) AS TotalEngagementMinutes,
        @PlatformTotalEngagement AS PlatformTotalEngagementMinutes,
        CASE
            WHEN @PlatformTotalEngagement > 0
            THEN CAST(SUM(ce.DurationMinutes) AS DECIMAL(18,8)) / @PlatformTotalEngagement
            ELSE 0
        END AS EngagementPercentage,
        @PlatformTotalRevenue AS TotalPlatformRevenue,
        @InstructorShareRevenue AS InstructorShareRevenue,
        CASE
            WHEN @PlatformTotalEngagement > 0
            THEN @InstructorShareRevenue * (CAST(SUM(ce.DurationMinutes) AS DECIMAL(18,8)) / @PlatformTotalEngagement)
            ELSE 0
        END AS PayoutAmount,
        20.00 AS PlatformFeePercentage,
        @PlatformTotalRevenue * 0.20 AS PlatformFeeAmount,
        'pending' AS Status,
        'EUR' AS Currency,
        GETUTCDATE() AS CalculatedAt
    FROM CourseEngagement ce
    INNER JOIN Courses c ON ce.CourseId = c.Id
    WHERE ce.IsValidated = 1
      AND YEAR(ce.StartedAt) = @Year
      AND MONTH(ce.StartedAt) = @Month
    GROUP BY c.InstructorId
    HAVING SUM(ce.DurationMinutes) > 0;

    -- Return summary
    SELECT
        COUNT(*) AS TotalInstructors,
        SUM(PayoutAmount) AS TotalPayoutAmount,
        MIN(PayoutAmount) AS MinPayout,
        MAX(PayoutAmount) AS MaxPayout,
        AVG(PayoutAmount) AS AvgPayout
    FROM InstructorPayouts
    WHERE Year = @Year AND Month = @Month AND Status = 'pending';
END;
GO

PRINT 'Stored Procedure sp_CalculateMonthlyPayouts created';

-- Stored Procedure: Get User Engagement Stats
IF OBJECT_ID('sp_GetUserEngagementStats', 'P') IS NOT NULL
    DROP PROCEDURE sp_GetUserEngagementStats;
GO

CREATE PROCEDURE sp_GetUserEngagementStats
    @UserId UNIQUEIDENTIFIER,
    @StartDate DATETIME2 = NULL,
    @EndDate DATETIME2 = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Set default dates if not provided
    IF @StartDate IS NULL SET @StartDate = DATEADD(MONTH, -1, GETUTCDATE());
    IF @EndDate IS NULL SET @EndDate = GETUTCDATE();

    SELECT
        @UserId AS UserId,
        SUM(DurationMinutes) AS TotalMinutes,
        COUNT(DISTINCT CourseId) AS CoursesWatched,
        COUNT(DISTINCT CASE WHEN EngagementType = 'video_watch' THEN LessonId END) AS LessonsCompleted,
        COUNT(DISTINCT CASE WHEN EngagementType = 'quiz_attempt' THEN Id END) AS QuizzesTaken,
        COUNT(DISTINCT CASE WHEN EngagementType = 'assignment_submit' THEN Id END) AS AssignmentsSubmitted,
        MAX(StartedAt) AS LastActivity
    FROM CourseEngagement
    WHERE UserId = @UserId
      AND IsValidated = 1
      AND StartedAt >= @StartDate
      AND StartedAt <= @EndDate;

    -- Engagement by type
    SELECT
        EngagementType,
        SUM(DurationMinutes) AS TotalMinutes,
        COUNT(*) AS TotalEvents
    FROM CourseEngagement
    WHERE UserId = @UserId
      AND IsValidated = 1
      AND StartedAt >= @StartDate
      AND StartedAt <= @EndDate
    GROUP BY EngagementType;
END;
GO

PRINT 'Stored Procedure sp_GetUserEngagementStats created';

-- =============================================================================
-- SECTION 9: UPDATE SYSTEM ENDPOINTS (DATABASE-DRIVEN ENDPOINT CONFIG)
-- =============================================================================

-- Add new subscription endpoints to SystemEndpoints table
DECLARE @MaxEndpointId INT;
SELECT @MaxEndpointId = ISNULL(MAX(Id), 0) FROM SystemEndpoints;

INSERT INTO SystemEndpoints (Id, Category, EndpointKey, EndpointPath, HttpMethod, Description, IsActive)
VALUES
-- Subscription Management
(@MaxEndpointId + 1, 'Subscriptions', 'GetPlans', 'api/subscriptions/plans', 'GET', 'Get all subscription plans', 1),
(@MaxEndpointId + 2, 'Subscriptions', 'Subscribe', 'api/subscriptions/subscribe', 'POST', 'Create new subscription', 1),
(@MaxEndpointId + 3, 'Subscriptions', 'GetMySubscription', 'api/subscriptions/my-subscription', 'GET', 'Get current user subscription', 1),
(@MaxEndpointId + 4, 'Subscriptions', 'Cancel', 'api/subscriptions/cancel', 'POST', 'Cancel subscription', 1),
(@MaxEndpointId + 5, 'Subscriptions', 'Resume', 'api/subscriptions/resume', 'POST', 'Resume cancelled subscription', 1),
(@MaxEndpointId + 6, 'Subscriptions', 'Upgrade', 'api/subscriptions/upgrade', 'POST', 'Upgrade subscription plan', 1),
(@MaxEndpointId + 7, 'Subscriptions', 'Downgrade', 'api/subscriptions/downgrade', 'POST', 'Downgrade subscription plan', 1),
(@MaxEndpointId + 8, 'Subscriptions', 'CreateCheckoutSession', 'api/subscriptions/create-checkout-session', 'POST', 'Create Stripe checkout session', 1),
(@MaxEndpointId + 9, 'Subscriptions', 'CreatePortalSession', 'api/subscriptions/create-portal-session', 'POST', 'Create Stripe customer portal session', 1),

-- Engagement Tracking
(@MaxEndpointId + 10, 'Engagement', 'Track', 'api/engagement/track', 'POST', 'Track engagement event', 1),
(@MaxEndpointId + 11, 'Engagement', 'VideoProgress', 'api/engagement/video-progress', 'POST', 'Update video watch progress', 1),
(@MaxEndpointId + 12, 'Engagement', 'GetMyStats', 'api/engagement/my-stats', 'GET', 'Get user engagement statistics', 1),

-- Instructor Earnings
(@MaxEndpointId + 13, 'Instructor', 'GetEarningsPreview', 'api/instructor/earnings/preview', 'GET', 'Preview current month earnings', 1),
(@MaxEndpointId + 14, 'Instructor', 'GetPayouts', 'api/instructor/payouts', 'GET', 'Get instructor payout history', 1),
(@MaxEndpointId + 15, 'Instructor', 'GetPayoutDetails', 'api/instructor/payouts/{0}', 'GET', 'Get payout details', 1),
(@MaxEndpointId + 16, 'Instructor', 'ConnectOnboard', 'api/instructor/connect/onboard', 'POST', 'Create Stripe Connect onboarding link', 1),

-- Admin Endpoints
(@MaxEndpointId + 17, 'Admin', 'CalculatePayouts', 'api/admin/payouts/calculate/{0}/{1}', 'POST', 'Calculate monthly payouts', 1),
(@MaxEndpointId + 18, 'Admin', 'ProcessPayout', 'api/admin/payouts/process/{0}', 'POST', 'Process a specific payout', 1),
(@MaxEndpointId + 19, 'Admin', 'GetPendingPayouts', 'api/admin/payouts/pending', 'GET', 'Get all pending payouts', 1),
(@MaxEndpointId + 20, 'Admin', 'GetCourseEngagement', 'api/admin/engagement/course/{0}', 'GET', 'Get engagement stats for a course', 1),
(@MaxEndpointId + 21, 'Admin', 'GetMonthlySummary', 'api/admin/engagement/monthly-summary', 'GET', 'Get monthly engagement summary', 1),
(@MaxEndpointId + 22, 'Admin', 'GetSubscriptionMetrics', 'api/admin/subscriptions/metrics', 'GET', 'Get subscription metrics', 1),

-- Webhook
(@MaxEndpointId + 23, 'Webhooks', 'Stripe', 'api/webhooks/stripe', 'POST', 'Handle Stripe webhook events', 1);

PRINT 'SystemEndpoints updated with subscription endpoints';

-- =============================================================================
-- SECTION 10: DATA VALIDATION
-- =============================================================================

-- Validate migration results
PRINT '=============================================================================';
PRINT 'MIGRATION VALIDATION RESULTS';
PRINT '=============================================================================';

-- Check subscription plans
DECLARE @PlanCount INT;
SELECT @PlanCount = COUNT(*) FROM SubscriptionPlans;
PRINT 'Subscription Plans Created: ' + CAST(@PlanCount AS NVARCHAR(10));

-- Check user subscriptions
DECLARE @SubscriptionCount INT;
SELECT @SubscriptionCount = COUNT(*) FROM UserSubscriptions;
PRINT 'User Subscriptions Created: ' + CAST(@SubscriptionCount AS NVARCHAR(10));

-- Check enrollment updates
DECLARE @LinkedEnrollments INT;
SELECT @LinkedEnrollments = COUNT(*) FROM Enrollments WHERE SubscriptionId IS NOT NULL;
PRINT 'Enrollments Linked to Subscriptions: ' + CAST(@LinkedEnrollments AS NVARCHAR(10));

-- Check triggers
DECLARE @TriggerCount INT;
SELECT @TriggerCount = COUNT(*)
FROM sys.triggers
WHERE name IN ('TR_AutoEnrollSubscribers', 'TR_UpdateEnrollmentsOnSubscriptionChange');
PRINT 'Triggers Created: ' + CAST(@TriggerCount AS NVARCHAR(10)) + ' (Expected: 2)';

-- Check views
DECLARE @ViewCount INT;
SELECT @ViewCount = COUNT(*)
FROM sys.views
WHERE name IN ('vw_ActiveSubscriptionsSummary', 'vw_InstructorEngagementSummary', 'vw_MonthlyRevenueBreakdown');
PRINT 'Views Created: ' + CAST(@ViewCount AS NVARCHAR(10)) + ' (Expected: 3)';

-- Check stored procedures
DECLARE @ProcCount INT;
SELECT @ProcCount = COUNT(*)
FROM sys.procedures
WHERE name IN ('sp_CalculateMonthlyPayouts', 'sp_GetUserEngagementStats');
PRINT 'Stored Procedures Created: ' + CAST(@ProcCount AS NVARCHAR(10)) + ' (Expected: 2)';

-- Check new endpoints
DECLARE @EndpointCount INT;
SELECT @EndpointCount = COUNT(*)
FROM SystemEndpoints
WHERE Category IN ('Subscriptions', 'Engagement', 'Instructor', 'Webhooks');
PRINT 'New SystemEndpoints Added: ' + CAST(@EndpointCount AS NVARCHAR(10));

PRINT '=============================================================================';
PRINT 'MIGRATION COMPLETED SUCCESSFULLY';
PRINT '=============================================================================';

-- =============================================================================
-- SECTION 11: POST-MIGRATION TASKS (MANUAL)
-- =============================================================================

/*
POST-MIGRATION MANUAL TASKS:

1. Stripe Configuration:
   - Create Stripe Products and Prices for each plan
   - Update SubscriptionPlans table with StripePriceIdMonthly and StripePriceIdYearly
   - Configure Stripe webhook endpoint: https://api.insightlearn.cloud/api/webhooks/stripe
   - Enable Stripe Connect in Stripe Dashboard

2. Application Configuration (appsettings.json):
   - Add Stripe.SecretKey
   - Add Stripe.PublishableKey
   - Add Stripe.WebhookSecret
   - Add Stripe.Connect.ClientId

3. Email Notifications:
   - Send welcome email to grandfathered users (explain free trial)
   - Create email templates for:
     * Subscription created
     * Trial ending (3 days before)
     * Payment failed
     * Subscription cancelled
     * Payout processed (for instructors)

4. Frontend Updates:
   - Update pricing page with new plans
   - Add "Subscribe" flow
   - Update user dashboard with subscription status
   - Add instructor earnings dashboard
   - Remove "Buy Course" buttons (replace with "Start Learning")

5. Testing:
   - Test subscription creation flow
   - Test engagement tracking with anti-fraud validation
   - Test payout calculation for sample data
   - Test Stripe webhook handling

6. Monitoring:
   - Set up alerts for:
     * Failed payments
     * High churn rate (> 5%)
     * Engagement tracking anomalies
     * Payout processing failures
   - Create Grafana dashboards for subscription metrics

7. Documentation:
   - Update API documentation with new endpoints
   - Create instructor guide for Stripe Connect onboarding
   - Update user help center with subscription information

8. Backup:
   - Keep backup tables for 30 days before deletion:
     * Courses_Backup_20250110
     * Enrollments_Backup_20250110
     * Users_Backup_20250110
     * Payments_Backup_20250110
*/

PRINT '=============================================================================';
PRINT 'IMPORTANT: Review POST-MIGRATION MANUAL TASKS section';
PRINT '=============================================================================';
