# Jenkins Automated Testing Setup

This directory contains Jenkins pipeline configuration and testing scripts for InsightLearn WASM.

## 📋 Contents

- **[Jenkinsfile](../Jenkinsfile)** - Main Jenkins pipeline for automated testing
- **[scripts/load-test.sh](scripts/load-test.sh)** - Load testing script (light/medium/heavy/stress)
- **[scripts/site-monitor.sh](scripts/site-monitor.sh)** - Continuous site monitoring

---

## 🚀 Quick Start

### 1. Access Jenkins

Jenkins is accessible at: **http://localhost:8080**

Default credentials (if configured):
- Username: `admin`
- Password: Check Jenkins initial password or `.env` file

### 2. Create Jenkins Pipeline Job

1. Open Jenkins: http://localhost:8080
2. Click **"New Item"**
3. Enter name: `insightlearn-automated-tests`
4. Select **"Pipeline"**
5. Click **OK**

### 3. Configure Pipeline

In the job configuration:

1. **General** section:
   - ✅ Check "GitHub project"
   - Project URL: `https://github.com/marypas74/InsightLearn_WASM`

2. **Build Triggers** section:
   - ✅ Check "Poll SCM" or "Build periodically"
   - Schedule: `H * * * *` (every hour)

3. **Pipeline** section:
   - Definition: **Pipeline script from SCM**
   - SCM: **Git**
   - Repository URL: `https://github.com/marypas74/InsightLearn_WASM.git`
   - Branch: `*/main`
   - Script Path: `Jenkinsfile`

4. Click **Save**

### 4. Run First Test

1. Click **"Build Now"**
2. Watch the console output
3. Review test results

---

## 🔧 Testing Scripts

### Load Testing Script

**File**: `scripts/load-test.sh`

**Usage**:
```bash
# Light load (10 concurrent, 50 total requests)
./jenkins/scripts/load-test.sh light

# Medium load (25 concurrent, 100 total requests)
./jenkins/scripts/load-test.sh medium

# Heavy load (50 concurrent, 200 total requests)
./jenkins/scripts/load-test.sh heavy

# Stress test (100 concurrent, 500 total requests) - USE WITH CAUTION!
./jenkins/scripts/load-test.sh stress
```

**Features**:
- Concurrent request simulation
- Response time analysis (min/max/average)
- Success/failure rate tracking
- Requests per second (RPS) calculation
- Automatic report generation

**Output**:
- Console: Real-time results
- File: `load-test-report-YYYYMMDD_HHMMSS.txt`

**Example Output**:
```
RESULTS:
  Total requests: 50
  Successful (200): 50
  Failed: 0
  Duration: 5s
  Requests/sec: 10.00
  Avg response time: 0.108s
  Min response time: 0.103s
  Max response time: 0.117s
```

---

### Site Monitoring Script

**File**: `scripts/site-monitor.sh`

**Usage**:
```bash
# Monitor every 60 seconds (default)
./jenkins/scripts/site-monitor.sh

# Monitor every 30 seconds
./jenkins/scripts/site-monitor.sh 30

# Monitor every 5 minutes (300 seconds)
./jenkins/scripts/site-monitor.sh 300
```

**Features**:
- Continuous endpoint health checks
- Response time monitoring
- Kubernetes pod status (if kubectl available)
- SSL certificate expiry check
- Uptime percentage calculation
- Alert generation on failures

**Thresholds**:
- Response time: >1.0s triggers warning
- Consecutive failures: 3 before critical alert
- SSL certificate: Alert if <30 days until expiry

**Output Files**:
- `monitoring-YYYYMMDD.log` - All events
- `alerts-YYYYMMDD.log` - Only alerts

**Example Log**:
```
[2025-11-08 17:00:00] [INFO] Endpoint / OK: 200 in 0.110s
[2025-11-08 17:01:00] [INFO] Endpoint /courses OK: 200 in 0.105s
[2025-11-08 17:05:00] [ERROR] Endpoint /api/info returned 502 (expected 200)
[2025-11-08 17:10:00] [INFO] Uptime: 95.00% (19/20 successful)
```

---

## 🎯 Jenkins Pipeline Stages

The [Jenkinsfile](../Jenkinsfile) includes these stages:

1. **Preparation** - Initialize environment variables
2. **Health Check** - Test main site and API endpoints
3. **Page Availability Test** - Check all pages for 404 errors
4. **Performance Benchmarking** - Run 10 tests, calculate average response time
5. **Load Testing** - Simulate 50 concurrent users
6. **Asset Validation** - Verify all CSS/JS/images load correctly
7. **Security Headers Check** - Validate security headers present
8. **Backend API Monitoring** - Check Kubernetes pod status
9. **Generate Report** - Create summary report

**Success Criteria**:
- ✅ All frontend pages return 200
- ✅ Average response time <500ms
- ✅ All static assets available
- ✅ Security headers present
- ✅ No pod failures (if Kubernetes)

