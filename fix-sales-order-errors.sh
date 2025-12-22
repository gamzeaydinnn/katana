#!/bin/bash

# Sales Order Auto-Fix Script
# Bu script yaygın hataları otomatik olarak düzeltmeye çalışır

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
echo -e "${BLUE}║   Sales Order Auto-Fix - Otomatik Hata Düzeltme Aracı   ║${NC}"
echo -e "${BLUE}╚════════════════════════════════════════════════════════════╝${NC}"
echo ""

# Tüm siparişleri al
echo -e "${CYAN}🔍 Hatalı siparişler getiriliyor...${NC}"
orders=$(api_call "GET" "/sales-orders?page=1&pageSize=100&syncStatus=error")

if [ -z "$orders" ] || [ "$orders" = "[]" ]; then
    echo -e "${GREEN}✅ Hatalı sipariş bulunamadı!${NC}"
    exit 0
fi

error_count=$(echo "$orders" | jq 'length' 2>/dev/null)
echo -e "${YELLOW}📋 $error_count adet hatalı sipariş bulundu${NC}"
echo ""

fixed_count=0
failed_count=0

# Her hatalı siparişi düzelt
echo "$orders" | jq -c '.[]' | while read -r order; do
    order_id=$(echo "$order" | jq -r '.id')
    order_no=$(echo "$order" | jq -r '.orderNo')
    last_error=$(echo "$order" | jq -r '.lastSyncError // "Bilinmeyen hata"')
    
    echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${YELLOW}🔧 Sipariş: $order_no (ID: $order_id)${NC}"
    echo "   Hata: $last_error"
    echo ""
    
    # Sipariş detayını al
    order_detail=$(api_call "GET" "/sales-orders/$order_id")
    
    # Düzeltme bayrakları
    needs_fix=false
    fix_data="{}"
    
    # 1. Belge Seri/No kontrolü ve düzeltmesi
    belge_seri=$(echo "$order_detail" | jq -r '.belgeSeri // ""')
    if [ -z "$belge_seri" ] || [ "$belge_seri" = "null" ]; then
        echo -e "${CYAN}   → BelgeSeri eksik, 'EFA2025' atanıyor...${NC}"
        fix_data=$(echo "$fix_data" | jq '. + {"belgeSeri": "EFA2025"}')
        needs_fix=true
    fi
    
    # 2. BelgeTurDetayId kontrolü
    belge_tur=$(echo "$order_detail" | jq -r '.belgeTurDetayId // 0')
    if [ "$belge_tur" = "0" ] || [ "$belge_tur" = "null" ]; then
        echo -e "${CYAN}   → BelgeTurDetayId eksik, '17' atanıyor...${NC}"
        fix_data=$(echo "$fix_data" | jq '. + {"belgeTurDetayId": 17}')
        needs_fix=true
    fi
    
    # 3. OnayFlag kontrolü
    onay_flag=$(echo "$order_detail" | jq -r '.onayFlag // false')
    if [ "$onay_flag" = "false" ]; then
        echo -e "${CYAN}   → OnayFlag false, true yapılıyor...${NC}"
        fix_data=$(echo "$fix_data" | jq '. + {"onayFlag": true}')
        needs_fix=true
    fi
    
    # 4. NakliyeBedeliTuru kontrolü
    nakliye=$(echo "$order_detail" | jq -r '.nakliyeBedeliTuru // null')
    if [ "$nakliye" = "null" ]; then
        echo -e "${CYAN}   → NakliyeBedeliTuru eksik, '0' atanıyor...${NC}"
        fix_data=$(echo "$fix_data" | jq '. + {"nakliyeBedeliTuru": 0}')
        needs_fix=true
    fi
    
    # 5. TeklifSiparisTur kontrolü
    teklif=$(echo "$order_detail" | jq -r '.teklifSiparisTur // null')
    if [ "$teklif" = "null" ]; then
        echo -e "${CYAN}   → TeklifSiparisTur eksik, '1' atanıyor...${NC}"
        fix_data=$(echo "$fix_data" | jq '. + {"teklifSiparisTur": 1}')
        needs_fix=true
    fi
    
    # Düzeltmeleri uygula
    if [ "$needs_fix" = true ]; then
        echo ""
        echo -e "${BLUE}   💾 Düzeltmeler uygulanıyor...${NC}"
        
        update_result=$(api_call "PATCH" "/sales-orders/$order_id/luca-fields" "$fix_data")
        
        if echo "$update_result" | jq -e '.id' > /dev/null 2>&1; then
            echo -e "${GREEN}   ✅ Düzeltmeler başarıyla uygulandı${NC}"
            
            # Tekrar senkronize et
            echo -e "${BLUE}   🔄 Tekrar senkronize ediliyor...${NC}"
            sync_result=$(api_call "POST" "/sales-orders/$order_id/sync" "{}")
            
            if echo "$sync_result" | jq -e '.isSuccess == true' > /dev/null 2>&1; then
                luca_id=$(echo "$sync_result" | jq -r '.lucaOrderId')
                echo -e "${GREEN}   ✅ Senkronizasyon başarılı! Luca ID: $luca_id${NC}"
                ((fixed_count++))
            else
                sync_error=$(echo "$sync_result" | jq -r '.message // "Bilinmeyen hata"')
                echo -e "${RED}   ❌ Senkronizasyon başarısız: $sync_error${NC}"
                ((failed_count++))
            fi
        else
            echo -e "${RED}   ❌ Düzeltmeler uygulanamadı${NC}"
            ((failed_count++))
        fi
    else
        echo -e "${YELLOW}   ⚠️  Otomatik düzeltme yapılamadı (manuel müdahale gerekli)${NC}"
        ((failed_count++))
    fi
    
    echo ""
    sleep 1
done

echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${BLUE}📊 Düzeltme Özeti:${NC}"
echo -e "   Düzeltilen: ${GREEN}$fixed_count${NC}"
echo -e "   Başarısız: ${RED}$failed_count${NC}"
echo ""

if [ $failed_count -gt 0 ]; then
    echo -e "${YELLOW}⚠️  Bazı siparişler düzeltilemedi${NC}"
    echo -e "${CYAN}Detaylı analiz için:${NC}"
    echo "   ./analyze-sales-order-errors.sh"
    echo ""
    echo -e "${CYAN}Manuel düzeltme sonrası test için:${NC}"
    echo "   ./test-sales-order-sync-loop.sh"
else
    echo -e "${GREEN}✅ Tüm siparişler başarıyla düzeltildi!${NC}"
fi
