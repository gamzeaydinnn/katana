# 🗑️ Kullanılmayan Kod ve Dosya Analiz Raporu

**Tarih:** 4 Aralık 2024  
**Analiz Kapsamı:** Tüm proje dosyaları

---

## 📊 Özet

Bu rapor, projede kullanılmayan veya gereksiz olabilecek dosya ve klasörleri listeler.

---

## 🔴 SİLİNMESİ ÖNERİLEN DOSYALAR

### 1. Log Dosyaları (Kök Dizin)
**Konum:** Proje kök dizini  
**Durum:** ❌ Silinmeli

```
.build_after_fix_stderr.log
.build_after_fix_stdout.log
.build_stderr.log
.build_stdout.log
.docker_api_logs.log
.docker_compose_results.log
.docker_down_up_ps.log
.dotnet_run_stderr.log
.dotnet_run_stdout.log
.run_after_fix_stderr.log
.run_after_fix_stdout.log
.run_full_stderr.log
.run_full_stdout.log
.run_portfix_stderr.log
.run_portfix_stdout.log
.run_start_stderr.log
.run_start_stdout.log
backend_err.txt
backend_out.txt
backend_out2.txt
backend_output.txt
db_apply_err.txt
db_apply_out.txt
```

**Neden:** Geçici log dosyaları, git'e commit edilmemeli. `.gitignore`'a eklenip silinmeli.

---

### 2. Geçici Test/Debug Dosyaları
**Konum:** Proje kök dizini  
**Durum:** ❌ Silinmeli

```
branches-body.txt
headers.txt
login-body.txt
put-enveloped.json
put.envelope.json
put.json
docker-nets.json
koza_category_tests_results.json
koza_debug_response.json
koza_debug_root.json
koza-setup-results.json
luca_categories.json
luca_categories_resp.html
luca_responses.csv
luca_responses.json
swagger.json
=  (boş dosya)
```

**Neden:** Test ve debug amaçlı geçici dosyalar, production'da gereksiz.

---

### 3. Backup Dosyaları
**Konum:** Proje kök dizini  
**Durum:** ❌ Silinmeli

```
AKSIYONLAR.md.backup
src/Katana.API/Controllers/AuthController.cs.bak2
src/Katana.API/Controllers/LucaCompatibilityController.cs.bak
```

**Neden:** Backup dosyaları git history'de zaten mevcut, gereksiz.

---

### 4. Logs Klasörü İçeriği
**Konum:** `logs/`  
**Durum:** ⚠️ Temizlenmeli (eski loglar)

```
logs/app-20251127.log
logs/app-20251128.log
... (1000+ log dosyası)
logs/AUTH_LOGIN_JSON_orgCode_userName_userPassword-cookies-*.json
logs/AUTH_LOGIN_JSON_orgCode_userName_userPassword-http-*.txt
logs/CHANGE_BRANCH_JSON_orgSirketSubeId-cookies-*.json
logs/SEND_STOCK_CARD_REQUEST_*
logs/SEND_STOCK_CARD_RESPONSE_*
```

**Neden:** Binlerce eski log dosyası disk alanı kaplıyor. Sadece son 7-30 günlük loglar tutulmalı.

**Öneri:** Log rotation policy uygulanmalı.

---

### 5. Boş Klasörler
**Konum:** Çeşitli  
**Durum:** ❌ Silinmeli

```
katana/  (boş klasör)
%USERPROFILE%/  (gereksiz)
```

**Neden:** Boş klasörler gereksiz.

---

## 🟡 İNCELENMESİ GEREKEN DOSYALAR

### 6. Çoklu Dokümantasyon Dosyaları
**Konum:** Proje kök dizini  
**Durum:** ⚠️ Birleştirilmeli veya organize edilmeli

```
BACKEND_INTEGRATION_REPORT.md
BACKEND_VALIDATION_REPORT.md
DATA_CORRECTION_README.md
DEPLOYMENT_CHECKLIST.md
DEPLOYMENT_SUCCESS.md
FRONTEND_CHECKLIST.md
IMPLEMENTATION_REPORT.md
INTEGRATION_TEST_GUIDE.md
PRODUCTION_UPDATE_FIX.md
PROJECT_AUDIT.md
QUICK_FIX_GUIDE.md
ROLE_AUTHORIZATION_UPDATE.md
STOCK_MANAGEMENT_GUIDE.md
TESTING_GUIDE.md
TEST_BACKEND_INTEGRATION.md
TODO.md
VALIDATION_REPORT.md
ORDER_CRUD_SUMMARY.md
```

**Öneri:** 
- Aktif dokümantasyon: `docs/` klasörüne taşınmalı
- Eski/tamamlanmış raporlar: `docs/archive/` klasörüne taşınmalı
- Gereksiz olanlar silinmeli

---

### 7. SQL Dosyaları
**Konum:** Proje kök dizini  
**Durum:** ⚠️ Organize edilmeli

```
CHECK_MANAGER_ROLE.sql
check-admin.sql
```

**Öneri:** `db/` veya `scripts/sql/` klasörüne taşınmalı.

---

### 8. PowerShell Script Dosyaları
**Konum:** Proje kök dizini  
**Durum:** ⚠️ Organize edilmeli

