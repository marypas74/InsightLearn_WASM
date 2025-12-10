#!/bin/bash
# Create 100 TEST Videos + FREE Courses for Stress Testing
# FIXED VERSION: Uses file-based JSON to avoid bash escaping issues with ! character
# Safe disk usage: 100 × 10MB = 1GB total

set -e

API_URL="http://localhost:31081"
VIDEO_DIR="/home/mpasqui/test-videos-temp"
ADMIN_EMAIL="admin@insightlearn.cloud"
ADMIN_PASSWORD="Admin123!Secure"

# Configuration
TOTAL_VIDEOS=100
FILE_SIZE_MB=10
COURSES_COUNT=10  # 10 courses × 10 lessons = 100 videos

echo "Creating 100 TEST Videos for Stress Testing"
echo "=============================================="
echo ""
echo "Configuration:"
echo "  - Videos: $TOTAL_VIDEOS × ${FILE_SIZE_MB}MB = ~1GB"
echo "  - Courses: $COURSES_COUNT FREE with tag TEST"
echo ""

# ============================================
# 1. Authentication (using file-based JSON to avoid bash ! escaping)
# ============================================
echo "Authenticating..."

# Create login JSON file (avoids bash history expansion of ! character)
LOGIN_JSON_FILE=$(mktemp)
cat > "$LOGIN_JSON_FILE" << 'LOGINEOF'
{"email":"admin@insightlearn.cloud","password":"Admin123!Secure"}
LOGINEOF

LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -d @"$LOGIN_JSON_FILE")

rm -f "$LOGIN_JSON_FILE"

# Extract token (API returns PascalCase)
TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.Token // .token // empty')
USER_ID=$(echo "$LOGIN_RESPONSE" | jq -r '.User.Id // .User.id // .userId // empty')

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    echo "Auth failed!"
    echo "Response: $LOGIN_RESPONSE"
    exit 1
fi

echo "Authenticated successfully!"
echo "  UserID: $USER_ID"
echo "  Token: ${TOKEN:0:50}..."
echo ""

# ============================================
# 2. Generate Test Video Files
# ============================================
echo "Generating $TOTAL_VIDEOS test files..."
mkdir -p "$VIDEO_DIR"

for i in $(seq 1 $TOTAL_VIDEOS); do
    FILE="$VIDEO_DIR/test-video-$(printf '%03d' $i).mp4"
    if [ ! -f "$FILE" ]; then
        dd if=/dev/urandom of="$FILE" bs=1M count=$FILE_SIZE_MB 2>/dev/null
        echo -ne "\r  Generated: $i/$TOTAL_VIDEOS"
    fi
done
echo ""
echo "All files generated"
echo ""

# ============================================
# 3. Get or Create Category
# ============================================
echo "Setting up category..."
CATEGORIES=$(curl -s "$API_URL/api/categories" -H "Authorization: Bearer $TOKEN")
CATEGORY_ID=$(echo "$CATEGORIES" | jq -r '.[] | select(.name | test("Test|Load|Stress"; "i")) | .id' | head -n1)

if [ -z "$CATEGORY_ID" ] || [ "$CATEGORY_ID" = "null" ]; then
    echo "Creating 'Stress Testing' category..."
    CREATE_CAT=$(curl -s -X POST "$API_URL/api/categories" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d '{"name":"Stress Testing","slug":"stress-testing","description":"Test courses for stress testing","iconClass":"fa-flask","colorCode":"#FF6B6B"}')
    CATEGORY_ID=$(echo "$CREATE_CAT" | jq -r '.id // .Id // empty')
fi

if [ -z "$CATEGORY_ID" ] || [ "$CATEGORY_ID" = "null" ]; then
    echo "Warning: Could not get category ID, using default"
    CATEGORY_ID="00000000-0000-0000-0000-000000000001"
fi

echo "Category ID: $CATEGORY_ID"
echo ""

# ============================================
# 4. Create Courses and Upload Videos
# ============================================
echo "Creating $COURSES_COUNT courses with videos..."

VIDEO_INDEX=1
UPLOADED=0
FAILED=0

