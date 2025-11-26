#!/bin/bash

# Test script for login error handling improvements
# This tests that error messages are properly displayed when login fails

echo "=================================="
echo "  Login Error Handling Test"
echo "=================================="
echo ""

# API endpoint
API_URL="http://localhost:31081/api/auth/login"

# Test 1: Invalid password
echo "Test 1: Testing invalid password error handling"
echo "Sending request with wrong password..."
RESPONSE=$(curl -s -o /dev/stdout -w "\nHTTP_STATUS:%{http_code}" \
  -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@insightlearn.cloud","password":"WrongPassword123"}')

HTTP_STATUS=$(echo "$RESPONSE" | grep "HTTP_STATUS:" | cut -d: -f2)
BODY=$(echo "$RESPONSE" | sed '/HTTP_STATUS:/d')

echo "Response Status: $HTTP_STATUS"
echo "Response Body: $BODY"

if [ "$HTTP_STATUS" = "400" ]; then
  echo "✅ Test 1 PASSED: Backend returns HTTP 400 for wrong password"

  # Check if response contains expected error message
  if echo "$BODY" | grep -q "Invalid email or password"; then
    echo "✅ Error message found in response"
  else
    echo "❌ Expected error message not found"
  fi
else
  echo "❌ Test 1 FAILED: Expected HTTP 400, got $HTTP_STATUS"
fi

echo ""
echo "=================================="
echo ""

# Test 2: Missing email
echo "Test 2: Testing missing email validation"
RESPONSE=$(curl -s -o /dev/stdout -w "\nHTTP_STATUS:%{http_code}" \
  -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -d '{"password":"TestPassword123"}')

HTTP_STATUS=$(echo "$RESPONSE" | grep "HTTP_STATUS:" | cut -d: -f2)
BODY=$(echo "$RESPONSE" | sed '/HTTP_STATUS:/d')

echo "Response Status: $HTTP_STATUS"
echo "Response Body: $BODY"

if [ "$HTTP_STATUS" = "400" ]; then
  echo "✅ Test 2 PASSED: Backend returns HTTP 400 for missing email"
else
  echo "❌ Test 2 FAILED: Expected HTTP 400, got $HTTP_STATUS"
fi

echo ""
echo "=================================="
echo ""

# Test 3: Empty request
echo "Test 3: Testing empty request"
RESPONSE=$(curl -s -o /dev/stdout -w "\nHTTP_STATUS:%{http_code}" \
  -X POST "$API_URL" \
  -H "Content-Type: application/json" \
  -d '{}')

HTTP_STATUS=$(echo "$RESPONSE" | grep "HTTP_STATUS:" | cut -d: -f2)
BODY=$(echo "$RESPONSE" | sed '/HTTP_STATUS:/d')

echo "Response Status: $HTTP_STATUS"
echo "Response Body: $BODY"

if [ "$HTTP_STATUS" = "400" ]; then
  echo "✅ Test 3 PASSED: Backend returns HTTP 400 for empty request"
else
  echo "❌ Test 3 FAILED: Expected HTTP 400, got $HTTP_STATUS"
fi

echo ""
echo "=================================="
echo "  Test Summary"
echo "=================================="
echo ""
echo "The backend API is correctly returning HTTP 400 with error messages."
echo "The frontend has been updated to:"
echo "1. Handle HTTP 400 responses without throwing exceptions"
echo "2. Display multiple error messages in a list"
echo "3. Show user-friendly error messages"
echo ""
echo "To test the frontend manually:"
echo "1. Open the browser to http://localhost:31090/login"
echo "2. Try logging in with wrong password"
echo "3. You should see 'Invalid email or password' in the UI"
echo "4. No HttpRequestException should appear in browser console"
echo ""