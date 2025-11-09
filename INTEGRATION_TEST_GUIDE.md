# Entegrasyon Test ve Doğrulama Sistemi

## ✅ Tamamlanan Özellikler

### 1. **Veri Doğrulama Sistemi (ValidationService)**

- ✅ Stok hareketi doğrulama
- ✅ Fatura/muhasebe doğrulama
- ✅ Müşteri bilgileri doğrulama
- ✅ Hata/uyarı kodlama sistemi
- ✅ Öneriler ve düzeltme ipuçları

### 2. **Entegrasyon Test Servisi (IntegrationTestService)**

- ✅ Katana → Luca stok akışı testi
- ✅ Katana → Luca fatura/muhasebe akışı testi
- ✅ Mapping tablosu tutarlılık kontrolü
- ✅ UAT (Kullanıcı Kabul Testi) paketi
- ✅ Detaylı test raporlama

### 3. **API Endpoints** (`/api/IntegrationTest`)

- `POST /api/IntegrationTest/stock-flow?sampleSize=10` - Stok akış testi
- `POST /api/IntegrationTest/invoice-flow?sampleSize=10` - Fatura akış testi
- `POST /api/IntegrationTest/mapping-consistency` - Mapping tutarlılık
- `POST /api/IntegrationTest/uat-suite` - Tam UAT paketi

### 4. **Mevcut Altyapı**

- ✅ Katana API Client (OAuth 2.0 Bearer Token)
- ✅ Luca Proxy Controller (session-based)
- ✅ MappingHelper (SKU ↔ Account, Location ↔ Warehouse)
- ✅ MappingService (dinamik eşleştirme)
- ✅ Doğru mimari: Katana → Middleware → Luca

## 📋 Kullanım Örnekleri

### Stok Akış Testi

```bash
curl -X POST "http://localhost:5055/api/IntegrationTest/stock-flow?sampleSize=20" \
  -H "Authorization: Bearer <JWT_TOKEN>" \
  -H "Content-Type: application/json"
```

**Yanıt:**

```json
{
  "testName": "Katana → Luca Stok Hareketi Entegrasyonu",
  "environment": "TEST",
  "success": true,
  "recordsTested": 20,
  "recordsPassed": 18,
  "recordsFailed": 2,
  "validationDetails": [
    {
      "recordId": "123",
      "recordType": "Stock",
      "isValid": false,
      "errors": ["STK002: Depo lokasyonu boş olamaz"],
      "warnings": []
    }
  ]
}
```

### Fatura Akış Testi

```bash
curl -X POST "http://localhost:5055/api/IntegrationTest/invoice-flow?sampleSize=10" \
  -H "Authorization: Bearer <JWT_TOKEN>"
```

### Tam UAT Paketi

```bash
curl -X POST "http://localhost:5055/api/IntegrationTest/uat-suite" \
  -H "Authorization: Bearer <JWT_TOKEN>"
```

**Yanıt:**

```json
{
  "success": true,
  "totalTests": 3,
  "passedTests": 3,
  "failedTests": 0,
  "results": [
    {
      "testName": "Mapping Tablosu Tutarlılık Kontrolü",
      "success": true,
      "recordsPassed": 45,
      "recordsFailed": 3
    },
    {
      "testName": "Katana → Luca Stok Hareketi Entegrasyonu",
      "success": true,
      "recordsPassed": 20,
      "recordsFailed": 0
    },
    {
      "testName": "Katana → Luca Fatura/Muhasebe Entegrasyonu",
      "success": true,
      "recordsPassed": 18,
      "recordsFailed": 2
    }
  ]
}
```

## 🔒 Güvenlik - API Key Yönetimi

### Mevcut Konfigürasyon (appsettings.json)

```json
{
  "KatanaApi": {
    "BaseUrl": "https://api.katanamrp.com/v1/",
    "ApiKey": "ed8c38d1-4015-45e5-9c28-381d3fe148b6",
    "TimeoutSeconds": 30,
    "MaxRetryAttempts": 3,
    "WebhookSecret": "katana-webhook-secret-change-in-production-2025"
  }
}
```

### ✅ Güvenlik Best Practices Uygulanmış:

1. ✅ **Server-Side Only**: API key'ler sadece backend'de
2. ✅ **Environment Variables**: Production'da env variables kullanılmalı
3. ✅ **Authorization Header**: `Bearer <token>` formatı
4. ✅ **JWT Authentication**: Frontend ↔ Backend arası JWT
5. ✅ **Secret Rotation**: Key değiştirilmesi için hazır yapı

## 📊 Doğrulama Hata Kodları

### Stok (STK)

- `STK001` - Ürün kodu boş
- `STK002` - Depo lokasyonu boş
- `STK002W` - Depo eşleşmesi bulunamadı
- `STK003W` - Miktar sıfır
- `STK004` - Hareket tipi boş

### Fatura (INV)

- `INV001` - Fatura numarası boş
- `INV002` - Vergi numarası boş
- `INV002W` - Vergi numarası 10/11 hane değil
- `INV003` - Toplam tutar ≤ 0
- `INV004` - KDV tutarı negatif
- `INV005` - Para birimi boş
- `INV005W` - Standart dışı para birimi
- `INV006W` - İleri tarihli fatura

