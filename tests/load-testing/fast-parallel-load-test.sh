#!/bin/bash
# Fast Parallel Load Testing Script
# Uploads multiple videos simultaneously without intermediate checks

set -e

API_URL="http://localhost:31081"
VIDEO_DIR="/tmp/insightlearn-test-videos"
ADMIN_EMAIL="admin@insightlearn.cloud"
ADMIN_PASSWORD=""

# Configuration
COURSES_COUNT=20
SECTIONS_PER_COURSE=3
LESSONS_PER_SECTION=4
VIDEOS_TO_GENERATE=240
VIDEO_SIZE_MB=100
PARALLEL_UPLOADS=20  # 20 simultaneous uploads

TOKEN_FILE="/tmp/insightlearn-load-test-token.txt"
LESSON_IDS_FILE="/tmp/insightlearn-lesson-ids.json"
UPLOAD_LOG="/tmp/insightlearn-upload.log"

echo "⚡ InsightLearn FAST Parallel Load Testing"
echo "=========================================="
echo "Parallel uploads: $PARALLEL_UPLOADS"
echo "Total videos: $VIDEOS_TO_GENERATE"
echo ""

# ============================================
# Authentication
# ============================================
read -sp "Admin password: " ADMIN_PASSWORD
echo ""

LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASSWORD\"}")

TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.token')
USER_ID=$(echo "$LOGIN_RESPONSE" | jq -r '.userId')
echo "$TOKEN" > "$TOKEN_FILE"
echo "✅ Auth OK"

# ============================================
# Category
# ============================================
CATEGORIES=$(curl -s "$API_URL/api/categories")
CATEGORY_ID=$(echo "$CATEGORIES" | jq -r '.[] | select(.name == "Load Testing") | .id' | head -n1)

if [ -z "$CATEGORY_ID" ]; then
    CREATE_CAT=$(curl -s -X POST "$API_URL/api/categories" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d '{"name":"Load Testing","slug":"load-testing","description":"Test","iconClass":"fa-flask","colorCode":"#FF6B6B"}')
    CATEGORY_ID=$(echo "$CREATE_CAT" | jq -r '.id')
fi
echo "✅ Category: $CATEGORY_ID"

# ============================================
# Generate Videos (parallel)
# ============================================
echo "🎬 Generating $VIDEOS_TO_GENERATE videos..."
mkdir -p "$VIDEO_DIR"

if ! command -v ffmpeg &> /dev/null; then
    sudo dnf install -y ffmpeg >/dev/null 2>&1
fi

generate_video() {
    i=$1
    VIDEO_FILE="$VIDEO_DIR/test-video-$(printf '%03d' $i).mp4"
    [ -f "$VIDEO_FILE" ] && return
    ffmpeg -f lavfi -i testsrc=duration=300:size=1280x720:rate=30 \
           -f lavfi -i sine=frequency=1000:duration=300 \
           -c:v libx264 -preset ultrafast -b:v 2700k -c:a aac -b:a 128k \
           -movflags +faststart "$VIDEO_FILE" -loglevel error -y 2>/dev/null
}

export -f generate_video
export VIDEO_DIR

# Parallel video generation
seq 1 $VIDEOS_TO_GENERATE | xargs -P 10 -I {} bash -c 'generate_video {}'
echo "✅ Videos ready"

# ============================================
# Create Courses (fast, no delays)
# ============================================
echo "📚 Creating $COURSES_COUNT courses..."
echo "[]" > "$LESSON_IDS_FILE"

