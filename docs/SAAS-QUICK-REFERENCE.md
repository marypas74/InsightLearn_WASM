# SaaS Subscription Model - Quick Reference

**Version**: 2.0.0
**Status**: Design Phase Complete
**Last Updated**: 2025-01-10

---

## Business Model At-a-Glance

### Pricing Tiers

| Plan | Price/Month | Price/Year | Features | Devices | Savings |
|------|-------------|------------|----------|---------|---------|
| Basic | €4.00 | €40.00 | Unlimited courses, certificates, mobile | 2 | 16% |
| Pro | €8.00 | €80.00 | + Offline downloads, priority support | 5 | 16% |
| Premium | €12.00 | €120.00 | + 1-on-1 mentorship, exclusive content | 10 | 16% |

### Revenue Sharing Formula

```
Platform Total Revenue: €50,000/month
├─ Platform Fee (20%): €10,000
└─ Instructor Pool (80%): €40,000

Instructor Payout Calculation:
payout = €40,000 * (instructor_engagement / total_engagement)

Example:
- Instructor engagement: 120,000 minutes (12% of 1M total)
- Instructor payout: €40,000 * 0.12 = €4,800/month
```

---

## Database Schema Summary

### New Tables (7)

1. **SubscriptionPlans** - Plan definitions (Basic/Pro/Premium)
2. **UserSubscriptions** - User subscription status & billing
3. **CourseEngagement** - Engagement tracking (video, quiz, assignments)
4. **InstructorPayouts** - Monthly payout calculations
5. **SubscriptionRevenue** - Revenue per billing period
6. **SubscriptionEvents** - Audit log for subscriptions
7. **InstructorConnectAccounts** - Stripe Connect integration

### Modified Tables (3)

1. **Courses**: Added `IsSubscriptionOnly`, `LegacyPrice`
2. **Enrollments**: Added `SubscriptionId`, `EnrolledViaSubscription`, `AutoEnrolled`
3. **Users**: Added `StripeCustomerId`, `SubscriptionStatus`, `CurrentSubscriptionId`

---

## API Endpoints (23 New)

### Subscriptions (9)
```
GET    /api/subscriptions/plans
POST   /api/subscriptions/subscribe
GET    /api/subscriptions/my-subscription
POST   /api/subscriptions/cancel
POST   /api/subscriptions/resume
POST   /api/subscriptions/upgrade
POST   /api/subscriptions/downgrade
POST   /api/subscriptions/create-checkout-session
POST   /api/subscriptions/create-portal-session
```

### Engagement Tracking (3)
```
POST   /api/engagement/track
POST   /api/engagement/video-progress
GET    /api/engagement/my-stats
```

### Instructor Earnings (4)
```
GET    /api/instructor/earnings/preview
GET    /api/instructor/payouts
GET    /api/instructor/payouts/{id}
POST   /api/instructor/connect/onboard
```

### Admin (6)
```
POST   /api/admin/payouts/calculate/{year}/{month}
POST   /api/admin/payouts/process/{id}
GET    /api/admin/payouts/pending
GET    /api/admin/engagement/course/{id}
GET    /api/admin/engagement/monthly-summary
GET    /api/admin/subscriptions/metrics
```

### Webhook (1)
```
POST   /api/webhooks/stripe
```

---

## Stripe Integration

### Products to Create

```bash
# Basic Plan
stripe products create --name "InsightLearn Basic"
stripe prices create --product prod_xxx --unit-amount 400 --currency eur --recurring[interval]=month
stripe prices create --product prod_xxx --unit-amount 4000 --currency eur --recurring[interval]=year

# Pro Plan
stripe products create --name "InsightLearn Pro"
stripe prices create --product prod_xxx --unit-amount 800 --currency eur --recurring[interval]=month
stripe prices create --product prod_xxx --unit-amount 8000 --currency eur --recurring[interval]=year

# Premium Plan
stripe products create --name "InsightLearn Premium"
stripe prices create --product prod_xxx --unit-amount 1200 --currency eur --recurring[interval]=month
stripe prices create --product prod_xxx --unit-amount 12000 --currency eur --recurring[interval]=year
```

### Webhook Events

Configure webhook endpoint: `https://api.insightlearn.cloud/api/webhooks/stripe`

Handle these events:
- `customer.subscription.created`
- `customer.subscription.updated`
- `customer.subscription.deleted`
- `invoice.payment_succeeded`
- `invoice.payment_failed`
- `customer.subscription.trial_will_end`

---

## Configuration

### Backend (appsettings.Production.json)

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

### Frontend (appsettings.json)

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

## Migration Strategy

