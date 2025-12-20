#!/bin/bash

# 🎯 Katana Sales Order Approval Test Script (Mac)
# Bu script SO-55 veya SO-53 gibi PENDING bir siparişi onaylar ve Content-Type header'ını test eder

echo "🔍 Katana Sales Order Approval Test"
echo "===================================="
echo ""

# Backend URL
BACKEND_URL="http://localhost:8080"

# 1️⃣ Login ve Token Al
echo "1️⃣ Login yapılıyor..."
LOGIN_RESPONSE=$(curl -s -X POST "${BACKEND_URL}/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Katana2025!"
  }')

TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
    echo "❌ Login başarısız! Token alınamadı."
    echo "Response: $LOGIN_RESPONSE"
    exit 1
fi

echo "✅ Login başarılı! Token alındı."
echo ""

# 2️⃣ PENDING Siparişleri Listele
echo "2️⃣ PENDING siparişler listeleniyor..."
ORDERS_RESPONSE=$(curl -s -X GET "${BACKEND_URL}/api/sales-orders?status=PENDING" \
  -H "Authorization: Bearer $TOKEN")

echo "📋 PENDING Siparişler:"
echo "$ORDERS_RESPONSE" | jq -r '.[] | "  - \(.orderNo) (ID: \(.id))"' 2>/dev/null || echo "$ORDERS_RESPONSE"
echo ""

# 3️⃣ İlk PENDING Siparişi Al
ORDER_ID=$(echo $ORDERS_RESPONSE | jq -r '.[0].id' 2>/dev/null)

if [ -z "$ORDER_ID" ] || [ "$ORDER_ID" = "null" ]; then
    echo "⚠️  PENDING sipariş bulunamadı. Test için yeni sipariş oluşturun."
    exit 0
fi

ORDER_NO=$(echo $ORDERS_RESPONSE | jq -r '.[0].orderNo' 2>/dev/null)

echo "🎯 Test edilecek sipariş: $ORDER_NO (ID: $ORDER_ID)"
echo ""

# 4️⃣ Siparişi Onayla
echo "3️⃣ Sipariş onaylanıyor..."
APPROVE_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X POST \
  "${BACKEND_URL}/api/sales-orders/${ORDER_ID}/approve" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

HTTP_STATUS=$(echo "$APPROVE_RESPONSE" | grep "HTTP_STATUS" | cut -d':' -f2)
RESPONSE_BODY=$(echo "$APPROVE_RESPONSE" | sed '/HTTP_STATUS/d')

echo "📡 HTTP Status: $HTTP_STATUS"
echo "📦 Response:"
echo "$RESPONSE_BODY" | jq '.' 2>/dev/null || echo "$RESPONSE_BODY"
echo ""

# 5️⃣ Sonuç Kontrolü
if [ "$HTTP_STATUS" = "200" ]; then
    echo "✅ Sipariş başarıyla onaylandı!"
    echo ""
    echo "🔍 Loglarda şunları kontrol edin:"
    echo "   - '🔍 Content-Type being sent: application/json' (charset YOK)"
    echo "   - '✅ Sipariş durumu: APPROVED'"
    echo "   - '✅ Katana Order ID: XXXXX'"
    echo ""
    echo "📝 Logları görmek için:"
    echo "   docker logs katana-backend 2>&1 | grep -A 5 'Content-Type being sent'"
else
    echo "❌ Sipariş onaylanamadı!"
    echo ""
    echo "🔍 Loglarda şunları kontrol edin:"
    echo "   - '🔍 Content-Type being sent: application/json; charset=utf-8' (charset VAR)"
    echo "   - '❌ Katana API hatası: 415 (Unsupported Media Type)'"
    echo ""
    echo "📝 Logları görmek için:"
    echo "   docker logs katana-backend 2>&1 | grep -A 10 'Content-Type being sent'"
fi

echo ""
echo "🎬 Test tamamlandı!"
