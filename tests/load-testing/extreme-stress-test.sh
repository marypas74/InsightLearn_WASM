#!/bin/bash
# Extreme Stress Test - Simultaneous Video Upload + API Load
# Tests server under MAXIMUM load (MongoDB + API + SQL + Redis)

set -e

API_URL="http://localhost:31081"
VIDEO_DIR="/tmp/insightlearn-test-videos"
STRESS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../stress" && pwd)"

# EXTREME CONFIGURATION
VIDEOS_TO_GENERATE=500        # 500 videos = 50GB
PARALLEL_UPLOADS=50           # 50 simultaneous uploads
K6_VIRTUAL_USERS=200          # 200 concurrent API users
GENERATE_VIDEO_PARALLEL=20    # 20 parallel video generation

echo "💥 InsightLearn EXTREME STRESS TEST"
echo "===================================="
echo ""
echo "⚠️  WARNING: This test will stress ALL system resources!"
echo ""
echo "Configuration:"
echo "  - Video uploads: $PARALLEL_UPLOADS simultaneous"
echo "  - K6 virtual users: $K6_VIRTUAL_USERS concurrent"
echo "  - Total videos: $VIDEOS_TO_GENERATE (~50GB)"
echo "  - Video generation: $GENERATE_VIDEO_PARALLEL parallel"
echo ""

read -p "Continue? (y/n) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "Aborted."
    exit 1
fi

# ============================================
# 0. Prerequisites Check
# ============================================
echo "🔍 Checking prerequisites..."

# Check ffmpeg
if ! command -v ffmpeg &> /dev/null; then
    echo "Installing ffmpeg..."
    sudo dnf install -y ffmpeg
fi

# Check k6
if ! command -v k6 &> /dev/null; then
    echo "Installing k6..."
    sudo dnf install -y https://dl.k6.io/rpm/repo.rpm
    sudo dnf install -y k6
fi

# Check API
if ! curl -s "$API_URL/health" > /dev/null; then
    echo "❌ API not accessible at $API_URL"
    exit 1
fi

echo "✅ All prerequisites OK"
echo ""

# ============================================
# 1. Generate Videos (parallel)
# ============================================
echo "🎬 Phase 1: Generating $VIDEOS_TO_GENERATE videos..."
mkdir -p "$VIDEO_DIR"

generate_video() {
    i=$1
    VIDEO_FILE="$VIDEO_DIR/stress-video-$(printf '%04d' $i).mp4"
    [ -f "$VIDEO_FILE" ] && return

    # Faster generation: 2min video, lower quality
    ffmpeg -f lavfi -i testsrc=duration=120:size=640x480:rate=24 \
           -f lavfi -i sine=frequency=800:duration=120 \
           -c:v libx264 -preset ultrafast -b:v 1500k \
           -c:a aac -b:a 96k \
           -movflags +faststart \
           "$VIDEO_FILE" -loglevel error -y 2>/dev/null
}

export -f generate_video
export VIDEO_DIR

echo "Generating videos in $GENERATE_VIDEO_PARALLEL parallel processes..."
seq 1 $VIDEOS_TO_GENERATE | xargs -P $GENERATE_VIDEO_PARALLEL -I {} bash -c 'generate_video {} && echo -n "."'
echo ""
echo "✅ $VIDEOS_TO_GENERATE videos generated"
echo ""

# ============================================
# 2. Start System Monitoring
# ============================================
echo "📊 Phase 2: Starting system monitoring..."

MONITOR_LOG="/tmp/insightlearn-stress-monitor.log"
cat > /tmp/monitor-stress.sh << 'MONITOR_EOF'
#!/bin/bash
while true; do
    echo "=== $(date '+%Y-%m-%d %H:%M:%S') ===" >> /tmp/insightlearn-stress-monitor.log

    # API pod stats
    kubectl top pod -n insightlearn -l app=insightlearn-api >> /tmp/insightlearn-stress-monitor.log 2>&1 || echo "API metrics unavailable"

    # MongoDB stats
    kubectl top pod -n insightlearn -l app=mongodb >> /tmp/insightlearn-stress-monitor.log 2>&1 || echo "MongoDB metrics unavailable"

    # SQL Server stats
    kubectl top pod -n insightlearn -l app=sqlserver >> /tmp/insightlearn-stress-monitor.log 2>&1 || echo "SQL metrics unavailable"

    # Node stats
    kubectl top node >> /tmp/insightlearn-stress-monitor.log 2>&1 || echo "Node metrics unavailable"

    echo "" >> /tmp/insightlearn-stress-monitor.log
    sleep 10
