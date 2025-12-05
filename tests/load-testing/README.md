# InsightLearn Load & Stress Testing Suite

Complete stress testing toolkit for InsightLearn platform with MongoDB GridFS load testing and K6 performance testing.

## 🎯 Objective

Fill **50% of MongoDB storage** (~112 GB) with real video files via GridFS, create test courses, and execute comprehensive stress tests.

## 📦 Components

### 1. **Fast Parallel Load Test** (Recommended)
- **File**: `fast-parallel-load-test.sh`
- **Duration**: ~2-3 hours (depends on upload speed)
- **Features**:
  - Generates 240x 100MB synthetic videos (~24 GB raw, ~14 GB compressed)
  - Creates 20 courses with sections and lessons
  - Uploads videos in **parallel** (20 simultaneous uploads)
  - Real GridFS storage (not links!)
  - Tags courses with "TEST" for easy filtering

**Usage**:
```bash
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/tests/load-testing
./fast-parallel-load-test.sh
```

### 2. **Complete Stress Test Suite** (Full Pipeline)
- **File**: `complete-stress-test.sh`
- **Duration**: ~4-5 hours
- **Phases**:
  1. MongoDB GridFS load (video generation + upload)
  2. K6 smoke test (30s validation)
  3. K6 load test (9 minutes)
  4. K6 stress test (16 minutes)
  5. Final report generation

**Usage**:
```bash
cd /home/mpasqui/insightlearn_WASM/InsightLearn_WASM/tests/load-testing
./complete-stress-test.sh
```

### 3. **Individual Scripts**

#### Generate Videos Only
```bash
./generate-test-videos.sh
# Output: /tmp/insightlearn-test-videos/*.mp4
```

#### Upload Videos Only
```bash
./upload-test-videos.sh
# Requires: videos in /tmp/insightlearn-test-videos/
```

#### Create Courses Only
```bash
./create-test-courses.sh
# Creates: 50 FREE courses with tag "TEST"
```

## 🔧 Requirements

### System Requirements
- **RAM**: 16 GB minimum (parallel video generation)
- **Disk Space**: 30 GB free (temporary video storage)
- **CPU**: 4+ cores recommended (parallel operations)

### Software Dependencies
- `ffmpeg` - Video generation (auto-installed)
- `k6` - Load testing framework (auto-installed)
- `jq` - JSON parsing
- `kubectl` - Kubernetes access
- `curl` - HTTP requests

### Kubernetes Access
- API endpoint accessible at `http://localhost:31081`
- MongoDB pod running in namespace `insightlearn`
- Valid admin credentials

## 📊 Configuration

Edit scripts to customize:

### fast-parallel-load-test.sh
```bash
COURSES_COUNT=20              # Number of courses
SECTIONS_PER_COURSE=3         # Sections per course
LESSONS_PER_SECTION=4         # Lessons per section
VIDEOS_TO_GENERATE=240        # Total videos
VIDEO_SIZE_MB=100             # Size per video
PARALLEL_UPLOADS=20           # Simultaneous uploads
```

### K6 Test Configuration
Edit `tests/stress/config.js`:
```javascript
export const LOAD_STAGES = {
    stress: [
        { duration: '2m', target: 50 },   // Ramp to 50 users
        { duration: '5m', target: 100 },  // Ramp to 100 users
        // ... customize stages
    ]
};
```

## 🚀 Quick Start

### Option 1: Full Automated Test
```bash
# One command to rule them all
cd tests/load-testing
./complete-stress-test.sh
```

### Option 2: Step-by-Step
```bash
# 1. Fast parallel load
./fast-parallel-load-test.sh

# 2. Browse test courses
open http://localhost:31090/courses?tags=TEST

# 3. Manual K6 tests
cd ../stress
k6 run load-test.js
k6 run stress-test.js
```

## 📈 Monitoring

### During Test Execution

**MongoDB Storage**:
```bash
kubectl exec -n insightlearn mongodb-0 -- mongosh -u insightlearn \
  -p "$(kubectl get secret -n insightlearn insightlearn-secrets -o jsonpath='{.data.mongodb-password}' | base64 -d)" \
  --authenticationDatabase admin insightlearn_videos \
  --eval "db.stats(1024*1024*1024)"
```

