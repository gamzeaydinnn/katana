import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '1m', target: 50 },
    { duration: '3m', target: 100 },
    { duration: '1m', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
    http_req_failed: ['rate<0.01'],
  },
};

const BASE_URL = __ENV.K6_BASE_URL || 'http://localhost:5055';
const TOKEN = __ENV.K6_TOKEN || '';

export default function () {
  const headers = TOKEN ? { Authorization: `Bearer ${TOKEN}` } : {};
  const res = http.get(`${BASE_URL}/api/Stock`, { headers });

  check(res, {
    'status is 200': (r) => r.status === 200,
    't < 500ms': (r) => r.timings.duration < 500,
  });

  sleep(1);
}

