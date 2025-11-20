# Katana-Luca Integration System 🎯

**Katana (MRP/ERP)** ile **Luca Koza (Muhasebe)** sistemleri arasında otomatik veri entegrasyonu sağlayan kapsamlı bir .NET Core projesidir.

## 🚨 IMPORTANT: Recent Code Audit

**Latest Analysis:** 2025  
**Branch Status:** Synced with master (commit 9963dde)

📊 **Open Items:**

- 🔐 JWT secret management for production (use env/Key Vault)
- 🔎 Review remaining `[AllowAnonymous]` usage (Health/Auth expected; Webhook via API key)
- 🧪 CI/CD pipeline + optional E2E job (Playwright)

📋 **Action Items:** See [TODO.md](TODO.md) for the current open tasks  
📄 **Open Report:** See [IMPLEMENTATION_REPORT.md](IMPLEMENTATION_REPORT.md) for open issues only

## 📋 Proje Amacı

- **Katana**: Üretim, stok ve satış verilerini yönetir
- **Luca Koza**: Mali kayıtları ve fatura/muhasebe işlemlerini yönetir
- **Entegrasyon**: Katana'dan gelen stok, satış, fatura ve muhasebe verilerini otomatik olarak Luca'ya aktarır
- **Çift Yönlü**: Gerektiğinde Luca'dan Katana'ya cari hesap veya mali tablo verisi aktarımını destekler
- **Admin Approval**: Pending stock adjustments workflow with real-time SignalR notifications

**Sonuç**: Şirketler manuel veri girişi yapmadan iki sistemi entegre bir şekilde kullanabilir.

## 🏗 Proje Mimarisi

```
katana/
├── src/
│   ├── Katana.Core/             # Domain entities, DTOs, interfaces (90+ files)
│   ├── Katana.Data/             # EF Core, migrations, DbContext (50+ files)
│   ├── Katana.Business/         # Services, use cases, validators (80+ files)
│   ├── Katana.API/              # Controllers, SignalR hubs, middleware (112+ files)
│   └── Katana.Infrastructure/   # Logging, workers, API clients (60+ files)
├── frontend/
│   └── katana-web/              # React + TypeScript + Material-UI (38 TSX files)
│       ├── src/components/      # Admin, Dashboard, Layout components
│       ├── src/services/        # SignalR client, API services
│       └── src/theme/           # MUI theme configuration
├── tests/
│   └── Katana.Tests/            # Unit & integration tests (4 test files)
├── scripts/
│   └── admin-e2e.ps1            # PowerShell E2E test script
└── docs/
    ├── api.md                   # API documentation
    ├── mapping.md               # Data mapping guide
    └── project_audit_and_action_plan.md
```

## 🧩 Katman Yapısı

### 🔹 Katana.Core (Domain Layer)

- **Entities**: Product, Stock, Invoice, Customer modelleri
- **DTOs**: KatanaStockDto, LucaInvoiceDto, SyncResultDto
- **Helpers**: MappingHelper, JwtTokenHelper, HashingHelper

### 🔹 Katana.Data (Data Access Layer)

- **DbContext**: IntegrationDbContext (EF Core)
- **Repositories**: Repository pattern implementasyonu
- **Models**: IntegrationLog, MappingTable, FailedSyncRecord
- **Migrations**: Veritabanı migration dosyaları

### 🔹 Katana.Business (Business Logic Layer)

- **KatanaService**: Katana API çağrıları ve veri çekme
- **LucaService**: Luca'ya veri gönderimi (API/CSV/XML)
- **SyncService**: Tüm entegrasyon sürecini koordine eder

### 🔹 Katana.API (Presentation Layer)

- **Controllers**: SyncController, ReportController, MappingController
- **Middleware**: ErrorHandling, Authentication, CORS
- **Endpoints**: REST API endpoints

### 🔹 Katana.Infrastructure (Infrastructure Layer)

- **Logging**: Serilog ile dosya + DB loglama
- **Jobs**: Quartz.NET ile zamanlanmış senkronizasyon
- **Config**: Yapılandırma yönetimi
- **Workers**: Background services

### 🔹 Katana.Tests (Test Layer)

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
- Node.js 18+ (Frontend için)
- SQL Server
- Visual Studio 2022 / VS Code

### Kurulum Adımları

```powershell
# 1. Projeyi klonlayın
git clone https://github.com/gamzeaydinnn/katana.git
cd katana

# 2. Backend Setup
dotnet restore
dotnet ef database update --project src\Katana.Data

# 3. Frontend Setup
cd frontend\katana-web
npm install

# 4. Yapılandırma dosyasını düzenleyin
# src/Katana.API/appsettings.json dosyasını düzenleyin (aşağıya bakın)

# 5. Backend'i çalıştırın
cd ..\..
dotnet run --project src\Katana.API --urls "http://localhost:5055"

# 6. Frontend'i çalıştırın (yeni terminal)
cd frontend\katana-web
npm start  # http://localhost:3000
```

