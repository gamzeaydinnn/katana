#!/bin/bash

# Test script for charset fix in Katana API
# Bu script Content-Type header'ının charset olmadan gönderildiğini test eder

BASE_URL="http://localhost:8080"
API_BASE="$BASE_URL/api"

echo "========================================"
echo "KATANA CHARSET FIX TEST"
echo "========================================"
echo ""

# 1. Login
echo "[1/4] Login yapılıyor..."
LOGIN_RESPONSE=$(curl -s -X POST "$API_BASE/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Katana2025!"}')

TOKEN=$(echo $LOGIN_RESPONSE | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "❌ Login başarısız!"
  echo "Response: $LOGIN_RESPONSE"
  exit 1
fi

echo "✅ Login başarılı!"
echo "   Token: ${TOKEN:0:20}..."
echo ""

# 2. Supplier kontrol
echo "[2/4] Tedarikçi kontrol ediliyor..."
SUPPLIERS=$(curl -s -X GET "$API_BASE/suppliers" \
  -H "Authorization: Bearer $TOKEN")

SUPPLIER_ID=$(echo $SUPPLIERS | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)

if [ -z "$SUPPLIER_ID" ]; then
  echo "❌ Tedarikçi bulunamadı!"
  exit 1
fi

echo "✅ Tedarikçi bulundu (ID: $SUPPLIER_ID)"
echo ""

# 3. Product kontrol
echo "[3/4] Ürün kontrol ediliyor..."
PRODUCTS=$(curl -s -X GET "$API_BASE/products" \
  -H "Authorization: Bearer $TOKEN")

PRODUCT_ID=$(echo $PRODUCTS | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)
PRODUCT_SKU=$(echo $PRODUCTS | grep -o '"sku":"[^"]*' | head -1 | cut -d'"' -f4)

if [ -z "$PRODUCT_ID" ]; then
  echo "❌ Ürün bulunamadı!"
  exit 1
fi

echo "✅ Ürün bulundu (ID: $PRODUCT_ID, SKU: $PRODUCT_SKU)"
echo ""

# 4. Purchase Order oluştur ve Katana'ya sync test et
echo "[4/4] Purchase Order oluşturuluyor ve Katana sync test ediliyor..."
echo "   Bu işlem Katana API'ye Content-Type: application/json (charset olmadan) gönderecek"
echo ""

ORDER_DATA=$(cat <<EOF
{
  "supplierId": $SUPPLIER_ID,
  "orderDate": "$(date -u +"%Y-%m-%dT%H:%M:%S")",
  "expectedDate": "$(date -u -v+7d +"%Y-%m-%dT%H:%M:%S")",
  "documentSeries": "A",
  "documentTypeDetailId": 2,
  "vatIncluded": true,
  "projectCode": "CHARSET-TEST",
  "description": "Charset fix test siparisi",
  "items": [
    {
      "productId": $PRODUCT_ID,
      "quantity": 3,
      "unitPrice": 150.00,
      "lucaStockCode": "$PRODUCT_SKU",
      "warehouseCode": "01",
      "vatRate": 20,
      "unitCode": "AD",
      "discountAmount": 0
    }
  ]
}
EOF
)

ORDER_RESPONSE=$(curl -s -X POST "$API_BASE/purchase-orders" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "$ORDER_DATA")

ORDER_ID=$(echo $ORDER_RESPONSE | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)
ORDER_NO=$(echo $ORDER_RESPONSE | grep -o '"orderNo":"[^"]*' | cut -d'"' -f4)

if [ -z "$ORDER_ID" ]; then
  echo "❌ Sipariş oluşturulamadı!"
  echo "Response: $ORDER_RESPONSE"
  exit 1
fi

echo "✅ Sipariş oluşturuldu!"
echo "   Order ID: $ORDER_ID"
echo "   Order No: $ORDER_NO"
echo ""

# 5. Siparişi onayla (Katana'ya gönderilecek)
echo "[5/5] Sipariş onaylanıyor (Katana'ya gönderilecek)..."
echo "   🔍 Log'larda Content-Type header'ını kontrol edin!"
echo ""

APPROVE_RESPONSE=$(curl -s -X PATCH "$API_BASE/purchase-orders/$ORDER_ID/status" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"newStatus": 1}')

echo "Onay Response: $APPROVE_RESPONSE"
echo ""

# Docker log'larını kontrol et
echo "========================================"
echo "DOCKER LOG KONTROLÜ"
echo "========================================"
echo "Son 20 satır log (Content-Type header'ını arayın):"
echo ""
docker logs katana-api-1 --tail 20 2>&1 | grep -E "(Content-Type|Content Headers|🔍)"

echo ""
echo "========================================"
echo "TEST TAMAMLANDI"
echo "========================================"
echo ""
echo "KONTROL EDİLECEKLER:"
echo "   1. Log'larda '🔍 Content Headers' satırını bulun"
echo "   2. Content-Type=application/json (charset OLMADAN) olmalı"
echo "   3. Katana API'den 415 hatası gelmemeli"
echo ""
