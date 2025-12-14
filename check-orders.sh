#!/bin/bash

# Login
TOKEN=$(curl -s -X POST "http://localhost:8080/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Katana2025!"}' | grep -o '"token":"[^"]*' | cut -d'"' -f4)

echo "🔍 Tüm Siparişler:"
echo "=================="
curl -s -X GET "http://localhost:8080/api/sales-orders" \
  -H "Authorization: Bearer $TOKEN" | jq -r '.[] | "\(.orderNo) - Status: \(.status) - ID: \(.id)"'

echo ""
echo "📋 PENDING Siparişler:"
echo "======================"
curl -s -X GET "http://localhost:8080/api/sales-orders?status=PENDING" \
  -H "Authorization: Bearer $TOKEN" | jq -r '.[] | "\(.orderNo) - ID: \(.id)"'
