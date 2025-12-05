#!/bin/bash
# Complete Stress Testing Suite
# 1. Generate videos
# 2. Upload to MongoDB GridFS (parallel)
# 3. Create TEST courses
# 4. Run K6 stress tests

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STRESS_DIR="$SCRIPT_DIR/../stress"

echo "🔥 InsightLearn COMPLETE Stress Testing Suite"
echo "=============================================="
echo ""

# ============================================
# Check/Install K6
# ============================================
if ! command -v k6 &> /dev/null; then
    echo "📦 Installing K6..."
    sudo dnf install -y https://dl.k6.io/rpm/repo.rpm
    sudo dnf install -y k6
    echo "✅ K6 installed"
fi

echo "📊 K6 version: $(k6 version)"
echo ""

# ============================================
# 1. Fast Parallel Load Test
# ============================================
echo "🎬 PHASE 1: MongoDB GridFS Load (50% capacity)"
echo "================================================"

if [ -f "$SCRIPT_DIR/fast-parallel-load-test.sh" ]; then
    bash "$SCRIPT_DIR/fast-parallel-load-test.sh"
else
    echo "❌ fast-parallel-load-test.sh not found!"
    exit 1
fi

echo ""
echo "✅ Phase 1 complete: MongoDB loaded with test videos"
echo ""

# ============================================
# 2. K6 Stress Tests
# ============================================
echo "🚀 PHASE 2: K6 Stress Testing"
echo "=============================="

cd "$STRESS_DIR" || exit 1

# Check API is accessible
API_URL="http://localhost:31081"
echo "Checking API at $API_URL..."

if curl -s "$API_URL/health" > /dev/null; then
    echo "✅ API is reachable"
else
    echo "❌ API not reachable at $API_URL"
    echo "Start port-forward: kubectl port-forward -n insightlearn svc/insightlearn-api 31081:80"
    exit 1
fi

# Run stress test sequence
echo ""
echo "📊 Running K6 stress tests..."
echo ""

# 1. Smoke test (quick validation)
echo "1️⃣ Smoke Test (30s)..."
k6 run --out json=smoke-test-results.json smoke-test.js

echo ""
echo "2️⃣ Load Test (9 minutes)..."
k6 run --out json=load-test-results.json load-test.js

echo ""
echo "3️⃣ Stress Test (16 minutes)..."
k6 run --out json=stress-test-results.json stress-test.js

echo ""
echo "✅ Phase 2 complete: K6 stress testing done"
echo ""

# ============================================
# 3. Final Report
# ============================================
echo "📈 PHASE 3: Final Report"
echo "========================"
echo ""

# MongoDB storage stats
echo "💾 MongoDB GridFS Storage:"
kubectl exec -n insightlearn mongodb-0 -- mongosh -u insightlearn \
    -p "$(kubectl get secret -n insightlearn insightlearn-secrets -o jsonpath='{.data.mongodb-password}' | base64 -d)" \
    --authenticationDatabase admin insightlearn_videos \
    --eval "
var stats = db.stats(1024*1024*1024);
print('  Data Size: ' + stats.dataSize.toFixed(2) + ' GB');
print('  Storage Size: ' + stats.storageSize.toFixed(2) + ' GB');
print('  % of 225GB total: ' + ((stats.storageSize / 225) * 100).toFixed(1) + '%');
db.fs.files.countDocuments().then(count => print('  Video files: ' + count));
" 2>/dev/null

echo ""

# K6 test results summary
echo "📊 K6 Test Results:"
if [ -f "$STRESS_DIR/load-test-results.json" ]; then
    echo "  Smoke: $STRESS_DIR/smoke-test-results.json"
    echo "  Load:  $STRESS_DIR/load-test-results.json"
    echo "  Stress: $STRESS_DIR/stress-test-results.json"
    echo ""
    echo "  HTML Reports:"
    [ -f "$STRESS_DIR/load-test-summary.html" ] && echo "  - $STRESS_DIR/load-test-summary.html"
    [ -f "$STRESS_DIR/stress-test-summary.html" ] && echo "  - $STRESS_DIR/stress-test-summary.html"
fi

echo ""
echo "🎉 Complete Stress Testing DONE!"
echo ""
echo "🌐 Browse test courses:"
echo "  http://localhost:31090/courses?tags=TEST"
echo ""
echo "📁 Test results location:"
echo "  $STRESS_DIR/"
