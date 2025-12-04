# 🔍 Detaylı Kullanılmayan Kod Analizi

**Tarih:** 4 Aralık 2024  
**Toplam C# Dosyası:** 350

---

## ❌ SİLİNMESİ GEREKEN TEST DOSYALARI

### 1. Controller'ı Olmayan Test Dosyaları

#### `tests/Katana.Tests/Controllers/AnalyticsControllerTests.cs`
**Durum:** ❌ Controller yok  
**Neden:** `AnalyticsController` bulunamadı  
**Aksiyon:** Sil

#### `tests/Katana.Tests/Controllers/DashboardControllerTests.cs`
**Durum:** ❌ Controller yok  
**Neden:** `DashboardController` bulunamadı  
**Aksiyon:** Sil

---

## 🟡 KULLANIMI ŞÜPHELİ CONTROLLER'LAR

### 2. Debug/Test Controller'ları

#### `src/Katana.API/Controllers/DebugKatanaController.cs`
**Durum:** ⚠️ Production'da olmamalı  
**Kullanım:** Debug amaçlı  
**Öneri:** `#if DEBUG` ile sarmalanmalı veya silinmeli

```csharp
#if DEBUG
[ApiController]
[Route("api/debug")]
public class DebugKatanaController : ControllerBase
{
    // ...
}
#endif
```

#### `src/Katana.API/Controllers/KozaDebugController.cs`
**Durum:** ⚠️ Production'da olmamalı  
**Kullanım:** Koza entegrasyonu debug  
**Öneri:** `#if DEBUG` ile sarmalanmalı

#### `src/Katana.API/Controllers/TestController.cs`
**Durum:** ⚠️ Production'da olmamalı  
**Kullanım:** Genel test endpoint'leri  
**Öneri:** Silinmeli veya `#if DEBUG`

---

## 📁 BACKUP DOSYALARI

### 3. Controller Backup'ları

