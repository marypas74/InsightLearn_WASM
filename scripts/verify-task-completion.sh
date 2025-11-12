#!/bin/bash
# Task Completion Verification Script
# Automatically checks which tasks have been completed

echo "========================================"
echo "  InsightLearn - Task Verification"
echo "========================================"
echo ""

PASSED=0
FAILED=0
SKIPPED=0

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Helper functions
check_pass() {
    echo -e "${GREEN}✅ PASS${NC}: $1"
    ((PASSED++))
}

check_fail() {
    echo -e "${RED}❌ FAIL${NC}: $1"
    ((FAILED++))
}

check_skip() {
    echo -e "${YELLOW}⚠️  SKIP${NC}: $1"
    ((SKIPPED++))
}

# Phase 1: Critical Fixes
echo "Phase 1: Critical Fixes"
echo "========================"

# P1.2: Check if GetAllEnrollmentsAsync exists in interface
if grep -q "GetAllEnrollmentsAsync" src/InsightLearn.Core/Interfaces/IEnrollmentService.cs 2>/dev/null; then
    check_pass "P1.2.1 - GetAllEnrollmentsAsync in interface"
else
    check_fail "P1.2.1 - GetAllEnrollmentsAsync NOT in interface"
fi

# P1.3: Check if method implemented in service
if grep -q "GetAllEnrollmentsAsync" src/InsightLearn.Application/Services/EnrollmentService.cs 2>/dev/null; then
    check_pass "P1.2.2 - GetAllEnrollmentsAsync implemented"
else
    check_fail "P1.2.2 - GetAllEnrollmentsAsync NOT implemented"
fi

# P1.4: Check if endpoint updated (not returning 501)
if grep -q "NotImplemented" src/InsightLearn.Application/Program.cs | grep -q "enrollments"; then
    check_fail "P1.2.3 - Enrollments endpoint still returns 501"
else
    check_pass "P1.2.3 - Enrollments endpoint updated"
fi

echo ""

# Phase 2: Security Hardening
echo "Phase 2: Security Hardening"
echo "==========================="

# P2.1: Check JWT secret validation
if grep -q "InvalidOperationException.*JWT Secret" src/InsightLearn.Application/Program.cs 2>/dev/null; then
    check_pass "P2.1.1 - JWT secret validation implemented"
else
    check_fail "P2.1.1 - JWT secret validation NOT implemented"
fi

# P2.2: Check rate limiting
if grep -q "AddRateLimiter" src/InsightLearn.Application/Program.cs 2>/dev/null; then
    check_pass "P2.2.1 - Rate limiter configured"
else
    check_fail "P2.2.1 - Rate limiter NOT configured"
fi

if grep -q "UseRateLimiter" src/InsightLearn.Application/Program.cs 2>/dev/null; then
    check_pass "P2.2.2 - Rate limiter middleware enabled"
else
    check_fail "P2.2.2 - Rate limiter middleware NOT enabled"
fi

# P2.3: Check security headers
if grep -q "X-Frame-Options" src/InsightLearn.Application/Program.cs 2>/dev/null; then
    check_pass "P2.3.1 - Security headers middleware added"
else
    check_fail "P2.3.1 - Security headers middleware NOT added"
fi

# P2.4: Check request validation middleware
if [ -f "src/InsightLearn.Application/Middleware/RequestValidationMiddleware.cs" ]; then
    check_pass "P2.4.1 - RequestValidationMiddleware created"
else
    check_fail "P2.4.1 - RequestValidationMiddleware NOT created"
fi

# P2.5: Check audit logging
if [ -f "src/InsightLearn.Core/Entities/AuditLog.cs" ]; then
    check_pass "P2.5.1 - AuditLog entity created"
else
    check_fail "P2.5.1 - AuditLog entity NOT created"
fi

if grep -q "DbSet<AuditLog>" src/InsightLearn.Infrastructure/Data/InsightLearnDbContext.cs 2>/dev/null; then
    check_pass "P2.5.2 - AuditLog added to DbContext"
else
    check_fail "P2.5.2 - AuditLog NOT in DbContext"
fi