**Failure Triggers**:
- ❌ Any page returns 404
- ❌ Average response time >500ms
- ❌ >5% failure rate in load test
- ❌ Missing critical assets

---

## 📊 Monitoring Dashboard (Optional)

### Jenkins Blue Ocean

For a better visual experience, install Blue Ocean plugin:

1. Go to **Manage Jenkins** → **Manage Plugins**
2. Search for **"Blue Ocean"**
3. Install and restart Jenkins
4. Access: http://localhost:8080/blue

### Prometheus + Grafana Integration

If you have Prometheus and Grafana running (from docker-compose.yml):

1. **Prometheus** is scraping metrics from the API (if `/metrics` endpoint enabled)
2. **Grafana** dashboards are available at: http://localhost:3000
3. Create custom dashboard for test results

---

## 📧 Notifications Setup

### Email Notifications

Edit `Jenkinsfile` line 231:
```groovy
emailext(
    subject: "...",
    body: "...",
    to: "your-email@example.com",  // <-- Change this
    attachLog: true
)
```

### Slack Notifications (Optional)

1. Install **Slack Notification Plugin** in Jenkins
2. Configure Slack workspace and channel
3. Uncomment Slack lines in `Jenkinsfile`:
```groovy
slackSend(
    channel: '#insightlearn-alerts',
    color: 'good',
    message: "Tests passed - Build #${env.BUILD_NUMBER}"
)
```

---

## 🔍 Troubleshooting

### Pipeline Fails on First Run

**Issue**: Permission denied on scripts

**Fix**:
```bash
chmod +x jenkins/scripts/*.sh
```

### "kubectl: command not found"

**Issue**: Kubernetes CLI not available in Jenkins container

**Fix**: Add kubectl to Jenkins Docker image or skip K8s checks

### Load Test Shows High Failure Rate

**Issue**: Backend API returning 502

**Fix**: This is expected until backend API issue is resolved. Update `Jenkinsfile` to expect 502 or disable API tests temporarily.

### Monitoring Script Exits Immediately

**Issue**: Script syntax error or permission issue

**Fix**:
```bash
# Check script
bash -n jenkins/scripts/site-monitor.sh

# Run with verbose output
bash -x jenkins/scripts/site-monitor.sh 60
```

---

## 📈 Performance Benchmarks

### Current Baseline (2025-11-08)

| Metric | Value | Status |
|--------|-------|--------|
| **Frontend Response Time** | 110ms average | ✅ Excellent |
| **Page Load (All Routes)** | 100% success | ✅ Perfect |
| **Static Assets** | 100% available | ✅ Perfect |
| **Security Headers** | 4/5 present | ⚠️ CSP missing |
| **Backend API** | 502 errors | ❌ Down |

### Performance Targets

- **Response Time**: <200ms (P95), <500ms (P99)
- **Uptime**: >99.9% (frontend), >99.5% (backend)
- **Load Capacity**: Support 100 concurrent users
- **Failure Rate**: <0.1%

---

## 🛠️ Advanced Configuration

### Custom Test Endpoints

Edit `load-test.sh` to add custom endpoints:
```bash
# Add after line 100
run_load_test 10 30 "/api/courses"
run_load_test 5 15 "/api/dashboard/stats"
```

### Adjust Thresholds

Edit `site-monitor.sh`:
```bash
# Line 12-13
RESPONSE_TIME_THRESHOLD=2.0  # Increase to 2 seconds
FAILURE_THRESHOLD=5          # Allow 5 consecutive failures
```

### Custom Alerts

Add webhook integration in `site-monitor.sh`:
```bash
send_alert() {
    # ... existing code ...

    # Add Slack webhook
    curl -X POST https://hooks.slack.com/services/YOUR/WEBHOOK/URL \
        -H 'Content-Type: application/json' \
        -d "{\"text\": \"$message\"}"
}
```

---

## 📚 Additional Resources

- **Jenkins Documentation**: https://www.jenkins.io/doc/
- **Jenkins Pipeline Syntax**: https://www.jenkins.io/doc/book/pipeline/syntax/
- **Load Testing Best Practices**: https://www.blazemeter.com/blog/load-testing-best-practices
- **Site Reliability Engineering**: https://sre.google/books/

---

## 📝 Next Steps

1. ✅ Configure Jenkins job with Jenkinsfile
2. ✅ Run first automated test
3. [ ] Fix backend API 502 errors
4. [ ] Update Jenkinsfile to test backend when fixed
5. [ ] Setup email/Slack notifications
6. [ ] Create Grafana dashboard for test metrics
7. [ ] Schedule regular load tests (weekly)
8. [ ] Add performance regression tests

---

**Last Updated**: 2025-11-08
**Maintainer**: marcello.pasqui@gmail.com
**Repository**: https://github.com/marypas74/InsightLearn_WASM
