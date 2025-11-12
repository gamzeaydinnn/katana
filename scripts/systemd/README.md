# Katana Systemd Services - Production Setup Guide

## 📋 Overview

This setup converts Katana's manual process management to **systemd services** for:
- ✅ Automatic startup on system boot
- ✅ Automatic restart on failure
- ✅ Centralized logging via `journalctl`
- ✅ Easy service management with standard commands

**Services:**
- `katana-api.service` - .NET 8 Backend API (port 5055)
- `katana-web.service` - React Frontend (port 3000)

---

## 🚀 Installation

### Step 1: Prepare Files

Ensure the service files exist:
```bash
cd /home/huseyinadm/katana
ls -l scripts/systemd/
# Should show:
#   katana-api.service
#   katana-web.service
```

### Step 2: Run Setup Script

```bash
cd /home/huseyinadm/katana
chmod +x scripts/setup-systemd-services.sh
sudo scripts/setup-systemd-services.sh
```

**What this does:**
1. Stops existing manual processes
2. Verifies backend and frontend builds
3. Copies service files to `/etc/systemd/system/`
4. Enables services for auto-start
5. Starts both services
6. Verifies ports 5055 and 3000 are listening

---

## 🎯 Service Management

### Using the Management Script (Recommended)

```bash
cd /home/huseyinadm/katana
chmod +x scripts/manage-services.sh

# Check status
./scripts/manage-services.sh status

# Start services
./scripts/manage-services.sh start

# Stop services
./scripts/manage-services.sh stop

# Restart services
./scripts/manage-services.sh restart

# Follow logs in real-time
./scripts/manage-services.sh logs
```

### Using Systemctl Directly

```bash
# Check status
sudo systemctl status katana-api
sudo systemctl status katana-web

# Start services
sudo systemctl start katana-api
sudo systemctl start katana-web

# Stop services
sudo systemctl stop katana-api
sudo systemctl stop katana-web

# Restart services
sudo systemctl restart katana-api
sudo systemctl restart katana-web

# Enable/disable auto-start
sudo systemctl enable katana-api katana-web
sudo systemctl disable katana-api katana-web
```

---

## 📊 Viewing Logs

### Backend Logs

```bash
# Last 50 lines
sudo journalctl -u katana-api -n 50

# Follow logs in real-time
sudo journalctl -u katana-api -f

# Today's logs
sudo journalctl -u katana-api --since today

# Last hour
sudo journalctl -u katana-api --since "1 hour ago"
```

### Frontend Logs

```bash
# Last 50 lines
sudo journalctl -u katana-web -n 50

# Follow logs in real-time
sudo journalctl -u katana-web -f
```

### Both Services Together

```bash
# Follow both services
sudo journalctl -u katana-api -u katana-web -f

# Last 100 lines from both
sudo journalctl -u katana-api -u katana-web -n 100
```

---

## 🔄 Reboot Behavior

### What Happens on System Reboot:

1. **System starts up**
2. **Network becomes available** → triggers `After=network.target`
3. **katana-api.service starts automatically**
   - Working directory: `/home/huseyinadm/katana/publish`
   - Command: `dotnet Katana.API.dll`
   - Port: 5055
4. **katana-web.service starts automatically** (after API)
   - Working directory: `/home/huseyinadm/katana/frontend/katana-web`
   - Command: `npx serve -s build -l 3000`
   - Port: 3000
5. **Both services are running** without manual intervention

### Testing Reboot

```bash
# Reboot the server
sudo reboot

# After reboot, SSH back in and verify
ssh huseyinadm@31.186.24.44

# Check if services auto-started
sudo systemctl status katana-api katana-web

# Verify ports
ss -tlnp | grep -E "5055|3000"

# Should show both ports listening
```

---

## 🛠️ Updating the Application

### Backend Update

```bash
cd /home/huseyinadm/katana

# Pull latest code
git pull origin sare-branch

# Build new version
dotnet publish src/Katana.API/Katana.API.csproj -c Release -o publish

# Restart service
sudo systemctl restart katana-api

# Verify
sudo systemctl status katana-api
sudo journalctl -u katana-api -n 30
```

### Frontend Update

```bash
cd /home/huseyinadm/katana

# Pull latest code
git pull origin sare-branch

# Rebuild frontend
cd frontend/katana-web
npm run build

# Restart service
sudo systemctl restart katana-web

# Verify
sudo systemctl status katana-web
sudo journalctl -u katana-web -n 30
```

### Update Both

