#!/bin/bash
# Katana Sync Test Script
# Kullanım: KATANA_ADMIN_PASS=<şifre> ./scripts/test-sync.sh

set -e

API_URL="${KATANA_API_URL:-http://localhost:5055}"
ADMIN_USER="${KATANA_ADMIN_USER:-admin}"
ADMIN_PASS="${KATANA_ADMIN_PASS:?KATANA_ADMIN_PASS env değişkeni gerekli}"

# Login ve token al
echo "🔐 Login..."
TOKEN=$(curl -s -X POST "${API_URL}/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"${ADMIN_USER}\",\"password\":\"${ADMIN_PASS}\"}" | jq -r '.token')

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
  echo "❌ Login başarısız"
  exit 1
fi
echo "✅ Token alındı"

# Sync test (limit=5, dryRun=true)
echo "🔄 Sync test (limit=5, dryRun=true)..."
curl -s -X POST "${API_URL}/api/Sync/to-luca/stock-cards" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"limit": 5, "dryRun": true, "forceSendDuplicates": false}' | jq '.'