#### `src/Katana.API/Controllers/AuthController.cs.bak2`
**Durum:** ❌ Gereksiz  
**Aksiyon:** Sil (Git history'de zaten var)

#### `src/Katana.API/Controllers/LucaCompatibilityController.cs.bak`
**Durum:** ❌ Gereksiz  
**Aksiyon:** Sil (Git history'de zaten var)

---

## 🗂️ ORGANIZE EDİLMESİ GEREKEN DOSYALAR

### 4. Kök Dizindeki Dokümantasyon

**Mevcut Durum:** 20+ MD dosyası kök dizinde  
**Öneri:** `docs/` altında organize et

```
docs/
├── active/           # Aktif dokümantasyon
│   ├── README.md
│   ├── DEPLOYMENT_CHECKLIST.md
│   └── TESTING_GUIDE.md
├── archive/          # Tamamlanmış raporlar
│   ├── BACKEND_INTEGRATION_REPORT.md
│   ├── DEPLOYMENT_SUCCESS.md
│   └── VALIDATION_REPORT.md
└── guides/           # Kullanım kılavuzları
    ├── STOCK_MANAGEMENT_GUIDE.md
    └── INTEGRATION_TEST_GUIDE.md
```

---

## 🔧 TEMİZLİK SCRIPT'İ

### Otomatik Temizlik

```bash
#!/bin/bash
# cleanup-unused-files.sh

echo "🧹 Kullanılmayan dosyaları temizliyorum..."

# 1. Test dosyalarını sil
echo "📝 Kullanılmayan test dosyalarını siliyorum..."
rm -f tests/Katana.Tests/Controllers/AnalyticsControllerTests.cs
rm -f tests/Katana.Tests/Controllers/DashboardControllerTests.cs

# 2. Backup dosyalarını sil
echo "💾 Backup dosyalarını siliyorum..."
find . -name "*.bak" -o -name "*.bak2" -o -name "*.backup" | xargs rm -f

# 3. Log dosyalarını temizle (30 günden eski)
echo "📋 Eski log dosyalarını siliyorum..."
find logs/ -name "*.log" -mtime +30 -delete
find logs/ -name "*.txt" -mtime +30 -delete
find logs/ -name "*.json" -mtime +30 -delete

# 4. Geçici dosyaları sil
echo "🗑️ Geçici dosyaları siliyorum..."
rm -f *.log *.txt
rm -f .build_* .run_* .docker_*
rm -f put*.json branches-body.txt headers.txt login-body.txt
rm -f koza_*.json luca_*.json luca_*.csv luca_*.html
rm -f docker-nets.json swagger.json

# 5. Boş dosyaları sil
echo "📄 Boş dosyaları siliyorum..."
find . -type f -empty -delete

# 6. Boş klasörleri sil
echo "📁 Boş klasörleri siliyorum..."
find . -type d -empty -delete

echo "✅ Temizlik tamamlandı!"
```

---

## 📊 KULLANIM ANALİZİ

### Controller Kullanım Durumu

| Controller | Endpoint Sayısı | Son Kullanım | Durum |
|-----------|----------------|--------------|-------|
| DebugKatanaController | 5 | Development | ⚠️ Debug only |
| KozaDebugController | 3 | Development | ⚠️ Debug only |
| TestController | 10+ | Development | ⚠️ Test only |
| OrderInvoiceSyncController | 8 | Production | ✅ Aktif |
| SalesOrdersController | 7 | Production | ✅ Aktif |
| PurchaseOrdersController | 10 | Production | ✅ Aktif |
| InvoicesController | 15 | Production | ✅ Aktif |
| ManufacturingOrdersController | 6 | Yeni | ✅ Aktif |

---

## 🎯 ÖNCELİKLİ AKSIYONLAR

### Hemen Yapılabilir (5 dk)
1. ✅ Backup dosyalarını sil
2. ✅ Kullanılmayan test dosyalarını sil
3. ✅ Geçici JSON/TXT dosyalarını sil

### Kısa Vadede (1 saat)
4. ⚠️ Debug controller'ları `#if DEBUG` ile sarmalanmalı
5. ⚠️ Dokümantasyonu organize et
6. ⚠️ Log rotation policy ayarla

### Orta Vadede (1 gün)
7. 📁 Publish klasörünü git'ten kaldır
8. 📁 Eski logları temizle
9. 📁 `.gitignore` dosyasını güncelle

---

## 🔍 DETAYLI DOSYA LİSTESİ

### Silinecek Dosyalar (Toplam: ~2050 dosya)

#### Kök Dizin Log Dosyaları (15 dosya)
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
```

#### Geçici Test Dosyaları (20 dosya)
```
backend_err.txt
backend_out.txt
backend_out2.txt
backend_output.txt
db_apply_err.txt
db_apply_out.txt
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
=
```

#### Logs Klasörü (~2000 dosya)
```
logs/app-*.log (8 dosya)
logs/AUTH_LOGIN_JSON_*.json (~500 dosya)
logs/AUTH_LOGIN_JSON_*.txt (~500 dosya)
logs/CHANGE_BRANCH_JSON_*.json (~500 dosya)
logs/CHANGE_BRANCH_JSON_*.txt (~500 dosya)
logs/SEND_STOCK_CARD_*.json (~100 dosya)
logs/SEND_STOCK_CARD_*.txt (~100 dosya)
logs/BRANCHES-*.json (~10 dosya)
logs/BRANCHES-*.txt (~10 dosya)
```

#### Backup Dosyaları (3 dosya)
```
AKSIYONLAR.md.backup
src/Katana.API/Controllers/AuthController.cs.bak2
src/Katana.API/Controllers/LucaCompatibilityController.cs.bak
```

#### Test Dosyaları (2 dosya)
```
tests/Katana.Tests/Controllers/AnalyticsControllerTests.cs
tests/Katana.Tests/Controllers/DashboardControllerTests.cs
```

---

## 💾 DISK ALANI KAZANCI

| Kategori | Dosya Sayısı | Tahmini Boyut |
|----------|--------------|---------------|
| Log dosyaları | ~2000 | 500 MB |
| Publish klasörü | ~200 | 100 MB |
| Geçici dosyalar | ~30 | 10 MB |
| Backup dosyaları | ~5 | 1 MB |
| **TOPLAM** | **~2235** | **~611 MB** |

---

## ✅ SONUÇ VE ÖNERİLER

### Özet
- **Toplam gereksiz dosya:** ~2235
- **Disk alanı kazancı:** ~611 MB
- **Temizlik süresi:** ~1 saat

### Öneriler
1. ✅ Cleanup script'ini çalıştır
2. ✅ `.gitignore` dosyasını güncelle
3. ✅ Log rotation policy ayarla
4. ⚠️ Debug controller'ları production'dan kaldır
5. ⚠️ Dokümantasyonu organize et
6. ⚠️ CI/CD pipeline'ı güncelle (publish klasörü için)

### Riskler
- ⚠️ Silme işlemlerinden önce mutlaka yedek alın
- ⚠️ Ekip ile görüşün (bazı dosyalar başkaları tarafından kullanılıyor olabilir)
- ⚠️ Production'da test etmeden önce staging'de deneyin

---

**Not:** Bu rapor otomatik analiz sonucudur. Manuel inceleme önerilir.
