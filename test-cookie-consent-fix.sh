#!/bin/bash

# Test script for Cookie Consent Wall redirect loop fix
# This script simulates the login flow and checks for redirect loops

echo "========================================"
echo "Cookie Consent Wall Fix Test Script"
echo "========================================"
echo

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
BASE_URL="${1:-https://wasm.insightlearn.cloud}"
LOGIN_ENDPOINT="${BASE_URL}/login"
DASHBOARD_ENDPOINT="${BASE_URL}/admin/dashboard"

echo "Testing endpoints:"
echo "  Base URL: $BASE_URL"
echo "  Login: $LOGIN_ENDPOINT"
echo "  Dashboard: $DASHBOARD_ENDPOINT"
echo

# Test 1: Check if login page is accessible without redirect loop
echo -n "Test 1: Login page accessibility... "
response=$(curl -s -o /dev/null -w "%{http_code}" -L --max-redirs 5 "$LOGIN_ENDPOINT" 2>/dev/null)
if [ "$response" = "200" ]; then
    echo -e "${GREEN}PASS${NC} (HTTP $response)"
else
    echo -e "${RED}FAIL${NC} (HTTP $response)"
fi

# Test 2: Check redirect chain for dashboard (should redirect to login)
echo -n "Test 2: Dashboard redirect to login... "
redirect_url=$(curl -s -o /dev/null -w "%{url_effective}" -L --max-redirs 1 "$DASHBOARD_ENDPOINT" 2>/dev/null)
if [[ "$redirect_url" == *"/login"* ]]; then
    echo -e "${GREEN}PASS${NC} (Redirects to login)"
else
    echo -e "${YELLOW}WARNING${NC} (Unexpected redirect: $redirect_url)"
fi

# Test 3: Check for infinite redirects
echo -n "Test 3: Checking for redirect loops... "
redirect_count=$(curl -s -o /dev/null -w "%{num_redirects}" -L --max-redirs 10 "$DASHBOARD_ENDPOINT" 2>/dev/null)
if [ "$redirect_count" -lt 10 ]; then
    echo -e "${GREEN}PASS${NC} ($redirect_count redirects)"
else
    echo -e "${RED}FAIL${NC} (Possible redirect loop: $redirect_count redirects)"
fi

# Test 4: Check if cookie consent wall JS is loaded
echo -n "Test 4: Cookie consent JS loaded... "
js_check=$(curl -s "$BASE_URL/js/cookie-consent-wall.js" | head -1)
if [[ "$js_check" == *"Cookie Consent Wall"* ]]; then
    echo -e "${GREEN}PASS${NC}"
else
    echo -e "${RED}FAIL${NC} (JS file not accessible)"
fi

echo
echo "========================================"
echo "Test Summary:"
echo "========================================"
echo "The cookie consent wall should:"
echo "1. NOT appear on login/register pages"
echo "2. NOT cause redirect loops"
echo "3. Show AFTER successful login on protected pages"
echo "4. Allow users to accept cookies and continue"
echo
echo "If all tests pass, the fix is working correctly."
echo "For full verification, manually test the login flow in a browser."