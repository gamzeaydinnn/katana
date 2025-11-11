#!/bin/bash

# Katana API Sunucu Erişim Düzeltme Script'i
# Bu script API'nin dış erişime açılmasını ve CORS ayarlarını düzenler

set -e  # Hata durumunda dur

# Renkli output için
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Sunucu bilgileri
SERVER_USER="huseyinadm"
SERVER_IP="31.186.24.44"
SERVER_HOST="${SERVER_USER}@${SERVER_IP}"
PROJECT_PATH="/home/huseyinadm/katana"
API_PATH="${PROJECT_PATH}/src/Katana.API"
SERVICE_FILE="/etc/systemd/system/katana-api.service"

echo -e "${BLUE}╔════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║   Katana API Sunucu Erişim Düzeltme Script'i  ║${NC}"
echo -e "${BLUE}╔════════════════════════════════════════════════╗${NC}"
echo ""

# 1. Sunucu bağlantı testi
echo -e "${YELLOW}[1/7]${NC} Sunucu bağlantısı test ediliyor..."
if ssh -o ConnectTimeout=5 -o StrictHostKeyChecking=no ${SERVER_HOST} 'echo "OK"' > /dev/null 2>&1; then
    echo -e "${GREEN}✓${NC} Sunucuya bağlantı başarılı"
else
    echo -e "${RED}✗${NC} Sunucuya bağlanılamıyor!"
    echo "Lütfen SSH bağlantınızı kontrol edin."
    exit 1
fi

# 2. Mevcut durumu yedekle
echo -e "\n${YELLOW}[2/7]${NC} Mevcut konfigürasyonlar yedekleniyor..."
ssh ${SERVER_HOST} << 'ENDSSH'
    # appsettings.json yedeği
    if [ -f /home/huseyinadm/katana/src/Katana.API/appsettings.json ]; then
        sudo cp /home/huseyinadm/katana/src/Katana.API/appsettings.json \
                /home/huseyinadm/katana/src/Katana.API/appsettings.json.backup.$(date +%Y%m%d_%H%M%S)
        echo "✓ appsettings.json yedeklendi"
    fi
    
    # Service dosyası yedeği
    if [ -f /etc/systemd/system/katana-api.service ]; then
        sudo cp /etc/systemd/system/katana-api.service \
                /etc/systemd/system/katana-api.service.backup.$(date +%Y%m%d_%H%M%S)
        echo "✓ katana-api.service yedeklendi"
    fi
ENDSSH
echo -e "${GREEN}✓${NC} Yedekleme tamamlandı"

# 3. Systemd service dosyasını güncelle
echo -e "\n${YELLOW}[3/7]${NC} Systemd service dosyası güncelleniyor..."
ssh ${SERVER_HOST} << 'ENDSSH'
    sudo tee /etc/systemd/system/katana-api.service > /dev/null << 'EOF'
[Unit]
Description=Katana API (.NET 8)
After=network.target

[Service]
WorkingDirectory=/home/huseyinadm/katana/src/Katana.API
ExecStart=/usr/bin/dotnet /home/huseyinadm/katana/src/Katana.API/bin/Release/net8.0/Katana.API.dll
Restart=always
RestartSec=5
User=huseyinadm
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5055
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF
    echo "✓ Service dosyası güncellendi (0.0.0.0:5055)"
ENDSSH
echo -e "${GREEN}✓${NC} Service dosyası düzenlendi"

# 4. appsettings.json'ı güncelle
echo -e "\n${YELLOW}[4/7]${NC} appsettings.json CORS ayarları güncelleniyor..."
ssh ${SERVER_HOST} << 'ENDSSH'
    cd /home/huseyinadm/katana/src/Katana.API
    
    # Mevcut dosyayı oku ve AllowedOrigins'i güncelle
    python3 << 'PYTHON'
import json
import sys

