#!/bin/bash
# Extreme Stress Test - DUMMY FILES VERSION (no ffmpeg required)
# Generates random binary files to test upload stress

set -e

API_URL="http://localhost:31081"
VIDEO_DIR="/tmp/insightlearn-dummy-videos"
STRESS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../stress" && pwd)"

# EXTREME CONFIGURATION
FILES_TO_GENERATE=500
PARALLEL_UPLOADS=50
K6_VIRTUAL_USERS=200
FILE_SIZE_MB=100

echo "💥 InsightLearn EXTREME STRESS TEST (Dummy Files)"
echo "==================================================="
echo ""
echo "Configuration:"
echo "  - Files: $FILES_TO_GENERATE × ${FILE_SIZE_MB}MB"
echo "  - Parallel uploads: $PARALLEL_UPLOADS"
echo "  - K6 users: $K6_VIRTUAL_USERS"
echo "  - Total: ~$((FILES_TO_GENERATE * FILE_SIZE_MB / 1024))GB"
echo ""
echo "✅ No ffmpeg required - using dummy binary files"
echo ""

# ============================================
# 1. Generate Dummy Files (FAST!)
# ============================================
echo "📦 Generating $FILES_TO_GENERATE dummy files..."
mkdir -p "$VIDEO_DIR"

generate_dummy() {
    i=$1
    FILE="$VIDEO_DIR/dummy-$(printf '%04d' $i).mp4"
    [ -f "$FILE" ] && return

    # Generate random binary file (much faster than video)
    dd if=/dev/urandom of="$FILE" bs=1M count=$FILE_SIZE_MB 2>/dev/null
    echo -n "."
}

export -f generate_dummy
export VIDEO_DIR FILE_SIZE_MB

seq 1 $FILES_TO_GENERATE | xargs -P 50 -I {} bash -c 'generate_dummy {}'
echo ""
echo "✅ Files generated"
echo ""

# ============================================
# 2. Start Monitoring
# ============================================
echo "📊 Starting monitoring..."

cat > /tmp/monitor-stress.sh << 'MONITOR_EOF'
#!/bin/bash
LOG="/tmp/stress-monitor.log"
while true; do
    echo "=== $(date '+%H:%M:%S') ===" >> "$LOG"
    kubectl top pod -n insightlearn 2>&1 | grep -E "insightlearn-api|mongodb|sqlserver" >> "$LOG" || true
    kubectl top node >> "$LOG" 2>&1 || true
    echo "" >> "$LOG"
    sleep 10
done
MONITOR_EOF

chmod +x /tmp/monitor-stress.sh
bash /tmp/monitor-stress.sh &
MONITOR_PID=$!
echo "✅ Monitor PID: $MONITOR_PID"
echo ""

# ============================================
# 3. Start K6 Stress Test
# ============================================
echo "🚀 Starting K6 stress ($K6_VIRTUAL_USERS users)..."

if command -v k6 &> /dev/null; then
    cat > /tmp/k6-extreme.js << 'K6EOF'
import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
    stages: [
        { duration: '2m', target: 50 },
        { duration: '3m', target: 100 },
        { duration: '5m', target: 200 },
        { duration: '5m', target: 200 },
        { duration: '2m', target: 0 },
    ],
};

export default function () {
    http.get('http://localhost:31081/health');
    sleep(0.5);
    http.get('http://localhost:31081/api/info');
    sleep(0.5);
    http.get('http://localhost:31081/api/courses');
    sleep(1);
}
K6EOF

    k6 run /tmp/k6-extreme.js > /tmp/k6-output.log 2>&1 &
    K6_PID=$!
    echo "✅ K6 PID: $K6_PID"
else
    echo "⚠️  k6 not installed, skipping API stress"
    K6_PID=0
fi

echo ""
echo "⏳ Waiting 2min for K6 ramp-up..."
sleep 120

# ============================================
# 4. MASSIVE Upload (50 parallel)
# ============================================
echo "📤 Starting MASSIVE upload ($PARALLEL_UPLOADS parallel)..."

