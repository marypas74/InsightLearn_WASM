# SaaS Subscription Model - Implementation Roadmap

**Project**: InsightLearn WASM v2.0.0
**Business Model**: Pay-per-course → SaaS Subscription
**Timeline**: 4 weeks
**Status**: Design Phase Complete

---

## Quick Reference

### Key Documents
1. **Architecture Design**: `/docs/SAAS-SUBSCRIPTION-ARCHITECTURE.md` - Complete technical specification
2. **Migration Script**: `/docs/SAAS-MIGRATION-SCRIPT.sql` - Database migration (UP only)
3. **This Document**: Implementation roadmap and checklist

### Business Model Summary

| Metric | Current | New SaaS |
|--------|---------|----------|
| User Payment | €49.99 per course | €4.00/month (Basic) |
| Instructor Revenue | 80% of course price | Engagement-based (80% of platform revenue) |
| User Access | Single course | Unlimited courses |
| Platform Revenue | Variable, one-time | Recurring (MRR) |

**Payout Formula**:
```
instructor_payout = (platform_revenue * 0.80) * (instructor_engagement / total_engagement)

Example:
- Platform Revenue: €50,000/month
- Instructor Share: €40,000 (80%)
- Instructor Engagement: 120,000 minutes (12% of 1M total)
- Instructor Payout: €40,000 * 0.12 = €4,800/month
```

---

## Implementation Timeline

### Week 1: Foundation (Backend)

**Day 1-2: Database Migration**
- [ ] Backup production database
- [ ] Run migration script on staging: `/docs/SAAS-MIGRATION-SCRIPT.sql`
- [ ] Verify all tables created (7 new tables)
- [ ] Verify all columns added to existing tables
- [ ] Verify triggers created (2 triggers)
- [ ] Verify views created (3 views)
- [ ] Verify stored procedures created (2 procedures)
- [ ] Test rollback procedure

**Day 3-4: Entity Models**
Create files in `/src/InsightLearn.Core/Entities/`:
- [ ] `SubscriptionPlan.cs` (85 lines)
- [ ] `UserSubscription.cs` (92 lines)
- [ ] `CourseEngagement.cs` (76 lines)
- [ ] `InstructorPayout.cs` (88 lines)
- [ ] `SubscriptionRevenue.cs` (67 lines)
- [ ] `SubscriptionEvent.cs` (45 lines)
- [ ] `InstructorConnectAccount.cs` (52 lines)

**Day 5: Update DbContext**
- [ ] Add DbSets to `InsightLearnDbContext.cs`
- [ ] Run `dotnet ef migrations add SaaSSubscriptionModel`
- [ ] Review generated migration (should match manual script)
- [ ] Apply migration to staging database
- [ ] Verify with `dotnet ef database update`

---

### Week 2: Service Layer

**Day 6-7: Core Services**
Create files in `/src/InsightLearn.Infrastructure/Services/`:

1. **SubscriptionService.cs** (~400 lines)
   - [ ] Implement `ISubscriptionService`
   - [ ] CreateSubscriptionAsync
   - [ ] GetUserSubscriptionAsync
   - [ ] CancelSubscriptionAsync
   - [ ] UpgradePlanAsync / DowngradePlanAsync
   - [ ] IsUserSubscribedAsync
   - [ ] Unit tests (10+ test cases)

2. **EngagementTrackingService.cs** (~500 lines)
   - [ ] Implement `IEngagementTrackingService`
   - [ ] TrackVideoWatchAsync
   - [ ] TrackQuizAttemptAsync
   - [ ] ValidateEngagementAsync (anti-fraud)
   - [ ] CalculateValidationScoreAsync
   - [ ] GetUserEngagementStatsAsync
   - [ ] Unit tests (15+ test cases)

**Day 8-9: Payout Services**

