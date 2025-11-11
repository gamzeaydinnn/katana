#!/bin/bash
# Production frontend deploy & restart helper for Katana
# Amacı: Mevcut mimariyi BOZMADAN (sadece statik build + serve) doğru API URL ile
# hızlıca yeniden derleyip 3000 portunda sunmak. Dev server (npm start) yerine
# production bundle + "serve" kullanılır.

set -euo pipefail

# === Renkler ===
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

SERVER_USER="huseyinadm"
SERVER_IP="31.186.24.44"
SERVER_HOST="${SERVER_USER}@${SERVER_IP}"
FRONTEND_PATH="/home/huseyinadm/katana/frontend/katana-web"
API_URL_DEFAULT="http://31.186.24.44:5055/api"

API_URL="${REACT_APP_API_URL:-$API_URL_DEFAULT}"
BUILD_MODE="production"
SKIP_BUILD=0
NO_RESTART=0

usage() {
    cat <<EOF
Katana Frontend Deploy Script

Kullanım:
    $(basename "$0") [--api <url>] [--skip-build] [--no-restart]

Seçenekler:
    --api <url>       : REACT_APP_API_URL değeri (varsayılan: $API_URL_DEFAULT)
    --skip-build      : Mevcut build'i kullan, yeniden derleme yapma
    --no-restart      : Var olan serve sürecini bırak, sadece durumu göster
    -h|--help         : Bu mesaj

Örnek:
    $0 --api http://31.186.24.44:5055/api
    $0 --skip-build --api http://31.186.24.44:5055/api
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --api)
            API_URL="$2"; shift 2;;
        --skip-build)
            SKIP_BUILD=1; shift;;
        --no-restart)
            NO_RESTART=1; shift;;
        -h|--help)
            usage; exit 0;;
        *) echo -e "${RED}Bilinmeyen argüman: $1${NC}"; usage; exit 1;;
    esac
done

echo -e "${BLUE}╔══════════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║   Katana Frontend Production Deploy & Restart (serve)    ║${NC}"
echo -e "${BLUE}╚══════════════════════════════════════════════════════════╝${NC}"
echo -e "API URL          : ${YELLOW}${API_URL}${NC}"
echo -e "Build Mode       : ${YELLOW}${BUILD_MODE}${NC}"
echo -e "Skip Build       : ${YELLOW}${SKIP_BUILD}${NC}"
echo -e "Restart Serve    : ${YELLOW}$([ $NO_RESTART -eq 0 ] && echo Evet || echo Hayır)${NC}"
echo ""

echo -e "${YELLOW}[1/6]${NC} Remote ortam değişkeni yazılıyor (.env.production)"
ssh -o StrictHostKeyChecking=no ${SERVER_HOST} "bash -s" <<ENDENV
set -e
cd ${FRONTEND_PATH}
cat > .env.production <<EOFENV
REACT_APP_API_URL=${API_URL}
PORT=3000
HOST=0.0.0.0
DANGEROUSLY_DISABLE_HOST_CHECK=true
WDS_SOCKET_HOST=31.186.24.44
WDS_SOCKET_PORT=3000
EOFENV
echo "✓ .env.production güncellendi"
ENDENV

echo -e "${YELLOW}[2/6]${NC} Bağımlılıklar kontrol ediliyor"
ssh -o StrictHostKeyChecking=no ${SERVER_HOST} "bash -s" <<'ENDDEPS'
set -e
cd /home/huseyinadm/katana/frontend/katana-web
if [ ! -d node_modules ]; then
    echo "node_modules yok → npm install"
    npm install --no-audit --no-fund
else
    echo "node_modules mevcut"
fi
ENDDEPS

if [ ${SKIP_BUILD} -eq 0 ]; then
    echo -e "${YELLOW}[3/6]${NC} Production build alınıyor (REACT_APP_API_URL gömülecek)"
    ssh -o StrictHostKeyChecking=no ${SERVER_HOST} "bash -s" <<'ENDBUILD'
