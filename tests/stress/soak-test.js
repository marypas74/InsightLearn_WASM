/**
 * k6 Soak Test for InsightLearn
 *
 * Purpose: Test system stability over extended period (detect memory leaks, resource exhaustion)
 * Duration: ~3 hours 10 minutes
 * VUs: 0-20 virtual users
 *
 * Run with: k6 run soak-test.js
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
const memoryLeakIndicator = new Trend('response_time_degradation');
const successfulRequests = new Counter('successful_requests');

export const options = {
    ...commonOptions,
    stages: LOAD_STAGES.soak,
    tags: {
        test_type: 'soak',
    },
    thresholds: {
        'http_req_duration': ['p(95)<1000'],
        'http_req_failed': ['rate<0.02'],  // Less than 2% error rate
        'checks': ['rate>0.95'],
    },
};

let startTime = new Date();
let initialResponseTime = 0;
let requestCount = 0;

export default function () {
    requestCount++;

    group('Soak Test - Extended Operations', function () {
        // Health check
        let res = http.get(`${BASE_API_URL}/health`, {
            headers: getHeaders(),
            tags: getTags('soak', 'health'),
        });

        const currentResponseTime = res.timings.duration;

        // Track first response time as baseline
        if (requestCount === 1) {
            initialResponseTime = currentResponseTime;
        }

        // Calculate degradation (indicator of memory leak)
        const degradation = currentResponseTime - initialResponseTime;
        memoryLeakIndicator.add(degradation);

        let success = check(res, {
            'Health check successful': (r) => r.status === 200,
            'No significant response time degradation': () => degradation < 500,
        });

        if (success) successfulRequests.add(1);
        errorRate.add(!success);

        sleep(2);

        // API operations
        res = http.get(`${BASE_API_URL}/api/courses`, {
            headers: getHeaders(),
            tags: getTags('soak', 'courses'),
        });

        success = check(res, {
            'Courses list loads': (r) => r.status === 200 || r.status === 401,
        });

        if (success) successfulRequests.add(1);
        errorRate.add(!success);

        sleep(3);

        // Authentication (every 10th iteration)
        if (requestCount % 10 === 0) {
            const loginPayload = JSON.stringify({
                email: TEST_USERS.student.email,
                password: TEST_USERS.student.password,
            });

            res = http.post(`${BASE_API_URL}/api/auth/login`, loginPayload, {
                headers: getHeaders(),
                tags: getTags('soak', 'login'),
            });

            success = check(res, {
                'Login responds': (r) => r.status === 200 || r.status === 401,
            });

            if (success) successfulRequests.add(1);
            errorRate.add(!success);
        }
    });

    sleep(Math.random() * THINK_TIME.max + 5);  // Longer think time for soak test
}

export function handleSummary(data) {
    const endTime = new Date();
    const durationHours = (endTime - startTime) / 1000 / 60 / 60;

    console.log('\n========================================');
    console.log('     SOAK TEST SUMMARY');
    console.log('========================================\n');

    const metrics = data.metrics;

    console.log(`Test Duration:        ${durationHours.toFixed(2)} hours`);
    console.log(`Total Requests:       ${metrics.http_reqs?.values.count || 0}`);
    console.log(`Successful Requests:  ${metrics.successful_requests?.values.count || 0}`);
    console.log(`Error Rate:           ${((metrics.http_req_failed?.values.rate || 0) * 100).toFixed(2)}%`);
    console.log(`\nResponse Time Stability:`);
    console.log(`  Average:            ${metrics.http_req_duration?.values.avg.toFixed(2) || 0}ms`);
    console.log(`  95th Percentile:    ${metrics.http_req_duration?.values['p(95)'].toFixed(2) || 0}ms`);
    console.log(`  Degradation Avg:    ${metrics.response_time_degradation?.values.avg.toFixed(2) || 0}ms`);
    console.log(`  Degradation Max:    ${metrics.response_time_degradation?.values.max.toFixed(2) || 0}ms`);

    const avgDegradation = metrics.response_time_degradation?.values.avg || 0;

    console.log(`\n========================================`);
    if (avgDegradation < 100) {
        console.log('     ✅ NO MEMORY LEAK DETECTED');
    } else if (avgDegradation < 300) {
        console.log('     ⚠️  MINOR DEGRADATION DETECTED');
    } else {
        console.log('     ❌ SIGNIFICANT DEGRADATION - INVESTIGATE');
    }
    console.log('========================================\n');

    return {
        'stdout': textSummary(data, { indent: ' ', enableColors: true }),
        'soak-test-results.json': JSON.stringify(data),
    };
}