3. **PayoutCalculationService.cs** (~450 lines)
   - [ ] Implement `IPayoutCalculationService`
   - [ ] CalculateMonthlyPayoutsAsync
   - [ ] ProcessPayoutAsync
   - [ ] GetInstructorPayoutPreviewAsync
   - [ ] GetPlatformRevenueAsync
   - [ ] Unit tests (12+ test cases)

4. **StripeConnectService.cs** (~350 lines)
   - [ ] Implement `IStripeConnectService`
   - [ ] CreateConnectAccountAsync
   - [ ] GetOnboardingLinkAsync
   - [ ] CreateTransferAsync
   - [ ] UpdateConnectAccountStatusAsync
   - [ ] Integration tests with Stripe test mode

**Day 10: Background Jobs**

5. **EngagementValidationBackgroundService.cs** (~200 lines)
   - [ ] Hourly job to validate engagement data
   - [ ] Flag suspicious patterns
   - [ ] Send admin alerts

6. **MonthlyPayoutCalculationBackgroundService.cs** (~150 lines)
   - [ ] Run on 1st of each month
   - [ ] Calculate all instructor payouts
   - [ ] Send email notifications

---

### Week 3: API Endpoints

**Day 11-12: Subscription Endpoints**
Add to `/src/InsightLearn.Application/Program.cs`:

```csharp
// Subscription Management (9 endpoints)
app.MapGet("/api/subscriptions/plans", GetPlans);
app.MapPost("/api/subscriptions/subscribe", Subscribe);
app.MapGet("/api/subscriptions/my-subscription", GetMySubscription);
app.MapPost("/api/subscriptions/cancel", CancelSubscription);
app.MapPost("/api/subscriptions/resume", ResumeSubscription);
app.MapPost("/api/subscriptions/upgrade", UpgradePlan);
app.MapPost("/api/subscriptions/downgrade", DowngradePlan);
app.MapPost("/api/subscriptions/create-checkout-session", CreateCheckoutSession);
app.MapPost("/api/subscriptions/create-portal-session", CreatePortalSession);
```

- [ ] Implement all 9 endpoints
- [ ] Add request/response DTOs
- [ ] Add validation attributes
- [ ] Add authorization policies
- [ ] Swagger documentation
- [ ] Integration tests

**Day 13: Engagement Endpoints**

```csharp
// Engagement Tracking (3 endpoints)
app.MapPost("/api/engagement/track", TrackEngagement);
app.MapPost("/api/engagement/video-progress", UpdateVideoProgress);
app.MapGet("/api/engagement/my-stats", GetMyEngagementStats);
```

- [ ] Implement all 3 endpoints
- [ ] Add rate limiting (prevent spam)
- [ ] Add anti-fraud validation
- [ ] Integration tests

**Day 14: Instructor & Admin Endpoints**

```csharp
// Instructor Earnings (4 endpoints)
app.MapGet("/api/instructor/earnings/preview", GetEarningsPreview);
app.MapGet("/api/instructor/payouts", GetInstructorPayouts);
app.MapGet("/api/instructor/payouts/{id}", GetPayoutDetails);
app.MapPost("/api/instructor/connect/onboard", CreateConnectOnboarding);

// Admin (6 endpoints)
app.MapPost("/api/admin/payouts/calculate/{year}/{month}", CalculatePayouts);
app.MapPost("/api/admin/payouts/process/{id}", ProcessPayout);
app.MapGet("/api/admin/payouts/pending", GetPendingPayouts);
app.MapGet("/api/admin/engagement/course/{id}", GetCourseEngagement);
app.MapGet("/api/admin/engagement/monthly-summary", GetMonthlySummary);
app.MapGet("/api/admin/subscriptions/metrics", GetSubscriptionMetrics);
```

- [ ] Implement all 10 endpoints
- [ ] Admin-only authorization
- [ ] Comprehensive logging
- [ ] Integration tests

**Day 15: Stripe Webhook**

