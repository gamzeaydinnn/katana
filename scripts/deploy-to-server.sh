#!/bin/bash

# Katana API Hızlı Deployment Script'i
# Yerel değişiklikleri sunucuya deploy eder

set -e

# Renkli output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

SERVER_USER="huseyinadm"
SERVER_IP="31.186.24.44"
SERVER_HOST="${SERVER_USER}@${SERVER_IP}"
PROJECT_PATH="/home/huseyinadm/katana"
LOCAL_PATH="/Users/dilarasara/katana"

echo -e "${BLUE}╔════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║       Katana API Deployment Script'i          ║${NC}"
echo -e "${BLUE}╔════════════════════════════════════════════════╗${NC}"
echo ""

# Git durumunu kontrol et
echo -e "${YELLOW}[1/6]${NC} Git durumu kontrol ediliyor..."
cd ${LOCAL_PATH}

if [[ -n $(git status -s) ]]; then
    echo -e "${RED}⚠${NC} Commit edilmemiş değişiklikler var:"
    git status -s
    echo ""
    read -p "Devam etmek istiyor musunuz? (y/n): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 0
    fi
fi

CURRENT_BRANCH=$(git branch --show-current)
echo -e "${GREEN}✓${NC} Mevcut branch: ${BLUE}${CURRENT_BRANCH}${NC}"

# Sunucuda git pull yap
echo -e "\n${YELLOW}[2/6]${NC} Sunucuda kod güncelleniyor..."
ssh ${SERVER_HOST} << ENDSSH
    cd ${PROJECT_PATH}
    
    # Git durumunu kaydet
    git stash
    
    # Güncel kodu çek
    git fetch origin
    git checkout ${CURRENT_BRANCH}
    git pull origin ${CURRENT_BRANCH}
    
    # Stash'i geri yükle
    git stash pop 2>/dev/null || true
    
    echo "✓ Kod güncellendi (${CURRENT_BRANCH})"
ENDSSH
echo -e "${GREEN}✓${NC} Kod güncelleme tamamlandı"

# Backend build
echo -e "\n${YELLOW}[3/6]${NC} Backend build ediliyor..."
ssh ${SERVER_HOST} << 'ENDSSH'
    cd /home/huseyinadm/katana
    
    dotnet restore
    dotnet build src/Katana.API/Katana.API.csproj -c Release
    dotnet publish src/Katana.API/Katana.API.csproj -c Release -o src/Katana.API/bin/Release/net8.0/publish
    
    echo "✓ Backend build tamamlandı"
ENDSSH
echo -e "${GREEN}✓${NC} Build başarılı"

# Database migration
echo -e "\n${YELLOW}[4/6]${NC} Database migration kontrol ediliyor..."
ssh ${SERVER_HOST} << 'ENDSSH'
    cd /home/huseyinadm/katana
    
    # Migration listesini al
    PENDING=$(dotnet ef migrations list --project src/Katana.Data --startup-project src/Katana.API 2>/dev/null | grep -c "Pending" || echo "0")
    
    if [ "$PENDING" -gt "0" ]; then
        echo "⚠ $PENDING bekleyen migration bulundu"
        dotnet ef database update --project src/Katana.Data --startup-project src/Katana.API
        echo "✓ Migration uygulandı"
    else
        echo "✓ Migration gerekmedi"
    fi
ENDSSH
echo -e "${GREEN}✓${NC} Database güncel"

# Servisi yeniden başlat
echo -e "\n${YELLOW}[5/6]${NC} API servisi yeniden başlatılıyor..."
ssh ${SERVER_HOST} << 'ENDSSH'
    sudo systemctl restart katana-api
    sleep 3
    
    if sudo systemctl is-active --quiet katana-api; then
        echo "✓ Servis başarıyla başlatıldı"
    else
        echo "✗ Servis başlatılamadı!"
        sudo systemctl status katana-api --no-pager -l | tail -20
        exit 1
    fi
ENDSSH
echo -e "${GREEN}✓${NC} Servis çalışıyor"

# Health check
echo -e "\n${YELLOW}[6/6]${NC} Health check yapılıyor..."
sleep 2

HTTP_CODE=$(ssh ${SERVER_HOST} 'curl -s -o /dev/null -w "%{http_code}" http://localhost:5055/health')
if [ "$HTTP_CODE" = "200" ]; then
    echo -e "${GREEN}✓${NC} API health check başarılı (HTTP $HTTP_CODE)"
else
    echo -e "${RED}✗${NC} API health check başarısız (HTTP $HTTP_CODE)"
fi

# Log kontrolü
echo -e "\n${BLUE}Son 10 log:${NC}"
ssh ${SERVER_HOST} 'sudo journalctl -u katana-api -n 10 --no-pager'

echo -e "\n${GREEN}╔════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║         Deployment Tamamlandı! ✓              ║${NC}"
echo -e "${GREEN}╔════════════════════════════════════════════════╗${NC}"
echo ""
echo -e "${BLUE}API Erişim:${NC}"
echo -e "  • Health: ${GREEN}http://31.186.24.44:5055/health${NC}"
echo -e "  • Swagger: ${GREEN}http://31.186.24.44:5055/swagger${NC}"
echo ""
echo -e "${BLUE}Kontrol Komutları:${NC}"
echo -e "  • Servis durumu: ${YELLOW}ssh ${SERVER_HOST} 'sudo systemctl status katana-api'${NC}"
echo -e "  • Canlı loglar: ${YELLOW}ssh ${SERVER_HOST} 'sudo journalctl -u katana-api -f'${NC}"
echo ""
