# Task Quick Reference Card

**Quick lookup for developers working on the 10/10 improvement plan**

---

## Today's Priority Tasks

### ⚡ Quick Wins (< 30 min each)
1. **P1.2**: Fix GET /api/enrollments - 30 min - [Details](#p12-fix-enrollments)
2. **P2.1**: JWT Secret Validation - 30 min - [Details](#p21-jwt-validation)
3. **P2.5**: Enable Rate Limiter - 5 min - [Details](#p25-rate-limiter)
4. **P2.9**: Security Headers - 20 min - [Details](#p29-security-headers)

### 🔥 Critical Path (Must do first)
1. Phase 1: Fix Enrollments (30 min)
2. Phase 2: Security (9 hours total, can split over 2 days)
3. Phase 4: Monitoring (7 hours)

---

## Task Details

### P1.2: Fix GET /api/enrollments {#p12-fix-enrollments}

**Files**:
- `src/InsightLearn.Core/Interfaces/IEnrollmentService.cs` (line 70)
- `src/InsightLearn.Application/Services/EnrollmentService.cs` (line 113)
- `src/InsightLearn.Application/Program.cs` (lines 2343-2349)

**Steps**:
1. Add method signature to interface
2. Implement method in service
3. Update endpoint to call service

**Test**:
```bash
curl http://localhost:31081/api/enrollments?page=1&pageSize=10
# Should return 200 OK (not 501)
```

---

### P2.1: JWT Secret Validation {#p21-jwt-validation}

**File**: `src/InsightLearn.Application/Program.cs` (lines 150-180)

**Replace**:
```csharp
var jwtSecret = config["JwtSettings:SecretKey"] ?? "fallback";
```

**With**:
```csharp
var jwtSecret = config["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException("JWT Secret Key is not configured or too short (min 32 chars)");
}
```

**Test**:
```bash
# Should fail to start
JWT_SECRET_KEY="" dotnet run --project src/InsightLearn.Application
```

---

### P2.5: Enable Rate Limiter {#p25-rate-limiter}

**File**: `src/InsightLearn.Application/Program.cs` (after `app.UseAuthorization()`)

**Add**:
```csharp
app.UseRateLimiter();
```

**Prerequisites**: P2.4 (Rate limiter config) must be done first

**Test**:
```bash
# Send 110 requests, last 10 should get 429
for i in {1..110}; do curl -s http://localhost:31081/health -w "%{http_code}\n"; done | grep 429 | wc -l
# Should show: 10
```

---

### P2.9: Security Headers {#p29-security-headers}

**File**: `src/InsightLearn.Application/Program.cs` (after `app.UseHttpsRedirection()`)

**Add**:
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'; script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval'; ...");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Add("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
    await next();
});
```

**Test**:
```bash
curl -I http://localhost:31081/health | grep -c "X-Frame\|X-Content\|Strict-Transport"
# Should show: 7
```

---

## Verification Commands

### Build Status
```bash
dotnet build InsightLearn.WASM.sln
# Target: 0 errors, 0 warnings
```

### Run Verification Script
```bash
./scripts/verify-task-completion.sh
# Shows completed tasks and phase progress
```

### Test Specific Feature
```bash
# Auth endpoints
curl -X POST http://localhost:31081/api/auth/login -d '{"email":"test","password":"test"}'

# Rate limiting
for i in {1..110}; do curl -s http://localhost:31081/health -w "%{http_code}\n"; done | grep 429

# Security headers
curl -I http://localhost:31081/health | grep X-Frame

# Health checks
curl http://localhost:31081/health | jq '.checks[] | {name, status}'

# Prometheus metrics
curl http://localhost:31081/metrics | grep insightlearn
```

---

## Common Issues & Fixes

### Build Fails After Adding Package
```bash
dotnet restore
dotnet clean
dotnet build
```

### API Won't Start
```bash
# Check logs
kubectl logs -n insightlearn deployment/insightlearn-api --tail=50

# Check JWT secret configured
echo $JWT_SECRET_KEY

# Check database connection
kubectl exec -it deployment/insightlearn-api -n insightlearn -- printenv | grep ConnectionString
```

### Rate Limiting Not Working
1. Check rate limiter service registered: `grep AddRateLimiter Program.cs`
2. Check middleware enabled: `grep UseRateLimiter Program.cs`
3. Check endpoints have policy: `grep RequireRateLimiting Program.cs`

### Health Check Fails
1. Check dependencies running: `kubectl get pods -n insightlearn`
2. Check connection strings configured
3. Check timeout settings (5 seconds default)

---

## File Locations Cheat Sheet

```
src/
├── InsightLearn.Core/
│   ├── DTOs/                    # Add validation attributes here
│   ├── Entities/                # Add AuditLog entity here
│   └── Interfaces/              # Add method signatures here
├── InsightLearn.Infrastructure/
│   ├── Data/
│   │   └── InsightLearnDbContext.cs  # Add DbSet<AuditLog>
│   └── Repositories/
├── InsightLearn.Application/
│   ├── Program.cs               # Main API, add middleware here
│   ├── Services/                # Implement service methods
│   └── Middleware/              # Create new middleware classes
└── InsightLearn.WebAssembly/    # Frontend (not in this task list)

k8s/
├── 06-api-deployment.yaml       # Update health probes
├── 08-ingress.yaml              # Add security headers
└── grafana-alerts.yaml          # New file, create alert rules

scripts/
├── verify-task-completion.sh    # Check task status
└── generate-jwt-secret.sh       # Generate secure JWT secret

InsightLearn.Tests/              # Create this directory
└── Services/                    # Write unit tests here
```

---

## Phase Checklist

### Phase 1: Critical Fixes ✅
- [ ] P1.2.1: Add method to interface
- [ ] P1.2.2: Implement method in service
- [ ] P1.2.3: Update endpoint

**Time**: 30 minutes
**Test**: `curl http://localhost:31081/api/enrollments?page=1`

---

### Phase 2: Security 🔐
- [ ] P2.1: JWT validation (30 min)
- [ ] P2.2: Rate limiting (2 hours)
- [ ] P2.3: Security headers (1 hour)
- [ ] P2.4: Request validation (2 hours)
- [ ] P2.5: Audit logging (3 hours)

**Time**: 9 hours
**Test**: `./scripts/verify-task-completion.sh | grep "Phase 2"`

---

### Phase 3: Validation 📝
- [ ] P3.1: Add validation to 48 DTOs (5 hours)
- [ ] P3.2: Model validation logging (30 min)
- [ ] P3.3: Safe error handling (2 hours)

**Time**: 7.5 hours
**Test**: Send invalid data, should get 400 with validation errors

---

### Phase 4: Monitoring 📊
- [ ] P4.1: Health checks (2 hours)
- [ ] P4.2: Prometheus metrics (3 hours)
- [ ] P4.3: Grafana alerts (2 hours)

**Time**: 7 hours
**Test**: `curl http://localhost:31081/health | jq`

---

### Phase 5: Certificate PDF 📜
- [ ] P5.1: Add QuestPDF (10 min)
- [ ] P5.2: Create template service (2 hours)
- [ ] P5.3: Update certificate service (45 min)
- [ ] P5.4: Test end-to-end (45 min)

**Time**: 4 hours
**Test**: Complete enrollment, verify PDF generated

---

### Phase 6: Code Quality 🎨
- [ ] P6.1: XML documentation (5 hours)
- [ ] P6.2: Error handling (2 hours)
- [ ] P6.3: Unit tests (16 hours)
- [ ] P6.4: Response consistency (4 hours)

**Time**: 27 hours
**Test**: `dotnet test InsightLearn.Tests/InsightLearn.Tests.csproj`

---

## Git Workflow

### Create Feature Branch
```bash
git checkout -b feature/phase-1-critical-fixes
# Work on tasks...
git add .
git commit -m "fix: implement GetAllEnrollmentsAsync endpoint (P1.2)"
git push -u origin feature/phase-1-critical-fixes
```

### Commit Message Format
```
<type>: <subject> (Task ID)

<body>

Task: P1.2.1
Phase: 1
Estimated: 5 minutes
```

**Types**: `feat`, `fix`, `docs`, `test`, `refactor`, `security`

---

## Daily Standup Template

**Yesterday**:
- Completed P1.2 (Fix enrollments endpoint)
- Started P2.1 (JWT validation)

**Today**:
- Finish P2.1 (JWT validation)
- Start P2.2 (Rate limiting)

**Blockers**:
- None / Need clarification on X

**Progress**: 5/72 tasks (7%)

---

## Success Criteria

### ✅ Definition of Done (per task)
- [ ] Code written and tested locally
- [ ] Build passes (0 errors, 0 warnings)
- [ ] Unit tests written (if applicable)
- [ ] Documentation updated (if needed)
- [ ] Acceptance criteria met
- [ ] Verification command passes
- [ ] Committed to feature branch

### ✅ Definition of Done (per phase)
- [ ] All phase tasks complete
- [ ] Phase verification tests pass
- [ ] Build clean (0 errors, 0 warnings)
- [ ] K8s deployment successful
- [ ] Smoke tests pass
- [ ] No regressions
- [ ] Code reviewed
- [ ] Merged to main

---

## Resources

- **Full Roadmap**: [ROADMAP-TO-PERFECTION.md](ROADMAP-TO-PERFECTION.md)
- **Task Breakdown**: [TASK-BREAKDOWN.md](TASK-BREAKDOWN.md)
- **Quick Start**: [QUICK-START-IMPROVEMENTS.md](QUICK-START-IMPROVEMENTS.md)
- **Tracking**: [IMPROVEMENT-TRACKING.md](IMPROVEMENT-TRACKING.md)
- **CLAUDE.md**: [CLAUDE.md](CLAUDE.md)

---

## Contact & Support

- **Questions**: Check TASK-BREAKDOWN.md for detailed steps
- **Issues**: Create GitHub issue with "improvement-plan" label
- **Urgent**: Ping team lead

---

**Last Updated**: 2025-11-10
**Document Version**: 1.0
