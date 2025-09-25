# Katana-Luca Integration System 🎯

**Katana (MRP/ERP)** ile **Luca Koza (Muhasebe)** sistemleri arasında otomatik veri entegrasyonu sağlayan kapsamlı bir .NET Core projesidir.

## 📋 Proje Amacı

- **Katana**: Üretim, stok ve satış verilerini yönetir
- **Luca Koza**: Mali kayıtları ve fatura/muhasebe işlemlerini yönetir
- **Entegrasyon**: Katana'dan gelen stok, satış, fatura ve muhasebe verilerini otomatik olarak Luca'ya aktarır
- **Çift Yönlü**: Gerektiğinde Luca'dan Katana'ya cari hesap veya mali tablo verisi aktarımını destekler

**Sonuç**: Şirketler manuel veri girişi yapmadan iki sistemi entegre bir şekilde kullanabilir.

## 🏗 Proje Mimarisi

```
ECommerce.Integration/
├── ECommerce.Core/          # Domain modelleri, DTO'lar, yardımcılar
├── ECommerce.Data/          # Veritabanı katmanı
├── ECommerce.Business/      # Servisler ve iş mantığı
├── ECommerce.API/           # REST API (kontroller, middleware)
├── ECommerce.Infrastructure/# Logging, jobs, background services
├── ECommerce.Tests/         # Unit & Integration testleri
└── docs/                    # Dökümantasyon, mapping tabloları
```

## 🧩 Katman Yapısı

### 🔹 ECommerce.Core (Domain Layer)
- **Entities**: Product, Stock, Invoice, Customer modelleri
- **DTOs**: KatanaStockDto, LucaInvoiceDto, SyncResultDto
- **Helpers**: MappingHelper, JwtTokenHelper, HashingHelper

### 🔹 ECommerce.Data (Data Access Layer)
- **DbContext**: IntegrationDbContext (EF Core)
- **Repositories**: Repository pattern implementasyonu
- **Models**: IntegrationLog, MappingTable, FailedSyncRecord
- **Migrations**: Veritabanı migration dosyaları

### 🔹 ECommerce.Business (Business Logic Layer)
- **KatanaService**: Katana API çağrıları ve veri çekme
- **LucaService**: Luca'ya veri gönderimi (API/CSV/XML)
- **SyncService**: Tüm entegrasyon sürecini koordine eder

### 🔹 ECommerce.API (Presentation Layer)
- **Controllers**: SyncController, ReportController, MappingController
- **Middleware**: ErrorHandling, Authentication, CORS
- **Endpoints**: REST API endpoints

### 🔹 ECommerce.Infrastructure (Infrastructure Layer)
- **Logging**: Serilog ile dosya + DB loglama
- **Jobs**: Quartz.NET ile zamanlanmış senkronizasyon
- **Config**: Yapılandırma yönetimi
- **Workers**: Background services

### 🔹 ECommerce.Tests (Test Layer)
- **Unit Tests**: Servislerin birim testleri
- **Integration Tests**: End-to-end test senaryoları
- **Contract Tests**: API şema doğrulamaları

## 🔄 Veri Akışı

1. **Katana'dan Veri Çekme**: KatanaService ile stok/satış verilerini API'den çeker
2. **Veri Dönüşümü**: MappingHelper ile Katana formatından Luca formatına dönüştürür
3. **Doğrulama**: Validator ile veri bütünlüğünü kontrol eder
4. **Loglama**: IntegrationDbContext ile işlem loglarını tutar
5. **Luca'ya Gönderim**: LucaService ile dönüştürülmüş veriyi Luca'ya gönderir
6. **Hata Yönetimi**: Başarısız kayıtları retry kuyruğuna alır

## 🛠 Kurulum

### Gereksinimler
- .NET 8.0+
- SQL Server / PostgreSQL / SQLite
- Visual Studio 2022 / VS Code

### Kurulum Adımları

```bash
# 1. Projeyi klonlayın
git clone https://github.com/gamzeaydinnn/katana.git
cd katana

# 2. NuGet paketlerini restore edin
dotnet restore

# 3. Veritabanını oluşturun
dotnet ef database update --project ECommerce.Data

# 4. Yapılandırma dosyasını düzenleyin
cp appsettings.json.example appsettings.json

# 5. Projeyi çalıştırın
dotnet run --project ECommerce.API
```

### Yapılandırma

`appsettings.json` dosyasında aşağıdaki ayarları yapılandırın:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "your-database-connection-string"
  },
  "KatanaApi": {
    "BaseUrl": "https://katana-api-url",
    "ApiKey": "your-katana-api-key"
  },
  "LucaApi": {
    "BaseUrl": "https://luca-api-url",
    "ApiKey": "your-luca-api-key"
  },
  "Sync": {
    "BatchSize": 100,
    "RetryCount": 3,
    "ScheduleInterval": "0 */6 * * *"
  }
}
```

## 🚀 Kullanım

### Manuel Senkronizasyon
```bash
POST /api/sync/run
```

### Rapor Alma
```bash
GET /api/reports/last
GET /api/reports/failed
```

### Mapping Yönetimi
```bash
GET /api/mapping
POST /api/mapping
PUT /api/mapping/{id}
```

## 🔐 Güvenlik

- **TLS**: HTTPS zorunlu
- **Authentication**: JWT token veya API key
- **Secrets**: Environment variables kullanımı
- **Audit**: Tüm işlemler loglanır

## ⚡ Performans

- **Batch Processing**: Toplu veri işleme
- **Parallel Processing**: Paralel işlem desteği
- **Caching**: Memory cache kullanımı
- **Monitoring**: Health check endpoints

## 📅 Geliştirme Yol Haritası

- [x] **Hafta 1**: Core modeller ve DataContext
- [x] **Hafta 2**: Servisler ve mapping
- [ ] **Hafta 3**: API endpoints ve logging
- [ ] **Hafta 4**: Scheduler, testler ve deployment

## 🧪 Test Etme

```bash
# Unit testleri çalıştır
dotnet test ECommerce.Tests

# Integration testleri çalıştır
dotnet test ECommerce.Tests --filter Category=Integration

# Test coverage raporu
dotnet test --collect:"XPlat Code Coverage"
```

## 📚 Dokümantasyon

- [API Dokümantasyonu](docs/api.md)
- [Veri Mapping Kılavuzu](docs/mapping.md)
- [Deployment Kılavuzu](docs/deployment.md)
- [Troubleshooting](docs/troubleshooting.md)

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/AmazingFeature`)
3. Değişikliklerinizi commit edin (`git commit -m 'Add some AmazingFeature'`)
4. Branch'inizi push edin (`git push origin feature/AmazingFeature`)
5. Pull Request açın

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır - [LICENSE](LICENSE) dosyasına bakın.

## 📞 İletişim

- **Geliştirici**: [Gamze Aydın](https://github.com/gamzeaydinnn)
- **Email**: [contact@example.com](mailto:contact@example.com)
- **Proje**: [https://github.com/gamzeaydinnn/katana](https://github.com/gamzeaydinnn/katana)
