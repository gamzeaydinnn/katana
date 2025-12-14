#!/bin/bash

# Login
TOKEN=$(curl -s -X POST "http://localhost:8080/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Katana2025!"}' | grep -o '"token":"[^"]*' | cut -d'"' -f4)

echo "🔍 APPROVED_WITH_ERRORS Siparişler:"
echo "===================================="
ORDERS=$(curl -s -X GET "http://localhost:8080/api/sales-orders?status=APPROVED_WITH_ERRORS" \
  -H "Authorization: Bearer $TOKEN")

echo "$ORDERS" | jq -r '.[] | "\(.orderNo) - ID: \(.id)"'

ORDER_ID=$(echo "$ORDERS" | jq -r '.[0].id' 2>/dev/null)
ORDER_NO=$(echo "$ORDERS" | jq -r '.[0].orderNo' 2>/dev/null)

if [ -z "$ORDER_ID" ] || [ "$ORDER_ID" = "null" ]; then
    echo ""
    echo "⚠️  APPROVED_WITH_ERRORS sipariş bulunamadı!"
    exit 0
fi

echo ""
echo "🔄 $ORDER_NO (ID: $ORDER_ID) siparişi PENDING'e çevriliyor..."
echo ""

# Siparişi PENDING'e çevir (database'de direkt güncelleme gerekebilir)
# API endpoint varsa kullan, yoksa SQL ile yapmalıyız

# Önce API'de update endpoint'i var mı kontrol edelim
RESULT=$(curl -s -w "\nSTATUS:%{http_code}" -X PUT \
  "http://localhost:8080/api/sales-orders/${ORDER_ID}" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"status\": \"PENDING\"}")

STATUS=$(echo "$RESULT" | grep "STATUS" | cut -d':' -f2)

if [ "$STATUS" = "200" ] || [ "$STATUS" = "204" ]; then
    echo "✅ Sipariş PENDING'e çevrildi!"
    echo ""
    echo "🎬 Şimdi test scriptini çalıştır:"
    echo "   ./test-sales-order-approval.sh"
else
    echo "⚠️  API ile güncellenemedi. Database'den manuel güncelleme gerekebilir."
    echo ""
    echo "SQL ile güncelle:"
    echo "docker exec -it katana-db-1 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'Admin00!S' -Q \"USE KatanaDB; UPDATE SalesOrders SET Status = 'PENDING', KatanaOrderId = NULL WHERE Id = $ORDER_ID;\""
fi
