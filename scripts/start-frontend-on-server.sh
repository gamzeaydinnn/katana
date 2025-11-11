#!/bin/bash

# Frontend Sunucu Başlatma Script'i
# Sunucuda npm start komutunu doğru ayarlarla çalıştırır

set -e

# Renkler
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

SERVER_USER="huseyinadm"
SERVER_IP="31.186.24.44"
SERVER_HOST="${SERVER_USER}@${SERVER_IP}"
FRONTEND_PATH="/home/huseyinadm/katana/frontend/katana-web"

echo -e "${BLUE}╔════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║      Frontend Sunucu Setup & Start            ║${NC}"
echo -e "${BLUE}╔════════════════════════════════════════════════╗${NC}"
echo ""

# 1. .env dosyasını kopyala
echo -e "${YELLOW}[1/4]${NC} .env.server dosyası sunucuya kopyalanıyor..."
scp .env.server ${SERVER_HOST}:${FRONTEND_PATH}/.env.local
echo -e "${GREEN}✓${NC} .env dosyası kopyalandı"

# 2. Dependencies kontrol
echo -e "\n${YELLOW}[2/4]${NC} Dependencies kontrol ediliyor..."
ssh ${SERVER_HOST} << 'ENDSSH'
    cd /home/huseyinadm/katana/frontend/katana-web
    
    if [ ! -d "node_modules" ]; then
        echo "⚠ node_modules bulunamadı, npm install yapılıyor..."
        npm install
    else
        echo "✓ node_modules mevcut"
    fi
ENDSSH
echo -e "${GREEN}✓${NC} Dependencies hazır"

# 3. Firewall port 3000'i aç
echo -e "\n${YELLOW}[3/4]${NC} Firewall ayarları kontrol ediliyor..."
ssh ${SERVER_HOST} << 'ENDSSH'
    if command -v ufw &> /dev/null; then
        sudo ufw allow 3000/tcp > /dev/null 2>&1 || true
        echo "✓ Port 3000 açıldı"
    else
        echo "⚠ UFW bulunamadı"
    fi
ENDSSH

# 4. Start komutu
echo -e "\n${YELLOW}[4/4]${NC} Frontend başlatma komutu hazırlandı"
echo ""
echo -e "${GREEN}Sunucuda şu komutu çalıştırın:${NC}"
echo -e "${BLUE}ssh ${SERVER_HOST}${NC}"
echo -e "${BLUE}cd ${FRONTEND_PATH}${NC}"
echo -e "${BLUE}npm start${NC}"
echo ""
echo -e "${GREEN}Ya da doğrudan:${NC}"
echo -e "${YELLOW}ssh ${SERVER_HOST} 'cd ${FRONTEND_PATH} && npm start'${NC}"
echo ""
echo -e "${BLUE}Frontend erişim adresi:${NC}"
echo -e "  ${GREEN}http://31.186.24.44:3000${NC}"
echo ""
