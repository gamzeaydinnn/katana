#!/bin/bash

# Login
TOKEN=$(curl -s -X POST "http://localhost:8080/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Katana2025!"}' | grep -o '"token":"[^"]*' | cut -d'"' -f4)

echo "🔍 Mevcut Customers:"
echo "===================="
CUSTOMERS=$(curl -s -X GET "http://localhost:8080/api/customers" \
  -H "Authorization: Bearer $TOKEN")
echo "$CUSTOMERS" | jq -r '.[] | "ID: \(.id) - \(.name)"' | head -5

CUSTOMER_ID=$(echo "$CUSTOMERS" | jq -r '.[0].id' 2>/dev/null)

echo ""
echo "🔍 Mevcut Products:"
echo "==================="
PRODUCTS=$(curl -s -X GET "http://localhost:8080/api/products" \
  -H "Authorization: Bearer $TOKEN")
echo "$PRODUCTS" | jq -r '.[] | "ID: \(.id) - \(.name) - SKU: \(.sku)"' | head -5

PRODUCT_ID=$(echo "$PRODUCTS" | jq -r '.[0].id' 2>/dev/null)
PRODUCT_PRICE=$(echo "$PRODUCTS" | jq -r '.[0].salesPrice // 100' 2>/dev/null)

if [ -z "$CUSTOMER_ID" ] || [ "$CUSTOMER_ID" = "null" ]; then
    echo ""
    echo "❌ Customer bulunamadı! Önce customer oluşturmalısınız."
    exit 1
fi

if [ -z "$PRODUCT_ID" ] || [ "$PRODUCT_ID" = "null" ]; then
    echo ""
    echo "❌ Product bulunamadı! Önce product oluşturmalısınız."
    exit 1
fi

echo ""
echo "🎯 Test Siparişi Oluşturuluyor..."
echo "Customer ID: $CUSTOMER_ID"
echo "Product ID: $PRODUCT_ID"
echo "Price: $PRODUCT_PRICE"
echo ""

ORDER_NO="TEST-$(date +%s)"

RESULT=$(curl -s -w "\nSTATUS:%{http_code}" -X POST "http://localhost:8080/api/salesorders" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"orderNo\": \"$ORDER_NO\",
    \"customerId\": $CUSTOMER_ID,
    \"orderDate\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",
    \"status\": \"PENDING\",
    \"totalAmount\": $PRODUCT_PRICE,
    \"items\": [
      {
        \"productId\": $PRODUCT_ID,
        \"quantity\": 1,
        \"unitPrice\": $PRODUCT_PRICE
      }
    ]
  }")

STATUS=$(echo "$RESULT" | grep "STATUS" | cut -d':' -f2)
BODY=$(echo "$RESULT" | sed '/STATUS/d')

echo "📡 HTTP Status: $STATUS"
echo "📦 Response:"
echo "$BODY" | jq '.' 2>/dev/null || echo "$BODY"

if [ "$STATUS" = "200" ] || [ "$STATUS" = "201" ]; then
    echo ""
    echo "✅ Test siparişi oluşturuldu: $ORDER_NO"
    echo ""
    echo "🎬 Şimdi test scriptini çalıştırabilirsin:"
    echo "   ./test-sales-order-approval.sh"
else
    echo ""
    echo "❌ Sipariş oluşturulamadı!"
fi
