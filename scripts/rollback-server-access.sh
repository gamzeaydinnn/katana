#!/bin/bash

# Katana API Sunucu Erişim Rollback Script'i
# Bu script değişiklikleri geri alır

set -e

# Renkli output için
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

SERVER_USER="huseyinadm"
SERVER_IP="31.186.24.44"
SERVER_HOST="${SERVER_USER}@${SERVER_IP}"
API_PATH="/home/huseyinadm/katana/src/Katana.API"

echo -e "${BLUE}╔════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║       Katana API Rollback Script'i            ║${NC}"
echo -e "${BLUE}╔════════════════════════════════════════════════╗${NC}"
echo ""

# Yedekleri listele
echo -e "${YELLOW}Mevcut yedekler:${NC}\n"
ssh ${SERVER_HOST} << 'ENDSSH'
    echo "appsettings.json yedekleri:"
    ls -lht /home/huseyinadm/katana/src/Katana.API/appsettings.json.backup.* 2>/dev/null || echo "  Yedek bulunamadı"
    echo ""
    echo "service dosyası yedekleri:"
    sudo ls -lht /etc/systemd/system/katana-api.service.backup.* 2>/dev/null || echo "  Yedek bulunamadı"
ENDSSH

echo ""
read -p "En son yedeğe dönmek istiyor musunuz? (y/n): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo -e "${YELLOW}İşlem iptal edildi.${NC}"
    exit 0
fi

echo -e "\n${YELLOW}Geri yükleme yapılıyor...${NC}"
ssh ${SERVER_HOST} << 'ENDSSH'
    # En son yedeği bul ve geri yükle
    LATEST_CONFIG=$(ls -t /home/huseyinadm/katana/src/Katana.API/appsettings.json.backup.* 2>/dev/null | head -1)
    LATEST_SERVICE=$(sudo ls -t /etc/systemd/system/katana-api.service.backup.* 2>/dev/null | head -1)
    
    if [ -n "$LATEST_CONFIG" ]; then
        cp "$LATEST_CONFIG" /home/huseyinadm/katana/src/Katana.API/appsettings.json
        echo "✓ appsettings.json geri yüklendi: $LATEST_CONFIG"
    fi
    
    if [ -n "$LATEST_SERVICE" ]; then
        sudo cp "$LATEST_SERVICE" /etc/systemd/system/katana-api.service
        echo "✓ service dosyası geri yüklendi: $LATEST_SERVICE"
    fi
    
    # Servisi yeniden başlat
    sudo systemctl daemon-reload
    sudo systemctl restart katana-api
    echo "✓ Servis yeniden başlatıldı"
ENDSSH

echo -e "\n${GREEN}Rollback tamamlandı!${NC}"
