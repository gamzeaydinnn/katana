#!/bin/bash

echo "=== Starting log monitoring in background ==="
docker logs -f katana-api-1 2>&1 | grep -E "(DEBUG|Katana sipariş|Processing order|existing orders)" &
LOG_PID=$!

sleep 2

echo ""
echo "=== Triggering sync (last 7 days) ==="
curl -X POST "http://localhost:5055/api/sync/from-katana/sales-orders?days=7" \
  -H "accept: application/json" \
  -s | jq '.'

echo ""
echo "=== Waiting for logs (10 seconds) ==="
sleep 10

echo ""
echo "=== Stopping log monitoring ==="
kill $LOG_PID 2>/dev/null

echo ""
echo "Done!"
