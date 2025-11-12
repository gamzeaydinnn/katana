# 🚀 Katana Production Deployment - Quick Start

## ✅ Yapılması Gerekenler (Sunucuda)

### 📋 Hazırlık (Tamamlandı ✓)
- ✅ Kod güncellendi: `git pull origin sare-branch`
- ✅ Backend build: `dotnet publish` ✓
- ✅ Frontend build: `npm run build` ✓

---

## 🔧 Systemd Servisleri Kurulumu

### SSH ile Bağlanın
```bash
ssh huseyinadm@31.186.24.44
```

### Adım 1: Manuel Servisleri Durdur
```bash
pkill -f "dotnet.*Katana.API.dll"
pkill -f "serve -s build"
pkill -f "react-scripts start"
sleep 2
```

### Adım 2: Systemd Servislerini Kopyala
```bash
sudo cp /home/huseyinadm/katana/scripts/systemd/katana-api.service /etc/systemd/system/
sudo cp /home/huseyinadm/katana/scripts/systemd/katana-web.service /etc/systemd/system/
sudo chmod 644 /etc/systemd/system/katana-*.service
```

### Adım 3: Systemd'yi Yenile ve Etkinleştir
```bash
sudo systemctl daemon-reload
sudo systemctl enable katana-api katana-web
```

### Adım 4: Servisleri Başlat
```bash
sudo systemctl start katana-api
sleep 3
sudo systemctl start katana-web
sleep 2
```

### Adım 5: Durum Kontrolü
```bash
sudo systemctl status katana-api
sudo systemctl status katana-web
```

**Beklenen:** Her iki servis de `active (running)` durumunda

### Adım 6: Port Kontrolü
```bash
ss -tlnp | grep -E "5055|3000"
```

**Beklenen:**
- `:5055` - API portu
- `:3000` - Frontend portu

### Adım 7: Endpoint Testleri
```bash
# API Health Check
curl http://localhost:5055/api/Health

# Frontend Ana Sayfa
curl -I http://localhost:3000

# Ürün Listesi (düzeltilen endpoint)
curl http://localhost:5055/api/Products/luca | jq '.data | length'
```

### Adım 8: Ürün Güncelleme Testi (DÜZELTİLEN)
```bash
curl -X PUT http://localhost:5055/api/Products/luca/1001 \
  -H "Content-Type: application/json" \
  -d '{
    "productCode": "SKU-1001",
    "productName": "Test Ürün Güncelleme",
    "unit": "Adet",
    "quantity": 150,
    "unitPrice": 25.50,
    "vatRate": 20
  }' -v
```

**Beklenen:** HTTP 200 OK

---

## 📊 Log Kontrolü

### Backend Logları
```bash
sudo journalctl -u katana-api -n 50
```

### Frontend Logları
```bash
sudo journalctl -u katana-web -n 50
```

### Canlı Log İzleme
```bash
sudo journalctl -u katana-api -u katana-web -f
```

---

## 🔄 Reboot Sonrası Otomatik Başlatma Testi

### Reboot Et
```bash
sudo reboot
```

### Reboot Sonrası Kontrol (SSH ile tekrar bağlandıktan sonra)
```bash
# Servis durumu
sudo systemctl status katana-api katana-web

# Port kontrolü
ss -tlnp | grep -E "5055|3000"

# API test
curl http://localhost:5055/api/Health
```

**Beklenen:** Her iki servis de otomatik başlamış olmalı ✅

---

## 🛠️ Servis Yönetim Komutları

### Hızlı Yönetim Script'i
```bash
cd /home/huseyinadm/katana
chmod +x scripts/manage-services.sh

# Durum
./scripts/manage-services.sh status

# Yeniden Başlat
./scripts/manage-services.sh restart

# Logları İzle
./scripts/manage-services.sh logs
```

### Manuel Komutlar
```bash
# Başlat
sudo systemctl start katana-api katana-web

# Durdur
sudo systemctl stop katana-api katana-web

# Yeniden Başlat
sudo systemctl restart katana-api katana-web

# Durum
sudo systemctl status katana-api katana-web

# Loglar
sudo journalctl -u katana-api -f
```

---

## 🐛 Sorun Giderme

### Servis Başlamıyorsa

**1. Log kontrolü:**
```bash
sudo journalctl -u katana-api -n 100 --no-pager
sudo journalctl -u katana-web -n 100 --no-pager
```

**2. Build kontrolü:**
```bash
ls -l /home/huseyinadm/katana/publish/Katana.API.dll
ls -l /home/huseyinadm/katana/frontend/katana-web/build/
```

**3. Port kullanımı:**
```bash
sudo ss -tlnp | grep 5055
sudo ss -tlnp | grep 3000
```

**4. Manuel başlatma testi:**
```bash
cd /home/huseyinadm/katana/publish
dotnet Katana.API.dll
# Ctrl+C ile durdur

cd /home/huseyinadm/katana/frontend/katana-web
npx serve -s build -l 3000
```

### Hata: Port Zaten Kullanımda

```bash
# Kullanılan portu bul
sudo ss -tlnp | grep 5055

# Process'i durdur
sudo pkill -f "dotnet.*Katana.API"

# Servisi yeniden başlat
sudo systemctl restart katana-api
```

---

## 📝 Güncelleme Yapıldığında

### Backend Güncelleme
```bash
cd /home/huseyinadm/katana
git pull origin sare-branch
dotnet publish src/Katana.API/Katana.API.csproj -c Release -o publish
sudo systemctl restart katana-api
sudo journalctl -u katana-api -n 30
```

### Frontend Güncelleme
```bash
cd /home/huseyinadm/katana/frontend/katana-web
npm run build
sudo systemctl restart katana-web
sudo journalctl -u katana-web -n 30
```

### Her İkisi Birden
```bash
cd /home/huseyinadm/katana
git pull origin sare-branch
dotnet publish src/Katana.API/Katana.API.csproj -c Release -o publish
cd frontend/katana-web && npm run build && cd ../..
sudo systemctl restart katana-api katana-web
./scripts/manage-services.sh status
```

---

## ✅ Kontrol Listesi

Kurulum sonrası bu kontrolleri yapın:

- [ ] `sudo systemctl is-enabled katana-api` → **enabled**
- [ ] `sudo systemctl is-enabled katana-web` → **enabled**
- [ ] `sudo systemctl is-active katana-api` → **active**
- [ ] `sudo systemctl is-active katana-web` → **active**
- [ ] `ss -tlnp | grep 5055` → **LISTEN**
- [ ] `ss -tlnp | grep 3000` → **LISTEN**
- [ ] `curl http://localhost:5055/api/Health` → **HTTP 200**
- [ ] `curl -I http://localhost:3000` → **HTTP 200**
- [ ] Tarayıcıdan: http://31.186.24.44:3000 → **Sayfa açılıyor**
- [ ] Admin Panel → Luca Ürünleri → Ürün Güncelleme → **Başarılı**

---

## 🎯 Sonuç

Kurulum tamamlandığında:

✅ **Backend:** Port 5055'te çalışıyor, otomatik başlatma aktif  
✅ **Frontend:** Port 3000'de çalışıyor, otomatik başlatma aktif  
✅ **Loglar:** `journalctl` ile erişilebilir  
✅ **Reboot:** Otomatik başlatma çalışıyor  
✅ **Ürün Güncelleme Hatası:** Düzeltildi (400/500 hataları giderildi)

---

**Oluşturulma:** 12 Kasım 2025  
**Durum:** ✅ Üretime Hazır  
**Test:** Ubuntu 24.04 LTS