```bash
cd /home/huseyinadm/katana

# Pull code
git pull origin sare-branch

# Build backend
dotnet publish src/Katana.API/Katana.API.csproj -c Release -o publish

# Build frontend
cd frontend/katana-web
npm run build
cd ../..

# Restart both services
sudo systemctl restart katana-api katana-web

# Verify
./scripts/manage-services.sh status
```

---

## 🚨 Troubleshooting

### Service Won't Start

**Check service status:**
```bash
sudo systemctl status katana-api
sudo systemctl status katana-web
```

**View detailed logs:**
```bash
sudo journalctl -u katana-api -n 100 --no-pager
sudo journalctl -u katana-web -n 100 --no-pager
```

**Common issues:**

1. **Port already in use:**
   ```bash
   # Check what's using the port
   sudo ss -tlnp | grep 5055
   sudo ss -tlnp | grep 3000
   
   # Kill the process if needed
   sudo pkill -f "dotnet.*Katana.API"
   sudo pkill -f "serve -s build"
   ```

2. **Missing build files:**
   ```bash
   # Backend
   ls -l /home/huseyinadm/katana/publish/Katana.API.dll
   
   # Frontend
   ls -l /home/huseyinadm/katana/frontend/katana-web/build/
   ```

3. **Permission issues:**
   ```bash
   # Ensure huseyinadm owns the files
   sudo chown -R huseyinadm:huseyinadm /home/huseyinadm/katana
   ```

### Service Crashes Immediately

**Check for syntax errors in service files:**
```bash
sudo systemd-analyze verify /etc/systemd/system/katana-api.service
sudo systemd-analyze verify /etc/systemd/system/katana-web.service
```

**View boot logs:**
```bash
sudo journalctl -b -u katana-api
sudo journalctl -b -u katana-web
```

### Logs Not Showing

**Ensure services are using journal:**
```bash
# Service files should have:
StandardOutput=journal
StandardError=journal

# Verify with:
cat /etc/systemd/system/katana-api.service | grep journal
```

---

## 📁 File Locations

### Service Files
- `/etc/systemd/system/katana-api.service` - Backend service definition
- `/etc/systemd/system/katana-web.service` - Frontend service definition

### Application Files
- `/home/huseyinadm/katana/publish/` - Backend executable location
- `/home/huseyinadm/katana/frontend/katana-web/build/` - Frontend static files

### Scripts
- `/home/huseyinadm/katana/scripts/setup-systemd-services.sh` - Initial setup
- `/home/huseyinadm/katana/scripts/manage-services.sh` - Daily management
- `/home/huseyinadm/katana/scripts/systemd/` - Service templates

---

## ✅ Verification Checklist

After setup, verify:

- [ ] Services enabled: `systemctl is-enabled katana-api katana-web`
- [ ] Services active: `systemctl is-active katana-api katana-web`
- [ ] Port 5055 listening: `ss -tlnp | grep 5055`
- [ ] Port 3000 listening: `ss -tlnp | grep 3000`
- [ ] API health: `curl http://localhost:5055/api/Health`
- [ ] Frontend accessible: `curl http://localhost:3000`
- [ ] Logs visible: `journalctl -u katana-api -n 10`
- [ ] Auto-start configured: Check service files have `WantedBy=multi-user.target`

---

## 🔐 Security Notes

**Service Hardening Features:**
- Services run as non-root user (`huseyinadm`)
- `NoNewPrivileges=true` - prevents privilege escalation
- `PrivateTmp=true` - isolates /tmp directory
- Resource limits configured (`LimitNOFILE`)
- Automatic restart on failure with delay

**Additional Security (Optional):**

To restrict service access further, edit service files and add:
```ini
[Service]
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/home/huseyinadm/katana
```

Then reload:
```bash
sudo systemctl daemon-reload
sudo systemctl restart katana-api katana-web
```

---

## 📞 Support Commands Reference

```bash
# Quick status check
./scripts/manage-services.sh status

# Full diagnostic
sudo systemctl status katana-api katana-web --no-pager -l
sudo journalctl -u katana-api -u katana-web -n 50
ss -tlnp | grep -E "5055|3000"

# Restart everything
sudo systemctl restart katana-api katana-web

# Watch logs live
sudo journalctl -u katana-api -u katana-web -f

# Service performance
systemd-analyze blame | grep katana
```

---

**Created:** 12 Kasım 2025  
**Status:** ✅ Production Ready  
**Tested:** Ubuntu 24.04 LTS  
**Architecture:** Systemd-based service management