**API Health**:
```bash
curl http://localhost:31081/health
```

**Test Courses**:
```bash
curl http://localhost:31081/api/courses?tags=TEST
```

### After Test Completion

**K6 Results**:
- JSON: `tests/stress/load-test-results.json`
- HTML: `tests/stress/load-test-summary.html`

**Upload Logs**:
- Log file: `/tmp/insightlearn-upload.log`

## 🎯 Expected Results

### MongoDB GridFS
- **Target**: ~112 GB (50% of 225 GB)
- **Actual**: ~14-20 GB after GZip compression
- **Files**: 240 video files in GridFS
- **Compression Ratio**: ~60% (GZip optimal)

### K6 Performance
- **Smoke Test**: All checks pass (< 30s)
- **Load Test**:
  - 10 concurrent users sustained
  - p95 response time < 500ms
  - Error rate < 1%
- **Stress Test**:
  - 100 concurrent users peak
  - p95 response time < 1000ms
  - Error rate < 1%

### Test Courses
- **Count**: 20 courses
- **Sections**: 60 total (3 per course)
- **Lessons**: 240 total (4 per section)
- **Videos**: 240 GridFS files (1 per lesson)
- **Tags**: All courses tagged "TEST", "GRIDFS", "FREE"

## 🧹 Cleanup

### Remove Test Courses
```bash
# Get all TEST courses
COURSE_IDS=$(curl -s http://localhost:31081/api/courses?tags=TEST | jq -r '.[].id')

# Delete each course
for id in $COURSE_IDS; do
  curl -X DELETE -H "Authorization: Bearer $TOKEN" \
    "http://localhost:31081/api/courses/$id"
done
```

### Remove Test Videos
```bash
kubectl exec -n insightlearn mongodb-0 -- mongosh -u insightlearn \
  -p "PASSWORD" --authenticationDatabase admin insightlearn_videos \
  --eval "db.fs.files.deleteMany({}); db.fs.chunks.deleteMany({})"
```

### Remove Local Video Files
```bash
rm -rf /tmp/insightlearn-test-videos
```

## 🐛 Troubleshooting

### API Not Accessible
```bash
# Check API pod status
kubectl get pods -n insightlearn | grep api

# Port-forward if needed
kubectl port-forward -n insightlearn svc/insightlearn-api 31081:80
```

### MongoDB Connection Error
```bash
# Check MongoDB pod
kubectl get pods -n insightlearn | grep mongodb

# Check secret
kubectl get secret -n insightlearn insightlearn-secrets -o jsonpath='{.data.mongodb-password}' | base64 -d
```

### Video Upload Failures
- **Check upload log**: `/tmp/insightlearn-upload.log`
- **Verify API token**: Token expires after 7 days
- **Check disk space**: `df -h /tmp`
- **Increase parallel uploads**: Lower `PARALLEL_UPLOADS` value if API overwhelmed

### K6 Test Failures
- **Check API health**: `curl http://localhost:31081/health`
- **Verify test users exist**: Check `tests/stress/config.js` credentials
- **Review thresholds**: Edit `config.js` if too strict

## 📚 References

- **K6 Documentation**: https://k6.io/docs/
- **MongoDB GridFS**: https://docs.mongodb.com/manual/core/gridfs/
- **InsightLearn API**: http://localhost:31081/swagger

## 🔒 Security Notes

⚠️ **Important**:
- Test courses are marked as FREE and PUBLIC
- Do NOT use production credentials in test scripts
- Clean up test data after testing
- Monitor MongoDB disk usage to avoid filling storage

## 📝 Notes

- Video files are **synthetic test patterns** (color bars + timer)
- Each video is ~5 minutes duration, 1280x720 resolution
- GZip compression reduces storage by ~60%
- K6 tests simulate realistic user behavior patterns
- All operations are **idempotent** (safe to re-run)

---

**Author**: Test Engineer
**Date**: 2025-12-05
**Version**: 1.0