```csharp
// Webhook (1 endpoint)
app.MapPost("/api/webhooks/stripe", HandleStripeWebhook);
```

- [ ] Implement webhook handler
- [ ] Handle 6 event types:
  - [ ] customer.subscription.created
  - [ ] customer.subscription.updated
  - [ ] customer.subscription.deleted
  - [ ] invoice.payment_succeeded
  - [ ] invoice.payment_failed
  - [ ] customer.subscription.trial_will_end
- [ ] Signature verification
- [ ] Idempotency handling
- [ ] Integration tests with Stripe CLI

---

### Week 4: Frontend & Deployment

**Day 16-17: Frontend Components**

Create files in `/src/InsightLearn.WebAssembly/`:

1. **Pages/Pricing.razor** (~300 lines)
   - [ ] Display 3 subscription plans
   - [ ] Pricing cards with features
   - [ ] Monthly/Yearly toggle
   - [ ] "Subscribe" button → Stripe Checkout

2. **Pages/User/Subscription.razor** (~250 lines)
   - [ ] Current plan display
   - [ ] Renewal date
   - [ ] Cancel/Resume buttons
   - [ ] Upgrade/Downgrade options
   - [ ] "Manage Billing" → Stripe Customer Portal

3. **Pages/Instructor/Earnings.razor** (~400 lines)
   - [ ] Current month earnings preview
   - [ ] Engagement stats
   - [ ] Payout history table
   - [ ] Stripe Connect onboarding flow
   - [ ] Charts (earnings over time)

4. **Pages/Admin/SubscriptionMetrics.razor** (~350 lines)
   - [ ] MRR chart
   - [ ] Active subscribers count
   - [ ] Churn rate
   - [ ] Plan breakdown (Basic/Pro/Premium)
   - [ ] Top instructors by engagement

5. **Pages/Admin/Payouts.razor** (~300 lines)
   - [ ] Pending payouts table
   - [ ] "Calculate Payouts" button
   - [ ] "Process All Payouts" button
   - [ ] Payout status filters

**Day 18: Services & State Management**

Create files in `/src/InsightLearn.WebAssembly/Services/`:

- [ ] `SubscriptionService.cs` (~200 lines)
- [ ] `EngagementTrackingService.cs` (~150 lines)
- [ ] `InstructorEarningsService.cs` (~180 lines)
- [ ] Update `EndpointConfigurationService.cs` with new endpoints

**Day 19: UI Updates**

- [ ] Remove "Buy Course" buttons from course pages
- [ ] Replace with "Start Learning" (auto-enroll for subscribers)
- [ ] Add subscription badge to user profile
- [ ] Update course cards (remove price display)
- [ ] Add "Subscription Required" banner for non-subscribers

**Day 20: Testing & QA**

- [ ] E2E test: Complete subscription flow
- [ ] E2E test: Engagement tracking
- [ ] E2E test: Instructor payout calculation
- [ ] E2E test: Stripe webhook handling
- [ ] Performance test: 10,000 concurrent users
- [ ] Load test: 1M engagement events/day
- [ ] Security audit: API authorization
- [ ] Accessibility audit (WCAG 2.1 AA)

---

### Week 5: Stripe Configuration & Go-Live

**Day 21: Stripe Setup**

1. **Create Stripe Products**
   ```bash
   # Basic Plan
   stripe products create \
     --name "InsightLearn Basic" \
     --description "Unlimited access to all courses"

   stripe prices create \
     --product prod_xxx \
     --unit-amount 400 \
     --currency eur \
     --recurring[interval]=month

   stripe prices create \
     --product prod_xxx \
     --unit-amount 4000 \
     --currency eur \
     --recurring[interval]=year
   ```

2. **Update Database**
   ```sql
   UPDATE SubscriptionPlans
   SET StripePriceIdMonthly = 'price_xxx',
       StripePriceIdYearly = 'price_yyy'
   WHERE Slug = 'basic';
   ```

