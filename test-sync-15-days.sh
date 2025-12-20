#!/bin/bash

echo "=== Testing sync with days=15 ==="
echo ""

# Get admin token first (if needed)
# For now, let's try without auth or check if there's a test endpoint

echo "Triggering sync for last 15 days..."
response=$(curl -X POST "http://localhost:8080/api/sync/from-katana/sales-orders?days=15" \
  -H "accept: application/json" \
  -s -w "\nHTTP_CODE:%{http_code}")

http_code=$(echo "$response" | grep "HTTP_CODE" | cut -d: -f2)
body=$(echo "$response" | grep -v "HTTP_CODE")

echo "HTTP Status: $http_code"
echo "Response: $body"

if [ "$http_code" = "401" ]; then
    echo ""
    echo "⚠️  Authentication required. Need admin token."
    echo "Let's check the logs instead..."
    echo ""
    
    # Check recent logs
    echo "=== Recent API Logs ==="
    docker logs katana-db-1 2>&1 | tail -30
fi
