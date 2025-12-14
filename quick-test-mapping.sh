#!/bin/bash

# Hızlı Mapping Kontrolü
# Sadece appsettings.json'daki mapping'leri ve backend log'larını kontrol eder

echo "🔍 Hızlı Mapping Kontrolü"
echo "========================="
echo ""

# Renkler
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m'

# 1. appsettings.json kontrolü
echo -e "${BLUE}📄 appsettings.json Mapping'leri:${NC}"
echo ""

echo -e "${GREEN}Kategori Mapping:${NC}"
cat src/Katana.API/appsettings.json | jq '.LucaApi.CategoryMapping'
echo ""

echo -e "${GREEN}Ölçü Birimi Mapping:${NC}"
cat src/Katana.API/appsettings.json | jq '.LucaApi.UnitMapping'
echo ""

# 2. Backend log kontrolü
echo -e "${BLUE}📋 Backend Log'ları (Son 30 satır):${NC}"
echo ""

if docker ps | grep -q katana-backend; then
  echo -e "${GREEN}✅ Backend container çalışıyor${NC}"
  echo ""
  echo "Mapping ile ilgili log'lar:"
  docker logs katana-backend 2>&1 | grep -E "(ÖLÇÜ BİRİMİ|MAPPING|Category|Unit)" | tail -30
else
  echo -e "${YELLOW}⚠️  Backend container çalışmıyor${NC}"
fi

echo ""
echo "========================="
echo "✅ Kontrol tamamlandı!"
echo ""
echo "💡 Tam test için çalıştırın:"
echo "   ${BLUE}./test-stock-card-mapping.sh${NC}"
