#!/bin/bash

BASE_URL="http://localhost:8080"

echo "=== Step 1: Login to get JWT token ==="
login_response=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Katana2025!"}')

token=$(echo "$login_response" | jq -r '.token // .accessToken // .access_token // empty')

if [ -z "$token" ] || [ "$token" = "null" ]; then
    echo "❌ Failed to get token"
    echo "Response: $login_response"
    exit 1
fi

echo "✅ Got token: ${token:0:20}..."
echo ""

echo "=== Step 2: Trigger sync for last 15 days ==="
sync_response=$(curl -s -X POST "$BASE_URL/api/sync/from-katana/sales-orders?days=15" \
  -H "Authorization: Bearer $token" \
  -H "Content-Type: application/json")

echo "Sync Response:"
echo "$sync_response" | jq '.' 2>/dev/null || echo "$sync_response"
echo ""

echo "=== Step 3: Wait for sync to complete (5 seconds) ==="
sleep 5
echo ""

echo "=== Step 4: Check database for new orders ==="
./check-database.sh
