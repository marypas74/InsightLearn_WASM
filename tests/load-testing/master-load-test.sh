#!/bin/bash
# Master Load Testing Script
# Complete workflow: Generate Videos → Create Courses → Create Lessons → Upload Videos to GridFS

set -e

API_URL="http://localhost:31081"
VIDEO_DIR="/tmp/insightlearn-test-videos"
ADMIN_EMAIL="admin@insightlearn.cloud"
ADMIN_PASSWORD=""

# Configuration
COURSES_COUNT=20           # 20 courses
SECTIONS_PER_COURSE=3      # 3 sections each
LESSONS_PER_SECTION=4      # 4 lessons each = 240 total lessons
VIDEOS_TO_GENERATE=240     # 1 video per lesson
VIDEO_SIZE_MB=100          # 100MB each = 24GB total (will compress with gzip)

TOKEN_FILE="/tmp/insightlearn-load-test-token.txt"
LESSON_IDS_FILE="/tmp/insightlearn-lesson-ids.json"

echo "🚀 InsightLearn Complete Load Testing"
echo "====================================="
echo "Configuration:"
echo "  - Courses: $COURSES_COUNT"
echo "  - Sections per course: $SECTIONS_PER_COURSE"
echo "  - Lessons per section: $LESSONS_PER_SECTION"
echo "  - Total lessons: $((COURSES_COUNT * SECTIONS_PER_COURSE * LESSONS_PER_SECTION))"
echo "  - Video size: ${VIDEO_SIZE_MB}MB each"
echo "  - Target MongoDB storage: ~$(((VIDEOS_TO_GENERATE * VIDEO_SIZE_MB * 60) / 1024))GB (after compression)"
echo ""

# ============================================
# STEP 1: Authentication
# ============================================
echo "🔐 STEP 1: Authentication"
read -sp "Enter admin password: " ADMIN_PASSWORD
echo ""

LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}")

TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.token // empty')
USER_ID=$(echo "$LOGIN_RESPONSE" | jq -r '.userId // empty')

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    echo "❌ Authentication failed!"
    exit 1
fi

echo "✅ Authenticated (UserID: $USER_ID)"
echo "$TOKEN" > "$TOKEN_FILE"
echo ""

# ============================================
# STEP 2: Get or Create Category
# ============================================
echo "📂 STEP 2: Category Setup"
CATEGORIES=$(curl -s "$API_URL/api/categories")
CATEGORY_ID=$(echo "$CATEGORIES" | jq -r '.[] | select(.name == "Load Testing") | .id // empty' | head -n1)

if [ -z "$CATEGORY_ID" ]; then
    echo "Creating 'Load Testing' category..."
    CREATE_CAT=$(curl -s -X POST "$API_URL/api/categories" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d '{"name":"Load Testing","slug":"load-testing","description":"Automated testing category","iconClass":"fa-flask","colorCode":"#FF6B6B"}')
    CATEGORY_ID=$(echo "$CREATE_CAT" | jq -r '.id')
fi

echo "✅ Using category: $CATEGORY_ID"
echo ""

# ============================================
# STEP 3: Generate Test Videos
# ============================================
echo "🎬 STEP 3: Generating Test Videos ($VIDEOS_TO_GENERATE videos)"
mkdir -p "$VIDEO_DIR"

if ! command -v ffmpeg &> /dev/null; then
    echo "Installing ffmpeg..."
    sudo dnf install -y ffmpeg
fi

