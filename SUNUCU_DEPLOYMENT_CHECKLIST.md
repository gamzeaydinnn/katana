# Sunucu Deployment Checklist - Admin Onay Düzeltmesi

## 🎯 Amaç

Sunucuda çalışmayan admin onayı ve Koza senkronizasyonunu düzeltmek.

## 📋 Yapılan Değişiklikler

### 1. Konfigürasyon Düzeltmesi

**Dosya**: `publish_test/appsettings.json`

**Değişiklik**:

```json
// ÖNCE
"ManualSessionCookie": "JSESSIONID=FILL_ME",

// SONRA
"ManualSessionCookie": "",
```

**Sebep**: Geçersiz placeholder cookie değeri authentication'ı engelliyor.

### 2. Kod Güvenlik İyileştirmesi

**Dosya**: `src/Katana.Infrastructure/APIClients/LucaService.Core.cs`

**Değişiklik**: `AuthenticateWithCookieAsync()` metodunda daha güçlü validasyon:

- Minimum cookie uzunluğu 20 → 30 karakter
- Ek placeholder kontrolleri (PLACEHOLDER, CHANGE_ME, TODO)
- Daha açıklayıcı log mesajları

**Sebep**: Gelecekte benzer sorunları önlemek.

## 🚀 Deployment Adımları

### Adım 1: Kod Değişikliklerini Build Et

```powershell
# Backend build
cd src/Katana.API
dotnet build -c Release

# Publish
dotnet publish -c Release -o ../../publish_new
```

### Adım 2: Sunucuya Deploy Et

#### Seçenek A: Docker (Önerilen)

```powershell
# 1. Docker image build et
docker-compose build katana-api

# 2. Container'ı yeniden başlat
docker-compose up -d katana-api

# 3. Logları kontrol et
docker-compose logs -f katana-api
```

#### Seçenek B: Manuel Deployment

```powershell
# 1. Uygulamayı durdur
systemctl stop katana-api

# 2. Yeni dosyaları kopyala
scp -r publish_new/* user@sunucu:/path/to/katana/

# 3. Konfigürasyonu güncelle
scp publish_test/appsettings.json user@sunucu:/path/to/katana/

# 4. Uygulamayı başlat
systemctl start katana-api
```

### Adım 3: Hızlı Düzeltme Scripti Çalıştır

```powershell
# Sunucuda
./SUNUCU_HIZLI_DUZELTME.ps1
```

Bu script:

- ✅ Mevcut konfigürasyonu kontrol eder
- ✅ Yedek alır
- ✅ ManualSessionCookie'yi temizler
- ✅ Değişiklikleri doğrular
- ✅ (Opsiyonel) Uygulamayı yeniden başlatır

### Adım 4: Doğrulama

#### 4.1 Log Kontrolü

**Başarılı Authentication**:

```
🔐 EnsureAuthenticatedAsync: UseTokenAuth=False, IsAuthenticated=True
✅ Koza Authentication Complete (WS/PerformLogin)
🏢 Branch selection completed successfully
```

**Başarısız Authentication** (düzeltme öncesi):

```
🔐 EnsureAuthenticatedAsync: ManualCookieValid=False
❌ Login olunmalı
```

#### 4.2 API Test

```powershell
# Health check
curl http://sunucu:5055/health

# Satış siparişleri listesi
curl http://sunucu:5055/api/sales-orders `
  -H "Authorization: Bearer TOKEN"

# Sipariş onaylama
curl -X POST http://sunucu:5055/api/sales-orders/123/approve `
  -H "Authorization: Bearer TOKEN" `
  -H "Content-Type: application/json"

# Kozaya senkronizasyon
curl -X POST http://sunucu:5055/api/sales-orders/123/sync `
  -H "Authorization: Bearer TOKEN" `
  -H "Content-Type: application/json"
```

#### 4.3 Admin Panel Test

1. **Login**: Admin paneline giriş yap
2. **Sipariş Listesi**: Satış siparişlerini görüntüle
3. **Onaylama**: Bir siparişi onayla
   - ✅ "Sipariş onaylandı" mesajı
   - ✅ Katana'ya stok eklendi
   - ✅ Status: APPROVED
4. **Senkronizasyon**: Kozaya senkronize et
   - ✅ "Luca'ya başarıyla senkronize edildi" mesajı
   - ✅ IsSyncedToLuca: true
   - ✅ LucaOrderId dolu

## 🔍 Troubleshooting

### Sorun 1: Hala "Login olunmalı" hatası

**Çözüm**:

```powershell
# 1. Session'ı tamamen temizle
docker-compose down
docker volume rm katana_redis_data  # Redis cache temizle
docker-compose up -d

# 2. Manuel cookie kontrolü
docker-compose exec katana-api cat /app/appsettings.json | grep ManualSessionCookie
# Çıktı: "ManualSessionCookie": "",
```

### Sorun 2: Branch selection hatası

**Çözüm**:

```powershell
# appsettings.json'da branch ID'yi kontrol et
"DefaultBranchId": 11746,
"ForcedBranchId": 11746,

# Luca'da geçerli branch'leri kontrol et
curl -X POST https://akozas.luca.com.tr/Yetki/YdlUserResponsibilityOrgSs.do `
  -H "Cookie: JSESSIONID=..." `
  -H "Content-Type: application/json" `
  -d "{}"
```

### Sorun 3: Timeout hataları

**Çözüm**:

```json
// appsettings.json
"LucaApi": {
  "TimeoutSeconds": 300,  // 5 dakika
  ...
}
```

## 📊 Monitoring

### Önemli Metrikler

1. **Authentication Success Rate**

   - Log: "Koza Authentication Complete"
   - Hedef: %100

2. **Approval Success Rate**

   - Endpoint: `/api/sales-orders/{id}/approve`
   - Hedef: %95+

3. **Sync Success Rate**
   - Endpoint: `/api/sales-orders/{id}/sync`
   - Hedef: %90+

### Log Monitoring

```powershell
# Real-time authentication logs
docker-compose logs -f katana-api | Select-String "Authentication|Session|Login"

# Approval logs
docker-compose logs -f katana-api | Select-String "approve|APPROVED"

# Sync logs
docker-compose logs -f katana-api | Select-String "sync|Luca|Koza"

# Error logs
docker-compose logs -f katana-api | Select-String "ERROR|Exception|Failed"
```

## ✅ Deployment Checklist

- [ ] Kod değişiklikleri build edildi
- [ ] Docker image oluşturuldu
- [ ] Yedek alındı (appsettings.json)
- [ ] ManualSessionCookie temizlendi
- [ ] Uygulama yeniden başlatıldı
- [ ] Health check başarılı
- [ ] Authentication logları kontrol edildi
- [ ] Admin panel login test edildi
- [ ] Sipariş onaylama test edildi
- [ ] Kozaya senkronizasyon test edildi
- [ ] Monitoring kuruldu
- [ ] Dokümantasyon güncellendi

## 🎉 Başarı Kriterleri

✅ **Tamamlandı** olarak işaretlenebilir:

1. Admin panelde sipariş onaylama çalışıyor
2. Katana'ya stok ekleme başarılı
3. Kozaya senkronizasyon başarılı
4. Loglar "Authentication Complete" gösteriyor
5. Hata oranı %5'in altında

## 📞 Destek

Sorun devam ederse:

1. Logları kaydet: `docker-compose logs katana-api > logs.txt`
2. Konfigürasyonu kaydet: `cat appsettings.json > config.txt`
3. API response'ları kaydet
4. Destek ekibine ilet

---

**Hazırlayan**: Kiro AI
**Tarih**: 2024-01-15
**Versiyon**: 1.0
