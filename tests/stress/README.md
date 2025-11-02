# InsightLearn Stress Testing with k6

**Professional Load, Stress, Spike, and Soak Testing for the InsightLearn Platform**

---

## 🎯 Overview

This directory contains comprehensive stress testing infrastructure using [k6](https://k6.io/), a modern open-source load testing tool. The tests are integrated with Jenkins CI/CD and provide detailed metrics via Grafana dashboards.

### What's Included

- **5 Test Types**: Smoke, Load, Stress, Spike, and Soak tests
- **Jenkins Integration**: Automated execution in CI/CD pipeline
- **Grafana Dashboard**: Real-time metrics visualization
- **Docker Support**: Containerized test execution
- **HTML Reports**: Beautiful test result reports

---

## 📋 Test Types

### 1. Smoke Test (`smoke-test.js`)
**Purpose**: Quick validation that the system is operational

- **Duration**: ~30 seconds
- **Virtual Users**: 1
- **Use Case**: Fast sanity check before deeper testing

```bash
k6 run smoke-test.js
```

### 2. Load Test (`load-test.js`)
**Purpose**: Test system under normal expected load

- **Duration**: ~9 minutes
- **Virtual Users**: 0 → 10 → 0
- **Stages**:
  - Ramp up to 10 users (2 min)
  - Stay at 10 users (5 min)
  - Ramp down (2 min)

```bash
k6 run load-test.js
```

### 3. Stress Test (`stress-test.js`)
**Purpose**: Test system beyond normal capacity to find breaking points

- **Duration**: ~16 minutes
- **Virtual Users**: 0 → 50 → 100 → 0
- **Stages**:
  - Ramp to 50 users (2 min)
  - Hold at 50 (5 min)
  - Ramp to 100 users (2 min)
  - Hold at 100 (5 min)
  - Ramp down (2 min)

```bash
k6 run stress-test.js
```

### 4. Spike Test (`spike-test.js`)
**Purpose**: Test system recovery from sudden traffic spikes

- **Duration**: ~4.5 minutes
- **Virtual Users**: 10 → 200 (sudden) → 10 → 0
- **Stages**:
  - Normal load (1 min)
  - Sudden spike to 200 (30 sec)
  - Maintain spike (1 min)
  - Return to normal (1 min)
  - Ramp down (1 min)

```bash
k6 run spike-test.js
```

### 5. Soak Test (`soak-test.js`)
**Purpose**: Detect memory leaks and resource exhaustion over extended period

- **Duration**: ~3 hours 10 minutes
- **Virtual Users**: 0 → 20 → 0
- **Stages**:
  - Ramp up (5 min)
  - Hold for 3 hours
  - Ramp down (5 min)

```bash
k6 run soak-test.js
```

---

## 🚀 Quick Start

### Option 1: One-Command Setup

```bash
cd /home/mpasqui/kubernetes/Insightlearn/tests/stress
./deploy-k6.sh
```

This will:
1. ✅ Install k6 (if not already installed)
2. ✅ Build Docker image
3. ✅ Deploy Grafana dashboard
4. ✅ Create results directory

### Option 2: Manual Setup

1. **Install k6**:

```bash
# Ubuntu/Debian
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg \
  --keyserver hkp://keyserver.ubuntu.com:80 \
  --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | \
  sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6

# macOS
brew install k6

# Docker
docker pull grafana/k6:latest
```

2. **Build Docker Image**:

```bash
docker build -t insightlearn/k6-tests:latest .
```

3. **Deploy Grafana Dashboard**:

```bash
kubectl apply -f ../../k8s/16-k6-grafana-dashboard.yaml
```

---

## 🧪 Running Tests

### Local Execution

```bash
# Navigate to stress test directory
cd /home/mpasqui/kubernetes/Insightlearn/tests/stress

# Run individual tests
k6 run smoke-test.js
k6 run load-test.js
k6 run stress-test.js

# Run with custom URLs
API_URL=http://192.168.49.2:31081 WEB_URL=http://192.168.49.2:31080 k6 run load-test.js

# Run all tests sequentially
./run-all-tests.sh

# Run all tests with custom URLs
./run-all-tests.sh http://192.168.49.2:31081 http://192.168.49.2:31080
```

### Docker Execution

```bash
# Run smoke test
docker run --rm \
  -v $(pwd):/tests \
  -v $(pwd)/results:/results \
  -e API_URL=http://192.168.49.2:31081 \
  -e WEB_URL=http://192.168.49.2:31080 \
  insightlearn/k6-tests:latest \
  run /tests/smoke-test.js

# Run load test
docker run --rm \
  -v $(pwd):/tests \
  -v $(pwd)/results:/results \
  -e API_URL=http://192.168.49.2:31081 \
  -e WEB_URL=http://192.168.49.2:31080 \
  insightlearn/k6-tests:latest \
  run --out json=/results/load-results.json /tests/load-test.js
```

### Jenkins Execution

The stress tests are automatically integrated into the Jenkins CI/CD pipeline:

1. **Access Jenkins**: http://192.168.49.2:32000
2. **Navigate to**: InsightLearn-CI-CD pipeline
3. **Click**: "Build Now"
4. **View Results**: In the build artifacts and HTML reports

**Environment Variables**:
- `FULL_STRESS_TEST=true` - Enable full stress test (16 minutes)
- Default: Only smoke and load tests run (~10 minutes total)

---

## 📊 Viewing Results

### Console Output

After each test run, you'll see a summary:

```
     running (9m09.0s), 00/10 VUs, 2847 complete and 0 interrupted iterations
     default ✓ [======================================] 00/10 VUs  9m9s

     ✓ Homepage loads successfully
     ✓ Health check is healthy
     ✓ Login responds

     checks.........................: 100.00% ✓ 8541       ✗ 0
     data_received..................: 1.2 MB  2.2 kB/s
     data_sent......................: 478 kB  876 B/s
     http_req_duration..............: avg=287.45ms min=12.34ms med=245.67ms max=987.12ms p(90)=456.78ms p(95)=567.89ms
     http_reqs......................: 2847    5.21/s
```

### HTML Reports

Load and stress tests generate beautiful HTML reports:

```
results/
├── load-test-summary.html      # Visual report with charts
├── stress-test-summary.html    # Detailed stress test results
└── SUMMARY.md                   # Combined markdown summary
```

Open in browser:
```bash
open results/20250119_143022/load-test-summary.html
```

### Grafana Dashboard

View real-time metrics during test execution:

1. **Access Grafana**: https://192.168.1.103/grafana
2. **Login**: admin / admin
3. **Navigate**: Dashboards → InsightLearn - k6 Stress Test Dashboard

**Dashboard Panels**:
- API Response Time (p95 & p99)
- Request Rate (req/s)
- Success Rate
- Error Rate
- HTTP Status Code Distribution
- Pod Availability

### Jenkins Reports

After Jenkins build completes:

1. Click on build number (e.g., #42)
2. View **Stress Test Reports** in the sidebar
3. Download artifacts from **Build Artifacts** section

---

## ⚙️ Configuration

### Environment Variables

Set these before running tests:

```bash
# API endpoint
export API_URL=http://192.168.49.2:31081

# Web endpoint
export WEB_URL=http://192.168.49.2:31080

# Run tests
k6 run load-test.js
```

### Test Configuration (`config.js`)

Edit `config.js` to customize:

```javascript
// Base URLs
export const BASE_API_URL = __ENV.API_URL || 'http://192.168.49.2:31081';
export const BASE_WEB_URL = __ENV.WEB_URL || 'http://192.168.49.2:31080';

// Thresholds
export const THRESHOLDS = {
    http_req_duration: ['p(95)<500'],    // 95% under 500ms
    http_req_failed: ['rate<0.01'],      // Less than 1% errors
};

// Load stages
export const LOAD_STAGES = {
    load: [
        { duration: '2m', target: 10 },   // Customize here
        { duration: '5m', target: 10 },
        { duration: '2m', target: 0 },
    ],
};
```

### Test Users

Default test users (configured in `config.js`):

```javascript
export const TEST_USERS = {
    admin: {
        email: 'admin@insightlearn.cloud',
        password: 'Admin123!'
    },
    student: {
        email: 'student@insightlearn.cloud',
        password: 'Student123!'
    }
};
```

---

## 📈 Understanding Metrics

### Response Time Percentiles

- **p(50) - Median**: 50% of requests complete in this time
- **p(90)**: 90% of requests complete in this time
- **p(95)**: 95% of requests - **Primary SLA metric**
- **p(99)**: 99% of requests - Worst case for most users

### Success Criteria

**Smoke Test**:
- ✅ All health checks pass
- ✅ Response time < 200ms

**Load Test**:
- ✅ p(95) < 500ms
- ✅ Error rate < 1%
- ✅ Check pass rate > 99%

**Stress Test**:
- ✅ p(95) < 2000ms (more lenient)
- ✅ Error rate < 5%
- ✅ Check pass rate > 90%
- ✅ System remains stable

**Spike Test**:
- ✅ System survives spike
- ✅ No cascade failures
- ✅ Recovery to normal within 1 min

**Soak Test**:
- ✅ No memory leaks
- ✅ Response time degradation < 100ms
- ✅ Error rate stable over 3 hours

---

## 🎯 Best Practices

### When to Run Each Test

| Test Type | Frequency | Trigger |
|-----------|-----------|---------|
| **Smoke** | Every commit | CI/CD pipeline |
| **Load** | Every merge to main | CI/CD pipeline |
| **Stress** | Weekly | Scheduled or manual |
| **Spike** | Before releases | Manual |
| **Soak** | Monthly | Scheduled overnight |

### Interpreting Results

**🟢 All Green** (Success):
- System is ready for production
- Performance meets SLA requirements
- No action needed

**🟡 Some Yellow** (Warning):
- Review logs for bottlenecks
- Consider optimizing slow endpoints
- Monitor in production

**🔴 Any Red** (Failure):
- **DO NOT DEPLOY to production**
- Investigate root cause
- Fix issues and re-test
- Consider scaling resources

### Common Issues

**High Response Times**:
- Check database query performance
- Review API endpoint efficiency
- Increase pod replicas
- Add caching

**High Error Rates**:
- Check application logs
- Review database connections
- Check resource limits (CPU/Memory)
- Verify network connectivity

**Memory Leaks** (Soak Test):
- Review application code for leaks
- Check connection pool management
- Monitor pod memory usage
- Consider pod restarts

---

## 🔧 Troubleshooting

### k6 Not Installed

```bash
# Check installation
k6 version

# If not found, run deployment script
./deploy-k6.sh
```

### Services Not Accessible

```bash
# Check if pods are running
kubectl get pods -n insightlearn

# Check service endpoints
kubectl get svc -n insightlearn

# Test connectivity
curl http://192.168.49.2:31081/health
curl http://192.168.49.2:31080/health
```

### Docker Image Build Fails

```bash
# Check Docker is running
docker ps

# Rebuild image
docker build -t insightlearn/k6-tests:latest . --no-cache

# Check logs
docker logs <container-id>
```

### Tests Fail Immediately

```bash
# Verify URLs are correct
echo $API_URL
echo $WEB_URL

# Test connectivity
curl -v $API_URL/health

# Run with verbose output
k6 run --verbose smoke-test.js
```

### No Grafana Dashboard

```bash
# Check monitoring namespace
kubectl get namespace monitoring

# Redeploy dashboard
kubectl apply -f ../../k8s/16-k6-grafana-dashboard.yaml

# Verify ConfigMap
kubectl get configmap k6-stress-test-dashboard -n monitoring
```

---

## 📁 File Structure

```
tests/stress/
├── config.js                 # Global configuration
├── smoke-test.js             # Smoke test (30s)
├── load-test.js              # Load test (9min)
├── stress-test.js            # Stress test (16min)
├── spike-test.js             # Spike test (4.5min)
├── soak-test.js              # Soak test (3h)
├── Dockerfile                # k6 Docker image
├── deploy-k6.sh              # Deployment script
├── run-all-tests.sh          # Run all tests script
├── README.md                 # This file
└── results/                  # Test results (auto-created)
    └── YYYYMMDD_HHMMSS/
        ├── smoke-results.json
        ├── load-results.json
        ├── load-test-summary.html
        ├── stress-results.json
        ├── stress-test-summary.html
        └── SUMMARY.md
```

---

## 🔗 Related Documentation

- [k8s/README.md](../../k8s/README.md) - Kubernetes deployment guide
- [README-TESTING.md](../../README-TESTING.md) - Overall testing guide
- [JENKINS-TESTING-GUIDE.md](../../JENKINS-TESTING-GUIDE.md) - Jenkins CI/CD guide
- [MONITORING-GUIDE.md](../../MONITORING-GUIDE.md) - Monitoring setup

---

## 📚 Resources

- **k6 Documentation**: https://k6.io/docs/
- **k6 Examples**: https://k6.io/docs/examples/
- **Grafana k6 Plugin**: https://grafana.com/grafana/plugins/grafana-k6-app/
- **Load Testing Best Practices**: https://k6.io/docs/testing-guides/

---

## 🤝 Contributing

To add new stress test scenarios:

1. Create new test file: `custom-test.js`
2. Import from `config.js`:
   ```javascript
   import { BASE_API_URL, commonOptions } from './config.js';
   ```
3. Define test stages and logic
4. Add to `run-all-tests.sh` if needed
5. Update this README

---

## 📝 Example Output

```
╔═══════════════════════════════════════════════════════════════╗
║     InsightLearn Comprehensive Stress Testing Suite          ║
╚═══════════════════════════════════════════════════════════════╝

API URL: http://192.168.49.2:31081
Web URL: http://192.168.49.2:31080
Results Directory: ./results/20250119_143022

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  Running: Smoke Test
  Estimated Duration: ~30 seconds
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ Smoke Test completed successfully!

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  Running: Load Test
  Estimated Duration: ~9 minutes
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

     checks.........................: 100.00% ✓ 2547 ✗ 0
     http_req_duration..............: avg=287.45ms p(95)=456.78ms
     http_reqs......................: 2547 4.71/s

✅ Load Test completed successfully!

╔═══════════════════════════════════════════════════════════════╗
║                    TEST SUITE SUMMARY                         ║
╚═══════════════════════════════════════════════════════════════╝

  ✅ Smoke Test
  ✅ Load Test
  ✅ Stress Test
  ✅ Spike Test

Results saved to: ./results/20250119_143022
```

---

## 🎓 Learning Resources

### Understanding Load Testing

- **Load Test**: Normal expected traffic
- **Stress Test**: Beyond capacity to find limits
- **Spike Test**: Sudden traffic increase
- **Soak Test**: Extended duration for stability

### k6 Learning Path

1. Start with smoke tests
2. Progress to load tests
3. Run stress tests to find limits
4. Use spike tests for resilience
5. Schedule soak tests for stability

---

**Happy Stress Testing! 🚀**

For questions or issues, refer to the troubleshooting section or check the Jenkins logs.