### Müşteri (CUS)

- `CUS001` - Vergi numarası boş
- `CUS002` - Vergi numarası format hatası
- `CUS003` - Müşteri ünvanı boş
- `CUS004W` - Geçersiz e-posta formatı

## 🎯 Mapping Eşleştirme Sistemi

### SKU → Hesap Kodu

```csharp
var skuMapping = await _mappingService.GetSkuToAccountMappingAsync();
// {
//   "PROD-001": "600.01.001",
//   "PROD-002": "600.01.002",
//   "DEFAULT": "600.01"
// }
```

### Lokasyon → Depo Kodu

```csharp
var locationMapping = await _mappingService.GetLocationMappingAsync();
// {
//   "WAREHOUSE-A": "LUCA-DEPO-1",
//   "WAREHOUSE-B": "LUCA-DEPO-2",
//   "DEFAULT": "MAIN"
// }
```

## 🔄 Entegrasyon Akışı

```
┌──────────┐      ┌─────────────┐      ┌──────────┐
│  Katana  │ ───► │ Middleware  │ ───► │   Luca   │
│   API    │      │ (Validator) │      │   Koza   │
└──────────┘      └─────────────┘      └──────────┘
     │                   │                    │
     │              ValidationService         │
     │              MappingHelper             │
     │              IntegrationTestService    │
     │                                        │
     └────────── Test Environment ───────────┘
```

### 1. **Extractor** (Katana'dan veri çekme)

- OAuth 2.0 Bearer Token authentication
- Products, Stock Movements, Sales Orders

### 2. **Transformer** (Veri dönüştürme)

- ValidationService ile doğrulama
- MappingHelper ile eşleştirme
- Hata/uyarı loglama

### 3. **Loader** (Luca'ya gönderme)

- LucaProxyController ile session yönetimi
- Batch processing
- Retry mekanizması

## 📈 Test Ortamı Senaryoları

### Senaryo 1: Stok Hareketi Testi

1. Test ortamında 10 adet stok hareketi oluştur
2. `POST /api/IntegrationTest/stock-flow?sampleSize=10`
3. Doğrulama sonuçlarını kontrol et
4. Hatalı kayıtları düzelt
5. Yeniden test et

### Senaryo 2: Fatura Muhasebe Testi

1. Test ortamında 5 adet fatura oluştur
2. `POST /api/IntegrationTest/invoice-flow?sampleSize=5`
3. Fatura kalemleri - muhasebe kayıtları eşleşmesini kontrol et
4. KDV oranları ve toplam tutarları doğrula
5. Luca'da manuel kontrol yap

### Senaryo 3: Mapping Tutarlılık

1. Tüm ürün SKU'larını listele
2. `POST /api/IntegrationTest/mapping-consistency`
3. Eksik mapping'leri tespit et
4. Varsayılan eşleştirmeler ekle
5. Yeniden test et

## ⚠️ Uyarı Sistemi

### Otomatik Uyarılar:

- ❌ **CRITICAL**: Veri kaybı riski (sync durur)
- ⚠️ **WARNING**: Düzeltilmesi önerilen (sync devam eder)
- ℹ️ **INFO**: Bilgilendirme (hiçbir etki yok)

### Notification Channels:

- Database: IntegrationLogs tablosu
- SignalR: Real-time bildirimler
- Email: Kritik hatalar için (yapılandırılabilir)

## 🚀 Production Hazırlık

### ✅ Yapılması Gerekenler:

1. **Environment Variables** ayarla:

   ```bash
   export KATANA_API_KEY="your-production-key"
   export LUCA_API_KEY="your-luca-key"
   export JWT_SECRET="production-secret-min-32-chars"
   ```

2. **Mapping Tablosunu** doldur:

   - Tüm aktif ürünler için SKU → Account mapping
   - Tüm depolar için Location → Warehouse mapping

3. **Test Senaryoları** çalıştır:

   ```bash
   POST /api/IntegrationTest/uat-suite
   ```

4. **Monitoring** aktif et:

   - Application Insights
   - Serilog file logging
   - Database log retention (90 gün)

5. **Kullanıcı Eğitimi** ver:
   - Frontend test dashboard
   - Hata kod referansları
   - Acil durum prosedürleri

## 📦 Yeni Dosyalar

1. `ValidationResultDto.cs` - Doğrulama sonuç modeli
2. `IntegrationTestResultDto.cs` - Test sonuç modeli
3. `IValidationService.cs` - Doğrulama interface
4. `ValidationService.cs` - Doğrulama servisi
5. `IIntegrationTestService.cs` - Test interface
6. `IntegrationTestService.cs` - Test servisi
7. `IntegrationTestController.cs` - Test API controller
8. `Program.cs` - Service registration (güncellendi)

## 📞 Destek

Sorular için:

- API Documentation: http://localhost:5055/swagger
- Test Dashboard: http://localhost:3000/integration-tests
- Logs: `./logs/` klasörü
