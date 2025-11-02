/**
 * k6 Smoke Test for InsightLearn
 *
 * Purpose: Verify that the system can handle minimal load
 * Duration: ~30 seconds
 * VUs: 1 virtual user
 *
 * Run with: k6 run smoke-test.js
 */

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate } from 'k6/metrics';
import {
    BASE_API_URL,
    BASE_WEB_URL,
    LOAD_STAGES,
    THRESHOLDS_BY_TYPE,
    commonOptions,
    getHeaders,
    getTags
} from './config.js';

// Custom metrics
const errorRate = new Rate('errors');

export const options = {
    ...commonOptions,
    stages: LOAD_STAGES.smoke,
    thresholds: THRESHOLDS_BY_TYPE.smoke,
    tags: {
        test_type: 'smoke',
    },
};

export default function () {
    // Test 1: API Health Check
    {
        const res = http.get(`${BASE_API_URL}/health`, {
            headers: getHeaders(),
            tags: getTags('smoke', '/health'),
        });

        const success = check(res, {
            'API health check status is 200': (r) => r.status === 200,
            'API health check response time < 200ms': (r) => r.timings.duration < 200,
            'API health check has body': (r) => r.body.length > 0,
        });

        errorRate.add(!success);
    }

    sleep(1);

    // Test 2: Web Health Check
    {
        const res = http.get(`${BASE_WEB_URL}/health`, {
            headers: getHeaders(),
            tags: getTags('smoke', '/health'),
        });

        const success = check(res, {
            'Web health check status is 200': (r) => r.status === 200,
            'Web health check response time < 200ms': (r) => r.timings.duration < 200,
        });

        errorRate.add(!success);
    }

    sleep(1);

    // Test 3: API Root Endpoint
    {
        const res = http.get(`${BASE_API_URL}/`, {
            headers: getHeaders(),
            tags: getTags('smoke', '/'),
        });

        const success = check(res, {
            'API root endpoint responds': (r) => r.status >= 200 && r.status < 500,
        });

        errorRate.add(!success);
    }

    sleep(1);

    // Test 4: Web Homepage
    {
        const res = http.get(`${BASE_WEB_URL}/`, {
            headers: getHeaders(),
            tags: getTags('smoke', '/'),
        });

        const success = check(res, {
            'Web homepage responds': (r) => r.status >= 200 && r.status < 500,
        });

        errorRate.add(!success);
    }

    sleep(2);
}

export function handleSummary(data) {
    return {
        'stdout': textSummary(data, { indent: ' ', enableColors: true }),
        'smoke-test-results.json': JSON.stringify(data),
    };
}
