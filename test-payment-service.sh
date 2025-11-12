#!/bin/bash

# Test script for Enhanced Payment Service
# This script tests the payment service functionality via API endpoints

API_URL="http://localhost:31081/api"
COURSE_ID="550e8400-e29b-41d4-a716-446655440001"  # Example course ID
USER_ID="550e8400-e29b-41d4-a716-446655440002"    # Example user ID

echo "================================"
echo "Enhanced Payment Service Testing"
echo "================================"
echo ""

# Function to test API endpoint
test_endpoint() {
    local method=$1
    local endpoint=$2
    local data=$3
    local description=$4

    echo "Testing: $description"
    echo "Method: $method"
    echo "Endpoint: $endpoint"

    if [ "$method" = "GET" ]; then
        response=$(curl -s -X GET "$API_URL/$endpoint" \
            -H "Content-Type: application/json")
    else
        response=$(curl -s -X POST "$API_URL/$endpoint" \
            -H "Content-Type: application/json" \
            -d "$data")
    fi

    echo "Response:"
    echo "$response" | python3 -m json.tool 2>/dev/null || echo "$response"
    echo "---"
    echo ""
}

# Test 1: Create Stripe Checkout
echo "1. CREATE STRIPE CHECKOUT"
stripe_data='{
    "userId": "'$USER_ID'",
    "courseId": "'$COURSE_ID'",
    "amount": 99.99,
    "currency": "USD",
    "couponCode": "SAVE20",
    "metadata": {
        "source": "web",
        "campaign": "spring-sale"
    }
}'
test_endpoint "POST" "payments/stripe/checkout" "$stripe_data" "Create Stripe checkout session"

# Test 2: Create PayPal Checkout
echo "2. CREATE PAYPAL CHECKOUT"
paypal_data='{
    "userId": "'$USER_ID'",
    "courseId": "'$COURSE_ID'",
    "amount": 99.99,
    "currency": "USD"
}'
test_endpoint "POST" "payments/paypal/checkout" "$paypal_data" "Create PayPal checkout"

# Test 3: Validate Coupon
echo "3. VALIDATE COUPON"
coupon_data='{
    "couponCode": "SAVE20",
    "courseId": "'$COURSE_ID'",
    "originalAmount": 99.99
}'
test_endpoint "POST" "payments/validate-coupon" "$coupon_data" "Validate coupon code"

# Test 4: Get User Transactions
echo "4. GET USER TRANSACTIONS"
test_endpoint "GET" "payments/transactions/user/$USER_ID" "" "Get user transaction history"

# Test 5: Get Revenue Report
echo "5. GET REVENUE REPORT"
test_endpoint "GET" "payments/revenue?year=2025" "" "Get revenue report for 2025"

echo "================================"
echo "Test completed!"
echo "================================"
echo ""
echo "Note: These are MOCK implementations. Real Stripe/PayPal integration requires:"
echo "  - Valid API keys in configuration"
echo "  - Stripe/PayPal SDK packages installed"
echo "  - Webhook endpoints configured"
echo "  - SSL certificate for production"