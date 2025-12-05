#!/bin/bash
# Upload Test Videos to MongoDB via API
# Uses InsightLearn API /api/video/upload endpoint

set -e

API_URL="http://localhost:31081"
VIDEO_DIR="/tmp/insightlearn-test-videos"
ADMIN_EMAIL="admin@insightlearn.cloud"
ADMIN_PASSWORD=""  # Will prompt

# Auth token storage
TOKEN_FILE="/tmp/insightlearn-load-test-token.txt"

echo "🚀 InsightLearn Video Upload for Load Testing"
echo "=============================================="
echo ""

# Prompt for admin password
if [ -z "$ADMIN_PASSWORD" ]; then
    read -sp "Enter admin password: " ADMIN_PASSWORD
    echo ""
fi

# Login and get JWT token
echo "🔐 Authenticating..."
LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}")

TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.token // empty')

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    echo "❌ Authentication failed!"
    echo "Response: $LOGIN_RESPONSE"
    exit 1
fi

echo "✅ Authenticated successfully"
echo "$TOKEN" > "$TOKEN_FILE"

# Count videos to upload
TOTAL_VIDEOS=$(ls -1 "$VIDEO_DIR"/*.mp4 2>/dev/null | wc -l)

if [ "$TOTAL_VIDEOS" -eq 0 ]; then
    echo "❌ No videos found in $VIDEO_DIR"
    echo "Run generate-test-videos.sh first"
    exit 1
fi

echo "📦 Found $TOTAL_VIDEOS videos to upload"
echo ""

# Create a test lesson ID (or use existing)
# For load testing, we'll use a fixed UUID
TEST_LESSON_ID="00000000-0000-0000-0000-000000000001"
TEST_USER_ID="00000000-0000-0000-0000-000000000002"

UPLOADED=0
FAILED=0

for VIDEO_FILE in "$VIDEO_DIR"/*.mp4; do
    VIDEO_NAME=$(basename "$VIDEO_FILE")

    echo "📤 Uploading $VIDEO_NAME..."

    UPLOAD_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/video/upload" \
        -H "Authorization: Bearer $TOKEN" \
        -F "file=@$VIDEO_FILE" \
        -F "lessonId=$TEST_LESSON_ID" \
        -F "userId=$TEST_USER_ID" \
        -F "title=Load Test Video - $VIDEO_NAME")

    HTTP_CODE=$(echo "$UPLOAD_RESPONSE" | tail -n1)
    RESPONSE_BODY=$(echo "$UPLOAD_RESPONSE" | head -n-1)

    if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "201" ]; then
        UPLOADED=$((UPLOADED + 1))
        FILE_ID=$(echo "$RESPONSE_BODY" | jq -r '.fileId // "unknown"')
        echo "   ✅ Uploaded successfully (FileID: $FILE_ID)"
    else
        FAILED=$((FAILED + 1))
        echo "   ❌ Upload failed (HTTP $HTTP_CODE)"
        echo "   Response: $RESPONSE_BODY"
    fi

    # Progress every 10 videos
    if [ $((UPLOADED % 10)) -eq 0 ]; then
        PROGRESS=$((UPLOADED * 100 / TOTAL_VIDEOS))
        echo "   📊 Progress: ${PROGRESS}% (${UPLOADED}/${TOTAL_VIDEOS})"
    fi

    # Small delay to avoid overwhelming the API
    sleep 0.5
done

echo ""
echo "🎉 Upload complete!"
echo "✅ Successful: $UPLOADED"
echo "❌ Failed: $FAILED"
echo "📊 Success rate: $((UPLOADED * 100 / TOTAL_VIDEOS))%"

# Check MongoDB storage
echo ""
echo "💾 Checking MongoDB storage..."
kubectl exec -n insightlearn mongodb-0 -- mongosh -u insightlearn -p "$(kubectl get secret -n insightlearn insightlearn-secrets -o jsonpath='{.data.mongodb-password}' | base64 -d)" --authenticationDatabase admin insightlearn_videos --eval "db.stats(1024*1024*1024)" 2>/dev/null | grep -E "dataSize|storageSize"
