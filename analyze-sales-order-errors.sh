#!/bin/bash

# Sales Order Error Analysis Script
# Bu script hatalı siparişleri analiz eder ve çözüm önerileri sunar

set -e

# Renkli output için
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# API base URL
API_URL="${API_URL:-http://localhost:5055/api}"

# Auth token
TOKEN_FILE=".auth_token"
if [ ! -f "$TOKEN_FILE" ]; then
    echo -e "${RED}❌ Token dosyası bulunamadı: $TOKEN_FILE${NC}"
    echo "Önce giriş yapın: ./test-jwt-auth.ps1"
    exit 1
fi

TOKEN=$(cat "$TOKEN_FILE")

# Fonksiyon: API çağrısı yap
api_call() {
    local method=$1
    local endpoint=$2
    local data=$3
    
    if [ -z "$data" ]; then
        curl -s -X "$method" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            "$API_URL$endpoint"
    else
        curl -s -X "$method" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "$data" \
            "$API_URL$endpoint"
    fi
}

echo -e "${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║     Sales Order Error Analysis - Hata Analiz Aracı       ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Tüm siparişleri al
echo -e "${CYAN}🔍 Siparişler getiriliyor...${NC}"
orders=$(api_call "GET" "/sales-orders?page=1&pageSize=100")

# Hatalı siparişleri filtrele
error_orders=$(echo "$orders" | jq -r '.[] | select(.lucaSyncStatus == "error")' 2>/dev/null)

if [ -z "$error_orders" ]; then
    echo -e "${GREEN}✅ Hatalı sipariş bulunamadı!${NC}"
    exit 0
fi

error_count=$(echo "$error_orders" | jq -s 'length' 2>/dev/null)
echo -e "${YELLOW}📋 $error_count adet hatalı sipariş bulundu${NC}"
echo ""

# Hata kategorileri
declare -A error_categories
error_categories["customer"]="Müşteri Verisi Hatası"
error_categories["stock"]="Stok Kartı Hatası"
error_categories["currency"]="Döviz Kuru Hatası"
error_categories["document"]="Belge Seri/No Hatası"
error_categories["validation"]="Validasyon Hatası"
error_categories["luca_api"]="Luca API Hatası"
error_categories["unknown"]="Bilinmeyen Hata"

# Hata sayaçları
declare -A error_counts
for category in "${!error_categories[@]}"; do
    error_counts[$category]=0
done

