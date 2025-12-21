#!/bin/bash

# Test All Orders Sync Script
# Tüm siparişleri admin onayına basıp sonra Koza'ya senkronize eder
# Hatalı siparişleri tespit etmek için kullanılır

set -e

BASE_URL="http://localhost:5055/api"
USERNAME="admin"
PASSWORD="Katana2025!"

# Renkli output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}  Sipariş Onay ve Senkronizasyon Testi${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# 1. Login
echo -e "${YELLOW}[1/4] Login yapılıyor...${NC}"
LOGIN_RESPONSE=$(curl -s -X POST "${BASE_URL}/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"${USERNAME}\",\"password\":\"${PASSWORD}\"}")

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.token')

if [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
  echo -e "${RED}❌ Login başarısız!${NC}"
  echo $LOGIN_RESPONSE | jq .
  exit 1
fi

echo -e "${GREEN}✅ Login başarılı${NC}"
echo ""

# 2. Tüm siparişleri getir
echo -e "${YELLOW}[2/4] Siparişler getiriliyor...${NC}"
ORDERS_RESPONSE=$(curl -s -X GET "${BASE_URL}/sales-orders" \
  -H "Authorization: Bearer ${TOKEN}")

ORDER_IDS=$(echo $ORDERS_RESPONSE | jq -r '.[] | select(.isSyncedToLuca == false) | .id' | head -10)
TOTAL_ORDERS=$(echo "$ORDER_IDS" | wc -l | tr -d ' ')

if [ -z "$ORDER_IDS" ] || [ "$TOTAL_ORDERS" -eq 0 ]; then
  echo -e "${YELLOW}⚠️  Senkronize edilmemiş sipariş bulunamadı${NC}"
  exit 0
fi

echo -e "${GREEN}✅ ${TOTAL_ORDERS} adet senkronize edilmemiş sipariş bulundu${NC}"
echo ""

# 3. Her siparişi admin onayına bas
echo -e "${YELLOW}[3/4] Siparişler admin onayına basılıyor...${NC}"
APPROVED_COUNT=0
APPROVAL_FAILED_COUNT=0

for ORDER_ID in $ORDER_IDS; do
  echo -e "${BLUE}  → Sipariş #${ORDER_ID} onaylanıyor...${NC}"
  
  APPROVE_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "${BASE_URL}/sales-orders/${ORDER_ID}/approve" \
    -H "Authorization: Bearer ${TOKEN}" \
    -H "Content-Type: application/json")
  
  HTTP_CODE=$(echo "$APPROVE_RESPONSE" | tail -n1)
  RESPONSE_BODY=$(echo "$APPROVE_RESPONSE" | sed '$d')
  
  if [ "$HTTP_CODE" -eq 200 ]; then
    echo -e "${GREEN}    ✅ Onaylandı${NC}"
    ((APPROVED_COUNT++))
  else
    echo -e "${RED}    ❌ Onay başarısız (HTTP ${HTTP_CODE})${NC}"
    echo "$RESPONSE_BODY" | jq -r '.message // .error // .' 2>/dev/null || echo "$RESPONSE_BODY"
    ((APPROVAL_FAILED_COUNT++))
  fi
  
  sleep 0.5
done

echo ""
echo -e "${GREEN}✅ Onaylanan: ${APPROVED_COUNT}${NC}"
echo -e "${RED}❌ Onay başarısız: ${APPROVAL_FAILED_COUNT}${NC}"
echo ""

# 4. Tüm siparişleri Koza'ya senkronize et
echo -e "${YELLOW}[4/4] Siparişler Koza'ya senkronize ediliyor...${NC}"
SYNC_SUCCESS_COUNT=0
SYNC_FAILED_COUNT=0
FAILED_ORDERS=()

# Siparişleri tekrar getir (onay sonrası güncel liste)
ORDERS_RESPONSE=$(curl -s -X GET "${BASE_URL}/sales-orders" \
  -H "Authorization: Bearer ${TOKEN}")

ORDER_IDS=$(echo $ORDERS_RESPONSE | jq -r '.[] | select(.isSyncedToLuca == false) | .id' | head -10)