```
run-uat-test.ps1
send-luca.ps1
start-katana.bat
test-rbac.sh
```

**Öneri:** `scripts/` klasörüne taşınmalı.

---

## 🟢 MUHTEMELEN KULLANILMAYAN CONTROLLER'LAR

### 9. Test/Debug Controller'ları
**Konum:** `src/Katana.API/Controllers/`  
**Durum:** ⚠️ Production'da disable edilmeli

```
DebugKatanaController.cs
KozaDebugController.cs
TestController.cs
```

**Öneri:** 
- Development ortamında aktif
- Production'da `#if DEBUG` ile disable edilmeli veya silinmeli

---

### 10. Duplicate/Backup Controller'lar
**Konum:** `src/Katana.API/Controllers/`  
**Durum:** ❌ Silinmeli

```
AuthController.cs.bak2
LucaCompatibilityController.cs.bak
```

**Neden:** Backup dosyaları gereksiz.

---

## 🔵 KULLANILMAYAN TEST DOSYALARI

### 11. Eski Test Dosyaları
**Konum:** `tests/Katana.Tests/Controllers/`  
**Durum:** ⚠️ İncelenmeli

Bazı test dosyaları compile hatası veriyor:
```
AnalyticsControllerTests.cs  (AnalyticsController bulunamıyor)
DashboardControllerTests.cs  (DashboardController bulunamıyor)
```

**Öneri:** 
- Controller silinmişse test de silinmeli
- Controller varsa test düzeltilmeli

---

## 📁 BÜYÜK/GEREKSİZ KLASÖRLER

### 12. Publish Klasörü
**Konum:** `publish/`  
**Durum:** ⚠️ Git'ten çıkarılmalı

**Boyut:** ~100+ MB  
**İçerik:** Compiled binaries, DLL'ler

**Öneri:** 
- `.gitignore`'a eklenip git'ten kaldırılmalı
- CI/CD pipeline'da build edilmeli

---

### 13. .venv Klasörü
**Konum:** `.venv/`  
**Durum:** ✅ Zaten .gitignore'da olmalı

**Öneri:** Git'te varsa kaldırılmalı.

---

### 14. Node Modules (E2E)
**Konum:** `e2e/node_modules/` (muhtemelen)  
**Durum:** ⚠️ Kontrol edilmeli

**Öneri:** `.gitignore`'da olmalı.

---

## 🛠️ TEMİZLİK AKSIYONLARI

### Öncelik 1: Hemen Silinebilir
```bash
# Log dosyalarını sil
rm -f *.log *.txt
rm -f .build_* .run_* .docker_*

# Geçici JSON dosyalarını sil
rm -f *.json (kök dizinde)

# Backup dosyalarını sil
rm -f *.backup *.bak *.bak2

# Boş klasörleri sil
rmdir katana
```

### Öncelik 2: Organize Et
```bash
# Dokümantasyonu organize et
mkdir -p docs/archive
mv *_REPORT.md docs/archive/
mv *_CHECKLIST.md docs/archive/

# SQL dosyalarını taşı
mv *.sql db/

# Script dosyalarını taşı
mv *.ps1 *.sh scripts/
```

### Öncelik 3: Git'ten Kaldır
```bash
# Publish klasörünü git'ten kaldır
git rm -r --cached publish/
echo "publish/" >> .gitignore

# Log klasörünü git'ten kaldır (sadece .gitkeep tut)
git rm -r --cached logs/*
echo "logs/*.log" >> .gitignore
echo "logs/*.txt" >> .gitignore
echo "logs/*.json" >> .gitignore
```

---

## 📊 İSTATİSTİKLER

### Dosya Sayıları
- **Toplam log dosyası:** ~2000+
- **Geçici dosya:** ~30
- **Backup dosya:** ~5
- **Dokümantasyon:** ~20

### Tahmini Disk Alanı Kazancı
- **Log dosyaları:** ~500 MB
- **Publish klasörü:** ~100 MB
- **Geçici dosyalar:** ~10 MB
- **Toplam:** ~610 MB

---

## ✅ ÖNERİLER

1. **Log Rotation:** Serilog'da log rotation policy ayarla (max 30 gün)
2. **Git Ignore:** `.gitignore` dosyasını güncelle
3. **CI/CD:** Build artifacts'ları git'e commit etme
4. **Dokümantasyon:** Aktif dokümanları `docs/` altında organize et
5. **Test Cleanup:** Kullanılmayan test dosyalarını sil veya düzelt
6. **Code Review:** Debug controller'ları production'da disable et

---

## 🎯 SONUÇ

Projede **~2000+ gereksiz dosya** ve **~610 MB disk alanı** temizlenebilir. 

Öncelikli olarak:
1. ✅ Log dosyalarını temizle
2. ✅ Geçici test dosyalarını sil
3. ✅ Backup dosyalarını sil
4. ✅ Publish klasörünü git'ten kaldır
5. ⚠️ Dokümantasyonu organize et

---

**Not:** Bu rapor otomatik analiz sonucudur. Silme işlemlerinden önce mutlaka yedek alın ve ekip ile görüşün.