# Her hatalı siparişi analiz et
echo "$error_orders" | jq -c '.' | while read -r order; do
    order_id=$(echo "$order" | jq -r '.id')
    order_no=$(echo "$order" | jq -r '.orderNo')
    customer_name=$(echo "$order" | jq -r '.customerName // "N/A"')
    last_error=$(echo "$order" | jq -r '.lastSyncError // "Bilinmeyen hata"')
    
    echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}📦 Sipariş: $order_no (ID: $order_id)${NC}"
    echo "   Müşteri: $customer_name"
    echo ""
    echo -e "${RED}❌ Hata Mesajı:${NC}"
    echo "   $last_error"
    echo ""
    
    # Hata kategorisini belirle
    category="unknown"
    if echo "$last_error" | grep -qi "müşteri\|customer\|cari\|vergi"; then
        category="customer"
    elif echo "$last_error" | grep -qi "stok\|stock\|sku\|variant"; then
        category="stock"
    elif echo "$last_error" | grep -qi "kur\|currency\|rate\|döviz"; then
        category="currency"
    elif echo "$last_error" | grep -qi "belge\|seri\|document\|no"; then
        category="document"
    elif echo "$last_error" | grep -qi "validation\|geçersiz\|invalid"; then
        category="validation"
    elif echo "$last_error" | grep -qi "luca\|api\|connection\|timeout"; then
        category="luca_api"
    fi
    
    echo -e "${CYAN}🏷️  Kategori: ${error_categories[$category]}${NC}"
    echo ""
    
    # Çözüm önerileri
    echo -e "${GREEN}💡 Çözüm Önerileri:${NC}"
    case $category in
        "customer")
            echo "   1. Müşteri verilerini kontrol edin:"
            echo "      - Vergi No / TC Kimlik No formatı doğru mu? (10 veya 11 haneli)"
            echo "      - Luca Cari Kodu atanmış mı?"
            echo "      - Müşteri adı/unvanı dolu mu?"
            echo ""
            echo "   2. Müşteri detayını görüntüleyin:"
            echo "      curl -H 'Authorization: Bearer \$TOKEN' $API_URL/customers/\$(jq -r '.customerId' <<< '$order')"
            ;;
        "stock")
            echo "   1. Stok kartlarını kontrol edin:"
            echo "      - SKU kodları Luca'da mevcut mu?"
            echo "      - Stok kartı eşleştirmeleri yapılmış mı?"
            echo "      - Variant ID'ler doğru mu?"
            echo ""
            echo "   2. Sipariş satırlarını görüntüleyin:"
            echo "      curl -H 'Authorization: Bearer \$TOKEN' $API_URL/sales-orders/$order_id"
            ;;
        "currency")
            echo "   1. Döviz kuru bilgilerini kontrol edin:"
            echo "      - ConversionRate değeri var mı?"
            echo "      - Kur değeri 0'dan büyük mü?"
            echo "      - Currency alanı doğru mu? (EUR, USD, TRY)"
            echo ""
            echo "   2. Sipariş detayını kontrol edin:"
            echo "      curl -H 'Authorization: Bearer \$TOKEN' $API_URL/sales-orders/$order_id | jq '.currency, .conversionRate'"
            ;;
        "document")
            echo "   1. Belge bilgilerini kontrol edin:"
            echo "      - BelgeSeri atanmış mı?"
            echo "      - BelgeNo formatı doğru mu?"
            echo "      - BelgeTurDetayId geçerli mi?"
            echo ""
            echo "   2. Luca alanlarını güncelleyin:"
            echo "      curl -X PATCH -H 'Authorization: Bearer \$TOKEN' \\"
            echo "           -H 'Content-Type: application/json' \\"
            echo "           -d '{\"belgeSeri\":\"EFA2025\",\"belgeTurDetayId\":17}' \\"
            echo "           $API_URL/sales-orders/$order_id/luca-fields"
            ;;
        "validation")
            echo "   1. Validasyon hatalarını düzeltin:"
            echo "      - Zorunlu alanlar dolu mu?"
            echo "      - Veri formatları doğru mu?"
            echo "      - İlişkili kayıtlar mevcut mu?"
            ;;
        "luca_api")
            echo "   1. Luca API bağlantısını kontrol edin:"
            echo "      - Luca servisi çalışıyor mu?"
            echo "      - Session geçerli mi?"
            echo "      - Network bağlantısı var mı?"
            echo ""
            echo "   2. Luca session'ı yenileyin:"
            echo "      curl -X POST -H 'Authorization: Bearer \$TOKEN' $API_URL/luca/refresh-session"
            ;;
        *)
            echo "   1. Hata mesajını detaylı inceleyin"
            echo "   2. Backend loglarını kontrol edin"
            echo "   3. Sipariş detayını görüntüleyin:"
            echo "      curl -H 'Authorization: Bearer \$TOKEN' $API_URL/sales-orders/$order_id"
            ;;
    esac
    
    echo ""
    echo -e "${CYAN}🔧 Hızlı Düzeltme Komutu:${NC}"
    echo "   # Siparişi tekrar senkronize et:"
    echo "   curl -X POST -H 'Authorization: Bearer \$TOKEN' $API_URL/sales-orders/$order_id/sync"
    echo ""
done

echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}📊 Hata Özeti:${NC}"
echo ""

# Hata kategorilerini say
for category in "${!error_categories[@]}"; do
    count=$(echo "$error_orders" | jq -r '.lastSyncError' | grep -ci "${category}" 2>/dev/null || echo "0")
    if [ "$count" -gt 0 ]; then
        echo -e "   ${error_categories[$category]}: ${YELLOW}$count${NC}"
    fi
done

echo ""
echo -e "${CYAN}💡 Genel Öneriler:${NC}"
echo "   1. Önce müşteri verilerini düzeltin (en yaygın hata)"
echo "   2. Stok kartı eşleştirmelerini kontrol edin"
echo "   3. Belge seri/no ayarlarını yapın"
echo "   4. Döviz kurlarını güncelleyin"
echo "   5. Hataları düzelttikten sonra test scriptini çalıştırın:"
echo "      ./test-sales-order-sync-loop.sh"
echo ""