### Yapılandırma

`appsettings.json` dosyasında aşağıdaki ayarları yapılandırın:

```json
{
  "ConnectionStrings": {
    "SqlServerConnection": "Server=localhost,1433;Database=KatanaDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;"
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

### Pending Stock Adjustments (Admin Workflow)

```bash
# List pending adjustments
GET /api/adminpanel/pending-adjustments

# Approve adjustment
POST /api/adminpanel/pending-adjustments/{id}/approve

# Reject adjustment
POST /api/adminpanel/pending-adjustments/{id}/reject

# Create test pending
POST /api/adminpanel/pending-adjustments/test-create
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

### E2E Test Script

```powershell
# PowerShell E2E test (login → create → approve workflow)
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\admin-e2e.ps1
```

### SignalR Real-time Notifications

Frontend automatically connects to SignalR hub at `/hubs/notifications`:

- Event: `PendingCreated` - New pending adjustment created
- Event: `PendingApproved` - Adjustment approved by admin
- Event: `PendingRejected` - Adjustment rejected

## 🔐 Güvenlik

- **TLS**: HTTPS zorunlu
- **Authentication**: JWT Bearer token (480 min expiry)
- **Authorization**: Role-based (Admin, StockManager)
- **Secrets**: Environment variables önerilir (production'da Azure Key Vault)
- **Audit**: Tüm işlemler AuditLogs tablosunda loglanır
- **CORS**: Configured origins only (localhost:3000 for dev)

**Known Open Items:**

- ⚠️ Review `[AllowAnonymous]` usage (Health/Auth expected; Webhook via API key)
- 🔑 JWT secret hardcoded in appsettings.json (use env/Key Vault in production)

### Alt Kullanıcı Ekleme (Admin)

Uygulamada alt kullanıcı eklemek için:

- Sol menüden "Admin Paneli"ne girin
- Üst sekmelerden "Kullanıcılar" sekmesini açın
- Formdaki alanları doldurup "Kullanıcı Ekle" butonuna basın

Teknik arka plan:

- Endpoint: `POST /api/Users` (yalnızca Admin)
- DTO: `{ username, password, role, email? }`
- Listeleme: `GET /api/Users`
- Silme: `DELETE /api/Users/{id}`

> Not: Roller `Admin`, `Manager`, `Staff` olarak kullanılabilir. Varsayılan `Staff`.

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
dotnet test tests/Katana.Tests

# Specific test file
dotnet test tests/Katana.Tests --filter FullyQualifiedName~PendingStockAdjustmentServiceTests

# Test coverage raporu
dotnet test --collect:"XPlat Code Coverage"

# Frontend tests (when implemented)
cd frontend/katana-web
npm test
```

**Current Test Status:**

- Backend: ~15 unit tests, 1 integration test (30% coverage)
- Frontend: 0 tests (setupTests.ts exists but no test files)
- E2E: 1 PowerShell script (admin-e2e.ps1)

**Missing Tests (High Priority):**

- Concurrent approval scenarios
- Role-based authorization tests
- SignalR event publishing tests
- Frontend component tests

See [IMPLEMENTATION_REPORT.md](IMPLEMENTATION_REPORT.md#-test-coverage) for detailed test analysis.

## 📚 Dokümantasyon

### Kullanıcı Dokümantasyonu

- [API Dokümantasyonu](docs/api.md)
- [Veri Mapping Kılavuzu](docs/mapping.md)
- [Luca/Koza Kavram Eşleştirme ve Akış](docs/luca-mapping.md)

### Geliştirici Dokümantasyonu

- 📊 **[Kod Audit Özeti](AUDIT_SUMMARY.md)** - Hızlı durum raporu (1 dakikalık okuma)
- 📄 **[Detaylı İmplementasyon Raporu](IMPLEMENTATION_REPORT.md)** - Kapsamlı kod analizi (30+ sayfa)
- 📋 **[TODO ve Aksiyon Planı](TODO.md)** - Sprint breakdown ve öncelikli görevler
- 📖 **[Proje Audit Planı](docs/project_audit_and_action_plan.md)** - Orijinal audit dokümanı

### Önemli Bulgular (Latest Audit - 2025)

1. **CRITICAL:** AdminController role-based authorization eksik ([Details](AUDIT_SUMMARY.md#1-admincontroller-authorization-gap-))
2. **HIGH:** Frontend SignalR UI update incomplete ([Details](AUDIT_SUMMARY.md#2-frontend-signalr-ui-update))
3. **MEDIUM:** Test coverage 30% (target: 60%) ([Details](IMPLEMENTATION_REPORT.md#-test-coverage))
4. **MEDIUM:** LogsController performance issues ([Details](IMPLEMENTATION_REPORT.md#1-logscontroller-slow-queries-))

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
