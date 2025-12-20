#!/bin/bash

##############################################
# Katana Systemd Services Setup Script
# Creates and enables systemd services for:
# - katana-api.service (Backend .NET API)
# - katana-web.service (Frontend React)
##############################################

set -e

# Colors
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${BLUE}╔═══════════════════════════════════════════════════════╗${NC}"
echo -e "${BLUE}║   Katana Systemd Services Installation               ║${NC}"
echo -e "${BLUE}╚═══════════════════════════════════════════════════════╝${NC}"
echo ""

# Check if running as root or with sudo
if [ "$EUID" -eq 0 ]; then 
    SUDO=""
else
    SUDO="sudo"
fi

echo -e "${YELLOW}[1/9]${NC} Checking prerequisites..."

# Check if systemd is available
if ! command -v systemctl &> /dev/null; then
    echo -e "${RED}❌ systemctl not found. This system doesn't use systemd.${NC}"
    exit 1
fi

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ dotnet not found. Please install .NET 8 SDK first.${NC}"
    exit 1
fi

# Check if node is available
if ! command -v node &> /dev/null; then
    echo -e "${RED}❌ node not found. Please install Node.js first.${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Prerequisites OK${NC}"

echo -e "${YELLOW}[2/9]${NC} Stopping existing manual processes..."

# Stop any existing manual processes
pkill -f "dotnet.*Katana.API.dll" || echo "  No dotnet processes found"
pkill -f "serve -s build -l 3000" || echo "  No serve processes found"
pkill -f "react-scripts start" || echo "  No react-scripts processes found"

sleep 2
echo -e "${GREEN}✅ Manual processes stopped${NC}"

echo -e "${YELLOW}[3/9]${NC} Verifying build directories..."

# Check if publish directory exists
if [ ! -d "/home/huseyinadm/katana/publish" ]; then
    echo -e "${RED}❌ /home/huseyinadm/katana/publish not found!${NC}"
    echo "Please run: cd /home/huseyinadm/katana && dotnet publish src/Katana.API/Katana.API.csproj -c Release -o publish"
    exit 1
fi

# Check if Katana.API.dll exists
if [ ! -f "/home/huseyinadm/katana/publish/Katana.API.dll" ]; then
    echo -e "${RED}❌ Katana.API.dll not found in publish directory!${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Backend build verified${NC}"

# Check if frontend build exists
if [ ! -d "/home/huseyinadm/katana/frontend/katana-web/build" ]; then
    echo -e "${YELLOW}⚠️  Frontend build/ directory not found${NC}"
    echo "Creating production build..."
    cd /home/huseyinadm/katana/frontend/katana-web
    npm run build
fi

echo -e "${GREEN}✅ Frontend build verified${NC}"

echo -e "${YELLOW}[4/9]${NC} Copying service files to /etc/systemd/system/..."

# Copy backend service file
$SUDO cp /home/huseyinadm/katana/scripts/systemd/katana-api.service /etc/systemd/system/
$SUDO chmod 644 /etc/systemd/system/katana-api.service

# Copy frontend service file
$SUDO cp /home/huseyinadm/katana/scripts/systemd/katana-web.service /etc/systemd/system/
$SUDO chmod 644 /etc/systemd/system/katana-web.service

echo -e "${GREEN}✅ Service files installed${NC}"

echo -e "${YELLOW}[5/9]${NC} Reloading systemd daemon..."
$SUDO systemctl daemon-reload
echo -e "${GREEN}✅ Systemd reloaded${NC}"

echo -e "${YELLOW}[6/9]${NC} Enabling services (auto-start on boot)..."
$SUDO systemctl enable katana-api.service
$SUDO systemctl enable katana-web.service
echo -e "${GREEN}✅ Services enabled${NC}"

echo -e "${YELLOW}[7/9]${NC} Starting katana-api service..."
$SUDO systemctl start katana-api.service
sleep 3

# Check if API started successfully
if $SUDO systemctl is-active --quiet katana-api.service; then
    echo -e "${GREEN}✅ katana-api service started${NC}"
else
    echo -e "${RED}❌ katana-api service failed to start${NC}"
    echo "Check logs: sudo journalctl -u katana-api -n 50 --no-pager"
    $SUDO journalctl -u katana-api -n 30 --no-pager
    exit 1
fi

echo -e "${YELLOW}[8/9]${NC} Starting katana-web service..."
$SUDO systemctl start katana-web.service
sleep 3

# Check if frontend started successfully
if $SUDO systemctl is-active --quiet katana-web.service; then
    echo -e "${GREEN}✅ katana-web service started${NC}"
else
    echo -e "${RED}❌ katana-web service failed to start${NC}"
    echo "Check logs: sudo journalctl -u katana-web -n 50 --no-pager"
    $SUDO journalctl -u katana-web -n 30 --no-pager
    exit 1
fi

echo -e "${YELLOW}[9/9]${NC} Verifying services..."

# Check API port
if ss -tlnp 2>/dev/null | grep -q ":5055"; then
    echo -e "${GREEN}✅ API listening on port 5055${NC}"
else
    echo -e "${RED}❌ API not listening on port 5055${NC}"
fi

# Check Frontend port
if ss -tlnp 2>/dev/null | grep -q ":3000"; then
    echo -e "${GREEN}✅ Frontend listening on port 3000${NC}"
else
    echo -e "${RED}❌ Frontend not listening on port 3000${NC}"
fi

echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}✅ Installation Complete!${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${YELLOW}Service Status:${NC}"
$SUDO systemctl status katana-api.service --no-pager -l | head -10
echo ""
$SUDO systemctl status katana-web.service --no-pager -l | head -10
echo ""

echo -e "${YELLOW}Useful Commands:${NC}"
echo "  Check status:      sudo systemctl status katana-api katana-web"
echo "  Stop services:     sudo systemctl stop katana-api katana-web"
echo "  Start services:    sudo systemctl start katana-api katana-web"
echo "  Restart services:  sudo systemctl restart katana-api katana-web"
echo "  View API logs:     sudo journalctl -u katana-api -f"
echo "  View Web logs:     sudo journalctl -u katana-web -f"
echo "  View all logs:     sudo journalctl -u katana-api -u katana-web -f"
echo ""

echo -e "${YELLOW}What happens on reboot:${NC}"
echo "  1. System starts"
echo "  2. katana-api.service starts automatically (port 5055)"
echo "  3. katana-web.service starts automatically (port 3000)"
echo "  4. Both services restart automatically on failure"
echo ""

echo -e "${GREEN}Access the application:${NC}"
echo "  Frontend: http://31.186.24.44:3000"
echo "  API:      http://31.186.24.44:5055/api"
echo "  Swagger:  http://31.186.24.44:5055"
echo ""

echo -e "${YELLOW}Testing reboot behavior:${NC}"
echo "  sudo reboot"
echo "  # After reboot, SSH back and run:"
echo "  sudo systemctl status katana-api katana-web"
echo ""
