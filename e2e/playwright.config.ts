import { defineConfig, devices } from '@playwright/test';

const apiBase = process.env.E2E_API_BASE || 'http://localhost:5055';
const webBase = process.env.E2E_WEB_BASE || 'http://localhost:3000';

export default defineConfig({
  testDir: './tests',
  timeout: 30_000,
  expect: { timeout: 5_000 },
  use: {
    baseURL: webBase,
    extraHTTPHeaders: {
      // helpful default to avoid caching in proxies
      'Accept': 'application/json, text/plain, */*'
    }
  },
  projects: [
    { name: 'API (chromium)', use: { ...devices['Desktop Chrome'], baseURL: apiBase } },
  ]
});

