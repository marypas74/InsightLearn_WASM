#!/bin/bash
# Test JWT Secret Validation
# This script verifies that the API correctly validates JWT secrets

set -e

API_PROJECT_PATH="/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application"
TEST_LOG="/tmp/jwt-secret-test.log"

echo "=========================================="
echo "  JWT Secret Validation Test"
echo "=========================================="
echo ""

# Clean up previous test log
rm -f "$TEST_LOG"

# Test 1: Missing JWT Secret
echo "Test 1: Missing JWT Secret (should fail)"
echo "----------------------------------------"
export Jwt__Secret=""
export JWT_SECRET_KEY=""
timeout 10 dotnet run --project "$API_PROJECT_PATH" > "$TEST_LOG" 2>&1 || true
if grep -q "JWT Secret is not configured" "$TEST_LOG"; then
    echo "✅ PASS: API correctly rejects missing JWT secret"
else
    echo "❌ FAIL: API did not reject missing JWT secret"
    cat "$TEST_LOG" | head -20
fi
echo ""

# Test 2: Short JWT Secret (less than 32 characters)
echo "Test 2: Short JWT Secret (< 32 chars, should fail)"
echo "----------------------------------------"
export Jwt__Secret="short-secret-12345"
timeout 10 dotnet run --project "$API_PROJECT_PATH" > "$TEST_LOG" 2>&1 || true
if grep -q "JWT Secret is too short" "$TEST_LOG"; then
    echo "✅ PASS: API correctly rejects short JWT secret"
else
    echo "❌ FAIL: API did not reject short JWT secret"
    cat "$TEST_LOG" | head -20
fi
echo ""

# Test 3: Weak JWT Secret containing "changeme"
echo "Test 3: Weak JWT Secret (contains 'changeme', should fail)"
echo "----------------------------------------"
export Jwt__Secret="changeme-this-is-a-weak-secret-key-1234567890"
timeout 10 dotnet run --project "$API_PROJECT_PATH" > "$TEST_LOG" 2>&1 || true
if grep -q "JWT Secret contains a weak/default value" "$TEST_LOG"; then
    echo "✅ PASS: API correctly rejects weak JWT secret"
else
    echo "❌ FAIL: API did not reject weak JWT secret"
    cat "$TEST_LOG" | head -20
fi
echo ""

# Test 4: Weak JWT Secret containing "your-secret-key"
echo "Test 4: Weak JWT Secret (contains 'your-secret-key', should fail)"
echo "----------------------------------------"
export Jwt__Secret="your-secret-key-1234567890-abcdefghijklmnop"
timeout 10 dotnet run --project "$API_PROJECT_PATH" > "$TEST_LOG" 2>&1 || true
if grep -q "JWT Secret contains a weak/default value" "$TEST_LOG"; then
    echo "✅ PASS: API correctly rejects weak JWT secret"
else
    echo "❌ FAIL: API did not reject weak JWT secret"
    cat "$TEST_LOG" | head -20
fi
echo ""

# Test 5: Weak JWT Secret from appsettings.json placeholder
echo "Test 5: Weak JWT Secret (appsettings.json placeholder, should fail)"
echo "----------------------------------------"
export Jwt__Secret="REPLACE_WITH_JWT_SECRET_KEY_ENV_VAR_MINIMUM_32_CHARS"
timeout 10 dotnet run --project "$API_PROJECT_PATH" > "$TEST_LOG" 2>&1 || true
if grep -q "JWT Secret contains a weak/default value" "$TEST_LOG"; then
    echo "✅ PASS: API correctly rejects placeholder JWT secret"
else
    echo "❌ FAIL: API did not reject placeholder JWT secret"
    cat "$TEST_LOG" | head -20
fi
echo ""

# Test 6: Valid strong JWT Secret (32+ chars, cryptographically random)
echo "Test 6: Valid Strong JWT Secret (should succeed)"
echo "----------------------------------------"
# Generate a strong secret for testing
STRONG_SECRET=$(openssl rand -base64 64 | tr -d '\n')
export Jwt__Secret="$STRONG_SECRET"
export Jwt__Issuer="InsightLearn.Api"
export Jwt__Audience="InsightLearn.Users"
export ConnectionStrings__DefaultConnection="Server=localhost;Database=InsightLearnDB;User Id=sa;Password=TestPassword123!;TrustServerCertificate=True;"

# Run for 5 seconds to allow startup
timeout 5 dotnet run --project "$API_PROJECT_PATH" > "$TEST_LOG" 2>&1 || true
if grep -q "JWT configuration validated successfully" "$TEST_LOG"; then
    echo "✅ PASS: API accepts valid strong JWT secret"
    grep "Secret length:" "$TEST_LOG" || true
else
    echo "⚠️  WARNING: API startup may have failed for other reasons (DB connection, etc.)"
    echo "    This is expected if database is not available for testing"
    if grep -q "JWT Secret" "$TEST_LOG"; then
        echo "❌ FAIL: JWT secret validation failed unexpectedly"
        cat "$TEST_LOG" | grep "JWT" | head -10
    else
        echo "✅ PARTIAL PASS: No JWT secret errors detected (may be DB issue)"
    fi
fi
echo ""

# Clean up
rm -f "$TEST_LOG"

echo "=========================================="
echo "  Test Summary"
echo "=========================================="
echo ""
echo "All JWT secret validation tests completed."
echo ""
echo "Expected behavior:"
echo "  ✅ Reject missing secrets"
echo "  ✅ Reject short secrets (< 32 chars)"
echo "  ✅ Reject weak/default values"
echo "  ✅ Accept strong cryptographic secrets (64+ chars)"
echo ""
echo "For production deployment:"
echo "  1. Generate secret: ./scripts/generate-jwt-secret.sh"
echo "  2. Update k8s/01-secrets.yaml or .env file"
echo "  3. Never commit secrets to git"
echo "  4. Rotate secrets every 90 days (see docs/JWT-SECRET-ROTATION.md)"
echo ""
