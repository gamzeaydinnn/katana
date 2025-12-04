#!/bin/bash

# Katana Uygulaması Test Script
# Bu script API ve frontend'in çalışıp çalışmadığını kontrol eder

set -e

echo "================================"
echo "Katana Uygulaması Test Script"
echo "================================"
echo ""

# Renkler
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test sayaçları
PASSED=0
FAILED=0

# Fonksiyon: Test sonucu yazdır
test_result() {
    local test_name=$1
    local result=$2
    
    if [ $result -eq 0 ]; then
        echo -e "${GREEN}✓ GEÇTI${NC}: $test_name"
        ((PASSED++))
    else
        echo -e "${RED}✗ BAŞARISIZ${NC}: $test_name"
        ((FAILED++))
    fi
}

echo -e "${YELLOW}1. Backend Health Check${NC}"
echo "---"

# API Health Check
if curl -s http://localhost:8080/api/health > /dev/null 2>&1; then
    test_result "API Health Endpoint" 0
else
    test_result "API Health Endpoint" 1
fi

# Database Connection Check
HEALTH_RESPONSE=$(curl -s http://localhost:8080/api/health)
if echo "$HEALTH_RESPONSE" | grep -q "database"; then
    test_result "Database Bağlantısı" 0
else
    test_result "Database Bağlantısı" 1
fi

echo ""
echo -e "${YELLOW}2. API Endpoint'leri${NC}"
echo "---"

# Suppliers Endpoint
SUPPLIERS_RESPONSE=$(curl -s -H "Authorization: Bearer test" http://localhost:8080/api/suppliers 2>&1 || echo "")
if [ ! -z "$SUPPLIERS_RESPONSE" ]; then
    test_result "Suppliers Endpoint" 0
else
    test_result "Suppliers Endpoint" 1
fi

# Products Endpoint
PRODUCTS_RESPONSE=$(curl -s -H "Authorization: Bearer test" http://localhost:8080/api/products 2>&1 || echo "")
if [ ! -z "$PRODUCTS_RESPONSE" ]; then
    test_result "Products Endpoint" 0
else
    test_result "Products Endpoint" 1
fi

# Purchase Orders Endpoint
PO_RESPONSE=$(curl -s -H "Authorization: Bearer test" http://localhost:8080/api/purchase-orders 2>&1 || echo "")
if [ ! -z "$PO_RESPONSE" ]; then
    test_result "Purchase Orders Endpoint" 0
else
    test_result "Purchase Orders Endpoint" 1
fi

echo ""
echo -e "${YELLOW}3. Docker Container'ları${NC}"
echo "---"

# API Container kontrolü
if docker ps | grep -q "katana-api"; then
    test_result "API Container Çalışıyor" 0
else
    test_result "API Container Çalışıyor" 1
fi

# Database Container kontrolü
if docker ps | grep -q "mssql"; then
    test_result "Database Container Çalışıyor" 0
else
    test_result "Database Container Çalışıyor" 1
fi

echo ""
echo -e "${YELLOW}4. Port Kontrolleri${NC}"
echo "---"

# Port 8080 (API)
if lsof -Pi :8080 -sTCP:LISTEN -t >/dev/null 2>&1 ; then
    test_result "API Port (8080) Açık" 0
else
    test_result "API Port (8080) Açık" 1
fi

# Port 1433 (Database)
if lsof -Pi :1433 -sTCP:LISTEN -t >/dev/null 2>&1 ; then
    test_result "Database Port (1433) Açık" 0
else
    test_result "Database Port (1433) Açık" 1
fi

echo ""
echo "================================"
echo -e "Sonuç: ${GREEN}$PASSED Geçti${NC}, ${RED}$FAILED Başarısız${NC}"
echo "================================"

# Exit code belirle
if [ $FAILED -eq 0 ]; then
    exit 0
else
    exit 1
fi
