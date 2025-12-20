#!/bin/bash

##############################################
# Katana Systemd Services - Manual Setup Steps
# Use these commands when SSH sudo doesn't work
##############################################

cat << 'EOF'

╔═══════════════════════════════════════════════════════╗
║   Katana Systemd Services - Manual Installation      ║
╚═══════════════════════════════════════════════════════╝

Follow these steps on the production server:

STEP 1: SSH to Server
───────────────────────
ssh huseyinadm@31.186.24.44

STEP 2: Stop Manual Processes
──────────────────────────────
pkill -f "dotnet.*Katana.API.dll"
pkill -f "serve -s build -l 3000"
pkill -f "react-scripts start"
sleep 2

STEP 3: Verify Builds
─────────────────────
# Backend check
ls -l /home/huseyinadm/katana/publish/Katana.API.dll

# Frontend check
ls -l /home/huseyinadm/katana/frontend/katana-web/build/

STEP 4: Install Service Files
──────────────────────────────
sudo cp /home/huseyinadm/katana/scripts/systemd/katana-api.service /etc/systemd/system/
sudo cp /home/huseyinadm/katana/scripts/systemd/katana-web.service /etc/systemd/system/
sudo chmod 644 /etc/systemd/system/katana-api.service
sudo chmod 644 /etc/systemd/system/katana-web.service

STEP 5: Reload Systemd
──────────────────────
sudo systemctl daemon-reload

STEP 6: Enable Services (Auto-start)
─────────────────────────────────────
sudo systemctl enable katana-api.service
sudo systemctl enable katana-web.service

STEP 7: Start Backend
─────────────────────
sudo systemctl start katana-api.service
sleep 3
sudo systemctl status katana-api.service

STEP 8: Start Frontend
──────────────────────
sudo systemctl start katana-web.service
sleep 3
sudo systemctl status katana-web.service

STEP 9: Verify Ports
────────────────────
ss -tlnp | grep -E "5055|3000"

# Should show:
# LISTEN 0 511 0.0.0.0:5055 (API)
# LISTEN 0 511 0.0.0.0:3000 (Frontend)

STEP 10: Check Logs
───────────────────
# API logs
sudo journalctl -u katana-api -n 30

# Frontend logs
sudo journalctl -u katana-web -n 30

STEP 11: Test Endpoints
────────────────────────
# API health check
curl http://localhost:5055/api/Health

# Frontend
curl -I http://localhost:3000

VERIFICATION CHECKLIST:
═══════════════════════
✓ Services enabled:  systemctl is-enabled katana-api katana-web
✓ Services active:   systemctl is-active katana-api katana-web
✓ Port 5055 open:    ss -tlnp | grep 5055
✓ Port 3000 open:    ss -tlnp | grep 3000
✓ Logs working:      journalctl -u katana-api -n 5

USEFUL COMMANDS:
════════════════
# Check status
sudo systemctl status katana-api katana-web

# Restart services
sudo systemctl restart katana-api katana-web

# View logs
sudo journalctl -u katana-api -u katana-web -f

# Quick management
cd /home/huseyinadm/katana
./scripts/manage-services.sh status
./scripts/manage-services.sh restart
./scripts/manage-services.sh logs

REBOOT TEST:
════════════
sudo reboot

# After reboot, SSH back and check:
sudo systemctl status katana-api katana-web
ss -tlnp | grep -E "5055|3000"

Both services should start automatically!

EOF
