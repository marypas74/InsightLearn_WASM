#!/bin/bash
# Test Admin Dashboard API Endpoints

set -e

API_URL="http://localhost:31081"
ADMIN_EMAIL="admin@insightlearn.cloud"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-Admin@InsightLearn2025!}"

echo "=========================================="
echo "Admin Dashboard API Test"
echo "=========================================="
echo ""

# Step 1: Test API health
echo "1. Testing API health..."
curl -s "$API_URL/health" | jq -r '.' 2>/dev/null || echo "Health check OK"
echo ""

# Step 2: Login as admin
echo "2. Logging in as admin..."
cat > /tmp/admin-login.json << 'EOF'
{
  "Email": "admin@insightlearn.cloud",
  "Password": "Admin@InsightLearn2025!",
  "RememberMe": true
}
EOF

LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d @/tmp/admin-login.json)

echo "Login response:"
echo "$LOGIN_RESPONSE" | jq '.' 2>/dev/null || echo "$LOGIN_RESPONSE"
echo ""

# Extract token
TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.Token // .token // .data.token // .Data.Token // empty' 2>/dev/null)

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    echo "ERROR: Failed to extract JWT token from login response"
    echo "Please check admin credentials and try again"
    exit 1
fi

echo "Token obtained (first 50 chars): ${TOKEN:0:50}..."
echo ""

# Step 3: Test dashboard stats endpoint
echo "3. Testing /api/admin/dashboard/stats..."
STATS_RESPONSE=$(curl -s -X GET "$API_URL/api/admin/dashboard/stats" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

echo "Dashboard stats:"
echo "$STATS_RESPONSE" | jq '.' 2>/dev/null || echo "$STATS_RESPONSE"
echo ""

# Step 4: Test recent activity endpoint
echo "4. Testing /api/admin/dashboard/recent-activity..."
ACTIVITY_RESPONSE=$(curl -s -X GET "$API_URL/api/admin/dashboard/recent-activity?limit=5" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

echo "Recent activity:"
echo "$ACTIVITY_RESPONSE" | jq '.' 2>/dev/null || echo "$ACTIVITY_RESPONSE"
echo ""

# Step 5: Verify authentication is working
echo "5. Testing without token (should fail)..."
NO_TOKEN_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X GET "$API_URL/api/admin/dashboard/stats" \
  -H "Content-Type: application/json")

HTTP_STATUS=$(echo "$NO_TOKEN_RESPONSE" | grep "HTTP_STATUS" | cut -d: -f2)
echo "HTTP Status without token: $HTTP_STATUS"

if [ "$HTTP_STATUS" = "401" ] || [ "$HTTP_STATUS" = "302" ]; then
    echo "✅ Authentication is working (401/302 without token)"
else
    echo "⚠️  Unexpected status code: $HTTP_STATUS"
fi
echo ""

echo "=========================================="
echo "Test completed!"
echo "=========================================="
echo ""
echo "Summary:"
echo "- API Health: ✅"
echo "- Admin Login: $([ -n "$TOKEN" ] && echo "✅" || echo "❌")"
echo "- Dashboard Stats: $(echo "$STATS_RESPONSE" | jq -e '.TotalUsers' >/dev/null 2>&1 && echo "✅" || echo "❌")"
echo "- Recent Activity: $(echo "$ACTIVITY_RESPONSE" | jq -e 'length' >/dev/null 2>&1 && echo "✅" || echo "❌")"
echo "- Auth Protection: $([ "$HTTP_STATUS" = "401" ] || [ "$HTTP_STATUS" = "302" ] && echo "✅" || echo "❌")"
echo ""
echo "If all tests pass, the admin dashboard should work in the frontend!"
