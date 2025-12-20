#!/bin/bash

##############################################
# Quick Service Management Script
# Manage Katana services easily
##############################################

BLUE='\033[0;34m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

usage() {
    echo -e "${BLUE}Katana Service Manager${NC}"
    echo ""
    echo "Usage: $0 {status|start|stop|restart|logs|enable|disable}"
    echo ""
    echo "Commands:"
    echo "  status    - Show service status"
    echo "  start     - Start both services"
    echo "  stop      - Stop both services"
    echo "  restart   - Restart both services"
    echo "  logs      - Follow logs (Ctrl+C to exit)"
    echo "  enable    - Enable auto-start on boot"
    echo "  disable   - Disable auto-start on boot"
    echo ""
    echo "Examples:"
    echo "  $0 status"
    echo "  $0 restart"
    echo "  $0 logs"
}

check_sudo() {
    if [ "$EUID" -ne 0 ]; then 
        SUDO="sudo"
    else
        SUDO=""
    fi
}

status_services() {
    echo -e "${YELLOW}Checking service status...${NC}"
    echo ""
    $SUDO systemctl status katana-api.service --no-pager -l | head -15
    echo ""
    $SUDO systemctl status katana-web.service --no-pager -l | head -15
    echo ""
    
    # Check ports
    echo -e "${YELLOW}Port Status:${NC}"
    if ss -tlnp 2>/dev/null | grep -q ":5055"; then
        echo -e "${GREEN}✅ API listening on port 5055${NC}"
    else
        echo -e "${RED}❌ API not listening on port 5055${NC}"
    fi
    
    if ss -tlnp 2>/dev/null | grep -q ":3000"; then
        echo -e "${GREEN}✅ Frontend listening on port 3000${NC}"
    else
        echo -e "${RED}❌ Frontend not listening on port 3000${NC}"
    fi
}

start_services() {
    echo -e "${YELLOW}Starting services...${NC}"
    $SUDO systemctl start katana-api.service
    sleep 2
    $SUDO systemctl start katana-web.service
    sleep 2
    echo -e "${GREEN}✅ Services started${NC}"
    status_services
}

stop_services() {
    echo -e "${YELLOW}Stopping services...${NC}"
    $SUDO systemctl stop katana-web.service
    $SUDO systemctl stop katana-api.service
    echo -e "${GREEN}✅ Services stopped${NC}"
}

restart_services() {
    echo -e "${YELLOW}Restarting services...${NC}"
    $SUDO systemctl restart katana-api.service
    sleep 2
    $SUDO systemctl restart katana-web.service
    sleep 2
    echo -e "${GREEN}✅ Services restarted${NC}"
    status_services
}

follow_logs() {
    echo -e "${YELLOW}Following logs... (Press Ctrl+C to exit)${NC}"
    echo ""
    $SUDO journalctl -u katana-api -u katana-web -f
}

enable_services() {
    echo -e "${YELLOW}Enabling auto-start on boot...${NC}"
    $SUDO systemctl enable katana-api.service
    $SUDO systemctl enable katana-web.service
    echo -e "${GREEN}✅ Services enabled${NC}"
}

disable_services() {
    echo -e "${YELLOW}Disabling auto-start on boot...${NC}"
    $SUDO systemctl disable katana-api.service
    $SUDO systemctl disable katana-web.service
    echo -e "${GREEN}✅ Services disabled${NC}"
}

# Main
check_sudo

if [ $# -eq 0 ]; then
    usage
    exit 0
fi

case "$1" in
    status)
        status_services
        ;;
    start)
        start_services
        ;;
    stop)
        stop_services
        ;;
    restart)
        restart_services
        ;;
    logs)
        follow_logs
        ;;
    enable)
        enable_services
        ;;
    disable)
        disable_services
        ;;
    *)
        usage
        exit 1
        ;;
esac
