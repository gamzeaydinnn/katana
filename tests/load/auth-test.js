import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  vus: 50,
  duration: '2m',
  thresholds: {
    http_req_duration: ['p(95)<400'],
    http_req_failed: ['rate<0.01'],
  },
};

const BASE_URL = __ENV.K6_BASE_URL || 'http://localhost:5055';
const ADMIN_USERNAME = __ENV.K6_ADMIN_USERNAME || 'admin';
const ADMIN_PASSWORD = __ENV.K6_ADMIN_PASSWORD || 'Katana2025!';

export function setup() {
  const res = http.post(
    `${BASE_URL}/api/Auth/login`,
    JSON.stringify({ username: ADMIN_USERNAME, password: ADMIN_PASSWORD }),
    { headers: { 'Content-Type': 'application/json' } },
  );

  check(res, { 'login 200': (r) => r.status === 200 && !!r.json('token') });
  return { token: res.json('token') };
}

export default function (data) {
  const token = data?.token || '';
  const headers = token ? { Authorization: `Bearer ${token}` } : {};

  
  const res = http.get(`${BASE_URL}/api/adminpanel/pending-adjustments`, { headers });
  check(res, { 'pending 200': (r) => r.status === 200 });

  sleep(1);
}