### Existing User Grandfathering

**Users with 5+ courses**: 1 year free Basic
**Users with 3-4 courses**: 6 months free Basic
**Users with 1-2 courses**: 3 months free Basic
**New users**: 7 days free trial

### Migration SQL

```sql
-- 5+ courses: 1 year free
INSERT INTO UserSubscriptions (UserId, PlanId, Status, TrialEnd, ...)
SELECT UserId, @BasicPlanId, 'trialing', DATEADD(YEAR, 1, GETUTCDATE()), ...
FROM (SELECT UserId, COUNT(DISTINCT CourseId) AS CourseCount FROM Enrollments GROUP BY UserId HAVING COUNT(DISTINCT CourseId) >= 5);

-- 3-4 courses: 6 months free
-- 1-2 courses: 3 months free
-- (Similar queries with DATEADD(MONTH, 6/3, GETUTCDATE()))
```

---

## Key Metrics to Monitor

### Business Metrics
- **MRR** (Monthly Recurring Revenue)
- **ARR** (Annual Recurring Revenue)
- **Churn Rate** (target: < 3%)
- **Trial-to-Paid Conversion** (target: > 30%)
- **ARPU** (Average Revenue Per User)

### Engagement Metrics
- **Platform-wide engagement minutes/day**
- **Average engagement per user**
- **Top 10 courses by engagement**

### Instructor Metrics
- **Active instructors** (with > 0 engagement)
- **Average payout per instructor**
- **Top 10 instructors by engagement**

### Revenue Metrics
- **Daily revenue**
- **Revenue by plan** (Basic/Pro/Premium split)
- **Refund rate** (target: < 2%)
- **Payment failure rate** (target: < 5%)

---

## Anti-Fraud Rules

### Engagement Tracking Validation

1. **Max Daily Cap**: 480 minutes (8 hours) per user
2. **Tab Visibility**: Only count active tab time
3. **Playback Speed Cap**: Max 2x speed
4. **Session Timeout**: Max 240 minutes (4 hours) continuous
5. **Quiz Time Cap**: Max 2x expected duration
6. **Device Fingerprinting**: Detect bots

### Validation Score Calculation

```csharp
decimal score = 1.0m;

// Daily cap exceeded
if (dailyTotal > 480) score -= 0.5m;

// Session too long
if (sessionDuration > 240) score -= 0.3m;

// Playback speed > 2x
if (playbackSpeed > 2.0) score -= 0.2m;

// No device fingerprint
if (string.IsNullOrEmpty(fingerprint)) score -= 0.1m;

return Math.Max(0, score);  // 0.0 - 1.0
```

Only engagement with `ValidationScore >= 0.7` counts toward instructor payouts.

---

## Implementation Checklist

### Week 1: Backend Foundation
- [ ] Run database migration script
- [ ] Create entity models (7 new classes)
- [ ] Update DbContext
- [ ] Generate EF Core migration
- [ ] Verify on staging database

### Week 2: Service Layer
- [ ] SubscriptionService (400 lines)
- [ ] EngagementTrackingService (500 lines)
- [ ] PayoutCalculationService (450 lines)
- [ ] StripeConnectService (350 lines)
- [ ] Background jobs (2 services)
- [ ] Unit tests (80+ tests)

### Week 3: API Endpoints
- [ ] Subscription endpoints (9 endpoints)
- [ ] Engagement endpoints (3 endpoints)
- [ ] Instructor endpoints (4 endpoints)
- [ ] Admin endpoints (6 endpoints)
- [ ] Stripe webhook handler (1 endpoint)
- [ ] Integration tests (50+ tests)

### Week 4: Frontend & Deployment
- [ ] Pricing page (300 lines)
- [ ] Subscription management (250 lines)
- [ ] Instructor earnings dashboard (400 lines)
- [ ] Admin metrics dashboard (350 lines)
- [ ] Admin payouts page (300 lines)
- [ ] Update course pages (remove buy buttons)
- [ ] Stripe configuration
- [ ] Production deployment
- [ ] User migration
- [ ] Monitoring setup

---

## Testing Scenarios

### E2E Test 1: New User Subscription
1. Register new user
2. View pricing page
3. Click "Subscribe to Basic"
4. Complete Stripe checkout (test mode)
5. Verify subscription created (status: "trialing")
6. Verify auto-enrolled to all courses
7. Watch video lesson (15 min)
8. Verify engagement tracked

### E2E Test 2: Instructor Payout
1. Seed engagement data (120,000 minutes)
2. Call `POST /api/admin/payouts/calculate/2025/1`
3. Verify payout created (amount: €4,800)
4. Call `POST /api/admin/payouts/process/{id}`
5. Verify Stripe transfer created
6. Verify payout status = "paid"

