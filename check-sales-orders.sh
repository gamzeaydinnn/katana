#!/bin/bash

baseUrl="http://localhost:5055"

echo "=== Checking Sales Orders in Database ==="
echo ""

# Get all sales orders
response=$(curl -s "$baseUrl/api/sales-orders?page=1&pageSize=50")
count=$(echo "$response" | jq '. | length')

echo "Total orders in DB: $count"
echo ""

if [ "$count" -gt 0 ]; then
    echo "Orders:"
    echo "$response" | jq -r '.[] | "  - \(.orderNo): Customer=\(.customerName), Status=\(.status), Total=\(.total) \(.currency), Created=\(.orderCreatedDate)"'
else
    echo "No orders found in database!"
fi

echo ""
echo "=== Checking Specific Katana Orders ==="
echo ""

# Check specific orders
for orderNo in "SO-41" "SO-47" "SO-56"; do
    echo "Checking $orderNo..."
    debugResponse=$(curl -s "$baseUrl/api/sync/debug/katana-order/$orderNo")
    
    inKatana=$(echo "$debugResponse" | jq -r '.found.inKatana')
    inDB=$(echo "$debugResponse" | jq -r '.found.inDatabase')
    
    echo "  Found in Katana: $inKatana"
    echo "  Found in DB: $inDB"
    
    issues=$(echo "$debugResponse" | jq -r '.analysis.issues[]' 2>/dev/null)
    if [ ! -z "$issues" ]; then
        echo "  Issues:"
        echo "$issues" | while read -r issue; do
            echo "    - $issue"
        done
    fi
    
    echo ""
done
