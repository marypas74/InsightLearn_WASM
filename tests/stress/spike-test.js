/**
 * k6 Spike Test for InsightLearn
 *
 * Purpose: Test system recovery from sudden traffic spikes
 * Duration: ~4.5 minutes
 * VUs: 10-200 virtual users (sudden spike)
 *
 * Run with: k6 run spike-test.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Trend } from 'k6/metrics';
import {
    BASE_API_URL,
    BASE_WEB_URL,
    LOAD_STAGES,
    commonOptions,
    getHeaders,
    getTags
} from './config.js';

// Custom metrics
const errorRate = new Rate('errors');
const apiResponseTime = new Trend('api_response_time');

export const options = {
    ...commonOptions,
    stages: LOAD_STAGES.spike,
    tags: {
        test_type: 'spike',
    },
    thresholds: {
        // Lenient thresholds - expect some degradation during spike
        'http_req_duration': ['p(95)<3000'],
        'http_req_failed': ['rate<0.10'],  // Allow 10% failure during spike
        'checks': ['rate>0.85'],  // 85% check pass rate
    },
};

export default function () {
    // Simple health check
    const res = http.get(`${BASE_API_URL}/health`, {
        headers: getHeaders(),
        tags: getTags('spike', 'health'),
    });

    const success = check(res, {
        'Health check responds': (r) => r.status === 200,
        'Response time acceptable': (r) => r.timings.duration < 5000,
    });

    apiResponseTime.add(res.timings.duration);
    errorRate.add(!success);

    sleep(1);
}

export function handleSummary(data) {
    console.log('\n========================================');
    console.log('     SPIKE TEST SUMMARY');
    console.log('========================================\n');

    const metrics = data.metrics;

    console.log(`Total Requests:       ${metrics.http_reqs?.values.count || 0}`);
    console.log(`Error Rate:           ${((metrics.http_req_failed?.values.rate || 0) * 100).toFixed(2)}%`);
    console.log(`Requests/sec (peak):  ${metrics.http_reqs?.values.rate.toFixed(2) || 0}`);
    console.log(`\nResponse Times During Spike (ms):`);
    console.log(`  Average:            ${metrics.http_req_duration?.values.avg.toFixed(2) || 0}`);
    console.log(`  95th Percentile:    ${metrics.http_req_duration?.values['p(95)'].toFixed(2) || 0}`);
    console.log(`  Max:                ${metrics.http_req_duration?.values.max.toFixed(2) || 0}`);

    return {
        'stdout': textSummary(data, { indent: ' ', enableColors: true }),
        'spike-test-results.json': JSON.stringify(data),
    };
}
