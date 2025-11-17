# SaaS Subscription Model - Task Decomposition & Execution Plan

**Project**: InsightLearn WASM v2.0.0
**Status**: Ready for Implementation
**Total Estimated Effort**: 152-188 hours (19-24 developer-days)
**Target Go-Live**: 2025-02-10 (4 weeks)

---

## Executive Summary

The database usage analysis revealed that **all infrastructure is in place** (6 repositories, 7 entity models, database schema), but **ZERO service implementations and ZERO API endpoints** exist for the SaaS subscription model.

**Critical Blocker**: Without implementing the 6 missing services and 23 API endpoints, v2.0.0 cannot launch.

**What Exists** ✅:
- ✅ 7 entity models (SubscriptionPlan, UserSubscription, CourseEngagement, InstructorPayout, etc.)
- ✅ 6 repositories (UserSubscriptionRepository, SubscriptionPlanRepository, CourseEngagementRepository, etc.)
- ✅ Interface definitions (ISubscriptionService, IEngagementTrackingService, IPayoutCalculationService)
- ✅ Database schema (see `/docs/SAAS-MIGRATION-SCRIPT.sql`)
- ✅ Complete architecture documentation

**What's Missing** ❌:
- ❌ 6 service implementations (SubscriptionService.cs, EngagementTrackingService.cs, etc.)
- ❌ 23 API endpoints in Program.cs (Subscriptions, Engagement, Payouts, Admin, Webhooks)
- ❌ DTO validation attributes for input DTOs
- ❌ Frontend components (Pricing page, Subscription management, Instructor earnings dashboard)
- ❌ Stripe integration code (checkout, webhooks, Connect)
- ❌ Background jobs (payout calculation, engagement validation)

**Implementation Strategy**:
1. **Week 1**: Service layer (6 services, 40-60 hours)
2. **Week 2**: API endpoints (23 endpoints, 30-40 hours)
3. **Week 3**: Frontend + Stripe (40-50 hours)
4. **Week 4**: Testing + Deployment (30-40 hours)

---

## Task Matrix

| Task ID | Component | Specialist | Effort | Dependencies | Priority | Status |
|---------|-----------|------------|--------|--------------|----------|--------|
| **T1** | SubscriptionService.cs | Backend Dev | 12h | Repos exist | P0 | ⏸️ Not Started |
| **T2** | EngagementTrackingService.cs | Backend Dev | 16h | Repos exist | P0 | ⏸️ Not Started |
| **T3** | PayoutCalculationService.cs | Backend Dev | 14h | T2 complete | P0 | ⏸️ Not Started |
| **T4** | StripeConnectService.cs | Backend Dev | 10h | None | P1 | ⏸️ Not Started |
| **T5** | SubscriptionRevenueService.cs | Backend Dev | 6h | T1 complete | P2 | ⏸️ Not Started |
| **T6** | InstructorConnectAccountService.cs | Backend Dev | 6h | T4 complete | P2 | ⏸️ Not Started |
| **T7** | SubscriptionPlanService.cs | Backend Dev | 4h | Repos exist | P2 | ⏸️ Not Started |
| **T8** | Background Jobs (2 jobs) | Backend Dev | 8h | T2, T3 complete | P1 | ⏸️ Not Started |
| **T9** | Subscription API Endpoints (9) | API Dev | 10h | T1 complete | P0 | ⏸️ Not Started |
| **T10** | Engagement API Endpoints (3) | API Dev | 4h | T2 complete | P0 | ⏸️ Not Started |
| **T11** | Instructor API Endpoints (4) | API Dev | 6h | T3, T4 complete | P0 | ⏸️ Not Started |
| **T12** | Admin API Endpoints (6) | API Dev | 8h | T2, T3 complete | P0 | ⏸️ Not Started |
| **T13** | Stripe Webhook Endpoint (1) | API Dev | 6h | T1 complete | P0 | ⏸️ Not Started |
| **T14** | DTO Validation (15 DTOs) | API Dev | 6h | None | P1 | ⏸️ Not Started |
| **T15** | Pricing.razor (Frontend) | Frontend Dev | 8h | None | P0 | ⏸️ Not Started |
| **T16** | Subscription.razor (User UI) | Frontend Dev | 10h | T9 complete | P0 | ⏸️ Not Started |
| **T17** | Earnings.razor (Instructor) | Frontend Dev | 12h | T11 complete | P0 | ⏸️ Not Started |
| **T18** | Admin Metrics/Payouts UI | Frontend Dev | 10h | T12 complete | P1 | ⏸️ Not Started |
| **T19** | Frontend Services (3) | Frontend Dev | 6h | API endpoints | P0 | ⏸️ Not Started |
| **T20** | Stripe Products Setup | DevOps | 2h | None | P0 | ⏸️ Not Started |
| **T21** | Database Migration | Database Expert | 4h | None | P0 | ⏸️ Not Started |
| **T22** | Unit Tests (80+ tests) | Testing Expert | 20h | Services done | P1 | ⏸️ Not Started |
| **T23** | Integration Tests (E2E) | Testing Expert | 12h | APIs done | P1 | ⏸️ Not Started |
| **T24** | Kubernetes Deployment | DevOps | 8h | All code ready | P0 | ⏸️ Not Started |

**Total Effort**: 188 hours (23.5 developer-days)
**Critical Path**: T1 → T9 → T16 (Subscription flow) = 32 hours
**Parallel Work**: T1+T2+T7, T9+T10+T11, T15+T16+T17 can run concurrently

---

## Detailed Task Descriptions

### Phase 1: Service Layer Implementation (76 hours)

#### **T1: SubscriptionService.cs** (12 hours) - Backend Developer

**File**: `/src/InsightLearn.Infrastructure/Services/SubscriptionService.cs`
**Interface**: `ISubscriptionService` (already exists)
**Repository**: `UserSubscriptionRepository`, `SubscriptionPlanRepository` (already exist)

**Methods to Implement** (17 methods):

1. **GetActiveSubscriptionPlansAsync()** (0.5h)
   - Query all active SubscriptionPlans (IsActive = true)
   - Order by OrderIndex
   - Include feature deserialization from JSON

2. **GetSubscriptionPlanByIdAsync(Guid planId)** (0.5h)
   - Single plan lookup by ID
   - Return null if not found or inactive

3. **GetActiveSubscriptionAsync(Guid userId)** (0.5h)
   - Use UserSubscriptionRepository.GetActiveByUserIdAsync
   - Include Plan navigation property
   - Check status IN ('active', 'trialing')

4. **CreateSubscriptionAsync(userId, planId, billingInterval, stripeSubscriptionId)** (2h)
   - **Transaction required** (use `using var transaction = await _context.Database.BeginTransactionAsync()`)
   - Validate plan exists and is active
   - Check user doesn't already have active subscription
   - Calculate CurrentPeriodStart/End based on billingInterval
   - Set TrialStart/TrialEnd (7 days if new user)
   - Set CurrentPrice from plan (lock-in pricing)
   - Insert UserSubscription via repository
   - Update User.SubscriptionStatus and User.CurrentSubscriptionId
   - **Auto-enroll to all courses** (call AutoEnrollSubscriberAsync)
   - Commit transaction
   - **Error Handling**: Rollback on failure, throw descriptive exception

5. **UpdateSubscriptionStatusAsync(stripeSubscriptionId, status)** (1h)
   - Find subscription by StripeSubscriptionId
   - Update Status field
   - If status = 'cancelled', set CancelledAt
   - If status = 'active', clear CancelledAt and CancelAtPeriodEnd
   - Save changes

