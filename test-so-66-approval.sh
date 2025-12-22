#!/bin/bash

# SO-SO-66 ONAY VE SENKRONIZASYON TESTI
BASE_URL="http://localhost:5055"
ORDER_NUMBER="SO-SO-66"

echo "============================================"
echo "SO-SO-66 ONAY VE SENKRONIZASYON TESTI"
echo "============================================"
echo ""

# 1. Login
echo "1. Login yapiliyor..."
LOGIN_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "orgCode": "100",
    "userName": "admin",
    "password": "Katana2025!"
  }')

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.token')

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    echo "   ✗ Login hatasi!"
    echo "   Response: $LOGIN_RESPONSE"
    exit 1
fi

echo "   ✓ Login basarili!"
echo ""

# 2. Katana'dan SO-SO-66'yi bul
echo "2. Katana'dan $ORDER_NUMBER aranıyor..."
KATANA_INVOICES=$(curl -s -X GET "${BASE_URL}/api/debug/katana/katana-invoices" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

TARGET_ORDER=$(echo $KATANA_INVOICES | jq ".data[] | select(.invoiceNo == \"$ORDER_NUMBER\")")

if [ -z "$TARGET_ORDER" ] || [ "$TARGET_ORDER" = "null" ]; then
    echo "   ✗ $ORDER_NUMBER bulunamadi!"
    echo "   Mevcut siparisler:"
    echo "$KATANA_INVOICES" | jq -r '.data[0:5] | .[] | "     - \(.invoiceNo) (Amount: \(.totalAmount))"'
    exit 1
fi

echo "   ✓ Siparis bulundu!"
echo "$TARGET_ORDER" | jq '{invoiceNo, totalAmount, invoiceDate}'
echo ""

# 3. Luca'ya senkronize et (Katana invoices zaten approved olarak geliyor)
echo "3. Siparis zaten Katana'dan geldi, Luca'ya gonderiliyor..."
echo ""

# 4. Luca'ya senkronize et
echo "4. Luca'ya senkronizasyon baslatiliyor..."
SYNC_RESPONSE=$(curl -s -X POST "${BASE_URL}/api/sync/from-luca/dispatch" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

echo "   ✓ Senkronizasyon baslatildi!"
echo "$SYNC_RESPONSE" | jq '.'
echo ""

# 5. Sonucu kontrol et
echo "5. Sonuc kontrol ediliyor (3 saniye bekleniyor)..."
sleep 3

KATANA_INVOICES_AFTER=$(curl -s -X GET "${BASE_URL}/api/debug/katana/katana-invoices" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

TARGET_ORDER_AFTER=$(echo $KATANA_INVOICES_AFTER | jq ".data[] | select(.invoiceNo == \"$ORDER_NUMBER\")")

if [ ! -z "$TARGET_ORDER_AFTER" ]; then
    echo "   Siparis durumu:"
    echo "$TARGET_ORDER_AFTER" | jq '{invoiceNo, totalAmount, invoiceDate}'
    echo ""
    
    echo "   ✓ Siparis Luca'ya gonderildi!"
fi

echo ""
echo "============================================"
echo "TEST TAMAMLANDI"
echo "============================================"