EXISTING_VIDEOS=$(ls -1 "$VIDEO_DIR"/*.mp4 2>/dev/null | wc -l)
echo "Found $EXISTING_VIDEOS existing videos"

if [ "$EXISTING_VIDEOS" -lt "$VIDEOS_TO_GENERATE" ]; then
    for i in $(seq $((EXISTING_VIDEOS + 1)) $VIDEOS_TO_GENERATE); do
        VIDEO_FILE="$VIDEO_DIR/test-video-$(printf '%03d' $i).mp4"

        # Generate 5-minute video (~100MB target)
        ffmpeg -f lavfi -i testsrc=duration=300:size=1280x720:rate=30 \
               -f lavfi -i sine=frequency=1000:duration=300 \
               -vf "drawtext=text='Test Video $i - %{pts\:hms}':x=(w-text_w)/2:y=h-50:fontsize=24:fontcolor=white" \
               -c:v libx264 -preset ultrafast -b:v 2700k \
               -c:a aac -b:a 128k \
               -movflags +faststart \
               "$VIDEO_FILE" \
               -loglevel error -y 2>/dev/null

        if [ $((i % 20)) -eq 0 ]; then
            echo "  Generated $i/$VIDEOS_TO_GENERATE videos ($(((i * 100) / VIDEOS_TO_GENERATE))%)"
        fi
    done
    echo "✅ All $VIDEOS_TO_GENERATE videos generated"
else
    echo "✅ Videos already generated, skipping"
fi
echo ""

# ============================================
# STEP 4: Create Courses with Sections and Lessons
# ============================================
echo "📚 STEP 4: Creating Courses, Sections, and Lessons"
echo "[]" > "$LESSON_IDS_FILE"  # Initialize JSON array

TOTAL_LESSONS=0
VIDEO_INDEX=1

for course_num in $(seq 1 $COURSES_COUNT); do
    echo "Creating course $course_num/$COURSES_COUNT..."

    COURSE_JSON=$(cat <<EOF
{
    "title": "[TEST] Load Testing Course #$course_num",
    "slug": "test-load-course-$course_num",
    "description": "Automated load testing course. Generated $(date '+%Y-%m-%d %H:%M:%S'). Contains real GridFS video files.",
    "shortDescription": "Load test course with GridFS videos",
    "categoryId": "$CATEGORY_ID",
    "instructorId": "$USER_ID",
    "price": 0.00,
    "currency": "EUR",
    "language": "en",
    "level": "Beginner",
    "thumbnailUrl": "/images/default-course.jpg",
    "tags": ["TEST", "LOAD-TESTING", "GRIDFS", "FREE"],
    "whatYouWillLearn": ["Load testing MongoDB GridFS", "Stress testing video storage"],
    "requirements": ["None"],
    "targetAudience": ["Automated testing"],
    "isPublished": true,
    "estimatedDurationMinutes": $((SECTIONS_PER_COURSE * LESSONS_PER_SECTION * 5))
}
EOF
)

    COURSE_RESPONSE=$(curl -s -X POST "$API_URL/api/courses" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "$COURSE_JSON")

    COURSE_ID=$(echo "$COURSE_RESPONSE" | jq -r '.id // empty')

    if [ -z "$COURSE_ID" ] || [ "$COURSE_ID" = "null" ]; then
        echo "  ❌ Failed to create course"
        continue
    fi

    # Create sections and lessons
    for section_num in $(seq 1 $SECTIONS_PER_COURSE); do
        SECTION_JSON=$(cat <<EOF
{
    "courseId": "$COURSE_ID",
    "title": "Section $section_num - Load Testing",
    "description": "Test section with video lessons",
    "orderIndex": $section_num
}
EOF
)

        SECTION_RESPONSE=$(curl -s -X POST "$API_URL/api/courses/$COURSE_ID/sections" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "$SECTION_JSON")

        SECTION_ID=$(echo "$SECTION_RESPONSE" | jq -r '.id // empty')

        if [ -z "$SECTION_ID" ] || [ "$SECTION_ID" = "null" ]; then
            echo "    ⚠️  Failed to create section $section_num"
            continue
        fi

        # Create lessons
        for lesson_num in $(seq 1 $LESSONS_PER_SECTION); do
            LESSON_JSON=$(cat <<EOF
{
    "sectionId": "$SECTION_ID",
    "title": "Lesson $lesson_num - Video Content",
    "description": "Load testing lesson with GridFS video",
    "type": "Video",
    "orderIndex": $lesson_num,
    "videoUrl": "pending",
    "durationMinutes": 5,
    "isFree": true
}
EOF
)

            LESSON_RESPONSE=$(curl -s -X POST "$API_URL/api/courses/$COURSE_ID/sections/$SECTION_ID/lessons" \
                -H "Authorization: Bearer $TOKEN" \
                -H "Content-Type: application/json" \
                -d "$LESSON_JSON")

            LESSON_ID=$(echo "$LESSON_RESPONSE" | jq -r '.id // empty')

            if [ ! -z "$LESSON_ID" ] && [ "$LESSON_ID" != "null" ]; then
                # Store lesson ID for video upload
                jq ". += [{\"lessonId\": \"$LESSON_ID\", \"videoFile\": \"test-video-$(printf '%03d' $VIDEO_INDEX).mp4\"}]" "$LESSON_IDS_FILE" > "${LESSON_IDS_FILE}.tmp"
                mv "${LESSON_IDS_FILE}.tmp" "$LESSON_IDS_FILE"
                TOTAL_LESSONS=$((TOTAL_LESSONS + 1))
                VIDEO_INDEX=$((VIDEO_INDEX + 1))
            fi
        done
    done

    echo "  ✅ Course $course_num created with sections and lessons"
done

echo "✅ Total lessons created: $TOTAL_LESSONS"
echo ""

# ============================================
# STEP 5: Upload Videos to GridFS
# ============================================
echo "📤 STEP 5: Uploading Videos to MongoDB GridFS"
UPLOADED=0
FAILED=0

LESSON_COUNT=$(jq '. | length' "$LESSON_IDS_FILE")

for i in $(seq 0 $((LESSON_COUNT - 1))); do
    LESSON_ID=$(jq -r ".[$i].lessonId" "$LESSON_IDS_FILE")
    VIDEO_FILE="$VIDEO_DIR/$(jq -r ".[$i].videoFile" "$LESSON_IDS_FILE")"

    if [ ! -f "$VIDEO_FILE" ]; then
        echo "  ⚠️  Video file not found: $VIDEO_FILE"
        FAILED=$((FAILED + 1))
        continue
    fi

    UPLOAD_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "$API_URL/api/video/upload" \
        -H "Authorization: Bearer $TOKEN" \
        -F "file=@$VIDEO_FILE" \
        -F "lessonId=$LESSON_ID" \
        -F "userId=$USER_ID" \
        -F "title=Load Test Video $(basename $VIDEO_FILE)")

    HTTP_CODE=$(echo "$UPLOAD_RESPONSE" | tail -n1)

    if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "201" ]; then
        UPLOADED=$((UPLOADED + 1))
        if [ $((UPLOADED % 10)) -eq 0 ]; then
            echo "  Uploaded $UPLOADED/$LESSON_COUNT videos ($(((UPLOADED * 100) / LESSON_COUNT))%)"
        fi
    else
        FAILED=$((FAILED + 1))
    fi

    # Small delay
    sleep 0.3
done

echo "✅ Upload complete: $UPLOADED successful, $FAILED failed"
echo ""

# ============================================
# STEP 6: Verify MongoDB Storage
# ============================================
echo "💾 STEP 6: MongoDB Storage Stats"
kubectl exec -n insightlearn mongodb-0 -- mongosh -u insightlearn -p "$(kubectl get secret -n insightlearn insightlearn-secrets -o jsonpath='{.data.mongodb-password}' | base64 -d)" --authenticationDatabase admin insightlearn_videos --eval "
print('Database: insightlearn_videos');
var stats = db.stats(1024*1024*1024);
print('Data Size: ' + stats.dataSize.toFixed(2) + ' GB');
print('Storage Size: ' + stats.storageSize.toFixed(2) + ' GB');
print('Collections: ' + stats.collections);
print('');
print('GridFS Files:');
db.fs.files.countDocuments().then(count => print('  Total files: ' + count));
" 2>/dev/null

echo ""
echo "🎉 Load Testing Setup Complete!"
echo ""
echo "📊 Summary:"
echo "  - Courses created: $COURSES_COUNT"
echo "  - Total lessons: $TOTAL_LESSONS"
echo "  - Videos uploaded: $UPLOADED"
echo "  - MongoDB GridFS used"
echo ""
echo "🌐 Browse test courses at:"
echo "  http://localhost:31090/courses?tags=TEST"
