#!/bin/bash

# Test script for clearing APPROVED_WITH_ERRORS status

BASE_URL="http://localhost:8080"
API_BASE="$BASE_URL/api"

echo "========================================"
echo "CLEAR APPROVED_WITH_ERRORS TEST"
echo "========================================"
echo ""

# 1. Login
echo "[1/2] Login yapılıyor..."
LOGIN_RESPONSE=$(curl -s -X POST "$API_BASE/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Katana2025!"}')

TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "❌ Login başarısız!"
  exit 1
fi

echo "✅ Login başarılı!"
echo ""

# 2. Clear errors
echo "[2/2] APPROVED_WITH_ERRORS durumundaki siparişler temizleniyor..."
CLEAR_RESPONSE=$(curl -s -X POST "$API_BASE/sales-orders/clear-errors" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

echo "Response: $CLEAR_RESPONSE"
echo ""

# Parse response
SUCCESS=$(echo $CLEAR_RESPONSE | grep -o '"success":[^,]*' | cut -d':' -f2)
MESSAGE=$(echo $CLEAR_RESPONSE | grep -o '"message":"[^"]*' | cut -d'"' -f4)
CLEARED_COUNT=$(echo $CLEAR_RESPONSE | grep -o '"clearedCount":[0-9]*' | cut -d':' -f2)

if [ "$SUCCESS" = "true" ]; then
  echo "✅ Başarılı!"
  echo "   Mesaj: $MESSAGE"
  echo "   Temizlenen sipariş sayısı: $CLEARED_COUNT"
else
  echo "❌ Başarısız!"
  echo "   Mesaj: $MESSAGE"
fi

echo ""
echo "========================================"
echo "TEST TAMAMLANDI"
echo "========================================"