### E2E Test 3: Subscription Cancellation
1. User has active subscription
2. Call `POST /api/subscriptions/cancel`
3. Verify `CancelAtPeriodEnd = true`
4. Verify enrollments still active (until period end)
5. Simulate period end (update `CurrentPeriodEnd`)
6. Verify trigger suspends enrollments
7. Resume subscription
8. Verify enrollments reactivated

---

## Performance Targets

### API Response Times
- Subscription endpoints: < 200ms p95
- Engagement tracking: < 100ms p95
- Payout calculation: < 5s for 10,000 instructors
- Webhook processing: < 500ms p95

### Database Performance
- Engagement inserts: 1,000/second sustained
- Payout queries: < 2s for monthly calculation
- Subscription lookups: < 50ms (with caching)

### Scalability
- Support 100,000 active subscribers
- Handle 1M engagement events/day
- Process 10,000 instructor payouts/month

---

## Rollback Plan

### Quick Rollback (if critical failure)

1. **Disable new subscriptions**:
   ```sql
   UPDATE SubscriptionPlans SET IsActive = 0;
   ```

2. **Revert to pay-per-course**:
   ```sql
   UPDATE Courses SET IsSubscriptionOnly = 0, Price = LegacyPrice;
   ```

3. **Preserve user access** (keep enrollments active)

4. **Database restore** (if needed):
   ```bash
   pg_restore -d insightlearn_db /backups/pre_migration_20250110.dump
   ```

5. **Notify users** via email

---

## Success Criteria

### Week 1 Post-Launch
- 50+ new subscriptions
- 0 critical payment errors
- < 5% churn rate

### Month 1 Post-Launch
- MRR > €10,000
- 2,500+ active subscribers
- Trial-to-paid > 30%
- 1M+ engagement events tracked
- First instructor payouts processed

### Month 3 Post-Launch
- MRR > €30,000
- 7,500+ active subscribers
- Churn rate < 3%
- 95% instructors onboarded to Stripe Connect
- Platform profitable

---

## Support & Resources

### Documentation
- **Architecture**: `/docs/SAAS-SUBSCRIPTION-ARCHITECTURE.md`
- **Migration Script**: `/docs/SAAS-MIGRATION-SCRIPT.sql`
- **Roadmap**: `/docs/SAAS-IMPLEMENTATION-ROADMAP.md`
- **This Document**: `/docs/SAAS-QUICK-REFERENCE.md`

### Stripe Documentation
- API Reference: https://stripe.com/docs/api
- Subscriptions Guide: https://stripe.com/docs/billing/subscriptions
- Connect Guide: https://stripe.com/docs/connect
- Webhooks: https://stripe.com/docs/webhooks
- .NET SDK: https://github.com/stripe/stripe-dotnet

### Code Examples
- Subscription SaaS Starter: https://github.com/stripe-samples/subscription-saas
- Connect Onboarding: https://github.com/stripe-samples/connect-onboarding

---

## Contact & Escalation

**Project Manager**: [TBD]
**Lead Backend Developer**: [TBD]
**Lead Frontend Developer**: [TBD]
**DevOps Engineer**: [TBD]

**Escalation Path**:
1. Critical payment failures → Page on-call (24/7)
2. Churn rate spike → Email PM + team
3. Payout calculation errors → Email lead backend dev
4. Stripe webhook failures → Email DevOps

---

## Quick Command Reference

### Database
```bash
# Run migration
sqlcmd -S localhost -U sa -P $MSSQL_SA_PASSWORD -i /docs/SAAS-MIGRATION-SCRIPT.sql

# Calculate payouts (manual)
sqlcmd -Q "EXEC sp_CalculateMonthlyPayouts @Year=2025, @Month=1"

# Check subscription count
sqlcmd -Q "SELECT Status, COUNT(*) FROM UserSubscriptions GROUP BY Status"
```

### Stripe CLI
```bash
# Test webhook locally
stripe listen --forward-to localhost:7001/api/webhooks/stripe

# Trigger test event
stripe trigger customer.subscription.created

# View recent events
stripe events list --limit 10
```

### Kubernetes
```bash
# Deploy migration job
kubectl apply -f k8s/saas-migration-job.yaml

# Check migration status
kubectl logs -n insightlearn job/saas-migration

# Rollback deployment
kubectl rollout undo deployment/insightlearn-api -n insightlearn
```

---

**Last Updated**: 2025-01-10
**Version**: 2.0.0-design
**Status**: Ready for Implementation Phase
