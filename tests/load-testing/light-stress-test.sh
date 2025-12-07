#!/bin/bash
# Light Stress Test - Limited disk usage (max 10GB)
# 100 files × 50MB = 5GB + safe margin

set -e

API_URL="http://localhost:31081"
VIDEO_DIR="/tmp/insightlearn-light-videos"

# LIGHT CONFIGURATION (disk-space safe)
FILES_TO_GENERATE=100
FILE_SIZE_MB=50
PARALLEL_UPLOADS=30
K6_VIRTUAL_USERS=100

echo "⚡ InsightLearn LIGHT STRESS TEST"
echo "=================================="
echo ""
echo "💾 Disk-space safe configuration:"
echo "  - Files: $FILES_TO_GENERATE × ${FILE_SIZE_MB}MB = ~5GB"
echo "  - Parallel uploads: $PARALLEL_UPLOADS"
echo "  - K6 users: $K6_VIRTUAL_USERS"
echo ""

# Check disk space
FREE_GB=$(df /tmp | tail -1 | awk '{print int($4/1024/1024)}')
echo "📊 /tmp free space: ${FREE_GB}GB"
if [ "$FREE_GB" -lt 10 ]; then
    echo "❌ Not enough space! Need at least 10GB free"
    exit 1
fi
echo ""

# Generate dummy files
echo "📦 Generating $FILES_TO_GENERATE files..."
mkdir -p "$VIDEO_DIR"

generate_file() {
    i=$1
    FILE="$VIDEO_DIR/light-$(printf '%03d' $i).mp4"
    [ -f "$FILE" ] && return
    dd if=/dev/urandom of="$FILE" bs=1M count=$FILE_SIZE_MB 2>/dev/null
    echo -n "."
}

export -f generate_file
export VIDEO_DIR FILE_SIZE_MB

seq 1 $FILES_TO_GENERATE | xargs -P 20 -I {} bash -c 'generate_file {}'
echo ""
echo "✅ Files ready"
echo ""

# Start monitoring
echo "📊 Starting monitoring..."
cat > /tmp/light-monitor.sh << 'EOF'
while true; do
    echo "$(date '+%H:%M:%S')" >> /tmp/light-monitor.log
    kubectl top pod -n insightlearn 2>&1 | grep -E "NAME|api|mongodb" >> /tmp/light-monitor.log || true
    sleep 15
done
EOF

chmod +x /tmp/light-monitor.sh
bash /tmp/light-monitor.sh &
MONITOR_PID=$!
echo "✅ Monitor PID: $MONITOR_PID"
echo ""

# Start K6
echo "🚀 Starting K6 ($K6_VIRTUAL_USERS users)..."
if command -v k6 &> /dev/null; then
    cat > /tmp/light-k6.js << 'K6EOF'
import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
    stages: [
        { duration: '1m', target: 50 },
        { duration: '2m', target: 100 },
        { duration: '3m', target: 100 },
        { duration: '1m', target: 0 },
    ],
};

export default function () {
    http.get('http://localhost:31081/health');
    sleep(0.5);
    http.get('http://localhost:31081/api/courses');
    sleep(1);
}
K6EOF

    k6 run /tmp/light-k6.js > /tmp/k6-light.log 2>&1 &
    K6_PID=$!
    echo "✅ K6 PID: $K6_PID"
else
    echo "⚠️  k6 not found"
    K6_PID=0
fi

echo ""
echo "⏳ Waiting 1min for K6 ramp-up..."
sleep 60

# Upload files
echo "📤 Starting uploads ($PARALLEL_UPLOADS parallel)..."

ADMIN_PASSWORD="Admin123!Secure"
TOKEN=$(curl -s -X POST "$API_URL/api/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"admin@insightlearn.cloud\",\"password\":\"$ADMIN_PASSWORD\"}" \
    | jq -r '.token // "dummy"' 2>/dev/null || echo "dummy")

USER_ID="00000000-0000-0000-0000-000000000001"
LESSON_ID="00000000-0000-0000-0000-000000000001"
UPLOAD_LOG="/tmp/light-upload.log"
echo "" > "$UPLOAD_LOG"

upload_file() {
    i=$1
    FILE="$VIDEO_DIR/light-$(printf '%03d' $i).mp4"
    [ ! -f "$FILE" ] && return

    HTTP_CODE=$(curl -s -w "%{http_code}" -o /dev/null \
        -X POST "$API_URL/api/video/upload" \
        -H "Authorization: Bearer $TOKEN" \
        -F "file=@$FILE" \
        -F "lessonId=$LESSON_ID" \
        -F "userId=$USER_ID" \
        -F "title=Light Stress $i" \
        --max-time 90 2>/dev/null || echo "000")

    echo "$i:$HTTP_CODE" >> "$UPLOAD_LOG"
}

export -f upload_file
export VIDEO_DIR API_URL TOKEN USER_ID LESSON_ID UPLOAD_LOG

# Progress
(
    while true; do
        DONE=$(wc -l < "$UPLOAD_LOG" 2>/dev/null || echo 0)
        [ "$DONE" -ge "$FILES_TO_GENERATE" ] && break
        echo -ne "\r  📤 $DONE/$FILES_TO_GENERATE ($(((DONE * 100) / FILES_TO_GENERATE))%)   K6: $(ps -p $K6_PID > /dev/null && echo 'Run' || echo 'Done')   "
        sleep 2
    done
    echo ""
) &
PROGRESS_PID=$!

echo "Launching uploads..."
seq 1 $FILES_TO_GENERATE | xargs -P $PARALLEL_UPLOADS -I {} bash -c 'upload_file {}'

kill $PROGRESS_PID 2>/dev/null || true
echo ""

OK=$(grep -E ":(200|201)$" "$UPLOAD_LOG" | wc -l)
FAIL=$((FILES_TO_GENERATE - OK))

echo "✅ Uploads: $OK OK / $FAIL FAIL"
echo ""

# Wait K6
if [ "$K6_PID" -ne 0 ]; then
    wait $K6_PID 2>/dev/null || true
fi

kill $MONITOR_PID 2>/dev/null || true

# Report
echo "📈 LIGHT STRESS TEST REPORT"
echo "============================"
echo ""
echo "📤 Success rate: $(((OK * 100) / FILES_TO_GENERATE))%"
echo ""

echo "💾 MongoDB:"
kubectl exec -n insightlearn mongodb-0 -- mongosh -u insightlearn \
    -p "$(kubectl get secret -n insightlearn insightlearn-secrets -o jsonpath='{.data.mongodb-password}' | base64 -d)" \
    --authenticationDatabase admin insightlearn_videos \
    --eval "var s=db.stats(1024*1024*1024); print('Size: '+s.dataSize.toFixed(2)+'GB'); db.fs.files.countDocuments().then(c=>print('Files: '+c))" 2>/dev/null || echo "Stats unavailable"

echo ""
echo "📁 Logs: $UPLOAD_LOG, /tmp/k6-light.log, /tmp/light-monitor.log"
echo ""
echo "🎉 DONE! Cleaning up..."
rm -rf "$VIDEO_DIR"
echo "✅ Temporary files deleted"