for ORDER_ID in $ORDER_IDS; do
  # Sipariş detayını al
  ORDER_DETAIL=$(curl -s -X GET "${BASE_URL}/sales-orders/${ORDER_ID}" \
    -H "Authorization: Bearer ${TOKEN}")
  
  ORDER_NO=$(echo $ORDER_DETAIL | jq -r '.orderNo // "N/A"')
  CUSTOMER_TITLE=$(echo $ORDER_DETAIL | jq -r '.customer.title // "N/A"')
  
  echo -e "${BLUE}  → Sipariş #${ORDER_ID} (${ORDER_NO}) - ${CUSTOMER_TITLE}${NC}"
  
  SYNC_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST "${BASE_URL}/sales-orders/${ORDER_ID}/sync" \
    -H "Authorization: Bearer ${TOKEN}" \
    -H "Content-Type: application/json")
  
  HTTP_CODE=$(echo "$SYNC_RESPONSE" | tail -n1)
  RESPONSE_BODY=$(echo "$SYNC_RESPONSE" | sed '$d')
  
  if [ "$HTTP_CODE" -eq 200 ]; then
    IS_SUCCESS=$(echo "$RESPONSE_BODY" | jq -r '.isSuccess // false')
    if [ "$IS_SUCCESS" == "true" ]; then
      LUCA_ID=$(echo "$RESPONSE_BODY" | jq -r '.lucaOrderId // "N/A"')
      echo -e "${GREEN}    ✅ Başarılı (Koza ID: ${LUCA_ID})${NC}"
      ((SYNC_SUCCESS_COUNT++))
    else
      ERROR_MSG=$(echo "$RESPONSE_BODY" | jq -r '.message // "Bilinmeyen hata"')
      ERROR_DETAILS=$(echo "$RESPONSE_BODY" | jq -r '.errorDetails // ""')
      echo -e "${RED}    ❌ Başarısız: ${ERROR_MSG}${NC}"
      if [ ! -z "$ERROR_DETAILS" ]; then
        echo -e "${RED}       Detay: ${ERROR_DETAILS}${NC}"
      fi
      FAILED_ORDERS+=("${ORDER_ID}|${ORDER_NO}|${ERROR_MSG}|${ERROR_DETAILS}")
      ((SYNC_FAILED_COUNT++))
    fi
  else
    ERROR_MSG=$(echo "$RESPONSE_BODY" | jq -r '.message // .error // .' 2>/dev/null || echo "$RESPONSE_BODY")
    ERROR_DETAILS=$(echo "$RESPONSE_BODY" | jq -r '.errorDetails // ""' 2>/dev/null || echo "")
    echo -e "${RED}    ❌ HTTP ${HTTP_CODE}: ${ERROR_MSG}${NC}"
    if [ ! -z "$ERROR_DETAILS" ]; then
      echo -e "${RED}       Detay: ${ERROR_DETAILS}${NC}"
    fi
    FAILED_ORDERS+=("${ORDER_ID}|${ORDER_NO}|${ERROR_MSG}|${ERROR_DETAILS}")
    ((SYNC_FAILED_COUNT++))
  fi
  
  sleep 0.5
done

echo ""
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}  SONUÇ ÖZETİ${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "${GREEN}✅ Başarılı senkronizasyon: ${SYNC_SUCCESS_COUNT}${NC}"
echo -e "${RED}❌ Başarısız senkronizasyon: ${SYNC_FAILED_COUNT}${NC}"
echo ""

if [ ${#FAILED_ORDERS[@]} -gt 0 ]; then
  echo -e "${RED}HATALI SİPARİŞLER:${NC}"
  echo -e "${RED}==================${NC}"
  for FAILED in "${FAILED_ORDERS[@]}"; do
    IFS='|' read -r ORDER_ID ORDER_NO ERROR_MSG ERROR_DETAILS <<< "$FAILED"
    echo -e "${RED}  • Sipariş #${ORDER_ID} (${ORDER_NO})${NC}"
    echo -e "${RED}    Hata: ${ERROR_MSG}${NC}"
    if [ ! -z "$ERROR_DETAILS" ]; then
      echo -e "${RED}    Detay: ${ERROR_DETAILS}${NC}"
    fi
  done
  echo ""
fi

echo -e "${BLUE}========================================${NC}"
echo -e "${GREEN}Test tamamlandı!${NC}"
echo ""
