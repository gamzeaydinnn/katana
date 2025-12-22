#!/bin/bash

echo "============================================"
echo "SO-75 ONAY VE SENKRONIZASYON TESTI"
echo "============================================"
echo ""

# Login
echo "1. Login yapiliyor..."
TOKEN=$(curl -s http://localhost:5055/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"orgCode":"100","userName":"admin","password":"Katana2025!"}' | jq -r '.token')

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
  echo "   ✗ Login hatasi!"
  exit 1
fi
echo "   ✓ Login basarili!"
echo ""

# SO-75 detaylarını al
echo "2. SO-75 siparis detaylari aliniyor..."
SO75=$(curl -s http://localhost:5055/api/debug/katana/katana-invoices \
  -H "Authorization: Bearer $TOKEN" | jq '.data[] | select(.invoiceNo == "SO-75")')

if [ -z "$SO75" ]; then
  echo "   ✗ SO-75 bulunamadi!"
  exit 1
fi

echo "   ✓ Siparis bulundu!"
echo "$SO75" | jq '{invoiceNo, externalCustomerId, amount, currency, itemCount: (.items | length)}'
echo ""

# Senkronizasyon başlat
echo "3. Luca'ya senkronizasyon baslatiliyor..."
SYNC_RESULT=$(curl -s -X POST http://localhost:5055/api/sync/from-luca/dispatch \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

echo "   Senkronizasyon sonucu:"
echo "$SYNC_RESULT" | jq '.'
echo ""

echo "============================================"
echo "TEST TAMAMLANDI"
echo "============================================"
