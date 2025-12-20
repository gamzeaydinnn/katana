# Katana E2E Tests (Playwright)

This package contains Playwright-based API E2E tests that cover the admin pending stock adjustment workflow:

- Login → Create Pending → List → Approve
- Login → Create Pending → Reject

## Prerequisites

- Backend running at `http://localhost:5055` (configurable via `E2E_API_BASE`)
- Valid admin credentials (defaults used in API): `admin` / `Katana2025!`

## Install & Run

```
cd e2e
npm install
npm test
```

Optional environment variables:

- `E2E_API_BASE` (default: `http://localhost:5055`)
- `E2E_WEB_BASE` (default: `http://localhost:3000`)

UI smoke tests can be added later by pointing `E2E_WEB_BASE` to a running frontend dev server.