done
MONITOR_EOF

chmod +x /tmp/monitor-stress.sh
bash /tmp/monitor-stress.sh &
MONITOR_PID=$!
echo "✅ Monitor started (PID: $MONITOR_PID, log: $MONITOR_LOG)"
echo ""

# ============================================
# 3. Start K6 Stress Test (background)
# ============================================
echo "🚀 Phase 3: Starting K6 stress test (background)..."

cat > /tmp/extreme-stress.js << 'K6_EOF'
import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '2m', target: 50 },   // Ramp to 50 users
        { duration: '3m', target: 100 },  // Ramp to 100 users
        { duration: '5m', target: 200 },  // EXTREME: 200 users
        { duration: '5m', target: 200 },  // Maintain 200 users
        { duration: '2m', target: 0 },    // Ramp down
    ],
};

export default function () {
    const BASE_URL = 'http://localhost:31081';

    // Scenario 1: Health check
    http.get(`${BASE_URL}/health`);
    sleep(0.5);

    // Scenario 2: API info
    http.get(`${BASE_URL}/api/info`);
    sleep(0.5);

    // Scenario 3: Course list
    http.get(`${BASE_URL}/api/courses`);
    sleep(1);

    // Scenario 4: Category list
    http.get(`${BASE_URL}/api/categories`);
    sleep(0.5);
}
K6_EOF

cd /tmp
k6 run --out json=extreme-k6-results.json extreme-stress.js > /tmp/k6-stress-output.log 2>&1 &
K6_PID=$!
echo "✅ K6 started (PID: $K6_PID, 200 virtual users max)"
echo ""

# ============================================
# 4. Wait for K6 to ramp up
# ============================================
echo "⏳ Waiting 2 minutes for K6 to ramp up..."
sleep 120

# ============================================
# 5. Start MASSIVE Parallel Video Upload
# ============================================
echo "📤 Phase 4: EXTREME parallel video upload ($PARALLEL_UPLOADS simultaneous)..."

# Get auth token (simplified - no interactive prompt)
ADMIN_PASSWORD="Admin123!Secure"
LOGIN_JSON=$(cat <<EOF
{
  "email": "admin@insightlearn.cloud",
  "password": "$ADMIN_PASSWORD"
}
EOF
)

TOKEN=$(curl -s -X POST "$API_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -d "$LOGIN_JSON" | jq -r '.token // empty' 2>/dev/null || echo "")

if [ -z "$TOKEN" ]; then
    echo "⚠️  Auth failed, continuing without token (some uploads may fail)"
    TOKEN="dummy-token"
fi

USER_ID="00000000-0000-0000-0000-000000000001"
LESSON_ID="00000000-0000-0000-0000-000000000001"

UPLOAD_LOG="/tmp/extreme-upload.log"
echo "" > "$UPLOAD_LOG"

upload_video_stress() {
    i=$1
    VIDEO_FILE="$VIDEO_DIR/stress-video-$(printf '%04d' $i).mp4"

    [ ! -f "$VIDEO_FILE" ] && return

    HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null \
        -X POST "$API_URL/api/video/upload" \
        -H "Authorization: Bearer $TOKEN" \
        -F "file=@$VIDEO_FILE" \
        -F "lessonId=$LESSON_ID" \
        -F "userId=$USER_ID" \
        -F "title=Stress Test Video $i" \
        --max-time 120)  # 2min timeout per upload

    if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "201" ]; then
        echo "OK $i" >> "$UPLOAD_LOG"
    else
        echo "FAIL $i ($HTTP_CODE)" >> "$UPLOAD_LOG"
    fi
}