for course_num in $(seq 1 $COURSES_COUNT); do
    COURSE_JSON='{"title":"[TEST] Load Course #'$course_num'","slug":"test-load-'$course_num'","description":"Load test","shortDescription":"Test","categoryId":"'$CATEGORY_ID'","instructorId":"'$USER_ID'","price":0,"currency":"EUR","language":"en","level":"Beginner","thumbnailUrl":"/img/test.jpg","tags":["TEST","GRIDFS","FREE"],"whatYouWillLearn":["Testing"],"requirements":["None"],"targetAudience":["Test"],"isPublished":true,"estimatedDurationMinutes":60}'

    COURSE_ID=$(curl -s -X POST "$API_URL/api/courses" \
        -H "Authorization: Bearer $TOKEN" \
        -H "Content-Type: application/json" \
        -d "$COURSE_JSON" | jq -r '.id')

    [ -z "$COURSE_ID" ] && continue

    for section_num in $(seq 1 $SECTIONS_PER_COURSE); do
        SECTION_JSON='{"courseId":"'$COURSE_ID'","title":"Section '$section_num'","description":"Test","orderIndex":'$section_num'}'

        SECTION_ID=$(curl -s -X POST "$API_URL/api/courses/$COURSE_ID/sections" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "$SECTION_JSON" | jq -r '.id')

        [ -z "$SECTION_ID" ] && continue

        for lesson_num in $(seq 1 $LESSONS_PER_SECTION); do
            LESSON_JSON='{"sectionId":"'$SECTION_ID'","title":"Lesson '$lesson_num'","description":"Video","type":"Video","orderIndex":'$lesson_num',"videoUrl":"pending","durationMinutes":5,"isFree":true}'

            LESSON_ID=$(curl -s -X POST "$API_URL/api/courses/$COURSE_ID/sections/$SECTION_ID/lessons" \
                -H "Authorization: Bearer $TOKEN" \
                -H "Content-Type: application/json" \
                -d "$LESSON_JSON" | jq -r '.id')

            [ ! -z "$LESSON_ID" ] && jq ". += [{\"lessonId\":\"$LESSON_ID\",\"videoFile\":\"test-video-$(printf '%03d' $((course_num * SECTIONS_PER_COURSE * LESSONS_PER_SECTION + section_num * LESSONS_PER_SECTION + lesson_num))).mp4\"}]" "$LESSON_IDS_FILE" > "${LESSON_IDS_FILE}.tmp" && mv "${LESSON_IDS_FILE}.tmp" "$LESSON_IDS_FILE"
        done
    done
    echo "  Course $course_num/$COURSES_COUNT"
done

TOTAL_LESSONS=$(jq '. | length' "$LESSON_IDS_FILE")
echo "✅ Created $TOTAL_LESSONS lessons"

# ============================================
# Parallel Video Upload (20 simultaneous)
# ============================================
echo "📤 Uploading $TOTAL_LESSONS videos (${PARALLEL_UPLOADS} parallel)..."
echo "" > "$UPLOAD_LOG"

upload_video() {
    i=$1
    LESSON_ID=$(jq -r ".[$i].lessonId" "$LESSON_IDS_FILE")
    VIDEO_FILE="$VIDEO_DIR/$(jq -r ".[$i].videoFile" "$LESSON_IDS_FILE")"
    TOKEN=$(cat "$TOKEN_FILE")

    [ ! -f "$VIDEO_FILE" ] && echo "FAIL $i" >> "$UPLOAD_LOG" && return

    HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null -X POST "$API_URL/api/video/upload" \
        -H "Authorization: Bearer $TOKEN" \
        -F "file=@$VIDEO_FILE" \
        -F "lessonId=$LESSON_ID" \
        -F "userId=$USER_ID" \
        -F "title=Load Test $(basename $VIDEO_FILE)")

    if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "201" ]; then
        echo "OK $i" >> "$UPLOAD_LOG"
    else
        echo "FAIL $i ($HTTP_CODE)" >> "$UPLOAD_LOG"
    fi
}

export -f upload_video
export LESSON_IDS_FILE VIDEO_DIR TOKEN_FILE API_URL USER_ID

# Progress monitor in background
(
    while true; do
        DONE=$(wc -l < "$UPLOAD_LOG" 2>/dev/null || echo 0)
        [ "$DONE" -ge "$TOTAL_LESSONS" ] && break
        echo -ne "\r  Uploaded: $DONE/$TOTAL_LESSONS ($(((DONE * 100) / TOTAL_LESSONS))%)"
        sleep 2
    done
) &
MONITOR_PID=$!

# Start parallel uploads
seq 0 $((TOTAL_LESSONS - 1)) | xargs -P $PARALLEL_UPLOADS -I {} bash -c 'upload_video {}'

# Kill monitor
kill $MONITOR_PID 2>/dev/null || true
echo ""

SUCCESSFUL=$(grep -c "^OK" "$UPLOAD_LOG" || echo 0)
FAILED=$(grep -c "^FAIL" "$UPLOAD_LOG" || echo 0)

echo "✅ Upload complete: $SUCCESSFUL OK, $FAILED FAIL"

# ============================================
# Final MongoDB Stats
# ============================================
echo ""
echo "💾 MongoDB GridFS Stats:"
kubectl exec -n insightlearn mongodb-0 -- mongosh -u insightlearn \
    -p "$(kubectl get secret -n insightlearn insightlearn-secrets -o jsonpath='{.data.mongodb-password}' | base64 -d)" \
    --authenticationDatabase admin insightlearn_videos \
    --eval "db.stats(1024*1024*1024)" 2>/dev/null | grep -E "dataSize|storageSize"

echo ""
echo "🎉 DONE! Browse: http://localhost:31090/courses?tags=TEST"
