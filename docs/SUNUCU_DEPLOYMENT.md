# 🚀 Sunucu Deployment Kılavuzu

Bu doküman Katana projesinin `31.186.24.44` sunucusunda deployment ve sorun giderme işlemlerini içerir.

## 📋 İçindekiler

1. [Sunucu Bilgileri](#sunucu-bilgileri)
2. [Tespit Edilen Sorunlar](#tespit-edilen-sorunlar)
3. [Otomatik Scriptler](#otomatik-scriptler)
4. [Manuel Çözümler](#manuel-çözümler)
5. [Frontend Setup](#frontend-setup)
6. [Troubleshooting](#troubleshooting)

---

## 🖥 Sunucu Bilgileri

**SSH Erişim:**
```bash
ssh huseyinadm@31.186.24.44
```
- **Kullanıcı:** huseyinadm
- **Şifre:** BmuirNsUcF
- **Proje Yolu:** `/home/huseyinadm/katana`

**Servisler:**
- **Backend API:** Port 5055 (systemd service: `katana-api`)
- **Frontend:** Port 3000 (npm start)
- **Database:** SQL Server (Docker container: `katana-sql`, Port 1433)

---

## 🔍 Tespit Edilen Sorunlar

### 1. Backend API Sadece Localhost'ta Dinliyor ❌

**Sorun:**
```
ASPNETCORE_URLS=http://localhost:5055  # Sadece 127.0.0.1
```

**Çözüm:**
```
ASPNETCORE_URLS=http://0.0.0.0:5055   # Tüm network interface'ler
```

### 2. CORS Konfigürasyonu Eksik ❌

**Sorun:**
```json
"AllowedOrigins": ["http://localhost:3000"]  // Sunucu IP'si yok
```

**Çözüm:**
```json
"AllowedOrigins": [
  "http://localhost:3000",
  "http://31.186.24.44:3000",
  "https://31.186.24.44:3000"
]
```

### 3. Frontend allowedHosts Hatası ❌

**Sorun:**
```
Invalid options object. Dev Server has been initialized using an options 
object that does not match the API schema.
 - options.allowedHosts[0] should be a non-empty string.
```

**Çözüm:** `.env.local` dosyasına doğru ayarları eklemek.

---

## ⚡ Otomatik Scriptler

### 1. 🔧 Backend Erişim Düzeltme

```bash
./scripts/fix-server-access.sh
```

**Ne yapar:**
- ✅ Systemd service dosyasını günceller (0.0.0.0:5055)
- ✅ CORS ayarlarını düzenler
- ✅ Firewall portlarını açar
- ✅ Servisi yeniden başlatır
- ✅ Health check yapar

**Çıktı:**
```
╔════════════════════════════════════════════════╗
║   Katana API Sunucu Erişim Düzeltme Script'i  ║
╔════════════════════════════════════════════════╗

[1/7] Sunucu bağlantısı test ediliyor...
✓ Sunucuya bağlantı başarılı

[2/7] Mevcut konfigürasyonlar yedekleniyor...
✓ Yedekleme tamamlandı

...

╔════════════════════════════════════════════════╗
║            Kurulum Tamamlandı! ✓              ║
╔════════════════════════════════════════════════╗

API Erişim Bilgileri:
  • Health Check: http://31.186.24.44:5055/health
  • API Base URL: http://31.186.24.44:5055/api
  • Swagger UI:   http://31.186.24.44:5055/swagger
```

---

### 2. 🔄 Rollback (Geri Alma)

```bash
./scripts/rollback-server-access.sh
```

**Ne yapar:**
- ✅ Mevcut yedekleri listeler
- ✅ En son yedeğe geri döner
- ✅ Servisi yeniden başlatır

---

### 3. 📦 Deployment

```bash
./scripts/deploy-to-server.sh
```

**Ne yapar:**
- ✅ Git durumunu kontrol eder
- ✅ Sunucuda `git pull` yapar
- ✅ Backend build eder
- ✅ Database migration uygular
- ✅ Servisi yeniden başlatır
- ✅ Health check yapar

**Çıktı:**
```
╔════════════════════════════════════════════════╗
║       Katana API Deployment Script'i          ║
╔════════════════════════════════════════════════╗

[1/6] Git durumu kontrol ediliyor...
✓ Mevcut branch: sare-branch

[2/6] Sunucuda kod güncelleniyor...
✓ Kod güncelleme tamamlandı

[3/6] Backend build ediliyor...
✓ Build başarılı

[4/6] Database migration kontrol ediliyor...
✓ Database güncel

[5/6] API servisi yeniden başlatılıyor...
✓ Servis çalışıyor

[6/6] Health check yapılıyor...
✓ API health check başarılı (HTTP 200)
```

---

### 4. 🌐 Frontend Setup

```bash
./scripts/start-frontend-on-server.sh
```

**Ne yapar:**
- ✅ `.env.server` dosyasını sunucuya kopyalar
- ✅ npm dependencies kontrol eder
- ✅ Port 3000'i açar
- ✅ Start komutunu gösterir

---

## 🛠 Manuel Çözümler

### Backend API Erişim Düzeltme (Manuel)

1. **Systemd Service Dosyasını Düzenle:**

```bash
ssh huseyinadm@31.186.24.44
sudo nano /etc/systemd/system/katana-api.service
```

Şu satırı değiştir:
```ini
Environment=ASPNETCORE_URLS=http://0.0.0.0:5055
```

2. **appsettings.json Düzenle:**

```bash
cd /home/huseyinadm/katana/src/Katana.API
nano appsettings.json
```

AllowedOrigins'i güncelle:
```json
"AllowedOrigins": [
  "http://localhost:3000",
  "https://localhost:3000",
  "http://31.186.24.44:3000",
  "https://31.186.24.44:3000"
]
```

3. **Firewall Aç:**

```bash
sudo ufw allow 5055/tcp
sudo ufw allow 3000/tcp
```

4. **Servisi Yeniden Başlat:**

```bash
sudo systemctl daemon-reload
sudo systemctl restart katana-api
sudo systemctl status katana-api
```

5. **Test Et:**

```bash
# Localhost test
curl http://localhost:5055/health

# External test
curl http://31.186.24.44:5055/health
```

---

## 🌐 Frontend Setup

### 1. Sunucuda .env Ayarları

`.env.local` dosyası oluştur:
```bash
cd /home/huseyinadm/katana/frontend/katana-web
nano .env.local
```

İçeriği:
```bash
# Backend API URL
REACT_APP_API_URL=http://31.186.24.44:5055/api

# Host ayarı
HOST=0.0.0.0
PORT=3000

# Webpack Dev Server ayarları
DANGEROUSLY_DISABLE_HOST_CHECK=true
WDS_SOCKET_HOST=31.186.24.44
WDS_SOCKET_PORT=3000
```

### 2. Dependencies Kur

```bash
npm install
```

### 3. Frontend'i Başlat

**Arka planda çalıştırma (önerilen):**
```bash
nohup npm start > frontend.log 2>&1 &
```

**Normal çalıştırma:**
```bash
npm start
```

### 4. Erişim

Frontend'e şu adresten erişebilirsiniz:
```
http://31.186.24.44:3000
```

---

## 🔧 Troubleshooting

### 1. API'ye Erişilemiyor

**Kontrol:**
```bash
# API çalışıyor mu?
ssh huseyinadm@31.186.24.44 'sudo systemctl status katana-api'

# Port dinliyor mu?
ssh huseyinadm@31.186.24.44 'ss -tlnp | grep 5055'

# Firewall açık mı?
ssh huseyinadm@31.186.24.44 'sudo ufw status'

# Logları kontrol et
ssh huseyinadm@31.186.24.44 'sudo journalctl -u katana-api -n 50'
```

### 2. Frontend allowedHosts Hatası

**Çözüm:**
```bash
# .env.local dosyası doğru mu?
cat /home/huseyinadm/katana/frontend/katana-web/.env.local

# node_modules temizle ve yeniden kur
rm -rf node_modules package-lock.json
npm install
```

### 3. CORS Hatası

**Kontrol:**
```bash
# appsettings.json'da AllowedOrigins kontrol et
cat /home/huseyinadm/katana/src/Katana.API/appsettings.json | grep -A5 AllowedOrigins

# Servisi yeniden başlat
sudo systemctl restart katana-api
```

### 4. Database Bağlantı Hatası

**Kontrol:**
```bash
# SQL Server container çalışıyor mu?
docker ps | grep katana-sql

# Container logları
docker logs katana-sql

# Port açık mı?
ss -tlnp | grep 1433
```

---

## 📊 Servis Yönetimi

### Backend API (systemd)

```bash
# Başlat
sudo systemctl start katana-api

# Durdur
sudo systemctl stop katana-api

# Yeniden başlat
sudo systemctl restart katana-api

# Durum
sudo systemctl status katana-api

# Loglar (canlı)
sudo journalctl -u katana-api -f

# Loglar (son 100)
sudo journalctl -u katana-api -n 100
```

### Database (Docker)

```bash
# Container durumu
docker ps -a | grep katana-sql

# Başlat
docker start katana-sql

# Durdur
docker stop katana-sql

# Loglar
docker logs -f katana-sql

# Container içine gir
docker exec -it katana-sql /bin/bash
```

### Frontend (npm)

```bash
# Arka planda başlat
cd /home/huseyinadm/katana/frontend/katana-web
nohup npm start > frontend.log 2>&1 &

# Process ID bul
ps aux | grep "react-scripts start"

# Durdur
kill <PID>

# Logları takip et
tail -f frontend.log
```

---

## 🔐 Güvenlik Notları

1. **JWT Secret:** Production'da environment variable kullanın
2. **Database Password:** Güçlü şifre kullanın, paylaşmayın
3. **SSH Key:** Şifre yerine SSH key kullanımı önerilir
4. **Firewall:** Sadece gerekli portları açın
5. **HTTPS:** Production'da SSL/TLS sertifikası kullanın

---

## 📝 Hızlı Komutlar

```bash
# Tüm servislerin durumu
ssh huseyinadm@31.186.24.44 '
  echo "=== Backend API ===" && sudo systemctl status katana-api --no-pager | head -10
  echo -e "\n=== Database ===" && docker ps | grep katana-sql
  echo -e "\n=== Ports ===" && ss -tlnp | grep -E "(5055|3000|1433)"
'

# Health check
curl -s http://31.186.24.44:5055/health && echo " ✓ API Healthy"

# Git pull ve restart
ssh huseyinadm@31.186.24.44 '
  cd /home/huseyinadm/katana
  git pull
  dotnet build src/Katana.API/Katana.API.csproj -c Release
  sudo systemctl restart katana-api
'
```

---

## 📞 Destek

Sorun devam ederse:

1. **Logları kontrol edin:** `sudo journalctl -u katana-api -n 200`
2. **Script'leri çalıştırın:** `./scripts/fix-server-access.sh`
3. **Manuel adımları takip edin:** Yukarıdaki "Manuel Çözümler" bölümü

---

**Son Güncelleme:** 11 Kasım 2025  
**Script Versiyonu:** 1.0
