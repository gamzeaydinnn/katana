#!/bin/bash

# Login
TOKEN=$(curl -s -X POST "http://localhost:8080/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Katana2025!"}' | grep -o '"token":"[^"]*' | cut -d'"' -f4)

echo "🔄 Katana'dan siparişler senkronize ediliyor..."
echo ""

# Sync endpoint'ini dene
RESULT=$(curl -s -w "\nSTATUS:%{http_code}" -X POST "http://localhost:8080/api/sync/sales-orders" \
  -H "Authorization: Bearer $TOKEN")

STATUS=$(echo "$RESULT" | grep "STATUS" | cut -d':' -f2)
BODY=$(echo "$RESULT" | sed '/STATUS/d')

echo "📡 HTTP Status: $STATUS"
echo "📦 Response:"
echo "$BODY" | jq '.' 2>/dev/null || echo "$BODY"

if [ "$STATUS" = "200" ]; then
    echo ""
    echo "✅ Senkronizasyon başarılı!"
    echo ""
    echo "🔍 Şimdi siparişleri kontrol et:"
    ./check-orders.sh
else
    echo ""
    echo "⚠️  Senkronizasyon başarısız veya endpoint farklı."
    echo ""
    echo "🔍 Alternatif: Swagger'dan manuel test yap"
    echo "   URL: http://localhost:8080/swagger"
fi
