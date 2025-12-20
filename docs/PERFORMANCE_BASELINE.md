# Performance Baseline

This document describes how to run quick load tests to establish a baseline for key endpoints.

## Tools

- k6 (recommended): https://k6.io/docs/getting-started/installation/
- ab (ApacheBench): often available via `brew install httpd` or package manager

## Environment

- BASE URL: set `K6_BASE_URL` (default: `http://localhost:5055`)
- Authorization:
  - For scripts requiring auth, set `K6_TOKEN` (Bearer token) or provide `K6_ADMIN_USERNAME`/`K6_ADMIN_PASSWORD` for login script.

## k6 Scenarios

1) Stock list under load

```
k6 run -e K6_BASE_URL=http://localhost:5055 -e K6_TOKEN=YOUR_JWT tests/load/stock-test.js
```

2) Auth + pending list (login once in setup)

```
k6 run -e K6_BASE_URL=http://localhost:5055 -e K6_ADMIN_USERNAME=admin -e K6_ADMIN_PASSWORD=Katana2025! tests/load/auth-test.js
```

3) Pending adjustments read-heavy

```
k6 run -e K6_BASE_URL=http://localhost:5055 -e K6_TOKEN=YOUR_JWT tests/load/pending-test.js
```

## ApacheBench (quick sanity)

```
ab -n 1000 -c 10 -H "Authorization: Bearer YOUR_JWT" http://localhost:5055/api/Stock
```

## Record Baseline Metrics

- Average response time (ms)
- 95th percentile latency (ms)
- Error rate (%)
- Throughput (req/sec)

Capture for each scenario and keep a dated record in this document or a separate CSV for trends.