3. **Configure Webhook**
   ```bash
   stripe listen --forward-to https://api.insightlearn.cloud/api/webhooks/stripe
   ```

4. **Enable Stripe Connect**
   - [ ] Enable in Stripe Dashboard
   - [ ] Set redirect URLs
   - [ ] Configure payout schedule (monthly)

**Day 22: Production Deployment**

1. **Backend Deployment**
   - [ ] Update `appsettings.Production.json` with Stripe keys
   - [ ] Deploy API to Kubernetes
   - [ ] Run database migration
   - [ ] Verify health checks
   - [ ] Test API endpoints

2. **Frontend Deployment**
   - [ ] Build Blazor WASM (`dotnet publish -c Release`)
   - [ ] Deploy to CDN/web server
   - [ ] Update DNS
   - [ ] Verify HTTPS

3. **Monitoring Setup**
   - [ ] Grafana dashboard for subscription metrics
   - [ ] Prometheus alerts for:
     - Payment failures > 5%
     - Churn rate > 5%
     - Engagement tracking errors
   - [ ] Sentry error tracking
   - [ ] LogRocket session replay

**Day 23-24: User Migration**

1. **Email Campaign**
   - [ ] Day 1: Announcement email (explain new model)
   - [ ] Day 3: Reminder email (free trial details)
   - [ ] Day 7: Last chance email (before migration)

2. **Grandfather Existing Users**
   ```sql
   -- Run migration script sections 5-6
   -- Users with 5+ courses: 1 year free
   -- Users with 3-4 courses: 6 months free
   -- Users with 1-2 courses: 3 months free
   ```

3. **Auto-Enroll to New Courses**
   ```sql
   -- Trigger TR_AutoEnrollSubscribers handles this
   -- Verify with sample course creation
   ```

**Day 25: Post-Launch Monitoring**

- [ ] Monitor MRR growth
- [ ] Track trial-to-paid conversion rate
- [ ] Monitor engagement tracking volume
- [ ] Check for payout calculation accuracy
- [ ] Review Stripe webhook logs
- [ ] User feedback collection

---

## Configuration Checklist

### Environment Variables

