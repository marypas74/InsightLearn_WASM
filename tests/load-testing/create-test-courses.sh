#!/bin/bash
# Create TEST Courses for Load Testing
# Creates FREE courses with tag "TEST" for stress testing

set -e

API_URL="http://localhost:31081"
COURSES_COUNT=50  # Create 50 test courses
ADMIN_EMAIL="admin@insightlearn.cloud"
ADMIN_PASSWORD=""

# Auth token storage
TOKEN_FILE="/tmp/insightlearn-load-test-token.txt"

echo "📚 InsightLearn Course Creator for Load Testing"
echo "==============================================="
echo "Courses to create: $COURSES_COUNT"
echo ""

# Prompt for admin password
if [ -z "$ADMIN_PASSWORD" ]; then
    if [ -f "$TOKEN_FILE" ]; then
        echo "🔑 Using cached authentication token"
        TOKEN=$(cat "$TOKEN_FILE")
    else
        read -sp "Enter admin password: " ADMIN_PASSWORD
        echo ""

        # Login and get JWT token
        echo "🔐 Authenticating..."
        LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/api/auth/login" \
            -H "Content-Type: application/json" \
            -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}")

        TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.token // empty')

        if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
            echo "❌ Authentication failed!"
            exit 1
        fi

        echo "✅ Authenticated successfully"
        echo "$TOKEN" > "$TOKEN_FILE"
    fi
fi

# Get categories (we'll use the first one found)
echo "📂 Fetching categories..."
CATEGORIES=$(curl -s "$API_URL/api/categories")
CATEGORY_ID=$(echo "$CATEGORIES" | jq -r '.[0].id // empty')

if [ -z "$CATEGORY_ID" ]; then
    echo "⚠️  No categories found, creating default..."
    CREATE_CAT=$(curl -s -X POST "$API_URL/api/categories" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d '{"name":"Load Testing","slug":"load-testing","description":"Test category","iconClass":"fa-flask","colorCode":"#FF6B6B"}')
    CATEGORY_ID=$(echo "$CREATE_CAT" | jq -r '.id')
    echo "   ✅ Created category: $CATEGORY_ID"
fi

echo "📁 Using category ID: $CATEGORY_ID"
echo ""

# Course creation loop
CREATED=0
FAILED=0

for i in $(seq 1 $COURSES_COUNT); do
    COURSE_TITLE="[TEST] Load Testing Course #$i"
    COURSE_DESCRIPTION="This is an automatically generated course for load and stress testing purposes. Course number: $i. Generated on $(date '+%Y-%m-%d %H:%M:%S')."

    echo "📝 Creating course $i/$COURSES_COUNT: $COURSE_TITLE"

    COURSE_JSON=$(cat <<EOF
{
    "title": "$COURSE_TITLE",
    "slug": "test-load-course-$i",
    "description": "$COURSE_DESCRIPTION",
    "shortDescription": "Load test course #$i",
    "categoryId": "$CATEGORY_ID",
    "price": 0.00,
    "originalPrice": 0.00,
    "discountPercentage": 0,
    "currency": "EUR",
    "language": "en",
    "level": "Beginner",
    "thumbnailUrl": "/images/test-course-thumbnail.jpg",
    "previewVideoUrl": null,
    "tags": ["TEST", "LOAD-TESTING", "AUTOMATED", "FREE"],
    "whatYouWillLearn": [
        "This is a test course",
        "Used for load testing",
        "Not for real students"
    ],
    "requirements": [
        "None - test course only"
    ],
    "targetAudience": [
        "Load testing automation",
        "System stress testing"
    ],
    "isPublished": true,
    "isFeatured": false,
    "estimatedDurationMinutes": 300
}
EOF
)

    CREATE_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/courses" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "$COURSE_JSON")

    HTTP_CODE=$(echo "$CREATE_RESPONSE" | tail -n1)
    RESPONSE_BODY=$(echo "$CREATE_RESPONSE" | head -n-1)

    if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "201" ]; then
        CREATED=$((CREATED + 1))
        COURSE_ID=$(echo "$RESPONSE_BODY" | jq -r '.id // "unknown"')
        echo "   ✅ Created (ID: $COURSE_ID)"
    else
        FAILED=$((FAILED + 1))
        echo "   ❌ Failed (HTTP $HTTP_CODE)"
        echo "   Response: $RESPONSE_BODY"
    fi

    # Progress every 10 courses
    if [ $((i % 10)) -eq 0 ]; then
        PROGRESS=$((i * 100 / COURSES_COUNT))
        echo "   📊 Progress: ${PROGRESS}%"
    fi
done

echo ""
echo "🎉 Course creation complete!"
echo "✅ Successful: $CREATED"
echo "❌ Failed: $FAILED"
echo ""
echo "🔍 Verify courses at: $API_URL/api/courses?tags=TEST"
echo "🌐 Browse courses: http://localhost:31090/courses (filter by tag TEST)"