try:
    with open('appsettings.json', 'r') as f:
        config = json.load(f)
    
    # AllowedOrigins güncelle
    config['AllowedOrigins'] = [
        "http://localhost:3000",
        "https://localhost:3000",
        "http://31.186.24.44:3000",
        "https://31.186.24.44:3000",
        "http://31.186.24.44:80",
        "https://31.186.24.44:443"
    ]
    
    with open('appsettings.json', 'w') as f:
        json.dump(config, f, indent=2)
    
    print("✓ AllowedOrigins güncellendi")
except Exception as e:
    print(f"✗ Hata: {e}")
    sys.exit(1)
PYTHON
ENDSSH
echo -e "${GREEN}✓${NC} CORS ayarları güncellendi"

# 5. Firewall portunu aç
echo -e "\n${YELLOW}[5/7]${NC} Firewall kuralları kontrol ediliyor..."
ssh ${SERVER_HOST} << 'ENDSSH'
    # UFW kurulu mu kontrol et
    if command -v ufw &> /dev/null; then
        sudo ufw allow 5055/tcp > /dev/null 2>&1 || true
        sudo ufw allow 3000/tcp > /dev/null 2>&1 || true
        echo "✓ Firewall kuralları (5055, 3000) eklendi"
    else
        echo "⚠ UFW kurulu değil, firewall kontrolü atlanıyor"
    fi
ENDSSH
echo -e "${GREEN}✓${NC} Firewall kontrol edildi"

# 6. Servisi yeniden başlat
echo -e "\n${YELLOW}[6/7]${NC} Katana API servisi yeniden başlatılıyor..."
ssh ${SERVER_HOST} << 'ENDSSH'
    sudo systemctl daemon-reload
    sudo systemctl restart katana-api
    sleep 3
    
    if sudo systemctl is-active --quiet katana-api; then
        echo "✓ Katana API servisi çalışıyor"
    else
        echo "✗ Servis başlatılamadı!"
        sudo systemctl status katana-api --no-pager -l
        exit 1
    fi
ENDSSH
echo -e "${GREEN}✓${NC} Servis yeniden başlatıldı"

# 7. Bağlantı testleri
echo -e "\n${YELLOW}[7/7]${NC} Bağlantı testleri yapılıyor..."

# Localhost testi
echo -n "  - Localhost (127.0.0.1:5055): "
if ssh ${SERVER_HOST} 'curl -s -o /dev/null -w "%{http_code}" http://localhost:5055/health' | grep -q "200"; then
    echo -e "${GREEN}✓ Çalışıyor${NC}"
else
    echo -e "${RED}✗ Başarısız${NC}"
fi

# External IP testi
echo -n "  - External IP (0.0.0.0:5055): "
if ssh ${SERVER_HOST} 'curl -s -o /dev/null -w "%{http_code}" http://0.0.0.0:5055/health' | grep -q "200"; then
    echo -e "${GREEN}✓ Çalışıyor${NC}"
else
    echo -e "${RED}✗ Başarısız${NC}"
fi

# Port dinleme kontrolü
echo -e "\n${BLUE}Port dinleme durumu:${NC}"
ssh ${SERVER_HOST} 'ss -tlnp 2>/dev/null | grep 5055 || netstat -tlnp 2>/dev/null | grep 5055' | head -2

echo -e "\n${GREEN}╔════════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║            Kurulum Tamamlandı! ✓              ║${NC}"
echo -e "${GREEN}╔════════════════════════════════════════════════╗${NC}"
echo ""
echo -e "${BLUE}API Erişim Bilgileri:${NC}"
echo -e "  • Health Check: ${GREEN}http://31.186.24.44:5055/health${NC}"
echo -e "  • API Base URL: ${GREEN}http://31.186.24.44:5055/api${NC}"
echo -e "  • Swagger UI:   ${GREEN}http://31.186.24.44:5055/swagger${NC}"
echo ""
echo -e "${BLUE}Test Komutu:${NC}"
echo -e "  curl http://31.186.24.44:5055/health"
echo ""
echo -e "${YELLOW}Not:${NC} Yedeğe dönmek için:"
echo -e "  ssh ${SERVER_HOST} 'ls -la ${API_PATH}/appsettings.json.backup.*'"
echo ""