export -f upload_video_stress
export VIDEO_DIR API_URL TOKEN USER_ID LESSON_ID UPLOAD_LOG

# Upload progress monitor
(
    TOTAL=$VIDEOS_TO_GENERATE
    while true; do
        DONE=$(wc -l < "$UPLOAD_LOG" 2>/dev/null || echo 0)
        [ "$DONE" -ge "$TOTAL" ] && break
        echo -ne "\r  📤 Uploading: $DONE/$TOTAL ($(((DONE * 100) / TOTAL))%)   K6: $(ps -p $K6_PID > /dev/null && echo 'Running' || echo 'Done')   "
        sleep 3
    done
    echo ""
) &
PROGRESS_PID=$!

# Launch parallel uploads
echo "Launching $PARALLEL_UPLOADS parallel upload workers..."
seq 1 $VIDEOS_TO_GENERATE | xargs -P $PARALLEL_UPLOADS -I {} bash -c 'upload_video_stress {}'

# Kill progress monitor
kill $PROGRESS_PID 2>/dev/null || true
echo ""

SUCCESSFUL=$(grep -c "^OK" "$UPLOAD_LOG" 2>/dev/null || echo 0)
FAILED=$(grep -c "^FAIL" "$UPLOAD_LOG" 2>/dev/null || echo 0)

echo "✅ Upload phase complete: $SUCCESSFUL OK, $FAILED FAIL"
echo ""

# ============================================
# 6. Wait for K6 to finish
# ============================================
echo "⏳ Waiting for K6 stress test to complete..."
wait $K6_PID
echo "✅ K6 stress test finished"
echo ""

# ============================================
# 7. Stop monitoring
# ============================================
kill $MONITOR_PID 2>/dev/null || true
echo "✅ Monitoring stopped"
echo ""

# ============================================
# 8. Generate Final Report
# ============================================
echo "📈 EXTREME STRESS TEST REPORT"
echo "=============================="
echo ""

echo "📤 Video Upload Results:"
echo "  Successful: $SUCCESSFUL"
echo "  Failed: $FAILED"
echo "  Success rate: $(((SUCCESSFUL * 100) / (SUCCESSFUL + FAILED)))%"
echo ""

echo "💾 MongoDB Storage:"
kubectl exec -n insightlearn mongodb-0 -- mongosh -u insightlearn \
    -p "$(kubectl get secret -n insightlearn insightlearn-secrets -o jsonpath='{.data.mongodb-password}' | base64 -d)" \
    --authenticationDatabase admin insightlearn_videos \
    --eval "
var stats = db.stats(1024*1024*1024);
print('  Data Size: ' + stats.dataSize.toFixed(2) + ' GB');
print('  Storage Size: ' + stats.storageSize.toFixed(2) + ' GB');
db.fs.files.countDocuments().then(count => print('  Video files: ' + count));
" 2>/dev/null || echo "  MongoDB stats unavailable"

echo ""
echo "🚀 K6 API Stress Results:"
if [ -f /tmp/k6-stress-output.log ]; then
    tail -20 /tmp/k6-stress-output.log | grep -E "http_req|checks|errors" || cat /tmp/k6-stress-output.log | tail -20
else
    echo "  K6 log not found"
fi

echo ""
echo "📊 System Resource Usage (last 5 minutes):"
if [ -f "$MONITOR_LOG" ]; then
    tail -50 "$MONITOR_LOG" | grep -E "insightlearn-api|mongodb|sqlserver|NODE" | tail -10
else
    echo "  Monitor log not found"
fi

echo ""
echo "📁 Logs Location:"
echo "  Upload: $UPLOAD_LOG"
echo "  K6: /tmp/k6-stress-output.log"
echo "  K6 JSON: /tmp/extreme-k6-results.json"
echo "  Monitor: $MONITOR_LOG"
echo ""

echo "🎉 EXTREME STRESS TEST COMPLETE!"
echo ""
echo "⚠️  Review logs to assess system behavior under extreme load"
