#!/bin/bash

# JWT Secret Hardening Test Script
# Tests all validation requirements from Phase 2.1

echo "======================================"
echo "JWT SECRET HARDENING TEST SUITE"
echo "======================================"
echo ""

PROJECT_PATH="/home/mpasqui/insightlearn_WASM/InsightLearn_WASM/src/InsightLearn.Application"

# Save original environment
ORIGINAL_JWT_SECRET_KEY="${JWT_SECRET_KEY:-}"

# Test 1: No JWT secret configured (should FAIL)
echo "TEST 1: No JWT secret configured"
echo "Expected: Application startup fails with clear error"
unset JWT_SECRET_KEY
cd "$PROJECT_PATH"
timeout 10s dotnet run --no-build 2>&1 | grep -A 3 "JWT Secret Key is not configured" && echo "✅ PASS: App correctly rejects missing JWT secret" || echo "❌ FAIL: App should reject missing JWT secret"
echo ""

# Test 2: JWT secret too short (should FAIL)
echo "TEST 2: JWT secret too short (test)"
echo "Expected: Application startup fails (less than 32 characters)"
export JWT_SECRET_KEY="test"
timeout 10s dotnet run --no-build 2>&1 | grep -A 2 "JWT Secret Key is too short" && echo "✅ PASS: App correctly rejects short secret" || echo "❌ FAIL: App should reject short secret"
echo ""

# Test 3: Insecure value "changeme" (should FAIL)
echo "TEST 3: Insecure value 'changeme' (32+ chars)"
echo "Expected: Application startup fails (insecure default value)"
export JWT_SECRET_KEY="changeme12345678901234567890123"
timeout 10s dotnet run --no-build 2>&1 | grep -A 2 "insecure/default value" && echo "✅ PASS: App correctly rejects insecure value" || echo "❌ FAIL: App should reject insecure value"
echo ""

# Test 4: Insecure value "test" (should FAIL)
echo "TEST 4: Insecure value 'test' in string"
echo "Expected: Application startup fails (insecure default value)"
export JWT_SECRET_KEY="mytestapikey1234567890123456789012"
timeout 10s dotnet run --no-build 2>&1 | grep -A 2 "insecure/default value" && echo "✅ PASS: App correctly rejects 'test' in secret" || echo "❌ FAIL: App should reject 'test' in secret"
echo ""

# Test 5: Valid secret from environment variable (should SUCCEED)
echo "TEST 5: Valid 64-character secret from ENV VAR"
echo "Expected: Application starts successfully, logs env var usage"
export JWT_SECRET_KEY="$(openssl rand -base64 48 | tr -d '\n')"
timeout 5s dotnet run --no-build 2>&1 | grep -A 1 "JWT Secret loaded from JWT_SECRET_KEY environment variable" && echo "✅ PASS: App accepts valid env var secret" || echo "⚠️  TIMEOUT/FAIL: App should start with valid secret (may need longer timeout)"
echo ""

# Test 6: Valid secret from appsettings.json with warning (should SUCCEED with warning)
echo "TEST 6: Valid secret from appsettings.json (should warn)"
echo "Expected: Application starts but logs security warning"
unset JWT_SECRET_KEY
# Update appsettings.json temporarily
cp "$PROJECT_PATH/appsettings.json" "$PROJECT_PATH/appsettings.json.bak"
cat > "$PROJECT_PATH/appsettings.json.tmp" << 'JSONEOF'
{
  "Jwt": {
    "Secret": "ThisIsASecureRandomSecretKeyForTestingPurposesOnly1234567890",
    "Issuer": "InsightLearn.Api",
    "Audience": "InsightLearn.Users",
    "ExpirationDays": 7
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InsightLearnDB;User Id=sa;Password=Test123!;TrustServerCertificate=True;"
  }
}
JSONEOF
mv "$PROJECT_PATH/appsettings.json.tmp" "$PROJECT_PATH/appsettings.json"
timeout 5s dotnet run --no-build 2>&1 | grep "JWT Secret is loaded from appsettings.json" && echo "✅ PASS: App warns about appsettings.json usage" || echo "⚠️  App may need configuration updates"
# Restore original
mv "$PROJECT_PATH/appsettings.json.bak" "$PROJECT_PATH/appsettings.json"
echo ""

# Restore original environment
if [ -n "$ORIGINAL_JWT_SECRET_KEY" ]; then
    export JWT_SECRET_KEY="$ORIGINAL_JWT_SECRET_KEY"
else
    unset JWT_SECRET_KEY
fi

echo "======================================"
echo "TEST SUITE COMPLETED"
echo "======================================"
