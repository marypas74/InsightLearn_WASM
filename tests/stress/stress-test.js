/**
 * k6 Stress Test for InsightLearn
 *
 * Purpose: Test system under extreme load (beyond normal capacity)
 * Duration: ~16 minutes
 * VUs: 0-100 virtual users
 *
 * Run with: k6 run stress-test.js
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend, Counter } from 'k6/metrics';
import {
    BASE_API_URL,
    BASE_WEB_URL,
    TEST_USERS,
    LOAD_STAGES,
    commonOptions,
    getHeaders,
    getTags,
    THINK_TIME
} from './config.js';

// Custom metrics
const errorRate = new Rate('errors');
const loginDuration = new Trend('login_duration');
const apiCallDuration = new Trend('api_call_duration');
const pageLoadDuration = new Trend('page_load_duration');
const successfulRequests = new Counter('successful_requests');
const failedRequests = new Counter('failed_requests');
const timeoutErrors = new Counter('timeout_errors');
const serverErrors = new Counter('server_errors');

export const options = {
    ...commonOptions,
    stages: LOAD_STAGES.stress,
    tags: {
        test_type: 'stress',
    },
    thresholds: {
        // More lenient thresholds for stress testing
        'http_req_duration': ['p(95)<2000'],  // 95% under 2s
        'http_req_duration{type:api}': ['p(99)<3000'],  // 99% under 3s
        'http_req_failed': ['rate<0.05'],  // Less than 5% error rate
        'checks': ['rate>0.90'],  // 90% check pass rate
    },
};

// Test scenarios with realistic user behavior
const scenarios = {
    concurrentBrowsing: {
        weight: 30,
        exec: 'browseCourses'
    },
    heavyApiUsage: {
        weight: 25,
        exec: 'apiOperations'
    },
    authenticationFlow: {
        weight: 20,
        exec: 'userAuthentication'
    },
    searchOperations: {
        weight: 15,
        exec: 'searchContent'
    },
    mixedOperations: {
        weight: 10,
        exec: 'mixedScenario'
    }
};

export default function () {
    // Randomly select a scenario based on weights
    const rand = Math.random() * 100;
    let cumulative = 0;

    for (const [name, scenario] of Object.entries(scenarios)) {
        cumulative += scenario.weight;
        if (rand <= cumulative) {
            __ENV.SCENARIO = name;
            eval(scenario.exec + '()');
            break;
        }
    }
}

// Scenario 1: Browse courses and content
export function browseCourses() {
    group('Browse Courses - Stress', function () {
        // Homepage
        let res = http.get(`${BASE_WEB_URL}/`, {
            headers: getHeaders(),
            tags: getTags('stress', 'homepage'),
        });

        let success = check(res, {
            'Homepage loads': (r) => r.status < 500,
        });

        pageLoadDuration.add(res.timings.duration);
        trackResult(success, res);

        sleep(1);

        // Courses list
        res = http.get(`${BASE_API_URL}/api/courses`, {
            headers: getHeaders(),
            tags: getTags('stress', 'courses-list'),
        });

        success = check(res, {
            'Courses list responds': (r) => r.status < 500,
        });

        apiCallDuration.add(res.timings.duration);
        trackResult(success, res);

        sleep(1);

        // Course detail
        const courseId = Math.floor(Math.random() * 100) + 1;
        res = http.get(`${BASE_API_URL}/api/courses/${courseId}`, {
            headers: getHeaders(),
            tags: getTags('stress', 'course-detail'),
        });

        success = check(res, {
            'Course detail responds': (r) => r.status < 500,
        });

        apiCallDuration.add(res.timings.duration);
        trackResult(success, res);
    });

    sleep(Math.random() * THINK_TIME.max);
}

// Scenario 2: Heavy API operations
export function apiOperations() {
    group('API Operations - Stress', function () {
        const batch = http.batch([
            ['GET', `${BASE_API_URL}/health`, null, { tags: getTags('stress', 'health') }],
            ['GET', `${BASE_API_URL}/api/courses`, null, { tags: getTags('stress', 'courses') }],
            ['GET', `${BASE_API_URL}/api/categories`, null, { tags: getTags('stress', 'categories') }],
        ]);

        batch.forEach((res, index) => {
            const success = check(res, {
                'Batch request successful': (r) => r.status < 500,
            });

            apiCallDuration.add(res.timings.duration);
            trackResult(success, res);
        });
    });

    sleep(Math.random() * THINK_TIME.max);
}

// Scenario 3: User authentication flow
export function userAuthentication() {
    group('Authentication - Stress', function () {
        // Login attempt
        const loginPayload = JSON.stringify({
            email: TEST_USERS.student.email,
            password: TEST_USERS.student.password,
        });

        const startTime = new Date().getTime();
        const res = http.post(`${BASE_API_URL}/api/auth/login`, loginPayload, {
            headers: getHeaders(),
            tags: getTags('stress', 'login'),
        });
        const duration = new Date().getTime() - startTime;

        const success = check(res, {
            'Login responds': (r) => r.status < 500,
            'Login time reasonable': (r) => r.timings.duration < 5000,
        });

        loginDuration.add(duration);
        trackResult(success, res);

        sleep(1);

        // If login successful, make authenticated requests
        if (res.status === 200) {
            try {
                const body = JSON.parse(res.body);
                if (body.token) {
                    // Profile access
                    const profileRes = http.get(`${BASE_API_URL}/api/user/profile`, {
                        headers: getHeaders(body.token),
                        tags: getTags('stress', 'profile'),
                    });

                    check(profileRes, {
                        'Profile access successful': (r) => r.status < 500,
                    });

                    trackResult(true, profileRes);

                    sleep(1);

                    // Enrolled courses
                    const enrolledRes = http.get(`${BASE_API_URL}/api/user/enrolled`, {
                        headers: getHeaders(body.token),
                        tags: getTags('stress', 'enrolled'),
                    });

                    check(enrolledRes, {
                        'Enrolled courses responds': (r) => r.status < 500,
                    });

                    trackResult(true, enrolledRes);
                }
            } catch (e) {
                failedRequests.add(1);
            }
        }
    });

    sleep(Math.random() * THINK_TIME.max);
}

// Scenario 4: Search operations
export function searchContent() {
    group('Search - Stress', function () {
        const searchTerms = [
            'programming', 'javascript', 'python', 'data science',
            'machine learning', 'web development', 'mobile apps',
            'database', 'cloud computing', 'cybersecurity'
        ];

        const searchTerm = searchTerms[Math.floor(Math.random() * searchTerms.length)];

        const res = http.get(`${BASE_API_URL}/api/search?q=${encodeURIComponent(searchTerm)}`, {
            headers: getHeaders(),
            tags: getTags('stress', 'search'),
        });

        const success = check(res, {
            'Search responds': (r) => r.status < 500,
            'Search completes in time': (r) => r.timings.duration < 3000,
        });

        apiCallDuration.add(res.timings.duration);
        trackResult(success, res);
    });

    sleep(Math.random() * THINK_TIME.max);
}

// Scenario 5: Mixed operations
export function mixedScenario() {
    group('Mixed Operations - Stress', function () {
        // Randomly execute different operations
        const operations = [
            () => http.get(`${BASE_WEB_URL}/`, { tags: getTags('stress', 'home') }),
            () => http.get(`${BASE_API_URL}/health`, { tags: getTags('stress', 'health') }),
            () => http.get(`${BASE_API_URL}/api/courses`, { tags: getTags('stress', 'courses') }),
            () => http.get(`${BASE_API_URL}/api/search?q=test`, { tags: getTags('stress', 'search') }),
        ];

        const operation = operations[Math.floor(Math.random() * operations.length)];
        const res = operation();

        const success = check(res, {
            'Mixed operation successful': (r) => r.status < 500,
        });

        trackResult(success, res);
    });

    sleep(Math.random() * THINK_TIME.max);
}

// Helper function to track results
function trackResult(success, response) {
    if (success && response.status < 400) {
        successfulRequests.add(1);
    } else {
        failedRequests.add(1);
        errorRate.add(1);

        if (response.status === 0) {
            timeoutErrors.add(1);
        } else if (response.status >= 500) {
            serverErrors.add(1);
        }
    }
}

export function handleSummary(data) {
    console.log('\n========================================');
    console.log('     STRESS TEST SUMMARY');
    console.log('========================================\n');

    const metrics = data.metrics;

    console.log(`Test Duration:        ${(data.state.testRunDurationMs / 1000 / 60).toFixed(2)} minutes`);
    console.log(`Total Requests:       ${metrics.http_reqs?.values.count || 0}`);
    console.log(`Successful Requests:  ${metrics.successful_requests?.values.count || 0}`);
    console.log(`Failed Requests:      ${metrics.failed_requests?.values.count || 0}`);
    console.log(`Timeout Errors:       ${metrics.timeout_errors?.values.count || 0}`);
    console.log(`Server Errors (5xx):  ${metrics.server_errors?.values.count || 0}`);
    console.log(`Requests/sec:         ${metrics.http_reqs?.values.rate.toFixed(2) || 0}`);
    console.log(`Error Rate:           ${((metrics.http_req_failed?.values.rate || 0) * 100).toFixed(2)}%`);
    console.log(`\nResponse Times (ms):`);
    console.log(`  Average:            ${metrics.http_req_duration?.values.avg.toFixed(2) || 0}`);
    console.log(`  Median:             ${metrics.http_req_duration?.values.med.toFixed(2) || 0}`);
    console.log(`  95th Percentile:    ${metrics.http_req_duration?.values['p(95)'].toFixed(2) || 0}`);
    console.log(`  99th Percentile:    ${metrics.http_req_duration?.values['p(99)'].toFixed(2) || 0}`);
    console.log(`  Max:                ${metrics.http_req_duration?.values.max.toFixed(2) || 0}`);

    // Determine if test passed based on thresholds
    const errorRate = (metrics.http_req_failed?.values.rate || 0) * 100;
    const p95 = metrics.http_req_duration?.values['p(95)'] || 0;
    const checkRate = (metrics.checks?.values.passes / metrics.checks?.values.count) * 100 || 0;

    console.log(`\n========================================`);
    if (errorRate < 5 && p95 < 2000 && checkRate > 90) {
        console.log('     ✅ STRESS TEST PASSED');
    } else {
        console.log('     ⚠️  STRESS TEST WARNING');
        if (errorRate >= 5) console.log(`     - Error rate too high: ${errorRate.toFixed(2)}%`);
        if (p95 >= 2000) console.log(`     - P95 response time too high: ${p95.toFixed(2)}ms`);
        if (checkRate <= 90) console.log(`     - Check pass rate too low: ${checkRate.toFixed(2)}%`);
    }
    console.log('========================================\n');

    return {
        'stdout': textSummary(data, { indent: ' ', enableColors: true }),
        'stress-test-results.json': JSON.stringify(data),
        'stress-test-summary.html': generateHTMLReport(data),
    };
}

function generateHTMLReport(data) {
    const metrics = data.metrics;
    const errorRate = ((metrics.http_req_failed?.values.rate || 0) * 100).toFixed(2);
    const passRate = ((metrics.checks?.values.passes / metrics.checks?.values.count) * 100 || 0).toFixed(2);

    return `
<!DOCTYPE html>
<html>
<head>
    <title>Stress Test Results - InsightLearn</title>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
        .container { max-width: 1400px; margin: 0 auto; background: white; padding: 40px; border-radius: 12px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); }
        h1 { color: #2c3e50; border-bottom: 4px solid #667eea; padding-bottom: 15px; margin-bottom: 30px; font-size: 36px; }
        .header-info { display: flex; justify-content: space-between; margin-bottom: 30px; padding: 20px; background: #f8f9fa; border-radius: 8px; }
        .metric-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 25px; margin: 30px 0; }
        .metric-card { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 25px; border-radius: 10px; color: white; box-shadow: 0 4px 15px rgba(0,0,0,0.1); transition: transform 0.3s; }
        .metric-card:hover { transform: translateY(-5px); }
        .metric-card h3 { margin: 0 0 15px 0; font-size: 14px; text-transform: uppercase; opacity: 0.9; }
        .metric-card .value { font-size: 42px; font-weight: bold; margin-bottom: 5px; }
        .metric-card .unit { font-size: 14px; opacity: 0.8; }
        .success { background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); }
        .warning { background: linear-gradient(135deg, #f2994a 0%, #f2c94c 100%); }
        .danger { background: linear-gradient(135deg, #eb3349 0%, #f45c43 100%); }
        table { width: 100%; border-collapse: collapse; margin: 30px 0; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
        th, td { padding: 15px; text-align: left; }
        th { background: #2c3e50; color: white; font-weight: 600; text-transform: uppercase; font-size: 12px; }
        tr { background: white; border-bottom: 1px solid #ecf0f1; }
        tr:nth-child(even) { background: #f8f9fa; }
        tr:hover { background: #e3f2fd; }
        .status-badge { display: inline-block; padding: 8px 16px; border-radius: 20px; font-weight: bold; font-size: 14px; }
        .badge-pass { background: #27ae60; color: white; }
        .badge-warn { background: #f39c12; color: white; }
        .badge-fail { background: #e74c3c; color: white; }
        .footer { margin-top: 40px; padding-top: 20px; border-top: 2px solid #ecf0f1; color: #7f8c8d; font-size: 13px; text-align: center; }
        .chart-container { margin: 30px 0; padding: 20px; background: #f8f9fa; border-radius: 8px; }
    </style>
</head>
<body>
    <div class="container">
        <h1>⚡ InsightLearn Stress Test Results</h1>

        <div class="header-info">
            <div>
                <strong>Test Type:</strong> Stress Test<br>
                <strong>Date:</strong> ${new Date().toISOString()}<br>
                <strong>Duration:</strong> ${(data.state.testRunDurationMs / 1000 / 60).toFixed(2)} minutes
            </div>
            <div>
                <strong>Max VUs:</strong> 100<br>
                <strong>Status:</strong>
                ${errorRate < 5 && passRate > 90 ?
                    '<span class="status-badge badge-pass">✅ PASSED</span>' :
                    errorRate < 10 ?
                    '<span class="status-badge badge-warn">⚠️ WARNING</span>' :
                    '<span class="status-badge badge-fail">❌ FAILED</span>'
                }
            </div>
        </div>

        <div class="metric-grid">
            <div class="metric-card">
                <h3>Total Requests</h3>
                <div class="value">${(metrics.http_reqs?.values.count || 0).toLocaleString()}</div>
                <div class="unit">requests executed</div>
            </div>

            <div class="metric-card ${passRate > 95 ? 'success' : passRate > 90 ? 'warning' : 'danger'}">
                <h3>Check Pass Rate</h3>
                <div class="value">${passRate}%</div>
                <div class="unit">checks passed</div>
            </div>

            <div class="metric-card ${errorRate < 5 ? 'success' : errorRate < 10 ? 'warning' : 'danger'}">
                <h3>Error Rate</h3>
                <div class="value">${errorRate}%</div>
                <div class="unit">failed requests</div>
            </div>

            <div class="metric-card">
                <h3>Throughput</h3>
                <div class="value">${metrics.http_reqs?.values.rate.toFixed(1) || 0}</div>
                <div class="unit">requests per second</div>
            </div>

            <div class="metric-card">
                <h3>Avg Response Time</h3>
                <div class="value">${metrics.http_req_duration?.values.avg.toFixed(0) || 0}</div>
                <div class="unit">milliseconds</div>
            </div>

            <div class="metric-card ${(metrics.http_req_duration?.values['p(95)'] || 0) < 2000 ? 'success' : 'warning'}">
                <h3>95th Percentile</h3>
                <div class="value">${metrics.http_req_duration?.values['p(95)'].toFixed(0) || 0}</div>
                <div class="unit">milliseconds</div>
            </div>

            <div class="metric-card success">
                <h3>Successful Requests</h3>
                <div class="value">${(metrics.successful_requests?.values.count || 0).toLocaleString()}</div>
                <div class="unit">200-399 responses</div>
            </div>

            <div class="metric-card ${(metrics.server_errors?.values.count || 0) === 0 ? 'success' : 'danger'}">
                <h3>Server Errors</h3>
                <div class="value">${metrics.server_errors?.values.count || 0}</div>
                <div class="unit">500+ responses</div>
            </div>
        </div>

        <h2>📊 Detailed Response Time Analysis</h2>
        <table>
            <thead>
                <tr>
                    <th>Percentile</th>
                    <th>Response Time (ms)</th>
                    <th>Status</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>Minimum</td>
                    <td>${metrics.http_req_duration?.values.min.toFixed(2) || 0}</td>
                    <td><span class="status-badge badge-pass">✓</span></td>
                </tr>
                <tr>
                    <td>Average</td>
                    <td>${metrics.http_req_duration?.values.avg.toFixed(2) || 0}</td>
                    <td><span class="status-badge ${(metrics.http_req_duration?.values.avg || 0) < 1000 ? 'badge-pass' : 'badge-warn'}">
                        ${(metrics.http_req_duration?.values.avg || 0) < 1000 ? '✓' : '⚠'}
                    </span></td>
                </tr>
                <tr>
                    <td>Median (50th)</td>
                    <td>${metrics.http_req_duration?.values.med.toFixed(2) || 0}</td>
                    <td><span class="status-badge badge-pass">✓</span></td>
                </tr>
                <tr>
                    <td>90th Percentile</td>
                    <td>${metrics.http_req_duration?.values['p(90)'].toFixed(2) || 0}</td>
                    <td><span class="status-badge ${(metrics.http_req_duration?.values['p(90)'] || 0) < 1500 ? 'badge-pass' : 'badge-warn'}">
                        ${(metrics.http_req_duration?.values['p(90)'] || 0) < 1500 ? '✓' : '⚠'}
                    </span></td>
                </tr>
                <tr>
                    <td><strong>95th Percentile (Target: < 2s)</strong></td>
                    <td><strong>${metrics.http_req_duration?.values['p(95)'].toFixed(2) || 0}</strong></td>
                    <td><span class="status-badge ${(metrics.http_req_duration?.values['p(95)'] || 0) < 2000 ? 'badge-pass' : 'badge-fail'}">
                        ${(metrics.http_req_duration?.values['p(95)'] || 0) < 2000 ? '✅ PASS' : '❌ FAIL'}
                    </span></td>
                </tr>
                <tr>
                    <td>99th Percentile</td>
                    <td>${metrics.http_req_duration?.values['p(99)'].toFixed(2) || 0}</td>
                    <td><span class="status-badge ${(metrics.http_req_duration?.values['p(99)'] || 0) < 3000 ? 'badge-pass' : 'badge-warn'}">
                        ${(metrics.http_req_duration?.values['p(99)'] || 0) < 3000 ? '✓' : '⚠'}
                    </span></td>
                </tr>
                <tr>
                    <td>Maximum</td>
                    <td>${metrics.http_req_duration?.values.max.toFixed(2) || 0}</td>
                    <td><span class="status-badge badge-warn">⚠</span></td>
                </tr>
            </tbody>
        </table>

        <h2>🔍 Error Analysis</h2>
        <table>
            <thead>
                <tr>
                    <th>Error Type</th>
                    <th>Count</th>
                    <th>Percentage</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>Successful Requests</td>
                    <td>${metrics.successful_requests?.values.count || 0}</td>
                    <td>${(((metrics.successful_requests?.values.count || 0) / (metrics.http_reqs?.values.count || 1)) * 100).toFixed(2)}%</td>
                </tr>
                <tr>
                    <td>Failed Requests</td>
                    <td>${metrics.failed_requests?.values.count || 0}</td>
                    <td>${(((metrics.failed_requests?.values.count || 0) / (metrics.http_reqs?.values.count || 1)) * 100).toFixed(2)}%</td>
                </tr>
                <tr>
                    <td>Timeout Errors</td>
                    <td>${metrics.timeout_errors?.values.count || 0}</td>
                    <td>${(((metrics.timeout_errors?.values.count || 0) / (metrics.http_reqs?.values.count || 1)) * 100).toFixed(2)}%</td>
                </tr>
                <tr>
                    <td>Server Errors (5xx)</td>
                    <td>${metrics.server_errors?.values.count || 0}</td>
                    <td>${(((metrics.server_errors?.values.count || 0) / (metrics.http_reqs?.values.count || 1)) * 100).toFixed(2)}%</td>
                </tr>
            </tbody>
        </table>

        <div class="footer">
            <p><strong>InsightLearn CI/CD Pipeline</strong> | Generated by k6 Stress Testing Framework</p>
            <p>For questions or issues, contact the DevOps team</p>
        </div>
    </div>
</body>
</html>
    `;
}