6. **CancelSubscriptionAsync(subscriptionId, reason, feedback)** (1.5h)
   - Get subscription by ID
   - Check ownership (userId matches current user)
   - Set CancelAtPeriodEnd = true (don't cancel immediately)
   - Set CancellationReason and CancellationFeedback
   - **Stripe API call**: Cancel subscription at period end
   - Save changes
   - Return true/false

7. **ReactivateSubscriptionAsync(subscriptionId)** (1h)
   - Get subscription by ID
   - Check status = 'cancelled' and CancelAtPeriodEnd = true
   - Set CancelAtPeriodEnd = false
   - **Stripe API call**: Resume subscription
   - Re-activate enrollments (via trigger or manual)
   - Save changes

8. **HasActiveSubscriptionAsync(userId)** (0.5h)
   - Simple boolean check
   - Query UserSubscriptions WHERE UserId = userId AND Status IN ('active', 'trialing') AND CurrentPeriodEnd > NOW()

9. **CanAccessCourseAsync(userId, courseId)** (1h)
   - Check HasActiveSubscriptionAsync
   - If course.IsSubscriptionOnly = true, require active subscription
   - If course.IsSubscriptionOnly = false, check Enrollments table

10. **AutoEnrollSubscriberAsync(userId, subscriptionId)** (2h)
    - **Complex logic**:
      - Get all active courses WHERE IsSubscriptionOnly = true
      - For each course:
        - Check if enrollment exists (skip if yes)
        - Insert Enrollment: Status='Active', SubscriptionId=subscriptionId, EnrolledViaSubscription=true, AutoEnrolled=true
      - Batch insert for performance (use AddRange)
    - **Error Handling**: Continue on conflict, log errors

11. **GetMonthlyRecurringRevenueAsync()** (0.5h)
    - Delegate to UserSubscriptionRepository.GetMonthlyRecurringRevenueAsync

12. **GetActiveSubscriptionCountAsync()** (0.5h)
    - Delegate to repository

13. **GetChurnRateAsync(month, year)** (1h)
    - Get churn count via repository
    - Get active count at month start
    - Calculate: (churn / active_at_start) * 100
    - Return decimal percentage

14. **GetExpiringSubscriptionsAsync(daysBeforeExpiry)** (0.5h)
    - Delegate to repository

15. **HandleSubscriptionCreatedAsync(stripeSubscriptionId, userId, planId)** (1h)
    - Webhook handler
    - Create subscription if doesn't exist
    - Update if exists
    - **Idempotency**: Check by StripeSubscriptionId first

16. **HandleSubscriptionUpdatedAsync(stripeSubscriptionId, currentPeriodEnd, status)** (0.5h)
    - Update CurrentPeriodEnd and Status

17. **HandleSubscriptionCancelledAsync(stripeSubscriptionId)** (0.5h)
    - Set Status = 'cancelled', CancelledAt = NOW()

18. **HandleInvoicePaidAsync(stripeInvoiceId, stripeSubscriptionId, amount)** (1h)
    - Insert SubscriptionRevenue record
    - Status = 'paid'
    - Link to UserSubscription

19. **HandleInvoicePaymentFailedAsync(stripeInvoiceId, stripeSubscriptionId, failureReason)** (0.5h)
    - Insert SubscriptionRevenue record with Status = 'failed'
    - Update subscription Status = 'past_due'
    - Send email notification (via background job)

**Acceptance Criteria**:
- [ ] All 17 methods implemented
- [ ] Unit tests: 15+ test cases
  - CreateSubscriptionAsync with valid plan
  - CreateSubscriptionAsync with invalid plan (should throw)
  - CreateSubscriptionAsync when user already has active subscription (should throw)
  - CancelSubscriptionAsync sets CancelAtPeriodEnd
  - AutoEnrollSubscriberAsync enrolls to all courses
  - GetMonthlyRecurringRevenueAsync calculates correctly
  - Webhook handlers are idempotent
- [ ] **Transaction handling** in CreateSubscriptionAsync
- [ ] **Error handling**: Descriptive exceptions with error codes
- [ ] **Logging**: All key operations logged with structured data

**Dependencies**:
- ✅ UserSubscriptionRepository (exists)
- ✅ SubscriptionPlanRepository (exists)
- ✅ EnrollmentRepository (exists)
- ⚠️ Stripe.NET SDK (add NuGet package: `Stripe.net` v43.x)

**Code Pattern** (based on UserSubscriptionRepository):
```csharp
public class SubscriptionService : ISubscriptionService
{
    private readonly IUserSubscriptionRepository _subscriptionRepo;
    private readonly ISubscriptionPlanRepository _planRepo;
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly InsightLearnDbContext _context;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly StripeService _stripe; // Inject Stripe.NET SDK

    public SubscriptionService(
        IUserSubscriptionRepository subscriptionRepo,
        ISubscriptionPlanRepository planRepo,
        IEnrollmentRepository enrollmentRepo,
        ICourseRepository courseRepo,
        InsightLearnDbContext context,
        ILogger<SubscriptionService> logger,
        StripeService stripe)
    {
        _subscriptionRepo = subscriptionRepo;
        _planRepo = planRepo;
        _enrollmentRepo = enrollmentRepo;
        _courseRepo = courseRepo;
        _context = context;
        _logger = logger;
        _stripe = stripe;
    }

    // Implement methods here...
}
```

---

#### **T2: EngagementTrackingService.cs** (16 hours) - Backend Developer

**File**: `/src/InsightLearn.Infrastructure/Services/EngagementTrackingService.cs`
**Interface**: `IEngagementTrackingService` (already exists in architecture doc)
**Repository**: `CourseEngagementRepository` (already exists)

**Methods to Implement** (12 methods):

1. **TrackVideoWatchAsync(userId, lessonId, durationMinutes, sessionId, metadata)** (2h)
   - Insert CourseEngagement record
   - EngagementType = 'video_watch'
   - Get CourseId from Lesson.CourseId
   - Set StartedAt = NOW(), CompletedAt = NOW() + duration
   - Set DeviceFingerprint, IpAddress, UserAgent from HttpContext
   - Serialize metadata to JSON
   - **Validation**: Call ValidateEngagementAsync
   - **Anti-Fraud**: Call CalculateValidationScoreAsync
   - Set IsValidated based on score > 0.7
   - Return CourseEngagement entity

2. **TrackQuizAttemptAsync(userId, quizId, durationMinutes, score, sessionId)** (1.5h)
   - Similar to TrackVideoWatchAsync
   - EngagementType = 'quiz_attempt'
   - Metadata includes quiz score
   - **Validation**: Quiz duration cap = 2x expected duration

3. **TrackAssignmentSubmitAsync(userId, assignmentId, durationMinutes, sessionId)** (1.5h)
   - EngagementType = 'assignment_submit'
   - Validation similar to quiz

4. **TrackReadingAsync(userId, lessonId, durationMinutes, sessionId)** (1h)
   - EngagementType = 'reading'

5. **TrackDiscussionPostAsync(userId, discussionId, durationMinutes)** (1h)
   - EngagementType = 'discussion_post'

6. **GetUserEngagementStatsAsync(userId, startDate, endDate)** (2h)
   - **Complex aggregation**:
     - Total minutes: SUM(DurationMinutes WHERE IsValidated=true)
     - Courses watched: COUNT(DISTINCT CourseId)
     - Lessons completed: COUNT(DISTINCT LessonId WHERE EngagementType='video_watch')
     - Quizzes taken: COUNT(DISTINCT WHERE EngagementType='quiz_attempt')
     - Assignments submitted: COUNT(DISTINCT WHERE EngagementType='assignment_submit')
     - Last activity: MAX(StartedAt)
     - Engagement by type: GROUP BY EngagementType, SUM(DurationMinutes)
   - Return UserEngagementStats DTO

7. **GetCourseEngagementStatsAsync(courseId, startDate, endDate)** (1.5h)
   - Total minutes: SUM(DurationMinutes WHERE CourseId=courseId)
   - Unique users: COUNT(DISTINCT UserId)
   - Total engagements: COUNT(*)
   - Average engagement: AVG(DurationMinutes)
   - Last engagement: MAX(StartedAt)
   - Return CourseEngagementStats DTO

8. **GetInstructorEngagementStatsAsync(instructorId, startDate, endDate)** (2h)
   - **Complex aggregation across courses**:
     - Get all courses WHERE InstructorId = instructorId
     - For each course: SUM(DurationMinutes)
     - Total minutes: SUM across all courses
     - Unique students: COUNT(DISTINCT UserId)
     - Courses count: COUNT(DISTINCT CourseId)
     - Engagement by course: Dictionary<CourseId, Minutes>
   - Return InstructorEngagementStats DTO

9. **GetPlatformTotalEngagementAsync(year, month)** (1h)
   - SUM(DurationMinutes WHERE IsValidated=true AND YEAR(StartedAt)=year AND MONTH(StartedAt)=month)
   - Return BIGINT (minutes)

10. **GetInstructorsEngagementMapAsync(year, month)** (1.5h)
    - **Complex aggregation**:
      - JOIN CourseEngagement → Courses → Users (Instructor)
      - GROUP BY InstructorId
      - SUM(DurationMinutes)
    - Return Dictionary<Guid instructorId, long minutes>

11. **ValidateEngagementAsync(engagement)** (2h)
    - **Anti-Fraud Rules**:
      - Rule 1: Max daily cap (480 minutes = 8 hours)
        - Query: SUM(DurationMinutes WHERE UserId=userId AND DATE(StartedAt)=DATE(engagement.StartedAt))
        - If total > 480: return false
      - Rule 2: Max session duration (240 minutes = 4 hours)
        - If engagement.DurationMinutes > 240: return false
      - Rule 3: Playback speed cap (max 2x)
        - Parse metadata, check playback_speed <= 2.0
      - Rule 4: Tab visibility (metadata must include tab_active=true)
      - Rule 5: Device fingerprint (must not be empty)
    - Return boolean

12. **CalculateValidationScoreAsync(engagement)** (2h)
    - **Scoring algorithm** (0.00-1.00):
      - Start score = 1.0
      - Deduct 0.5 if daily total > 480 min
      - Deduct 0.3 if session > 240 min
      - Deduct 0.2 if playback_speed > 2.0
      - Deduct 0.1 if no device fingerprint
      - Deduct 0.1 if tab_active = false
      - Final score = MAX(0, score)
    - Return decimal (3,2)

**Acceptance Criteria**:
- [ ] All 12 methods implemented
- [ ] Unit tests: 20+ test cases
  - TrackVideoWatchAsync creates CourseEngagement
  - TrackVideoWatchAsync validates against daily cap (480 min)
  - ValidateEngagementAsync rejects > 8 hours/day
  - ValidateEngagementAsync rejects > 4 hours/session
  - CalculateValidationScoreAsync returns 0.0 for 10-hour session
  - CalculateValidationScoreAsync returns 1.0 for normal usage
  - GetUserEngagementStatsAsync aggregates correctly
  - GetInstructorEngagementStatsAsync includes all courses
  - GetPlatformTotalEngagementAsync filters by month/year
- [ ] **Anti-fraud validation** enforced
- [ ] **Metadata JSON** serialization/deserialization
- [ ] **Performance**: Aggregation queries use indexes (IX_CourseEngagement_InstructorRevenue)

**Dependencies**:
- ✅ CourseEngagementRepository (exists)
- ✅ LessonRepository (exists)
- ⚠️ HttpContext injection (for IpAddress, UserAgent)

---

#### **T3: PayoutCalculationService.cs** (14 hours) - Backend Developer

**File**: `/src/InsightLearn.Infrastructure/Services/PayoutCalculationService.cs`
**Interface**: `IPayoutCalculationService` (architecture doc)
**Repository**: `InstructorPayoutRepository`, `CourseEngagementRepository`, `SubscriptionRevenueRepository`

**Methods to Implement** (9 methods):

1. **CalculateMonthlyPayoutsAsync(year, month)** (4h)
   - **CRITICAL**: This is the core payout calculation algorithm
   - **Steps**:
     1. Get platform total engagement: `SELECT SUM(DurationMinutes) FROM CourseEngagement WHERE IsValidated=1 AND YEAR(StartedAt)=year AND MONTH(StartedAt)=month`
     2. Get platform total revenue: `SELECT SUM(Amount) FROM SubscriptionRevenue WHERE Status='paid' AND YEAR(BillingPeriodStart)=year AND MONTH(BillingPeriodStart)=month`
     3. Calculate instructor share (80%): `instructorShare = totalRevenue * 0.80`
     4. For each instructor:
        - Get instructor engagement: `SELECT SUM(DurationMinutes) FROM CourseEngagement ce JOIN Courses c ON ce.CourseId=c.Id WHERE c.InstructorId=instructorId AND IsValidated=1 AND YEAR(ce.StartedAt)=year AND MONTH(ce.StartedAt)=month`
        - Calculate engagement percentage: `instructorEngagement / platformTotalEngagement`
        - Calculate payout: `instructorShare * engagementPercentage`
     5. Insert InstructorPayout records (Status='pending')
     6. Return List<InstructorPayout>
   - **Transaction**: Use database transaction
   - **Idempotency**: Delete existing pending payouts for this period first

2. **CalculateInstructorPayoutAsync(instructorId, year, month)** (1.5h)
   - Same logic as CalculateMonthlyPayoutsAsync but for single instructor
   - Return InstructorPayout or null

3. **ProcessPayoutAsync(payoutId)** (2.5h)
   - **Stripe Connect Transfer**:
     1. Get InstructorPayout by ID
     2. Check Status = 'pending'
     3. Get InstructorConnectAccount (StripeConnectAccountId)
     4. Validate PayoutsEnabled = true
     5. Call Stripe Transfer API:
        ```csharp
        var transfer = await _stripeTransferService.CreateAsync(new TransferCreateOptions
        {
            Amount = (long)(payout.PayoutAmount * 100), // Convert to cents
            Currency = "eur",
            Destination = instructor.StripeConnectAccountId,
            Description = $"Payout for {payout.Year}-{payout.Month:D2}",
            Metadata = new Dictionary<string, string>
            {
                { "payoutId", payout.Id.ToString() },
                { "period", $"{payout.Year}-{payout.Month:D2}" }
            }
        });
        ```
     6. Update payout: Status='paid', StripeTransferId=transfer.Id, PaidAt=NOW()
     7. Save changes
   - **Error Handling**: If Stripe fails, set Status='failed', log error
   - Return true/false

4. **ProcessAllPendingPayoutsAsync(year, month)** (1h)
   - Get all pending payouts for period
   - Loop: Call ProcessPayoutAsync for each
   - Return count of successfully processed

5. **GetInstructorPayoutPreviewAsync(instructorId)** (2h)
   - **Real-time preview** for current month:
     - CurrentMonth = NOW().Month, CurrentYear = NOW().Year
     - Calculate as if month ended today
     - Use GetPlatformTotalEngagementAsync
     - Use GetInstructorEngagementStatsAsync
     - Return InstructorPayoutPreview DTO (not saved to DB)

6. **GetInstructorPayoutsAsync(instructorId, year, month)** (0.5h)
   - Query InstructorPayouts WHERE InstructorId=id AND (optional) Year=year AND Month=month
   - Order by Year DESC, Month DESC
   - Return List<InstructorPayout>

7. **GetPendingPayoutsAsync(year, month)** (0.5h)
   - Query InstructorPayouts WHERE Status='pending' AND (optional) Year=year AND Month=month
   - Include Instructor navigation property
   - Return List<InstructorPayout>

8. **GetPlatformRevenueAsync(year, month)** (0.5h)
   - Delegate to SubscriptionRevenueRepository
   - SUM(Amount WHERE Status='paid' AND period matches)

9. **GetInstructorShareRevenueAsync(year, month)** (0.5h)
   - GetPlatformRevenueAsync * 0.80

10. **GetRevenueBreakdownAsync(year, month)** (2h)
    - **Complex aggregation**:
      - TotalRevenue: GetPlatformRevenueAsync
      - InstructorShare: TotalRevenue * 0.80
      - PlatformFee: TotalRevenue * 0.20
      - TotalSubscribers: COUNT(DISTINCT UserSubscriptions WHERE Status='active' AND period matches)
      - NewSubscribers: COUNT(DISTINCT WHERE Status='active' AND CreatedAt in period)
      - CancelledSubscriptions: COUNT(DISTINCT WHERE Status='cancelled' AND CancelledAt in period)
      - MRR: GetMonthlyRecurringRevenueAsync
      - ChurnRate: (CancelledSubscriptions / TotalSubscribers) * 100
    - Return PlatformRevenueBreakdown DTO

**Acceptance Criteria**:
- [ ] All 9 methods implemented
- [ ] Unit tests: 18+ test cases
  - CalculateMonthlyPayoutsAsync with sample data (120k engagement, €50k revenue → €4,800 payout)
  - CalculateMonthlyPayoutsAsync with zero engagement (should return empty list)
  - ProcessPayoutAsync calls Stripe Transfer API
  - ProcessPayoutAsync handles Stripe failure (sets status='failed')
  - GetInstructorPayoutPreviewAsync calculates preview without saving
  - GetRevenueBreakdownAsync calculates churn rate correctly
- [ ] **Transaction handling** in CalculateMonthlyPayoutsAsync
- [ ] **Stripe API integration** in ProcessPayoutAsync
- [ ] **Idempotency**: Can re-run CalculateMonthlyPayoutsAsync without duplicates

**Dependencies**:
- ✅ InstructorPayoutRepository (exists)
- ✅ CourseEngagementRepository (exists)
- ✅ SubscriptionRevenueRepository (exists)
- ✅ IEngagementTrackingService (T2 must be complete)
- ⚠️ Stripe.NET SDK for Transfer API

---

#### **T4: StripeConnectService.cs** (10 hours) - Backend Developer

**File**: `/src/InsightLearn.Infrastructure/Services/StripeConnectService.cs`
**Interface**: `IStripeConnectService` (architecture doc)
**Repository**: `InstructorConnectAccountRepository`

**Methods to Implement** (7 methods):

1. **CreateConnectAccountAsync(instructorId, country)** (2h)
   - **Stripe Account Create**:
     ```csharp
     var account = await _accountService.CreateAsync(new AccountCreateOptions
     {
         Type = "express",
         Country = country,
         Email = instructor.Email,
         Capabilities = new AccountCapabilitiesOptions
         {
             Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
         },
         Metadata = new Dictionary<string, string>
         {
             { "instructorId", instructorId.ToString() }
         }
     });
     ```
   - Insert InstructorConnectAccount record
   - Return entity

2. **GetOnboardingLinkAsync(instructorId, returnUrl, refreshUrl)** (1.5h)
   - Get InstructorConnectAccount
   - **Stripe Account Link Create**:
     ```csharp
     var accountLink = await _accountLinkService.CreateAsync(new AccountLinkCreateOptions
     {
         Account = account.StripeConnectAccountId,
         RefreshUrl = refreshUrl,
         ReturnUrl = returnUrl,
         Type = "account_onboarding"
     });
     ```
   - Update OnboardingLink field
   - Return accountLink.Url

3. **GetConnectAccountAsync(instructorId)** (0.5h)
   - Query InstructorConnectAccount WHERE InstructorId=id
   - Return entity or null

4. **IsConnectAccountActiveAsync(instructorId)** (0.5h)
   - Get account
   - Check: ChargesEnabled=true AND PayoutsEnabled=true
   - Return boolean

5. **CreateTransferAsync(instructorId, amount, currency, description)** (2h)
   - Get InstructorConnectAccount
   - Validate PayoutsEnabled=true
   - **Stripe Transfer Create**:
     ```csharp
     var transfer = await _transferService.CreateAsync(new TransferCreateOptions
     {
         Amount = (long)(amount * 100),
         Currency = currency.ToLowerInvariant(),
         Destination = account.StripeConnectAccountId,
         Description = description
     });
     ```
   - Return transfer.Id

6. **VerifyTransferStatusAsync(transferId)** (1.5h)
   - **Stripe Transfer Retrieve**:
     ```csharp
     var transfer = await _transferService.GetAsync(transferId);
     ```
   - Check transfer.Status (pending, paid, failed, cancelled)
   - Return true if paid

7. **UpdateConnectAccountStatusAsync(instructorId)** (2h)
   - Get InstructorConnectAccount
   - **Stripe Account Retrieve**:
     ```csharp
     var account = await _accountService.GetAsync(connectAccount.StripeConnectAccountId);
     ```
   - Update fields:
     - ChargesEnabled = account.ChargesEnabled
     - PayoutsEnabled = account.PayoutsEnabled
     - DetailsSubmitted = account.DetailsSubmitted
   - If DetailsSubmitted and not OnboardingCompletedAt: Set OnboardingCompletedAt = NOW()
   - Save changes

**Acceptance Criteria**:
- [ ] All 7 methods implemented
- [ ] Integration tests with Stripe test mode (12+ tests)
  - CreateConnectAccountAsync creates Stripe account
  - GetOnboardingLinkAsync returns valid URL
  - CreateTransferAsync sends money to Connect account
  - VerifyTransferStatusAsync retrieves status
  - UpdateConnectAccountStatusAsync syncs from Stripe
- [ ] **Stripe API error handling**: Retry on network failures, log errors
- [ ] **Test mode**: Use Stripe test API keys

**Dependencies**:
- ✅ InstructorConnectAccountRepository (exists)
- ⚠️ Stripe.NET SDK (AccountService, AccountLinkService, TransferService)

---

#### **T5-T7: Minor Services** (16 hours total)

**T5: SubscriptionRevenueService.cs** (6h)
- Simple CRUD wrapper around SubscriptionRevenueRepository
- Methods: CreateRevenueRecord, GetRevenueByPeriod, GetRevenueBySubscription

**T6: InstructorConnectAccountService.cs** (6h)
- Simple wrapper around InstructorConnectAccountRepository
- Methods: GetAccount, CreateAccount, UpdateAccount

**T7: SubscriptionPlanService.cs** (4h)
- Simple wrapper around SubscriptionPlanRepository
- Methods: GetPlans, GetPlanBySlug, CreatePlan, UpdatePlan

---

#### **T8: Background Jobs** (8 hours) - Backend Developer

**File 1**: `/src/InsightLearn.Application/Services/EngagementValidationBackgroundService.cs` (4h)

```csharp
public class EngagementValidationBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Run every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

            // Get unvalidated engagement records (last 24 hours)
            var engagements = await _engagementRepo.GetUnvalidatedAsync();

            foreach (var engagement in engagements)
            {
                var isValid = await _engagementService.ValidateEngagementAsync(engagement);
                var score = await _engagementService.CalculateValidationScoreAsync(engagement);

                engagement.IsValidated = isValid;
                engagement.ValidationScore = score;
                await _engagementRepo.UpdateAsync(engagement);
            }

            _logger.LogInformation("Validated {Count} engagement records", engagements.Count);
        }
    }
}
```

**File 2**: `/src/InsightLearn.Application/Services/MonthlyPayoutCalculationBackgroundService.cs` (4h)

```csharp
public class MonthlyPayoutCalculationBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;

            // Wait until 1st of month, 00:00 UTC
            var nextRun = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
            var delay = nextRun - now;

            await Task.Delay(delay, stoppingToken);

            // Calculate payouts for previous month
            var year = now.Year;
            var month = now.Month - 1;
            if (month == 0) { month = 12; year--; }

            var payouts = await _payoutService.CalculateMonthlyPayoutsAsync(year, month);

            _logger.LogInformation("Calculated {Count} payouts for {Year}-{Month:D2}", payouts.Count, year, month);

            // Send email notifications to instructors
            foreach (var payout in payouts)
            {
                await _emailService.SendPayoutNotificationAsync(payout);
            }
        }
    }
}
```

**Registration** (in Program.cs):
```csharp
builder.Services.AddHostedService<EngagementValidationBackgroundService>();
builder.Services.AddHostedService<MonthlyPayoutCalculationBackgroundService>();
```

**Acceptance Criteria**:
- [ ] Both background services implemented
- [ ] Unit tests: 5+ tests
  - EngagementValidationBackgroundService processes unvalidated records
  - MonthlyPayoutCalculationBackgroundService runs on 1st of month
  - Both services handle cancellation token
- [ ] **Graceful shutdown**: Respect CancellationToken

---

### Phase 2: API Endpoints Implementation (34 hours)

#### **T9: Subscription API Endpoints (9 endpoints)** (10 hours) - API Developer

**File**: `/src/InsightLearn.Application/Program.cs` (add after existing endpoints)

**Endpoints to Implement**:

1. **GET /api/subscriptions/plans** (0.5h)
   ```csharp
   app.MapGet("/api/subscriptions/plans", async (ISubscriptionService subscriptionService) =>
   {
       var plans = await subscriptionService.GetActiveSubscriptionPlansAsync();
       return Results.Ok(new { success = true, data = plans });
   }).WithTags("Subscriptions").WithOpenApi();
   ```

2. **POST /api/subscriptions/subscribe** (1.5h)
   ```csharp
   app.MapPost("/api/subscriptions/subscribe",
       async (CreateSubscriptionDto dto, ISubscriptionService subscriptionService, ClaimsPrincipal user) =>
   {
       var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));

       var subscription = await subscriptionService.CreateSubscriptionAsync(
           userId, dto.PlanId, dto.BillingCycle, dto.StripeSubscriptionId);

       return Results.Ok(new { success = true, data = subscription });
   })
   .RequireAuthorization()
   .WithTags("Subscriptions")
   .WithOpenApi();
   ```
   - **DTO Validation**: CreateSubscriptionDto with [Required] attributes
   - **Error Handling**: Try-catch with 400 Bad Request

3. **GET /api/subscriptions/my-subscription** (0.5h)
   - Get current user subscription
   - Return 404 if none

4. **POST /api/subscriptions/cancel** (1h)
   - Input: SubscriptionId, CancellationReason, CancelImmediately
   - Validate ownership (userId matches)
   - Call CancelSubscriptionAsync
   - Return 200 OK with message

5. **POST /api/subscriptions/resume** (0.5h)
   - Input: SubscriptionId
   - Call ReactivateSubscriptionAsync

6. **POST /api/subscriptions/upgrade** (1h)
   - Input: SubscriptionId, NewPlanId
   - Validate: NewPlan.Price > CurrentPlan.Price
   - Call Stripe Subscription Update API
   - Update subscription in DB

7. **POST /api/subscriptions/downgrade** (1h)
   - Similar to upgrade but apply at period end

8. **POST /api/subscriptions/create-checkout-session** (2h)
   - **Stripe Checkout Session Create**:
     ```csharp
     var session = await _checkoutSessionService.CreateAsync(new SessionCreateOptions
     {
         Customer = stripeCustomerId,
         PaymentMethodTypes = new List<string> { "card" },
         LineItems = new List<SessionLineItemOptions>
         {
             new SessionLineItemOptions
             {
                 Price = plan.StripePriceIdMonthly, // or Yearly
                 Quantity = 1
             }
         },
         Mode = "subscription",
         SuccessUrl = dto.SuccessUrl,
         CancelUrl = dto.CancelUrl
     });
     ```
   - Return sessionId and URL

9. **POST /api/subscriptions/create-portal-session** (1h)
   - **Stripe Customer Portal Session**:
     ```csharp
     var session = await _portalSessionService.CreateAsync(new SessionCreateOptions
     {
         Customer = stripeCustomerId,
         ReturnUrl = dto.ReturnUrl
     });
     ```
   - Return portal URL

**Acceptance Criteria**:
- [ ] All 9 endpoints implemented
- [ ] DTO validation with DataAnnotations
- [ ] Error handling (400, 401, 404, 500)
- [ ] Swagger documentation
- [ ] Integration tests (25+ tests)

---

#### **T10-T13: Remaining API Endpoints** (24 hours)

**T10: Engagement API Endpoints (3)** (4h)
- POST /api/engagement/track
- POST /api/engagement/video-progress
- GET /api/engagement/my-stats

**T11: Instructor API Endpoints (4)** (6h)
- GET /api/instructor/earnings/preview
- GET /api/instructor/payouts
- GET /api/instructor/payouts/{id}
- POST /api/instructor/connect/onboard

**T12: Admin API Endpoints (6)** (8h)
- POST /api/admin/payouts/calculate/{year}/{month}
- POST /api/admin/payouts/process/{id}
- GET /api/admin/payouts/pending
- GET /api/admin/engagement/course/{id}
- GET /api/admin/engagement/monthly-summary
- GET /api/admin/subscriptions/metrics

**T13: Stripe Webhook Endpoint (1)** (6h)
- POST /api/webhooks/stripe
- **Signature Verification**:
  ```csharp
  var stripeEvent = EventUtility.ConstructEvent(
      json, stripeSignature, webhookSecret);
  ```
- Handle 6 event types (subscription.created, updated, deleted, invoice.paid, invoice.payment_failed, trial_will_end)
- **Idempotency**: Check by StripeEventId, skip if already processed

---

#### **T14: DTO Validation** (6 hours) - API Developer

**Create Input DTOs** (15 DTOs):

1. **CreateSubscriptionDto**
   ```csharp
   public class CreateSubscriptionDto
   {
       [Required]
       public Guid PlanId { get; set; }

       [Required]
       [RegularExpression("^(monthly|yearly)$")]
       public string BillingCycle { get; set; }

       [StringLength(255)]
       public string? CouponCode { get; set; }
   }
   ```

2. **CancelSubscriptionDto**
3. **UpgradePlanDto**
4. **CreateCheckoutSessionDto**
5. **TrackEngagementDto**
6. **TrackVideoProgressDto**
7. **CreatePayoutDto** (Admin)
8. **ProcessPayoutDto** (Admin)
9. ... (7 more)

**Acceptance Criteria**:
- [ ] All DTOs with validation attributes
- [ ] Unit tests for validation (15+ tests)

---

### Phase 3: Frontend Implementation (46 hours)

#### **T15: Pricing.razor** (8 hours) - Frontend Developer

**File**: `/src/InsightLearn.WebAssembly/Pages/Pricing.razor`

**Features**:
- Display 3 plans (Basic, Pro, Premium)
- Monthly/Yearly toggle
- Feature comparison table
- "Subscribe" button → Stripe Checkout

**Mockup**:
```
┌────────────────────────────────────────────┐
│          Choose Your Plan                  │
│  [Monthly] / [Yearly] (Save 16%)          │
├──────────┬──────────┬──────────────────────┤
│  Basic   │   Pro    │   Premium           │
│  €4/mo   │  €8/mo   │   €12/mo            │
├──────────┼──────────┼─────────────────────┤
│ ✓ Unlim. │ ✓ All    │ ✓ All Pro features  │
│   courses│   Basic  │ ✓ 1-on-1 Mentorship │
│ ✓ Certs  │ ✓ Offline│ ✓ Exclusive Content │
│          │ ✓ Support│                     │
├──────────┼──────────┼─────────────────────┤
│[Subscribe│[Subscribe│[Subscribe]          │
└──────────┴──────────┴─────────────────────┘
```

**Code Pattern**:
```csharp
@page "/pricing"
@inject ISubscriptionService SubscriptionService
@inject NavigationManager Navigation

<div class="pricing-container">
    @foreach (var plan in plans)
    {
        <div class="pricing-card">
            <h3>@plan.Name</h3>
            <p class="price">€@(billingCycle == "monthly" ? plan.PriceMonthly : plan.PriceYearly/12)/mo</p>
            <button @onclick="() => Subscribe(plan.Id)">Subscribe</button>
        </div>
    }
</div>

@code {
    private List<SubscriptionPlan> plans;
    private string billingCycle = "monthly";

    protected override async Task OnInitializedAsync()
    {
        plans = await SubscriptionService.GetPlansAsync();
    }

    private async Task Subscribe(Guid planId)
    {
        var session = await SubscriptionService.CreateCheckoutSessionAsync(
            planId, billingCycle,
            successUrl: Navigation.ToAbsoluteUri("/subscription/success").ToString(),
            cancelUrl: Navigation.ToAbsoluteUri("/pricing").ToString()
        );

        Navigation.NavigateTo(session.Url, forceLoad: true);
    }
}
```

---

#### **T16-T19: Remaining Frontend** (38 hours)

**T16: Subscription.razor (User Dashboard)** (10h)
- Current plan display
- Renewal date countdown
- Cancel/Resume buttons
- Upgrade/Downgrade flow
- Billing history

**T17: Earnings.razor (Instructor Dashboard)** (12h)
- Current month earnings preview (real-time)
- Engagement stats chart
- Payout history table
- Stripe Connect onboarding button
- Downloadable payout statements (PDF)

**T18: Admin Metrics/Payouts UI** (10h)
- MRR/ARR charts (Chart.js)
- Active subscribers gauge
- Churn rate trend
- Pending payouts table with "Process All" button
- Subscription metrics dashboard

**T19: Frontend Services** (6h)
- SubscriptionService.cs (Blazor)
- EngagementTrackingService.cs (Blazor)
- InstructorEarningsService.cs (Blazor)

---

### Phase 4: Deployment & Testing (62 hours)

#### **T20: Stripe Products Setup** (2 hours) - DevOps

**Steps**:
1. Create 3 Stripe Products (Basic, Pro, Premium)
2. Create 6 Prices (2 per product: monthly + yearly)
3. Update database with Stripe Price IDs
4. Configure webhook endpoint
5. Enable Stripe Connect

**Script**:
```bash
# Basic Plan - Monthly
stripe products create \
  --name "InsightLearn Basic" \
  --description "Unlimited access to all courses"

stripe prices create \
  --product prod_xxx \
  --unit-amount 400 \
  --currency eur \
  --recurring[interval]=month

# Update database
psql -c "UPDATE SubscriptionPlans SET StripePriceIdMonthly='price_xxx' WHERE Slug='basic'"
```

---

#### **T21: Database Migration** (4 hours) - Database Expert

**Steps**:
1. Backup production database
2. Run `/docs/SAAS-MIGRATION-SCRIPT.sql` on staging
3. Verify validation results
4. Test rollback procedure
5. Run on production (maintenance window)

---

#### **T22: Unit Tests** (20 hours) - Testing Expert

**80+ Tests**:
- SubscriptionService: 15 tests
- EngagementTrackingService: 20 tests
- PayoutCalculationService: 18 tests
- StripeConnectService: 12 tests
- API Endpoints: 25 tests
- Frontend Components: 10 tests

---

#### **T23: Integration Tests (E2E)** (12 hours) - Testing Expert

**4 E2E Scenarios**:
1. **New User Subscription Flow** (3h)
   - Register → View Pricing → Checkout (Stripe test) → Verify Subscription → Verify Auto-Enrollment

2. **Engagement Tracking** (3h)
   - Watch Video → Track Engagement → Verify Validation → Check Daily Cap

3. **Instructor Payout** (4h)
   - Seed Engagement Data → Calculate Payouts → Verify Amount → Process Payout (Stripe test)

4. **Subscription Cancellation** (2h)
   - Cancel Subscription → Verify Enrollments Suspended → Resume → Verify Reactivation

---

#### **T24: Kubernetes Deployment** (8 hours) - DevOps

**Steps**:
1. Build Docker image with new code
2. Update ConfigMaps (Stripe keys)
3. Apply database migration
4. Deploy API (rolling update)
5. Deploy frontend
6. Verify health checks
7. Monitor logs for errors

---

## Sprint Plan

### **Sprint 1 (Week 1): Service Layer**

| Day | Tasks | Effort | Assignee |
|-----|-------|--------|----------|
| **Mon** | T1 SubscriptionService (12h) | 12h | Backend Dev A |
| **Tue** | T2 EngagementTrackingService (16h) - Part 1 | 8h | Backend Dev A |
| **Wed** | T2 (continued) + Start T3 | 10h | Backend Dev A |
| **Thu** | T3 PayoutCalculationService (14h) | 10h | Backend Dev A |
| **Fri** | T3 (finish) + T4 StripeConnectService (start) | 8h | Backend Dev A |

**Parallel Work**:
- Backend Dev B: T7 SubscriptionPlanService (4h) + T5 SubscriptionRevenueService (6h) + T6 InstructorConnectAccountService (6h)

**Deliverables**:
- ✅ 6 service implementations
- ✅ 50+ unit tests

---

### **Sprint 2 (Week 2): API Endpoints**

| Day | Tasks | Effort | Assignee |
|-----|-------|--------|----------|
| **Mon** | T9 Subscription Endpoints (10h) + T14 DTO Validation (6h) | 16h | API Dev |
| **Tue** | T10 Engagement Endpoints (4h) + T11 Instructor Endpoints (6h) | 10h | API Dev |
| **Wed** | T12 Admin Endpoints (8h) | 8h | API Dev |
| **Thu** | T13 Stripe Webhook (6h) + T8 Background Jobs (8h) | 14h | API Dev + Backend Dev |
| **Fri** | Integration Testing (4h) | 4h | API Dev |

**Deliverables**:
- ✅ 23 API endpoints
- ✅ 15 input DTOs with validation
- ✅ 2 background jobs

---

### **Sprint 3 (Week 3): Frontend + Stripe**

| Day | Tasks | Effort | Assignee |
|-----|-------|--------|----------|
| **Mon** | T15 Pricing.razor (8h) + T20 Stripe Setup (2h) | 10h | Frontend Dev + DevOps |
| **Tue** | T16 Subscription.razor (10h) | 10h | Frontend Dev |
| **Wed** | T17 Earnings.razor (12h) - Part 1 | 8h | Frontend Dev |
| **Thu** | T17 (continued) + T19 Frontend Services (6h) | 10h | Frontend Dev |
| **Fri** | T18 Admin UI (10h) | 10h | Frontend Dev |

**Deliverables**:
- ✅ 5 frontend pages/components
- ✅ Stripe products configured

---

### **Sprint 4 (Week 4): Testing + Deployment**

| Day | Tasks | Effort | Assignee |
|-----|-------|--------|----------|
| **Mon** | T22 Unit Tests (20h) - Part 1 | 8h | Testing Expert |
| **Tue** | T22 (continued) | 8h | Testing Expert |
| **Wed** | T23 E2E Tests (12h) | 8h | Testing Expert |
| **Thu** | T21 Database Migration (4h) + T24 K8s Deployment (8h) | 12h | DevOps |
| **Fri** | Final verification, bug fixes, go-live | 8h | All Team |

**Deliverables**:
- ✅ 80+ unit tests (80% coverage)
- ✅ 4 E2E test scenarios
- ✅ Production deployment
- ✅ v2.0.0 live

---

## Critical Path

**Longest Dependency Chain** (32 hours):
```
T1 (SubscriptionService) [12h]
  ↓
T9 (Subscription API Endpoints) [10h]
  ↓
T16 (Subscription.razor Frontend) [10h]
```

**Parallel Critical Paths**:
- **Payout Flow**: T2 → T3 → T11 → T17 (16h + 14h + 6h + 12h = 48h)
- **Engagement Flow**: T2 → T10 (16h + 4h = 20h)

**Total Critical Path**: 48 hours (6 developer-days)

**Risk**: If Backend Dev is unavailable, entire project stalls.

**Mitigation**:
- Assign 2 backend developers (split T1-T7)
- Start frontend work on T15 (Pricing page) independently
- Start database migration (T21) early

---

## Testing Strategy

### **Unit Tests** (Target: 80% Coverage)

**Test Files**:
1. `SubscriptionServiceTests.cs` (15 tests)
   - CreateSubscriptionAsync_ValidPlan_CreatesSubscription
   - CreateSubscriptionAsync_InvalidPlan_ThrowsException
   - CreateSubscriptionAsync_UserHasActiveSubscription_ThrowsException
   - CancelSubscriptionAsync_SetsCancelAtPeriodEnd
   - AutoEnrollSubscriberAsync_EnrollsToAllCourses
   - GetMonthlyRecurringRevenueAsync_CalculatesCorrectly
   - HandleSubscriptionCreatedAsync_IsIdempotent

2. `EngagementTrackingServiceTests.cs` (20 tests)
   - TrackVideoWatchAsync_CreatesEngagement
   - TrackVideoWatchAsync_ExceedsDailyCap_Rejects
   - ValidateEngagementAsync_8Hours_ReturnsFalse
   - ValidateEngagementAsync_4HourSession_ReturnsFalse
   - CalculateValidationScoreAsync_NormalUsage_Returns1_0
   - CalculateValidationScoreAsync_10HourSession_Returns0_0
   - GetUserEngagementStatsAsync_AggregatesCorrectly
   - GetInstructorEngagementStatsAsync_IncludesAllCourses

3. `PayoutCalculationServiceTests.cs` (18 tests)
   - CalculateMonthlyPayoutsAsync_SampleData_Calculates4800Payout
   - CalculateMonthlyPayoutsAsync_ZeroEngagement_ReturnsEmpty
   - ProcessPayoutAsync_CallsStripeTransferAPI
   - ProcessPayoutAsync_StripeFailure_SetsStatusFailed
   - GetInstructorPayoutPreviewAsync_CalculatesPreviewWithoutSaving

4. `StripeConnectServiceTests.cs` (12 tests) - **Integration Tests with Stripe Test Mode**
   - CreateConnectAccountAsync_CreatesStripeAccount
   - GetOnboardingLinkAsync_ReturnsValidURL
   - CreateTransferAsync_SendsMoneyToConnectAccount
   - VerifyTransferStatusAsync_RetrievesStatus

5. `SubscriptionEndpointsTests.cs` (25 tests) - **API Integration Tests**
   - GET_SubscriptionsPlans_ReturnsAllPlans
   - POST_Subscribe_ValidPlan_CreatesSubscription
   - POST_Cancel_SetsStatus
   - POST_CreateCheckoutSession_ReturnsStripeURL

**Test Execution**:
```bash
dotnet test --filter "Category=Unit" --collect:"XPlat Code Coverage"
dotnet test --filter "Category=Integration"
```

---

### **E2E Tests** (4 Scenarios)

**Playwright/Selenium Tests**:

1. **E2E_NewUserSubscriptionFlow.cs** (3h)
   ```csharp
   [Fact]
   public async Task NewUser_SubscribesToBasicPlan_GetAutoEnrolled()
   {
       // 1. Register account
       var user = await RegisterUserAsync("test@example.com", "Password123!");

       // 2. Navigate to /pricing
       await NavigateToAsync("/pricing");

       // 3. Click "Subscribe" on Basic plan
       await ClickAsync("#basic-subscribe-btn");

       // 4. Complete Stripe checkout (test mode)
       await FillStripeCheckoutAsync("4242424242424242", "12/30", "123");
       await ClickAsync("#stripe-submit-btn");

       // 5. Verify redirect to /subscription/success
       await WaitForUrlAsync("/subscription/success");

       // 6. Verify subscription created in DB
       var subscription = await GetUserSubscriptionAsync(user.Id);
       Assert.Equal("active", subscription.Status);

       // 7. Verify auto-enrolled to all courses
       var enrollments = await GetUserEnrollmentsAsync(user.Id);
       Assert.True(enrollments.Count >= 100); // Assuming 100+ courses
   }
   ```

2. **E2E_EngagementTracking.cs** (3h)
   - Login → Watch Video → Verify Engagement Recorded → Verify Validation Score → Check Daily Cap

3. **E2E_InstructorPayout.cs** (4h)
   - Seed Engagement Data → Admin Login → Calculate Payouts → Verify Amount → Process Payout

4. **E2E_SubscriptionCancellation.cs** (2h)
   - Login → Cancel Subscription → Verify Enrollments Suspended → Resume → Verify Reactivated

---

## Risk Mitigation

### **Technical Risks**

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **Stripe webhook failures** | HIGH | MEDIUM | Implement retry logic (3 attempts), daily reconciliation job, manual sync UI |
| **Engagement tracking DB overload** | HIGH | MEDIUM | Use background job queue (Hangfire), batch insert, read replicas, archive old data |
| **Payout calculation errors** | CRITICAL | LOW | Preview payouts before processing, detailed breakdown UI, admin adjustment capability, audit log |
| **Deadlock in CreateSubscriptionAsync** | MEDIUM | MEDIUM | Use READ_COMMITTED_SNAPSHOT isolation level, optimize transaction scope, add retry logic |
| **Stripe Connect onboarding friction** | MEDIUM | HIGH | Clear instructor guide, video tutorial, support chat, fallback to manual bank transfer |

### **Business Risks**

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| **High churn rate (>5%)** | HIGH | MEDIUM | Trial end reminders (3 days before), exit survey, pause subscription option, discount codes |
| **Instructors unhappy with payouts** | HIGH | MEDIUM | Transparent earnings preview (daily updates), comparison vs old model, minimum guarantee (first 3 months), feedback forum |
| **Platform revenue < costs** | CRITICAL | LOW | Platform fee adjustable (20% default), Premium tier, Enterprise plans, cost optimization |
| **Database migration failure** | CRITICAL | LOW | Backup + rollback tested, staging migration first, maintenance window, rollback script ready |

---

## Monitoring & Alerts

### **Prometheus Alerts**

**Critical Alerts** (Page On-Call):
```yaml
- alert: PaymentFailureRateHigh
  expr: rate(stripe_payment_failures_total[5m]) > 0.05
  for: 5m
  annotations:
    summary: "Payment failure rate > 5%"

- alert: ChurnRateHigh
  expr: subscription_churn_rate > 0.05
  for: 1h
  annotations:
    summary: "Churn rate > 5%"

- alert: EngagementTrackingErrors
  expr: rate(engagement_tracking_errors_total[1h]) > 100
  for: 10m
  annotations:
    summary: "Engagement tracking errors > 100/hour"

- alert: PayoutCalculationFailure
  expr: payout_calculation_failures_total > 0
  for: 1m
  annotations:
    summary: "Payout calculation failed"
```

**Warning Alerts**:
- Trial-to-paid conversion < 30%
- MRR decrease > 5% week-over-week
- Stripe webhook processing delay > 5 min

---

### **Grafana Dashboards**

**Subscription Health Dashboard**:
- MRR/ARR trend (line chart)
- Active subscribers gauge
- Subscriber growth rate (%)
- Trial-to-paid conversion funnel
- Churn rate trend
- Revenue by plan (pie chart)

**Engagement Dashboard**:
- Platform-wide engagement minutes/day
- Average engagement per user
- Top 10 courses by engagement
- Engagement heatmap (hour/day)
- Validation score distribution

**Instructor Dashboard**:
- Active instructors (with > 0 engagement)
- Average payout per instructor
- Top 10 instructors by engagement
- Payout processing status
- Instructor retention rate

---

## Success Criteria

### **Week 1 (Post-Launch)**
- [ ] 50+ new subscriptions created
- [ ] 0 critical payment errors
- [ ] 0 data loss incidents
- [ ] < 5% churn rate
- [ ] All 6 services deployed without errors

### **Month 1 (Post-Launch)**
- [ ] MRR > €10,000
- [ ] 2,500+ active subscribers
- [ ] Trial-to-paid conversion > 30%
- [ ] Engagement tracking 1M+ events
- [ ] First instructor payouts processed successfully
- [ ] 0 payout disputes

### **Month 3 (Post-Launch)**
- [ ] MRR > €30,000
- [ ] 7,500+ active subscribers
- [ ] Churn rate < 3%
- [ ] 95% of instructors onboarded to Stripe Connect
- [ ] Platform profitable (revenue > costs)

---

## Team Assignments

**Backend Team** (2 developers):
- **Backend Dev A** (Senior): T1, T2, T3, T8 (SubscriptionService, EngagementTrackingService, PayoutCalculationService, Background Jobs)
- **Backend Dev B** (Mid-level): T4, T5, T6, T7 (StripeConnectService, minor services)

**API Team** (1 developer):
- **API Dev** (Senior): T9, T10, T11, T12, T13, T14 (All 23 API endpoints, DTO validation)

**Frontend Team** (1 developer):
- **Frontend Dev** (Senior): T15, T16, T17, T18, T19 (Pricing, Subscription, Earnings, Admin UI, Services)

**QA Team** (1 tester):
- **Testing Expert**: T22, T23 (Unit tests, E2E tests)

**DevOps** (1 engineer):
- **DevOps**: T20, T21, T24 (Stripe setup, Database migration, Kubernetes deployment)

**Product Manager**:
- User communication plan
- Pricing strategy
- Success metrics tracking
- Instructor onboarding

---

## Dependencies Summary

**External Dependencies**:
- ✅ Stripe.NET SDK (v43.x) - `dotnet add package Stripe.net`
- ✅ libphonenumber-csharp (for phone validation) - Already added in P1.3c
- ⚠️ Email service (for notifications) - Use existing or add SendGrid
- ⚠️ Hangfire (for background job queue) - Optional, can use BackgroundService

**Internal Dependencies**:
- ✅ All repositories implemented (6 repos)
- ✅ All entity models implemented (7 entities)
- ✅ All interface definitions exist
- ✅ Database schema ready (migration script exists)

**Blocking Dependencies**:
1. T2 (EngagementTrackingService) blocks T3, T10, T12
2. T1 (SubscriptionService) blocks T9, T16
3. T3 (PayoutCalculationService) blocks T11, T17
4. T4 (StripeConnectService) blocks T11

**Recommendation**: Start T1, T2, T7 in parallel on Day 1.

---

## Next Steps

**Immediate Actions** (this week):
1. [ ] Review this task decomposition with team
2. [ ] Approve budget for Stripe fees (~2.9% + €0.25 per transaction)
3. [ ] Create Stripe test account
4. [ ] Set up staging environment
5. [ ] Assign development resources (confirm team availability)
6. [ ] Create JIRA/GitHub Issues from this task breakdown

**Phase 1 Start** (next week):
1. [ ] Backend Dev A: Start T1 (SubscriptionService)
2. [ ] Backend Dev B: Start T7 (SubscriptionPlanService)
3. [ ] Database Expert: Run T21 (Database Migration) on staging
4. [ ] DevOps: Prepare T20 (Stripe Products Setup)
5. [ ] Set up CI/CD pipeline for new code
6. [ ] Daily standup: Review progress, blockers, dependencies

---

## Document History

**Version**: 1.0
**Date**: 2025-01-13
**Author**: Task Decomposition Expert
**Approved By**: (Pending review)

**Changes**:
- 2025-01-13: Initial task decomposition based on database usage analysis
- Created detailed specifications for all 24 tasks
- Defined critical path, sprint plan, testing strategy
- Identified all dependencies and risks
- Ready for team assignment and execution

---

**Files Referenced**:
1. `/docs/SAAS-SUBSCRIPTION-ARCHITECTURE.md` - Technical specification (2,268 lines)
2. `/docs/SAAS-IMPLEMENTATION-ROADMAP.md` - 4-week timeline (737 lines)
3. `/docs/SAAS-MIGRATION-SCRIPT.sql` - Database migration (1,015 lines)
4. `/src/InsightLearn.Infrastructure/Repositories/UserSubscriptionRepository.cs` - Implementation pattern
5. `/src/InsightLearn.Core/Interfaces/ISubscriptionService.cs` - Service interface

**Total Pages**: 42 pages (if printed)
**Total Implementation Effort**: 188 hours (23.5 developer-days)
**Target Go-Live**: 2025-02-10 (4 weeks from now)

---

## APPENDIX: Quick Reference

### **Service Layer** (6 services, 76 hours)
- T1: SubscriptionService (12h, 17 methods)
- T2: EngagementTrackingService (16h, 12 methods)
- T3: PayoutCalculationService (14h, 9 methods)
- T4: StripeConnectService (10h, 7 methods)
- T5-T7: Minor services (16h, 10 methods)
- T8: Background Jobs (8h, 2 jobs)

### **API Layer** (23 endpoints, 34 hours)
- T9: Subscription Endpoints (9 endpoints, 10h)
- T10: Engagement Endpoints (3 endpoints, 4h)
- T11: Instructor Endpoints (4 endpoints, 6h)
- T12: Admin Endpoints (6 endpoints, 8h)
- T13: Stripe Webhook (1 endpoint, 6h)
- T14: DTO Validation (15 DTOs, 6h)

### **Frontend Layer** (5 components, 46 hours)
- T15: Pricing.razor (8h)
- T16: Subscription.razor (10h)
- T17: Earnings.razor (12h)
- T18: Admin UI (10h)
- T19: Frontend Services (6h)

### **Deployment** (3 tasks, 14 hours)
- T20: Stripe Setup (2h)
- T21: Database Migration (4h)
- T24: Kubernetes Deployment (8h)

### **Testing** (2 tasks, 32 hours)
- T22: Unit Tests (20h, 80+ tests)
- T23: E2E Tests (12h, 4 scenarios)

**Total**: 24 tasks, 188 hours, 4 weeks