ADMIN_PASSWORD="Admin123!Secure"
TOKEN=$(curl -s -X POST "$API_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"admin@insightlearn.cloud\",\"password\":\"$ADMIN_PASSWORD\"}" \
    | jq -r '.token // "dummy"' 2>/dev/null || echo "dummy")

USER_ID="00000000-0000-0000-0000-000000000001"
LESSON_ID="00000000-0000-0000-0000-000000000001"
UPLOAD_LOG="/tmp/upload-results.log"
echo "" > "$UPLOAD_LOG"

upload_file() {
    i=$1
    FILE="$VIDEO_DIR/dummy-$(printf '%04d' $i).mp4"
    [ ! -f "$FILE" ] && return

    HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null \
        -X POST "$API_URL/api/video/upload" \
        -H "Authorization: Bearer $TOKEN" \
        -F "file=@$FILE" \
        -F "lessonId=$LESSON_ID" \
        -F "userId=$USER_ID" \
        -F "title=Stress $i" \
        --max-time 120 2>/dev/null || echo "000")

    echo "$i:$HTTP_CODE" >> "$UPLOAD_LOG"
}

export -f upload_file
export VIDEO_DIR API_URL TOKEN USER_ID LESSON_ID UPLOAD_LOG

# Progress monitor
(
    while true; do
        DONE=$(wc -l < "$UPLOAD_LOG" 2>/dev/null || echo 0)
        [ "$DONE" -ge "$FILES_TO_GENERATE" ] && break
        PERCENT=$(((DONE * 100) / FILES_TO_GENERATE))
        echo -ne "\r  📤 Progress: $DONE/$FILES_TO_GENERATE ($PERCENT%)   K6: $(ps -p $K6_PID > /dev/null && echo 'Running' || echo 'Done')   "
        sleep 2
    done
    echo ""
) &
PROGRESS_PID=$!

echo "Launching $PARALLEL_UPLOADS workers..."
seq 1 $FILES_TO_GENERATE | xargs -P $PARALLEL_UPLOADS -I {} bash -c 'upload_file {}'

kill $PROGRESS_PID 2>/dev/null || true
echo ""

OK=$(grep -E ":(200|201)$" "$UPLOAD_LOG" | wc -l)
FAIL=$(grep -v -E ":(200|201)$" "$UPLOAD_LOG" | wc -l)

echo "✅ Upload complete: $OK OK, $FAIL FAIL"
echo ""

# ============================================
# 5. Wait K6 & Stop Monitor
# ============================================
if [ "$K6_PID" -ne 0 ]; then
    echo "⏳ Waiting K6..."
    wait $K6_PID 2>/dev/null || true
    echo "✅ K6 done"
fi

kill $MONITOR_PID 2>/dev/null || true
echo ""

# ============================================
# 6. Report
# ============================================
echo "📈 STRESS TEST REPORT"
echo "====================="
echo ""
echo "📤 Uploads: $OK OK / $FAIL FAIL ($(((OK * 100) / (OK + FAIL)))%)"
echo ""

echo "💾 MongoDB:"
kubectl exec -n insightlearn mongodb-0 -- mongosh -u insightlearn \
    -p "$(kubectl get secret -n insightlearn insightlearn-secrets -o jsonpath='{.data.mongodb-password}' | base64 -d)" \
    --authenticationDatabase admin insightlearn_videos \
    --eval "db.stats(1024*1024*1024)" 2>/dev/null | grep -E "dataSize|storageSize" || echo "  Stats unavailable"

echo ""
echo "🚀 K6:"
if [ -f /tmp/k6-output.log ]; then
    tail -15 /tmp/k6-output.log | grep -E "http_req|checks" || echo "  No metrics"
fi

echo ""
echo "📊 Resources (last 5min):"
if [ -f /tmp/stress-monitor.log ]; then
    tail -30 /tmp/stress-monitor.log | grep -E "insightlearn-api|mongodb|NODE" | tail -8
fi

echo ""
echo "📁 Logs:"
echo "  Uploads: $UPLOAD_LOG"
echo "  K6: /tmp/k6-output.log"
echo "  Monitor: /tmp/stress-monitor.log"
echo ""
echo "🎉 EXTREME STRESS TEST DONE!"