**Backend (`appsettings.Production.json`)**:
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
  },
  "EngagementTracking": {
    "MaxDailyMinutes": 480,
    "MaxSessionMinutes": 240,
    "ValidationThreshold": 0.7
  }
}
```

**Frontend (`appsettings.json`)**:
```json
{
  "Stripe": {
    "PublishableKey": "pk_live_xxx"
  },
  "SubscriptionPlans": {
    "BasicSlug": "basic",
    "ProSlug": "pro",
    "PremiumSlug": "premium"
  }
}
```

---

## Testing Strategy

### Unit Tests (Target: 80% Coverage)

**Service Layer** (`/tests/InsightLearn.Infrastructure.Tests/`):
- [ ] SubscriptionServiceTests.cs (15 tests)
- [ ] EngagementTrackingServiceTests.cs (20 tests)
- [ ] PayoutCalculationServiceTests.cs (18 tests)
- [ ] StripeConnectServiceTests.cs (12 tests)

**API Layer** (`/tests/InsightLearn.Application.Tests/`):
- [ ] SubscriptionEndpointsTests.cs (25 tests)
- [ ] EngagementEndpointsTests.cs (15 tests)
- [ ] InstructorEndpointsTests.cs (12 tests)
- [ ] AdminEndpointsTests.cs (18 tests)
- [ ] StripeWebhookTests.cs (10 tests)

### Integration Tests

**E2E Scenarios**:
1. **New User Subscription Flow**
   - Register account
   - View pricing page
   - Start checkout (Stripe test mode)
   - Complete payment
   - Verify subscription created
   - Verify auto-enrolled to all courses

2. **Engagement Tracking**
   - Watch video lesson (15 min)
   - Verify engagement recorded
   - Verify validation score > 0.7
   - Check daily cap enforcement

3. **Instructor Payout**
   - Seed engagement data (120k minutes)
   - Calculate monthly payouts
   - Verify payout amount = €4,800
   - Process payout (Stripe test mode)
   - Verify status = "paid"

4. **Subscription Cancellation**
   - Cancel subscription
   - Verify enrollments suspended
   - Resume subscription
   - Verify enrollments reactivated

---

## Monitoring & Metrics

### Business Metrics (Grafana Dashboard)

**Subscription Health**:
- MRR (Monthly Recurring Revenue)
- ARR (Annual Recurring Revenue)
- Total Active Subscribers
- Subscriber Growth Rate (%)
- Churn Rate (%)
- Trial-to-Paid Conversion Rate (%)
- Average Revenue Per User (ARPU)

**Engagement Metrics**:
- Platform-wide engagement minutes/day
- Average engagement per user
- Top 10 courses by engagement
- Engagement by hour/day of week

**Instructor Metrics**:
- Active instructors (with > 0 engagement)
- Average payout per instructor
- Top 10 instructors by engagement
- Instructor retention rate

**Revenue Metrics**:
- Daily revenue
- Revenue by plan (Basic/Pro/Premium)
- Refund rate
- Payment failure rate

### Alerts (Prometheus)

**Critical Alerts**:
- Payment failure rate > 5% (page on-call)
- Churn rate > 5% (page on-call)
- Engagement tracking errors > 100/hour
- Payout calculation failures

**Warning Alerts**:
- Trial-to-paid conversion < 30%
- MRR decrease > 5% week-over-week
- Engagement validation failures > 50/hour
- Stripe webhook processing delay > 5 min

---

## Rollback Plan

### Emergency Rollback (if critical issues)

**Step 1: Disable New Subscriptions**
```sql
UPDATE SubscriptionPlans SET IsActive = 0;
```

**Step 2: Revert to Pay-Per-Course**
```sql
UPDATE Courses SET IsSubscriptionOnly = 0, Price = LegacyPrice;
```

**Step 3: Preserve User Access**
```sql
-- Keep existing enrollments active
-- Do NOT delete UserSubscriptions table
```

**Step 4: Database Rollback (if needed)**
```bash
# Restore from backup
pg_restore -d insightlearn_db /backups/insightlearn_20250110_pre_migration.dump
```

**Step 5: Notify Users**
- [ ] Send email explaining temporary rollback
- [ ] Offer compensation (1 month free when re-launched)

### Partial Rollback (component-specific)

**If Engagement Tracking Fails**:
- Continue subscriptions
- Manual payout calculation for first month
- Fix and re-deploy

**If Stripe Connect Fails**:
- Process payouts via manual bank transfer
- Fix Stripe integration
- Resume automated payouts

**If Webhook Fails**:
- Manual subscription status sync from Stripe Dashboard
- Fix webhook handler
- Backfill missed events

---

## Success Criteria

### Week 1 (Post-Launch)
- [ ] 50+ new subscriptions created
- [ ] 0 critical payment errors
- [ ] 0 data loss incidents
- [ ] < 5% churn rate

### Month 1 (Post-Launch)
- [ ] MRR > €10,000
- [ ] 2,500+ active subscribers
- [ ] Trial-to-paid conversion > 30%
- [ ] Engagement tracking 1M+ events
- [ ] First instructor payouts processed successfully

### Month 3 (Post-Launch)
- [ ] MRR > €30,000
- [ ] 7,500+ active subscribers
- [ ] Churn rate < 3%
- [ ] 95% of instructors onboarded to Stripe Connect
- [ ] Platform profitable (revenue > costs)

---

## Risk Mitigation

### Technical Risks

**Risk**: Stripe webhook failures cause subscription status desync
**Mitigation**:
- Implement retry logic (3 attempts)
- Daily reconciliation job (compare Stripe vs DB)
- Manual sync UI for admins

**Risk**: Engagement tracking overwhelms database (1M+ events/day)
**Mitigation**:
- Use background job queue (Hangfire)
- Batch insert (100 events at a time)
- Read replicas for reporting queries
- Archive old engagement data (> 1 year)

**Risk**: Payout calculation errors cause instructor disputes
**Mitigation**:
- Preview payouts before processing
- Detailed breakdown UI (show exact calculation)
- Admin manual adjustment capability
- Audit log all payout changes

### Business Risks

**Risk**: High churn rate (users cancel after trial)
**Mitigation**:
- Trial end reminder emails (3 days before)
- Exit survey to understand why
- Offer pause subscription (up to 3 months)
- Discount codes for at-risk users

**Risk**: Instructors unhappy with engagement-based payouts
**Mitigation**:
- Transparent earnings preview (updated daily)
- Comparison vs old model (show gains)
- Minimum payout guarantee for first 3 months
- Feedback forum for instructor concerns

**Risk**: Platform revenue insufficient to cover costs
**Mitigation**:
- Platform fee adjustable (20% default, can increase if needed)
- Premium tier with higher price point
- Enterprise/team plans
- Cost optimization (caching, CDN)

---

## Next Steps

**Immediate Actions** (this week):
1. [ ] Review architecture document with team
2. [ ] Approve budget for Stripe fees (~2.9% + €0.25 per transaction)
3. [ ] Create Stripe test account
4. [ ] Set up staging environment
5. [ ] Assign development resources

**Phase 1 Start** (next week):
1. [ ] Run database migration on staging
2. [ ] Create entity models
3. [ ] Set up CI/CD pipeline for new code
4. [ ] Begin SubscriptionService implementation

---

## Team Assignments

**Backend Team** (2 developers):
- Subscription & Engagement services
- Payout calculation logic
- API endpoints
- Stripe integration
- Background jobs

**Frontend Team** (1 developer):
- Pricing page
- Subscription management UI
- Instructor earnings dashboard
- Admin metrics dashboard

**QA Team** (1 tester):
- Write integration tests
- Perform manual testing
- Load testing
- Security testing

**DevOps** (1 engineer):
- Database migration
- Kubernetes deployment
- Monitoring setup
- Backup strategy

**Product Manager**:
- User communication plan
- Pricing strategy
- Success metrics tracking
- Instructor onboarding

---

## Support Resources

**Documentation**:
- Stripe API Docs: https://stripe.com/docs/api
- Stripe Connect Guide: https://stripe.com/docs/connect
- Stripe Webhooks: https://stripe.com/docs/webhooks

**Code Examples**:
- Stripe .NET SDK: https://github.com/stripe/stripe-dotnet
- Subscription SaaS Starter: https://github.com/stripe-samples/subscription-saas

**Community**:
- Stripe Discord: https://discord.gg/stripe
- r/stripe subreddit

---

## Summary

This implementation roadmap provides a complete 4-week plan to transition InsightLearn from pay-per-course to a SaaS subscription model.

**Key Deliverables**:
- 7 new database tables + 4 modified tables
- 7 new entity models
- 4 major service classes
- 23 new API endpoints
- 5 frontend pages/components
- 2 background jobs
- Stripe integration (subscriptions + Connect)
- Complete testing suite
- Monitoring & alerting

**Total Effort Estimate**: ~320 hours (4 weeks * 2 developers * 40 hours/week)

**Go-Live Date**: 2025-02-10 (4 weeks from now)

---

**Files Created**:
1. `/docs/SAAS-SUBSCRIPTION-ARCHITECTURE.md` - Technical specification (600+ lines)
2. `/docs/SAAS-MIGRATION-SCRIPT.sql` - Database migration (800+ lines)
3. `/docs/SAAS-IMPLEMENTATION-ROADMAP.md` - This file (implementation plan)

**Next Action**: Review architecture with team and approve to proceed to Phase 1 (Database Migration).
