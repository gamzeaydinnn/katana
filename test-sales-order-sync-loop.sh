#!/bin/bash

# Sales Order Sync Test Loop Script
# Bu script tüm pending siparişleri test eder ve hataları çözene kadar devam eder

set -e

# Renkli output için
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
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

# Log dosyası
LOG_FILE="sales-order-sync-test-$(date +%Y%m%d-%H%M%S).log"
echo "📝 Log dosyası: $LOG_FILE"

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

# Fonksiyon: Sipariş listesini al
get_orders() {
    api_call "GET" "/sales-orders?page=1&pageSize=100"
}

# Fonksiyon: Sipariş detayını al
get_order_detail() {
    local order_id=$1
    api_call "GET" "/sales-orders/$order_id"
}

# Fonksiyon: Siparişi senkronize et
sync_order() {
    local order_id=$1
    api_call "POST" "/sales-orders/$order_id/sync" "{}"
}

# Fonksiyon: İstatistikleri al
get_stats() {
    api_call "GET" "/sales-orders/stats"
}

echo -e "${BLUE}╔════════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║  Sales Order Sync Test Loop - Otomatik Test ve Düzeltme  ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Ana döngü
iteration=1
max_iterations=10
success_count=0
error_count=0

while [ $iteration -le $max_iterations ]; do
    echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}Iterasyon #$iteration / $max_iterations${NC}"
    echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo ""
    
    # İstatistikleri göster
    echo -e "${BLUE}📊 Mevcut İstatistikler:${NC}"
    stats=$(get_stats)
    echo "$stats" | jq '.' 2>/dev/null || echo "$stats"
    echo ""
    
    # Pending siparişleri al
    echo -e "${BLUE}🔍 Pending siparişler getiriliyor...${NC}"
    orders=$(get_orders)
    
    # Pending siparişleri filtrele (not_synced ve error durumundakiler)
    pending_orders=$(echo "$orders" | jq -r '.[] | select(.lucaSyncStatus == "not_synced" or .lucaSyncStatus == "error") | .id' 2>/dev/null)
    
    if [ -z "$pending_orders" ]; then
        echo -e "${GREEN}✅ Tüm siparişler senkronize edildi!${NC}"
        echo ""
        echo -e "${GREEN}╔════════════════════════════════════════╗${NC}"
        echo -e "${GREEN}║  TEST BAŞARIYLA TAMAMLANDI!           ║${NC}"
        echo -e "${GREEN}║  Başarılı: $success_count                        ║${NC}"
        echo -e "${GREEN}║  Hatalı: $error_count                          ║${NC}"
        echo -e "${GREEN}╚════════════════════════════════════════╝${NC}"
        exit 0
    fi
    
    pending_count=$(echo "$pending_orders" | wc -l)
    echo -e "${YELLOW}📋 $pending_count adet pending sipariş bulundu${NC}"
    echo ""
    
    # Her pending siparişi test et
    for order_id in $pending_orders; do
        echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
        echo -e "${BLUE}🔄 Sipariş ID: $order_id${NC}"
        
        # Sipariş detayını al
        order_detail=$(get_order_detail "$order_id")
        order_no=$(echo "$order_detail" | jq -r '.orderNo' 2>/dev/null)
        customer_name=$(echo "$order_detail" | jq -r '.customerName // "N/A"' 2>/dev/null)
        status=$(echo "$order_detail" | jq -r '.status' 2>/dev/null)
        last_error=$(echo "$order_detail" | jq -r '.lastSyncError // "Yok"' 2>/dev/null)
        
        echo "   Sipariş No: $order_no"
        echo "   Müşteri: $customer_name"
        echo "   Durum: $status"
        
        if [ "$last_error" != "Yok" ] && [ "$last_error" != "null" ]; then
            echo -e "   ${RED}Son Hata: $last_error${NC}"
        fi
        
        echo ""
        echo -e "${YELLOW}   ⏳ Senkronizasyon başlatılıyor...${NC}"
        
        # Senkronizasyon yap
        sync_result=$(sync_order "$order_id" 2>&1)
        sync_success=$(echo "$sync_result" | jq -r '.isSuccess // false' 2>/dev/null)
        sync_message=$(echo "$sync_result" | jq -r '.message // "Bilinmeyen hata"' 2>/dev/null)
        sync_error=$(echo "$sync_result" | jq -r '.errorDetails // ""' 2>/dev/null)
        luca_order_id=$(echo "$sync_result" | jq -r '.lucaOrderId // "N/A"' 2>/dev/null)
        
        # Sonucu logla
        echo "[$iteration] Order $order_id ($order_no): $sync_message" >> "$LOG_FILE"
        
        if [ "$sync_success" = "true" ]; then
            echo -e "   ${GREEN}✅ BAŞARILI!${NC}"
            echo -e "   ${GREEN}Luca Order ID: $luca_order_id${NC}"
            echo -e "   ${GREEN}Mesaj: $sync_message${NC}"
            ((success_count++))
        else
            echo -e "   ${RED}❌ HATA!${NC}"
            echo -e "   ${RED}Mesaj: $sync_message${NC}"
            if [ -n "$sync_error" ] && [ "$sync_error" != "null" ]; then
                echo -e "   ${RED}Detay: $sync_error${NC}"
                echo "[$iteration] ERROR DETAIL: $sync_error" >> "$LOG_FILE"
            fi
            ((error_count++))
            
            # Hata analizi
            echo ""
            echo -e "${YELLOW}   🔍 Hata Analizi:${NC}"
            
            # Müşteri verisi kontrolü
            if echo "$sync_error" | grep -qi "müşteri\|customer\|cari"; then
                echo -e "   ${YELLOW}   → Müşteri verisi problemi tespit edildi${NC}"
                echo "   → Müşteri bilgilerini kontrol edin"
                echo "   → Vergi No / Luca Cari Kodu geçerli mi?"
            fi
            
            # Stok kartı kontrolü
            if echo "$sync_error" | grep -qi "stok\|stock\|sku"; then
                echo -e "   ${YELLOW}   → Stok kartı problemi tespit edildi${NC}"
                echo "   → SKU kodları Luca'da mevcut mu?"
                echo "   → Stok kartı eşleştirmelerini kontrol edin"
            fi
            
            # Döviz kuru kontrolü
            if echo "$sync_error" | grep -qi "kur\|currency\|rate"; then
                echo -e "   ${YELLOW}   → Döviz kuru problemi tespit edildi${NC}"
                echo "   → Conversion rate değeri kontrol edin"
            fi
            
            # Belge seri/no kontrolü
            if echo "$sync_error" | grep -qi "belge\|seri\|document"; then
                echo -e "   ${YELLOW}   → Belge seri/no problemi tespit edildi${NC}"
                echo "   → BelgeSeri ve BelgeNo alanlarını kontrol edin"
            fi
        fi
        
        echo ""
        sleep 1  # Rate limiting için kısa bekleme
    done
    
    echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}Iterasyon #$iteration Özeti:${NC}"
    echo -e "   Başarılı: ${GREEN}$success_count${NC}"
    echo -e "   Hatalı: ${RED}$error_count${NC}"
    echo ""
    
    # Eğer hata varsa, bir sonraki iterasyona geç
    if [ $error_count -gt 0 ]; then
        echo -e "${YELLOW}⏳ 5 saniye bekleniyor...${NC}"
        sleep 5
    fi
    
    ((iteration++))
done

echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${YELLOW}Test Tamamlandı (Max iterasyon sayısına ulaşıldı)${NC}"
echo -e "   Toplam Başarılı: ${GREEN}$success_count${NC}"
echo -e "   Toplam Hatalı: ${RED}$error_count${NC}"
echo ""
echo -e "${BLUE}📝 Detaylı log: $LOG_FILE${NC}"

if [ $error_count -gt 0 ]; then
    echo ""
    echo -e "${RED}⚠️  Hala hatalı siparişler var!${NC}"
    echo -e "${YELLOW}Hataları analiz etmek için:${NC}"
    echo "   cat $LOG_FILE | grep ERROR"
    exit 1
else
    echo ""
    echo -e "${GREEN}✅ Tüm siparişler başarıyla senkronize edildi!${NC}"
    exit 0
fi