for course_num in $(seq 1 $COURSES_COUNT); do
    echo ""
    echo "Course $course_num/$COURSES_COUNT..."

    # Create course JSON file
    COURSE_JSON_FILE=$(mktemp)
    TIMESTAMP=$(date +%s)
    cat > "$COURSE_JSON_FILE" << EOF
{
    "title": "[TEST] Stress Test Course #$course_num",
    "slug": "test-stress-course-$course_num-$TIMESTAMP",
    "description": "Automated stress testing course #$course_num. Contains 10 video lessons. Generated $(date '+%Y-%m-%d %H:%M').",
    "shortDescription": "Stress test course with real video uploads",
    "categoryId": "$CATEGORY_ID",
    "instructorId": "$USER_ID",
    "price": 0.00,
    "currency": "EUR",
    "language": "en",
    "level": "Beginner",
    "thumbnailUrl": "/images/default-course.jpg",
    "tags": ["TEST", "FREE", "STRESS-TEST", "AUTOMATED"],
    "whatYouWillLearn": ["Stress testing MongoDB GridFS", "API load testing"],
    "requirements": ["None - test only"],
    "targetAudience": ["Automated testing systems"],
    "isPublished": true,
    "isFeatured": false,
    "estimatedDurationMinutes": 50
}
EOF

    COURSE_RESPONSE=$(curl -s -X POST "$API_URL/api/courses" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d @"$COURSE_JSON_FILE")

    rm -f "$COURSE_JSON_FILE"

    COURSE_ID=$(echo "$COURSE_RESPONSE" | jq -r '.id // .Id // empty')

    if [ -z "$COURSE_ID" ] || [ "$COURSE_ID" = "null" ]; then
        echo "  Failed to create course: $(echo "$COURSE_RESPONSE" | head -c 200)"
        continue
    fi

    echo "  Course created: $COURSE_ID"

    # Create section
    SECTION_JSON='{"courseId":"'$COURSE_ID'","title":"Video Lessons","description":"Test video content","orderIndex":1}'
    SECTION_RESPONSE=$(curl -s -X POST "$API_URL/api/courses/$COURSE_ID/sections" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "$SECTION_JSON")

    SECTION_ID=$(echo "$SECTION_RESPONSE" | jq -r '.id // .Id // empty')

    if [ -z "$SECTION_ID" ] || [ "$SECTION_ID" = "null" ]; then
        echo "  Warning: Failed to create section, using course ID as fallback"
        SECTION_ID=$COURSE_ID
    fi

    # Create 10 lessons per course and upload videos
    LESSONS_PER_COURSE=10

    for lesson_num in $(seq 1 $LESSONS_PER_COURSE); do
        if [ $VIDEO_INDEX -gt $TOTAL_VIDEOS ]; then
            break
        fi

        # Create lesson
        LESSON_JSON='{"sectionId":"'$SECTION_ID'","title":"Lesson '$lesson_num' - Video Content","description":"Test video lesson","type":"Video","orderIndex":'$lesson_num',"durationMinutes":5,"isFree":true}'

        LESSON_RESPONSE=$(curl -s -X POST "$API_URL/api/courses/$COURSE_ID/sections/$SECTION_ID/lessons" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "$LESSON_JSON")

        LESSON_ID=$(echo "$LESSON_RESPONSE" | jq -r '.id // .Id // empty')

        if [ -z "$LESSON_ID" ] || [ "$LESSON_ID" = "null" ]; then
            # Generate a random UUID as fallback
            LESSON_ID=$(cat /proc/sys/kernel/random/uuid)
        fi

        # Upload video
        VIDEO_FILE="$VIDEO_DIR/test-video-$(printf '%03d' $VIDEO_INDEX).mp4"

        if [ -f "$VIDEO_FILE" ]; then
            HTTP_CODE=$(curl -s -w "%{http_code}" -o /tmp/upload_response.json \
                -X POST "$API_URL/api/video/upload" \
                -H "Authorization: Bearer $TOKEN" \
                -F "file=@$VIDEO_FILE" \
                -F "lessonId=$LESSON_ID" \
                -F "userId=$USER_ID" \
                -F "title=Test Video $VIDEO_INDEX" \
                --max-time 120)

            if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "201" ]; then
                UPLOADED=$((UPLOADED + 1))
                echo -ne "\r  Uploaded: $UPLOADED/$TOTAL_VIDEOS (video #$VIDEO_INDEX)   "
            else
                FAILED=$((FAILED + 1))
                echo -ne "\r  Failed upload #$VIDEO_INDEX (HTTP $HTTP_CODE)   "
            fi
        fi

        VIDEO_INDEX=$((VIDEO_INDEX + 1))
    done
done

echo ""
echo ""

# ============================================
# 5. Report
# ============================================
echo "========================================"
echo "           FINAL REPORT"
echo "========================================"
echo ""
echo "Uploads:"
echo "  Successful: $UPLOADED"
echo "  Failed: $FAILED"
if [ $TOTAL_VIDEOS -gt 0 ]; then
    echo "  Success rate: $(((UPLOADED * 100) / TOTAL_VIDEOS))%"
fi
echo ""

echo "MongoDB GridFS Status:"
kubectl exec -n insightlearn mongodb-0 -- mongosh -u insightlearn \
    -p "$(kubectl get secret -n insightlearn insightlearn-secrets -o jsonpath='{.data.mongodb-password}' | base64 -d)" \
    --authenticationDatabase admin insightlearn_videos \
    --eval "
var stats = db.stats(1024*1024);
print('  Data Size: ' + stats.dataSize.toFixed(2) + ' MB');
db.fs.files.countDocuments().then(count => print('  Video Files: ' + count));
" 2>/dev/null || echo "  Stats unavailable"

echo ""
echo "Courses Created:"
COURSE_COUNT=$(curl -s "$API_URL/api/courses?tags=TEST" -H "Authorization: Bearer $TOKEN" | jq 'length' 2>/dev/null || echo "N/A")
echo "  TEST courses: $COURSE_COUNT"

echo ""
echo "Cleaning up temporary files..."
rm -rf "$VIDEO_DIR"
echo "Temporary files deleted"

echo ""
echo "DONE!"
echo ""
echo "Browse TEST courses at:"
echo "  http://localhost:31090/courses (filter by tag TEST)"
