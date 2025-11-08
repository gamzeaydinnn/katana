import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 20 },
    { duration: '2m', target: 80 },
    { duration: '30s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<600'],
    http_req_failed: ['rate<0.02'],
  },
};

const BASE_URL = __ENV.K6_BASE_URL || 'http://localhost:5055';
const TOKEN = __ENV.K6_TOKEN || '';

export default function () {
  const headers = TOKEN ? { Authorization: `Bearer ${TOKEN}` } : {};
  // Read workloads for admin pending adjustments
  const res = http.get(`${BASE_URL}/api/adminpanel/pending-adjustments`, { headers });
  check(res, { 'pending list ok': (r) => r.status === 200 });
  sleep(1);
}