if [ -f "src/InsightLearn.Application/Services/AuditService.cs" ]; then
    check_pass "P2.5.3 - AuditService created"
else
    check_fail "P2.5.3 - AuditService NOT created"
fi

echo ""

# Phase 3: Validation
echo "Phase 3: Validation"
echo "==================="

# Check if DTOs have validation attributes
ENROLLMENT_DTO_COUNT=$(grep -l "Required\|StringLength\|Range" src/InsightLearn.Core/DTOs/Enrollment/*.cs 2>/dev/null | wc -l)
if [ "$ENROLLMENT_DTO_COUNT" -ge 2 ]; then
    check_pass "P3.1.1 - Enrollment DTOs validated ($ENROLLMENT_DTO_COUNT files)"
else
    check_fail "P3.1.1 - Enrollment DTOs validation incomplete ($ENROLLMENT_DTO_COUNT files)"
fi

COURSE_DTO_COUNT=$(grep -l "Required\|StringLength\|Range" src/InsightLearn.Core/DTOs/Course/*.cs 2>/dev/null | wc -l)
if [ "$COURSE_DTO_COUNT" -ge 2 ]; then
    check_pass "P3.1.2 - Course DTOs validated ($COURSE_DTO_COUNT files)"
else
    check_fail "P3.1.2 - Course DTOs validation incomplete ($COURSE_DTO_COUNT files)"
fi

# P3.2: Check error handling
if [ -f "src/InsightLearn.Core/DTOs/ErrorResponse.cs" ]; then
    check_pass "P6.2.1 - ErrorResponse DTO created"
else
    check_fail "P6.2.1 - ErrorResponse DTO NOT created"
fi

if [ -f "src/InsightLearn.Application/Middleware/GlobalExceptionHandlerMiddleware.cs" ]; then
    check_pass "P6.2.2 - GlobalExceptionHandlerMiddleware created"
else
    check_fail "P6.2.2 - GlobalExceptionHandlerMiddleware NOT created"
fi

echo ""

# Phase 4: Monitoring
echo "Phase 4: Monitoring"
echo "==================="

# P4.1: Check health checks
if grep -q "AddSqlServer" src/InsightLearn.Application/Program.cs 2>/dev/null; then
    check_pass "P4.1.1 - SQL Server health check added"
else
    check_fail "P4.1.1 - SQL Server health check NOT added"
fi

if grep -q "AddMongoDb" src/InsightLearn.Application/Program.cs 2>/dev/null; then
    check_pass "P4.1.2 - MongoDB health check added"
else
    check_fail "P4.1.2 - MongoDB health check NOT added"
fi

if grep -q "AddRedis" src/InsightLearn.Application/Program.cs 2>/dev/null; then
    check_pass "P4.1.3 - Redis health check added"
else
    check_fail "P4.1.3 - Redis health check NOT added"
fi

# Check for multiple health endpoints
if grep -q "/health/live" src/InsightLearn.Application/Program.cs 2>/dev/null; then
    check_pass "P4.1.6 - Multiple health endpoints (/live, /ready)"
else
    check_fail "P4.1.6 - Multiple health endpoints NOT added"
fi

# P4.2: Check metrics
if [ -f "src/InsightLearn.Application/Services/MetricsService.cs" ]; then
    check_pass "P4.2.1 - MetricsService created"
else
    check_fail "P4.2.1 - MetricsService NOT created"
fi

# P4.3: Check Grafana alerts
if [ -f "k8s/grafana-alerts.yaml" ]; then
    check_pass "P4.3.1 - Grafana alert rules created"
else
    check_fail "P4.3.1 - Grafana alert rules NOT created"
fi

echo ""

# Phase 5: Certificate Generation
echo "Phase 5: Certificate Generation"
echo "==============================="

# P5.1: Check QuestPDF
if grep -q "QuestPDF" src/InsightLearn.Application/InsightLearn.Application.csproj 2>/dev/null; then
    check_pass "P5.1.1 - QuestPDF package added"
else
    check_fail "P5.1.1 - QuestPDF package NOT added"
fi

# P5.2: Check CertificateTemplateService
if [ -f "src/InsightLearn.Application/Services/CertificateTemplateService.cs" ]; then
    check_pass "P5.1.2 - CertificateTemplateService created"
else
    check_fail "P5.1.2 - CertificateTemplateService NOT created"
fi

echo ""

# Phase 6: Code Quality
echo "Phase 6: Code Quality"
echo "====================="

# P6.1: Check XML documentation
if grep -q "GenerateDocumentationFile" src/InsightLearn.Application/InsightLearn.Application.csproj 2>/dev/null; then
    check_pass "P6.1.1 - XML documentation generation enabled"
else
    check_fail "P6.1.1 - XML documentation NOT enabled"
fi

# P6.3: Check unit tests
if [ -d "InsightLearn.Tests" ]; then
    check_pass "P6.3.1 - Unit test project created"

    # Count test files
    TEST_FILE_COUNT=$(find InsightLearn.Tests -name "*Tests.cs" 2>/dev/null | wc -l)
    if [ "$TEST_FILE_COUNT" -ge 4 ]; then
        check_pass "P6.3.2-5 - Test files created ($TEST_FILE_COUNT files)"
    else
        check_fail "P6.3.2-5 - Not enough test files ($TEST_FILE_COUNT files, need 4+)"
    fi
else
    check_fail "P6.3.1 - Unit test project NOT created"
fi

# P6.4: Check response wrappers
if [ -f "src/InsightLearn.Core/DTOs/ApiResponse.cs" ]; then
    check_pass "P6.4.1 - ApiResponse wrapper created"
else
    check_fail "P6.4.1 - ApiResponse wrapper NOT created"
fi

echo ""

# Build Verification
echo "Build Verification"
echo "=================="

# Check if solution builds
if dotnet build InsightLearn.WASM.sln --no-restore > /tmp/build.log 2>&1; then
    ERROR_COUNT=$(grep -c "error" /tmp/build.log || echo 0)
    WARNING_COUNT=$(grep -c "warning" /tmp/build.log || echo 0)

    if [ "$ERROR_COUNT" -eq 0 ]; then
        check_pass "Build: 0 errors"
    else
        check_fail "Build: $ERROR_COUNT errors"
    fi

    if [ "$WARNING_COUNT" -eq 0 ]; then
        check_pass "Build: 0 warnings"
    else
        check_fail "Build: $WARNING_COUNT warnings"
    fi
else
    check_fail "Build: Failed to compile"
fi

echo ""
echo "========================================"
echo "  Summary"
echo "========================================"
echo -e "${GREEN}Passed:${NC}  $PASSED"
echo -e "${RED}Failed:${NC}  $FAILED"
echo -e "${YELLOW}Skipped:${NC} $SKIPPED"
echo ""

TOTAL=$((PASSED + FAILED + SKIPPED))
PERCENTAGE=$(echo "scale=1; $PASSED * 100 / $TOTAL" | bc)
echo "Completion: $PERCENTAGE% ($PASSED/$TOTAL tasks)"

# Calculate phase completion
echo ""
echo "Phase Completion:"
echo "  Phase 1 (Critical Fixes):     $([ $PASSED -ge 3 ] && echo "✅ Complete" || echo "⏳ In Progress")"
echo "  Phase 2 (Security):           $([ $PASSED -ge 10 ] && echo "✅ Complete" || echo "⏳ In Progress")"
echo "  Phase 3 (Validation):         $([ $PASSED -ge 15 ] && echo "✅ Complete" || echo "⏳ In Progress")"
echo "  Phase 4 (Monitoring):         $([ $PASSED -ge 22 ] && echo "✅ Complete" || echo "⏳ In Progress")"
echo "  Phase 5 (Certificate):        $([ $PASSED -ge 24 ] && echo "✅ Complete" || echo "⏳ In Progress")"
echo "  Phase 6 (Quality):            $([ $PASSED -ge 28 ] && echo "✅ Complete" || echo "⏳ In Progress")"

echo ""
if [ "$FAILED" -eq 0 ]; then
    echo -e "${GREEN}🎉 All checked tasks completed successfully!${NC}"
    exit 0
else
    echo -e "${YELLOW}⚠️  Some tasks incomplete. See details above.${NC}"
    exit 1
fi
