#!/bin/bash
# Debug script to analyze a specific order (bash version)

ORDER_NO="${1:-SO-56}"
BASE_URL="${2:-http://localhost:5000}"
API_KEY="${3:-your-api-key-here}"

echo "🔍 Debugging Order: $ORDER_NO"
echo "================================"
echo ""

# Make the request
response=$(curl -s -X GET "$BASE_URL/api/sync/debug/katana-order/$ORDER_NO" \
  -H "X-API-Key: $API_KEY" \
  -H "Content-Type: application/json")

# Check if request was successful
if [ $? -ne 0 ]; then
    echo "❌ Error: Failed to connect to API"
    exit 1
fi

# Pretty print the JSON response
echo "$response" | jq '.' 2>/dev/null || echo "$response"

echo ""
echo "================================"
echo "✅ Debug completed!"
echo ""
echo "Usage:"
echo "  ./test-debug-order.sh SO-56"
echo "  ./test-debug-order.sh SO-41 http://localhost:5000 your-api-key"
echo ""
echo "💡 Tip: Install 'jq' for better JSON formatting"
echo "   macOS: brew install jq"
echo "   Ubuntu: sudo apt-get install jq"
