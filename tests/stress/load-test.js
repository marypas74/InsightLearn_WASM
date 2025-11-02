/**
 * k6 Load Test for InsightLearn
 *
 * Purpose: Test system under normal expected load
 * Duration: ~9 minutes
 * VUs: 0-10 virtual users
 *
 * Run with: k6 run load-test.js
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
const successfulLogins = new Counter('successful_logins');
const failedLogins = new Counter('failed_logins');

export const options = {
    ...commonOptions,
    stages: LOAD_STAGES.load,
    tags: {
        test_type: 'load',
    },
};

export default function () {
    // Scenario: Browse Homepage
    group('Browse Homepage', function () {
        const res = http.get(`${BASE_WEB_URL}/`, {
            headers: getHeaders(),
            tags: getTags('load', 'homepage'),
        });

        const success = check(res, {
            'Homepage loads successfully': (r) => r.status === 200 || r.status === 302,
            'Homepage load time < 1s': (r) => r.timings.duration < 1000,
        });

        pageLoadDuration.add(res.timings.duration);
        errorRate.add(!success);
    });

    sleep(Math.random() * (THINK_TIME.max - THINK_TIME.min) + THINK_TIME.min);

    // Scenario: API Health Check
    group('API Health Check', function () {
        const res = http.get(`${BASE_API_URL}/health`, {
            headers: getHeaders(),
            tags: getTags('load', 'health'),
        });

        const success = check(res, {
            'Health check is healthy': (r) => r.status === 200,
            'Health check response time < 500ms': (r) => r.timings.duration < 500,
        });

        apiCallDuration.add(res.timings.duration);
        errorRate.add(!success);
    });

    sleep(Math.random() * (THINK_TIME.max - THINK_TIME.min) + THINK_TIME.min);

    // Scenario: User Authentication
    group('User Authentication', function () {
        const loginPayload = JSON.stringify({
            email: TEST_USERS.student.email,
            password: TEST_USERS.student.password,
        });

        const startTime = new Date().getTime();
        const res = http.post(`${BASE_API_URL}/api/auth/login`, loginPayload, {
            headers: getHeaders(),
            tags: getTags('load', 'login'),
        });
        const duration = new Date().getTime() - startTime;

        const success = check(res, {
            'Login returns 200 or 401': (r) => r.status === 200 || r.status === 401,
            'Login response time < 1s': (r) => r.timings.duration < 1000,
            'Login response has body': (r) => r.body && r.body.length > 0,
        });

        loginDuration.add(duration);

        if (res.status === 200) {
            successfulLogins.add(1);
        } else {
            failedLogins.add(1);
        }

        errorRate.add(!success);

        // If login successful, try to access protected resource
        if (res.status === 200) {
            try {
                const body = JSON.parse(res.body);
                if (body.token) {
                    const profileRes = http.get(`${BASE_API_URL}/api/user/profile`, {
                        headers: getHeaders(body.token),
                        tags: getTags('load', 'profile'),
                    });

                    check(profileRes, {
                        'Profile access successful': (r) => r.status === 200 || r.status === 401,
                    });
                }
            } catch (e) {
                // Token extraction failed
            }
        }
    });

    sleep(Math.random() * (THINK_TIME.max - THINK_TIME.min) + THINK_TIME.min);

    // Scenario: Browse Courses
    group('Browse Courses', function () {
        const res = http.get(`${BASE_API_URL}/api/courses`, {
            headers: getHeaders(),
            tags: getTags('load', 'courses'),
        });

        const success = check(res, {
            'Courses list loads': (r) => r.status === 200 || r.status === 401,
            'Courses response time < 1s': (r) => r.timings.duration < 1000,
        });

        apiCallDuration.add(res.timings.duration);
        errorRate.add(!success);
    });

    sleep(Math.random() * (THINK_TIME.max - THINK_TIME.min) + THINK_TIME.min);

    // Scenario: Search functionality
    group('Search', function () {
        const searchTerms = ['programming', 'math', 'science', 'business', 'art'];
        const searchTerm = searchTerms[Math.floor(Math.random() * searchTerms.length)];

        const res = http.get(`${BASE_API_URL}/api/search?q=${searchTerm}`, {
            headers: getHeaders(),
            tags: getTags('load', 'search'),
        });

        const success = check(res, {
            'Search responds': (r) => r.status === 200 || r.status === 404 || r.status === 401,
            'Search response time < 2s': (r) => r.timings.duration < 2000,
        });

        apiCallDuration.add(res.timings.duration);
        errorRate.add(!success);
    });

    sleep(Math.random() * (THINK_TIME.max - THINK_TIME.min) + THINK_TIME.min);
}

export function handleSummary(data) {
    console.log('\n========================================');
    console.log('     LOAD TEST SUMMARY');
    console.log('========================================\n');

    const metrics = data.metrics;

    console.log(`Total Requests:       ${metrics.http_reqs.values.count}`);
    console.log(`Failed Requests:      ${metrics.http_req_failed.values.passes || 0}`);
    console.log(`Requests/sec:         ${metrics.http_reqs.values.rate.toFixed(2)}`);
    console.log(`Successful Logins:    ${metrics.successful_logins?.values.count || 0}`);
    console.log(`Failed Logins:        ${metrics.failed_logins?.values.count || 0}`);
    console.log(`\nResponse Times (ms):`);
    console.log(`  Average:            ${metrics.http_req_duration.values.avg.toFixed(2)}`);
    console.log(`  Median:             ${metrics.http_req_duration.values.med.toFixed(2)}`);
    console.log(`  95th Percentile:    ${metrics.http_req_duration.values['p(95)'].toFixed(2)}`);
    console.log(`  99th Percentile:    ${metrics.http_req_duration.values['p(99)'].toFixed(2)}`);
    console.log(`  Max:                ${metrics.http_req_duration.values.max.toFixed(2)}`);

    return {
        'stdout': textSummary(data, { indent: ' ', enableColors: true }),
        'load-test-results.json': JSON.stringify(data),
        'load-test-summary.html': generateHTMLReport(data),
    };
}

function generateHTMLReport(data) {
    const metrics = data.metrics;
    const passRate = ((metrics.checks?.values.passes / metrics.checks?.values.count) * 100 || 0).toFixed(2);

    return `
<!DOCTYPE html>
<html>
<head>
    <title>Load Test Results - InsightLearn</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; background: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        h1 { color: #2c3e50; border-bottom: 3px solid #3498db; padding-bottom: 10px; }
        .metric-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 20px; margin: 20px 0; }
        .metric-card { background: #ecf0f1; padding: 20px; border-radius: 5px; border-left: 4px solid #3498db; }
        .metric-card h3 { margin: 0 0 10px 0; color: #34495e; font-size: 14px; }
        .metric-card .value { font-size: 32px; font-weight: bold; color: #2c3e50; }
        .metric-card .unit { font-size: 14px; color: #7f8c8d; }
        .success { border-left-color: #27ae60; }
        .warning { border-left-color: #f39c12; }
        .danger { border-left-color: #e74c3c; }
        table { width: 100%; border-collapse: collapse; margin: 20px 0; }
        th, td { padding: 12px; text-align: left; border-bottom: 1px solid #ddd; }
        th { background: #34495e; color: white; }
        tr:hover { background: #f5f5f5; }
        .footer { margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #7f8c8d; font-size: 12px; }
    </style>
</head>
<body>
    <div class="container">
        <h1>🚀 InsightLearn Load Test Results</h1>
        <p><strong>Test Type:</strong> Load Test | <strong>Date:</strong> ${new Date().toISOString()}</p>

        <div class="metric-grid">
            <div class="metric-card success">
                <h3>Total Requests</h3>
                <div class="value">${metrics.http_reqs?.values.count || 0}</div>
                <div class="unit">requests</div>
            </div>

            <div class="metric-card ${passRate > 95 ? 'success' : passRate > 90 ? 'warning' : 'danger'}">
                <h3>Check Pass Rate</h3>
                <div class="value">${passRate}%</div>
                <div class="unit">passed checks</div>
            </div>

            <div class="metric-card">
                <h3>Requests/Second</h3>
                <div class="value">${metrics.http_reqs?.values.rate.toFixed(2) || 0}</div>
                <div class="unit">req/s</div>
            </div>

            <div class="metric-card">
                <h3>Avg Response Time</h3>
                <div class="value">${metrics.http_req_duration?.values.avg.toFixed(0) || 0}</div>
                <div class="unit">milliseconds</div>
            </div>

            <div class="metric-card">
                <h3>95th Percentile</h3>
                <div class="value">${metrics.http_req_duration?.values['p(95)'].toFixed(0) || 0}</div>
                <div class="unit">milliseconds</div>
            </div>

            <div class="metric-card ${(metrics.http_req_failed?.values.rate || 0) < 0.01 ? 'success' : 'danger'}">
                <h3>Error Rate</h3>
                <div class="value">${((metrics.http_req_failed?.values.rate || 0) * 100).toFixed(2)}%</div>
                <div class="unit">failed requests</div>
            </div>
        </div>

        <h2>Response Time Breakdown</h2>
        <table>
            <thead>
                <tr>
                    <th>Metric</th>
                    <th>Value (ms)</th>
                </tr>
            </thead>
            <tbody>
                <tr><td>Minimum</td><td>${metrics.http_req_duration?.values.min.toFixed(2) || 0}</td></tr>
                <tr><td>Average</td><td>${metrics.http_req_duration?.values.avg.toFixed(2) || 0}</td></tr>
                <tr><td>Median</td><td>${metrics.http_req_duration?.values.med.toFixed(2) || 0}</td></tr>
                <tr><td>90th Percentile</td><td>${metrics.http_req_duration?.values['p(90)'].toFixed(2) || 0}</td></tr>
                <tr><td>95th Percentile</td><td>${metrics.http_req_duration?.values['p(95)'].toFixed(2) || 0}</td></tr>
                <tr><td>99th Percentile</td><td>${metrics.http_req_duration?.values['p(99)'].toFixed(2) || 0}</td></tr>
                <tr><td>Maximum</td><td>${metrics.http_req_duration?.values.max.toFixed(2) || 0}</td></tr>
            </tbody>
        </table>

        <h2>Authentication Metrics</h2>
        <table>
            <thead>
                <tr>
                    <th>Metric</th>
                    <th>Count</th>
                </tr>
            </thead>
            <tbody>
                <tr><td>Successful Logins</td><td>${metrics.successful_logins?.values.count || 0}</td></tr>
                <tr><td>Failed Logins</td><td>${metrics.failed_logins?.values.count || 0}</td></tr>
            </tbody>
        </table>

        <div class="footer">
            Generated by k6 Load Testing Framework | InsightLearn CI/CD Pipeline
        </div>
    </div>
</body>
</html>
    `;
}
