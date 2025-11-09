#!/bin/bash

# Test login with special characters in password
echo "Testing login JSON serialization with special characters..."

# Password with special characters
PASSWORD='Test123!'
EMAIL='admin@insightlearn.cloud'

# Create JSON payload
JSON_PAYLOAD=$(cat <<EOF
{
  "Email": "${EMAIL}",
  "Password": "${PASSWORD}",
  "RememberMe": true
}
EOF
)

echo "=== JSON Payload being sent ==="
echo "$JSON_PAYLOAD"
echo ""

echo "=== Testing API endpoint ==="
echo "URL: http://localhost:31081/api/auth/login"
echo ""

# Make the API call
curl -X POST http://localhost:31081/api/auth/login \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d "$JSON_PAYLOAD" \
  -v 2>&1 | grep -E "< HTTP|< |{|}|400|500|200"

echo ""
echo "=== Expected successful response should contain ==="
echo '{"isSuccess":true,"token":"...", "user":{...}}'
echo ""
echo "=== If error, check that JSON uses PascalCase ==="
echo "Email (not email), Password (not password), RememberMe (not rememberMe)"