set -e
cd /home/huseyinadm/katana/frontend/katana-web
rm -rf build
NODE_ENV=production npm run build
echo "✓ Build tamamlandı"
ENDBUILD
else
    echo -e "${YELLOW}Build atlandı (--skip-build).${NC}"
fi

echo -e "${YELLOW}[4/6]${NC} Mevcut serve süreci var mı kontrol ediliyor"
ssh -o StrictHostKeyChecking=no ${SERVER_HOST} "bash -s" <<'ENDKILL'
set -e
EXISTING=
if pgrep -f "serve -s build -l 3000" >/dev/null 2>&1; then
    EXISTING=1
    echo "Önceki serve süreci bulundu → durduruluyor"
    pkill -f "serve -s build -l 3000" || true
    sleep 1
else
    echo "Önceki serve süreci yok"
fi
ENDKILL

if [ ${NO_RESTART} -eq 0 ]; then
    echo -e "${YELLOW}[5/6]${NC} Yeni serve süreci başlatılıyor"
    ssh -o StrictHostKeyChecking=no ${SERVER_HOST} "bash -s" <<'ENDSTART'
set -e
cd /home/huseyinadm/katana/frontend/katana-web
nohup npx serve -s build -l 3000 > /tmp/katana-frontend.log 2>&1 &
sleep 2
if ss -tlnp | grep -q ":3000"; then
    echo "✓ 3000 portu dinlemede"
else
    echo "✗ 3000 portu dinlemiyor"
fi
ENDSTART
else
    echo -e "${YELLOW}Serve yeniden başlatılmadı (--no-restart).${NC}"
fi

echo -e "${YELLOW}[6/6]${NC} Sağlık & endpoint doğrulaması (lokal + opsiyonel domain)"
ssh -o StrictHostKeyChecking=no ${SERVER_HOST} "bash -s" <<'ENDCHECK'
set -e
echo "→ Backend /api/health (lowercase alias)"
curl -fsS http://127.0.0.1:5055/api/health || echo "Health endpoint erişilemedi"
echo "→ Backend /api/Products/luca (ilk 200 byte)"
curl -fsS http://127.0.0.1:5055/api/Products/luca | head -c 200 || echo "Products/luca erişilemedi"
echo "→ Backend /api/Luca/products (alias) (ilk 200 byte)"
curl -fsS http://127.0.0.1:5055/api/Luca/products | head -c 200 || echo "Luca/products erişilemedi"
echo "→ Backend /api/adminpanel/failed-records (ilk satır)"
curl -fsS "http://127.0.0.1:5055/api/adminpanel/failed-records?page=1&pageSize=1" | head -n 1 || echo "adminpanel/failed-records erişilemedi"
echo "→ Frontend ana sayfa (ilk satır)"
curl -fsS http://127.0.0.1:3000 | head -n 1 || echo "Frontend açılmıyor"
ENDCHECK

# Opsiyonel domain doğrulaması (Nginx reverse proxy). API_URL bir https domain ise test et.
if [[ "${API_URL}" == https://* ]]; then
    DOMAIN_BASE="${API_URL%%/api*}" # https://bfmmrp.com gibi
    echo -e "${YELLOW}→ Domain üzerinden Nginx proxy doğrulanıyor: ${DOMAIN_BASE}/api/...${NC}"
    {
        curl -Is "${DOMAIN_BASE}/api/health" | head -n1; \
        curl -Is "${DOMAIN_BASE}/api/Products/luca" | head -n1; \
        curl -Is "${DOMAIN_BASE}/api/adminpanel/failed-records" | head -n1
    } || echo -e "${RED}Domain üzerinden /api istekleri başarısız. Nginx location /api/ konfigürasyonunu kontrol edin.${NC}"
fi

echo ""
echo -e "${GREEN}Tamamlandı. Tarayıcıdan erişim: http://31.186.24.44:3000${NC}"
echo -e "Log: ssh ${SERVER_HOST} 'tail -n 50 /tmp/katana-frontend.log'"
echo -e "If 404 continues for /Products/luca → cache temizleyip hard refresh yapın (Ctrl+Shift+R)."
