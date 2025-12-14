#!/bin/bash

# Test Stock Card Mapping (Category & Unit)
# Bu script Katana'dan Luca'ya stok kartı senkronizasyonunda
# kategori ve ölçü birimi mapping'lerinin doğru çalıştığını test eder

set -e

echo "🧪 Stok Kartı Mapping Testi Başlıyor..."
echo "========================================"
echo ""

# Renkli output için
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# API base URL
API_URL="http://localhost:5055"

# Admin token al
echo "🔐 Admin token alınıyor..."
TOKEN_RESPONSE=$(curl -s -X POST "$API_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "Katana2025!"
  }')

TOKEN=$(echo $TOKEN_RESPONSE | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo -e "${RED}❌ Token alınamadı!${NC}"
  echo "Response: $TOKEN_RESPONSE"
  exit 1
fi

echo -e "${GREEN}✅ Token alındı${NC}"
echo ""

# 1. Katana'dan ürünleri çek
echo "📥 Katana'dan ürünler çekiliyor..."
KATANA_PRODUCTS=$(curl -s -X GET "$API_URL/api/katana/products?limit=5" \
  -H "Authorization: Bearer $TOKEN")

echo "Katana'dan gelen ilk 5 ürün:"
echo "$KATANA_PRODUCTS" | jq -r '.[] | "  - SKU: \(.sku // .SKU), Name: \(.name // .Name), Category: \(.category // .Category), Unit: \(.unit // .Unit)"' 2>/dev/null || echo "$KATANA_PRODUCTS"
echo ""

# 2. Dry-run payload'ı kontrol et (mapping'lerin uygulandığını görmek için)
echo "🔍 Luca'ya gönderilecek payload kontrol ediliyor (dry-run)..."
DRY_PAYLOAD=$(curl -s -X GET "$API_URL/api/koza-debug/dry-payload?limit=5" \
  -H "Authorization: Bearer $TOKEN")

echo "Luca'ya gönderilecek mapping'li veriler:"
echo "$DRY_PAYLOAD" | jq -r '.[] | "  - SKU: \(.Sku), KartKodu: \(.KartKodu), Kategori: \(.KategoriAgacKod // "null"), Barkod: \(.Barkod // "null")"' 2>/dev/null || echo "$DRY_PAYLOAD"
echo ""

# 3. Mapping kontrolü
echo "🔎 Mapping Kontrolü:"
echo "-------------------"

# appsettings.json'dan mapping'leri oku
CATEGORY_MAPPING=$(cat src/Katana.API/appsettings.json | jq -r '.LucaApi.CategoryMapping')
UNIT_MAPPING=$(cat src/Katana.API/appsettings.json | jq -r '.LucaApi.UnitMapping')

echo -e "${BLUE}Kategori Mapping'leri:${NC}"
echo "$CATEGORY_MAPPING" | jq '.'
echo ""

echo -e "${BLUE}Ölçü Birimi Mapping'leri:${NC}"
echo "$UNIT_MAPPING" | jq '.'
echo ""

# 4. Test: Belirli bir ürünü senkronize et (dry-run)
echo "🧪 Test: Dry-run ile senkronizasyon simülasyonu..."
SYNC_RESULT=$(curl -s -X POST "$API_URL/api/sync/products-to-luca" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "dryRun": true,
    "limit": 3
  }')

echo "Senkronizasyon sonucu:"
echo "$SYNC_RESULT" | jq '.' 2>/dev/null || echo "$SYNC_RESULT"
echo ""

# 5. Backend log'larını kontrol et
echo "📋 Backend log'larını kontrol ediyoruz..."
echo "Son 50 satır (mapping ile ilgili):"
docker logs katana-backend 2>&1 | grep -E "(ÖLÇÜ BİRİMİ|MAPPING|KategoriAgacKod|OlcumBirimiId)" | tail -20 || echo "Log bulunamadı veya docker container çalışmıyor"
echo ""

# 6. Özet
echo "========================================"
echo "📊 Test Özeti"
echo "========================================"
echo ""

# Kategori mapping kontrolü
CATEGORY_COUNT=$(echo "$CATEGORY_MAPPING" | jq 'length')
echo -e "${GREEN}✅ Kategori Mapping Sayısı: $CATEGORY_COUNT${NC}"

# Ölçü birimi mapping kontrolü
UNIT_COUNT=$(echo "$UNIT_MAPPING" | jq 'length')
echo -e "${GREEN}✅ Ölçü Birimi Mapping Sayısı: $UNIT_COUNT${NC}"

# Dry-run sonucu kontrolü
if echo "$SYNC_RESULT" | jq -e '.isDryRun == true' > /dev/null 2>&1; then
  echo -e "${GREEN}✅ Dry-run başarılı${NC}"
  
  PROCESSED=$(echo "$SYNC_RESULT" | jq -r '.processedRecords // 0')
  NEW_CREATED=$(echo "$SYNC_RESULT" | jq -r '.newCreated // 0')
  
  echo -e "${BLUE}   - İşlenen ürün: $PROCESSED${NC}"
  echo -e "${BLUE}   - Yeni oluşturulacak: $NEW_CREATED${NC}"
else
  echo -e "${YELLOW}⚠️  Dry-run sonucu beklendiği gibi değil${NC}"
fi

echo ""
echo "========================================"
echo "🎯 Manuel Kontrol Önerileri:"
echo "========================================"
echo ""
echo "1. Backend log'larında şu mesajları arayın:"
echo "   ${BLUE}✅ ÖLÇÜ BİRİMİ MAPPING: 'adet' → Luca ID: 5${NC}"
echo "   ${BLUE}⚠️ ÖLÇÜ BİRİMİ MAPPING BULUNAMADI: 'xyz'${NC}"
echo ""
echo "2. Luca'da bir stok kartı açın ve kontrol edin:"
echo "   - Kategori doğru mu?"
echo "   - Ölçü birimi doğru mu?"
echo ""
echo "3. Gerçek senkronizasyon için (dry-run olmadan):"
echo "   ${YELLOW}curl -X POST \"$API_URL/api/sync/products-to-luca\" \\${NC}"
echo "   ${YELLOW}  -H \"Authorization: Bearer \$TOKEN\" \\${NC}"
echo "   ${YELLOW}  -H \"Content-Type: application/json\" \\${NC}"
echo "   ${YELLOW}  -d '{\"dryRun\": false, \"limit\": 1}'${NC}"
echo ""
echo "✅ Test tamamlandı!"
