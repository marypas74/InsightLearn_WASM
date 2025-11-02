/**
 * k6 Configuration for InsightLearn Stress Testing
 *
 * This file contains global configuration and utility functions
 * used across all k6 test scenarios
 */

// Base URL for API testing
export const BASE_API_URL = __ENV.API_URL || 'http://192.168.49.2:31081';
export const BASE_WEB_URL = __ENV.WEB_URL || 'http://192.168.49.2:31080';

// Test users credentials
export const TEST_USERS = {
    admin: {
        email: 'admin@insightlearn.cloud',
        password: 'Admin123!'
    },
    teacher: {
        email: 'teacher@insightlearn.cloud',
        password: 'Teacher123!'
    },
    student: {
        email: 'student@insightlearn.cloud',
        password: 'Student123!'
    }
};

// Thresholds for test success/failure
export const THRESHOLDS = {
    // 95% of requests must complete in less than 500ms
    http_req_duration: ['p(95)<500'],

    // 99% of requests must complete in less than 1000ms
    'http_req_duration{type:api}': ['p(99)<1000'],

    // Error rate must be less than 1%
    http_req_failed: ['rate<0.01'],

    // Check pass rate must be above 99%
    checks: ['rate>0.99']
};

// Thresholds per test type
export const THRESHOLDS_BY_TYPE = {
    smoke: {
        ...THRESHOLDS,
        // Smoke test: just verify it works, no rate requirement
    },
    load: {
        ...THRESHOLDS,
        http_reqs: ['rate>10'], // At least 10 req/s
    },
    stress: {
        ...THRESHOLDS,
        http_reqs: ['rate>50'], // At least 50 req/s
    },
    spike: {
        ...THRESHOLDS,
        http_reqs: ['rate>100'], // At least 100 req/s during spike
    },
    soak: {
        ...THRESHOLDS,
        http_reqs: ['rate>10'], // At least 10 req/s sustained
    }
};

// Load test stages configuration
export const LOAD_STAGES = {
    // Smoke test: minimal load
    smoke: [
        { duration: '30s', target: 1 },
    ],

    // Load test: normal expected load
    load: [
        { duration: '2m', target: 10 },   // Ramp up to 10 users
        { duration: '5m', target: 10 },   // Stay at 10 users
        { duration: '2m', target: 0 },    // Ramp down
    ],

    // Stress test: beyond normal load
    stress: [
        { duration: '2m', target: 50 },   // Ramp up to 50 users
        { duration: '5m', target: 50 },   // Stay at 50 users
        { duration: '2m', target: 100 },  // Ramp up to 100 users
        { duration: '5m', target: 100 },  // Stay at 100 users
        { duration: '2m', target: 0 },    // Ramp down
    ],

    // Spike test: sudden load spike
    spike: [
        { duration: '1m', target: 10 },   // Normal load
        { duration: '30s', target: 200 }, // Sudden spike
        { duration: '1m', target: 200 },  // Maintain spike
        { duration: '1m', target: 10 },   // Return to normal
        { duration: '1m', target: 0 },    // Ramp down
    ],

    // Soak test: extended load
    soak: [
        { duration: '5m', target: 20 },   // Ramp up
        { duration: '3h', target: 20 },   // Maintain for 3 hours
        { duration: '5m', target: 0 },    // Ramp down
    ],
};

// Test scenario weights (percentage of virtual users)
export const SCENARIO_WEIGHTS = {
    browsing: 40,      // 40% of users just browsing
    registration: 10,  // 10% registering
    authentication: 20, // 20% logging in/out
    courseAccess: 20,  // 20% accessing courses
    api: 10,          // 10% direct API calls
};

// Headers for API requests
export function getHeaders(token = null) {
    const headers = {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
        'User-Agent': 'k6-stress-test/1.0',
    };

    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    return headers;
}

// Sleep configuration (think time between requests)
export const THINK_TIME = {
    min: 1,  // minimum 1 second
    max: 5,  // maximum 5 seconds
};

// Request timeouts
export const TIMEOUT = '30s';

// Tags for metrics grouping
export function getTags(scenario, endpoint) {
    return {
        scenario: scenario,
        endpoint: endpoint,
    };
}

// Export common options
export const commonOptions = {
    thresholds: THRESHOLDS,
    summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
    summaryTimeUnit: 'ms',
};
