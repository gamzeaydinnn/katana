# Katana Projesi - Kod Denetim ve Eksikler Raporu (Türkçe)

Bu belge, çalışma alanındaki (workspace) Katana projesinin hızlı bir denetimini ve tespit edilen eksikleri, riskleri ve önerileri madde madde verir. Amaç: öncelikli hataları, güvenlik ve mimari sorunlarını, geliştirilmeye açık alanları ve yapılacakları kısa ve uygulanabilir şekilde sıralamaktır.

Not: Dosya yolları proje köküne göredir (ör. `src/Katana.API/...`, `frontend/katana-web/...`).

---

## 1) Özet (kısa)

- Backend: ASP.NET Core 8, Entity Framework Core, Serilog, Quartz zamanlayıcı; genel mimari iyi yapılandırılmış. Ancak çalışma zamanı hataları ve konfigürasyon sorunları (JWT doğrulama hataları, önce SQL Server bağlantı hataları) gözlendi.
- Frontend: React + MUI, axios; genel tasarım iyi fakat bazı UI/UX eksiklikleri ve token yönetimi hataları vardı (malformed token gönderimi). Şube seçimi için modal eklendi ama birkaç iyileştirme gerekli.
- Veri katmanı: `IntegrationDbContext` kapsamlı ancak seed/DB migration yönetimi eksik; prod DB bağlantı ve user yetkilendirme (SQL login) ile ilgili sorunlar kaydedildi.

---

## 2) Kritik / Yüksek Öncelikli Bulgular (Hemen düzeltilmeli)

1. JWT parsing hatası (IDX14100: JWT is not well formed)

   - Yer: Backend loglarında yoğun bir şekilde görülüyor.
   - Sebep: Frontend `localStorage.authToken` içinde JWT formatında olmayan bir string (ör. API key veya boş değer) Authorization header olarak gönderiliyor.
   - Etki: API her istek için Bearer doğrulaması sırasında uyarı/failure çıkartıyor, bazı endpoint'lerde 401/500 davranışları tetiklenebiliyor.
   - Öneri: Frontend interceptor güncellendi (yapıldı). Ayrıca backend, Bearer middleware konfigürasyonunda toleranslı logging/handle yapılabilir.
   codex scan --include "

2. Veritabanı bağlantı/kimlik doğrulama hataları

   - Yer: Önceki oturumlarda `Login failed for user 'admin'` hatası raporlandı.
   - Durum (güncellendi): Geliştirme ortamında eksik olan `katanaluca-db` veritabanı oluşturuldu ve bağlantı doğrulandı — ADO.NET ile `katanaluca-db` açılabiliyor ve uygulama Development modunda başlatıldığında health endpoint'i başarılı yanıt veriyor. Quartz ve background worker (RetryPendingDbWritesService) da başlatıldı.
   - Sebep (muhtemel üretim riski): Uzak RDS örneğinde veritabanı yoksa veya login kullanıcısının (admin) varsayılan veritabanına erişim izni yoksa 4060 hatası oluşur. Geliştirme için DB oluşturuldu; production için credential/permission doğrulaması hâlâ zorunlu.
   - Etki: DB erişim hataları admin paneli ve log gösteriminde 500 sonuçlarına yol açıyor; geliştirme sırasında bu sorun giderildi ancak prod ortamında kontroller yapılmalı.
   - Öneri: Dev ortamı için `appsettings.Development.json` içinde SQLite (`Data Source=katanaluca-dev.db`) fallback seçeneği kullanılabilir. Production için ise:
     - Veritabanı `katanaluca-db` varlığını teyit edin veya oluşturun.
     - Uzak SQL login (ör. `admin`) için veritabanı içinde uygun kullanıcı-mapping ve `CONNECT`/`db_owner` yetkilerini verin.
     - Credential'ları güvenli şekilde saklayın (KeyVault/SecretManager) ve connection string'lerin tam/parolalı olduğundan emin olun.
     codex scan --include "
Katana.API/appsettings.json,
Katana.API/appsettings.Development.json,
Katana.API/Program.cs,
Katana.Data/Context/IntegrationDbContext.cs,
Katana.Data/Context/IntegrationDbContextFactory.cs,
Katana.Data/Migrations/**,
Katana.Data/Configuration/KatanaApiSettings.cs,
Katana.Data/Configuration/SyncSettings.cs,
Katana.Data/Configuration/LucaApiSettings.cs,
Katana.Infrastructure/Services/PendingDbWriteQueue.cs,
Katana.Infrastructure/Workers/RetryPendingDbWritesService.cs
" --exclude "
**/node_modules/**,
**/bin/**,
**/obj/**,
**/build/**,
**/dist/**,
**/.next/**,
**/logs/**,
**/*.map,
**/*.d.ts
" --focus "
Fix SQL Server authentication and connection issues (Login failed for user 'admin').
Verify connection strings in appsettings and Development settings.
Ensure IntegrationDbContext properly connects to SQL Server and applies migrations automatically.
Add fallback SQLite configuration for dev mode (Data Source=katanaluca-dev.db).
Confirm RetryPendingDbWritesService and Quartz jobs start without DB permission errors.
Ensure production DB users have correct login mapping and permissions.
"



3. Backend 500 hataları (özellikle Admin panel çağrıları)
   - Yer: Frontend konsolunda `/api/adminpanel/*` çağrıları 500 dönüyordu.
   - Sebep: İlk tespitler DB erişim/kimlik hatalarına bağlıydı; ayrıca bazı middleware veya servislerin eksik bağımlılık çözümlemesi olabilir.
   - Öneri: Uygulamayı Development modunda çalıştırıp (ASPNETCORE_ENVIRONMENT=Development) detaylı stacktrace topla. Critical: `ErrorHandlingMiddleware` ve logger servisleri startup sırasında hataya neden olmamalı.

   💻 Codex Komutu (Hazır, Token Dostu)
codex scan --include "
Katana.API/Program.cs,
Katana.API/Middleware/ErrorHandlingMiddleware.cs,
Katana.API/Middleware/AuthMiddleware.cs,
Katana.API/Controllers/AdminController.cs,
Katana.Infrastructure/Logging/LoggingService.cs,
Katana.Infrastructure/Logging/AuditService.cs,
Katana.Infrastructure/Logging/SerilogExtensions.cs,
Katana.Business/Services/AdminService.cs,
Katana.Business/Interfaces/IAdminService.cs,
Katana.Business/Services/ErrorHandlerService.cs,
Katana.Business/Interfaces/IErrorHandler.cs,
Katana.Business/Jobs/RetryJob.cs,
Katana.Infrastructure/Workers/RetryPendingDbWritesService.cs
" --exclude "
**/node_modules/**,
**/bin/**,
**/obj/**,
**/build/**,
**/dist/**,
**/.next/**,
**/logs/**,
**/*.map,
**/*.d.ts
" --focus "
Fix 500 Internal Server Errors from /api/adminpanel endpoints.
Check middleware (ErrorHandlingMiddleware, AuthMiddleware) for unhandled exceptions or missing DI services.
Verify AdminService and its dependencies are properly registered in Program.cs and DI container.
Ensure ErrorHandlerService and Serilog logging are initialized correctly at startup.
Run app in Development mode (ASPNETCORE_ENVIRONMENT=Development) to capture full stacktraces and identify null dependency or configuration issues.
"


---

## 3) Güvenlik ve Konfigürasyon (Önemli)

1. NuGet uyarısı: `Microsoft.Extensions.Caching.Memory` 8.0.0'da bilinen CVE uyarısı (NU1903)

   - Dosya: seen in runtime logs.
   - Öneri: Paketleri tarayıp güvenli versiyona yükselt.

2. JWT Key/Issuer/Audience konfigürasyonu

   - Dosya: `Program.cs` ve `appsettings*.json` içinde `Jwt` bölümü zorunlu.
   - Öneri: Secret anahtar güvenli saklanmalı (kopyalanmışsa değiştir), prod için KeyVault/SecretManager kullan.

3. CORS
   - Mevcut: `AllowFrontend` policy localhost:3000 izinli, credentials true — uygun.
   - Öneri: Prod ortamlar için whitelisting ve secure ayarlar gözden geçirilmeli.

---

## 4) Backend - Kod, Mimari ve Öneriler

1. `LucaProxyController`

   - Güncelleme: Proxy `branches` endpoint'i normalize yanıt döndürüyor (yapıldı). Ancak:
     - Hala remote response içeriği farklı olabilir; logging açık tutulmalı kısa süre için.
   - Öneri: Proxy her zaman `{ branches: [ { id, name, ... } ], raw: ... }` döndürecek şekilde standardize edilmeli.

2. `AuthController` ve token yönetimi

   - `AuthController.Login` JWT üretiyor; frontend login akışı token'ı `localStorage`'a kaydetmeli. Ayrıca token kaydı varsa on startup doğrulanmalı.
   - Öneri: Frontend login sonrası token kaydı garanti altına alınsın ve kötü formatlı token otomatik temizlensin.

3. `IntegrationDbContext`

   - Çok sayıda DbSet ve konfigürasyon var — iyi.
   - Eksik: Migrations (EF Migrations) dosyaları ve deployment süreci yok ya da kapatılmış.
   - Öneri: `dotnet ef migrations add Initial` ve `dotnet ef database update` için geliştirici yönergesi ekle. CI'da migration check ekle.

4. Error handling / Logging

   - Serilog kullanılıyor; fakat `ErrorHandlingMiddleware` startup sırasında scoped servisleri konstruktörde çözmemeli. (Noted earlier.)
   - Öneri: Middleware içinde gerektiğinde `context.RequestServices.GetRequiredService<...>()` kullan.

5. Background jobs (Quartz)
   - Joblar eklenmiş (StockSyncJob, InvoiceSyncJob). Prod ortamda planlama ve enqueuing test edilmeli.
   - Öneri: Jobs'ın izolasyon testi ve idempotency kontrolleri oluşturulsun.

---

## 5) Frontend - Kod, UX, ve Öneriler

1. Token yönetimi

   - Hata: Authorization header'a JWT olmayan string gönderiliyordu (düzeltildi: interceptor artık 3 parça kontrolü yapıyor).
   - Öneri: Login sonrası token set/clear rutinleri ve `AuthContext` veya Redux ile yönetim daha sağlam olur.

2. Branch Selection UI

   - Mevcut: `BranchSelector` modal eklendi ve `App.tsx` üzerinden otomatik seçim kuralları uygulandı.
   - Sorun: Başlangıçta floating button vardı; tasarım temizlendi ve kontrol header içine taşındı (tamamlandı).
   - Eksik: Branch listesinde sadece 1 eleman görünmesi, backend yanıtının nasıl geldiğine bağlıydı; backend normalize yapıldı.
   - Öneri: Branch listesi artık modal'da doğru gösterilmeli. Kullanıcı seçimi `preferredBranchId` localStorage'da saklanıyor — opsiyonel: kullanıcı profilinde sakla.

3. UI hataları

   - Uyarı: "Encountered two children with the same key" — list render ederken key'ler unique değil.
   - Öneri: `BranchSelector` ve listeler için unique key (örn. id veya index fallback) kullanıldı; diğer listelerde de kontrol et.

4. Dependency versions
   - package.json: birkaç package (MUI) güncel; test kütüphanelerinin sürümleri bazıları eski/uyumsuz görünüyor.
   - Öneri: `npm audit` ve `npm outdated` çalıştır, kritik güvenlik güncellemelerini yap.

---

## 6) Tests, CI/CD ve DevOps

1. Testler

   - Mevcut: `frontend` test scaffold var; backend unit tests/ integration tests görünmüyor.
   - Öneri: Minimum birim test seti (Business logic için) ekle. En azından `Katana.Business` için birkaç unit test ve `IntegrationDbContext` için in-memory db testleri.

2. CI

   - `.github/workflows/ci-cd.yml` var; branch hedefleri kontrol edilmeli.
   - Öneri: CI'da `dotnet build`, `dotnet test`, `npm ci && npm run build` adımları ekle; migration check eklensin.

3. Migrations ve DB provisioning
   - Öneri: `migrations/` klasörü ve `README` içinde DB başlatma talimatları ekle (dev/prod ayrımı). Local dev için Docker Compose (SQL Server veya using SQLite) yedeği düşün.

---

## 7) Önceliklendirilmiş Yapılacaklar (Kısa vadede, önem sırasına göre)

1. (Kritik) Frontend token temizleme + interceptor kuralı — tamamlandı.
   - Verify: Başlangıçta localStorage içinde malformed (JWT olmayan) token varsa, module-load sırasında temizlenir ve request interceptor bu tür tokenları Authorization başlığına eklemez. Doğrulama: Local çalışma sırasında backend artık IDX14100 (JWT malformed) logu üretmiyor.

Recent runtime verification (local)

- Build: `dotnet build` başarılı şekilde tamamlandı (uyarılar mevcut). Uyarılar arasında `NU1903` paket CVE uyarısı ve birkaç nullable/warning notu bulunuyor.
- Uygulama başlatıldı (`dotnet run --project src\Katana.API`) ve aşağıdaki durumlar doğrulandı:
  - Quartz scheduler başlatıldı ve 2 job (StockSyncJob, InvoiceSyncJob) eklendi.
  - `RetryPendingDbWritesService` başlatıldı (interval: 15s).
  - Health endpoint (`/api/Health`) başarılı yanıt döndü.
  - ADO.NET ile `katanaluca-db` veritabanına bağlantı açıldı (DB oluşturma gerekiyordu; development testi sırasında CREATE DATABASE çalıştırıldı ve bağlantı doğrulandı).

Not: Bu doğrulamalar development ortamına yöneliktir; production ortamı için credential doğrulama, DB izinleri ve paket CVE uyarıları hâlâ ele alınmalıdır. 2. (Kritik) Production DB credentials & user/DB kontrolü. Adımlar:

- DB var mı (katanaluca-db)? Oluşturun veya connection string güncelleyin.
- SQL login `admin` yetkileri verin: CREATE DATABASE, CREATE USER/ALTER ROLE as needed.

3. (Yüksek) EF Migrations ekle ve dev/prod DB provisioning talimatı ekle.
4. (Orta) Backend hata günlüklerini (Serilog) review, sensitive data mask, ve ayrıntılı exception logging (Dev vs Prod) ayarla.
5. (Orta) Frontend: Header'da seçilen şubenin gösterimi ve listelerde duplicate key kontrolleri — yapıldı/iyileştir.
6. (Düşük) Paket güncellemeleri, audit ve CI eklemeleri.

---

## 8) Hızlı Onarım Komutları / Nasıl Başlatılır (kısa)

- Backend (Development):

```powershell
# API kök dizininde
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src\Katana.API\Katana.API.csproj
```

- Frontend (Development):

```powershell
cd frontend\katana-web
npm install
npm start
```

- EF Migrations (örn. local SQLite kullanıyorsanız):

```powershell
# backend projesi kökünde
dotnet tool install --global dotnet-ef
cd src\Katana.API
dotnet ef migrations add Initial -s ..\Katana.API.csproj -p ..\Katana.Data\Katana.Data.csproj
dotnet ef database update
```

> Not: Yukarıdaki migration komutları proje yapısına göre küçük ayarlama gerektirebilir (DbContext proje ve startup/root seçimleri).

---

## 9) Önerilen Dosya/Configuration İyileştirmeleri (Kısa liste)

- `appsettings.Development.json` ve `appsettings.Production.json` örnekleri (secrets şablonu) — `appsettings.Production.json.sample` ekle.
- `README.md` kökünde: adım adım development setup (dotnet, node, env vars, DB provisioning).
- `PROJECT_AUDIT.md` bu dosya — repoda saklanmalı, ekiple paylaşılmalı.

---

## 10) Sonuç

Bu rapor, projedeki gözlemlenen eksiklerin bir başlangıç listesidir. En kritik iki konu: (1) prod DB erişimi ve yetkilendirme, (2) uygulamanın prod ortamında güvenli token, secret yönetimi. Bunlar çözülmeden bazı admin fonksiyonları ve tam entegrasyon testleri devamlı sorun çıkarabilir.

İsterseniz şimdi öncelik 1 (DB credentials + role ataması) için adım adım komut/metin hazırlayıp uygulayabilirim. Ayrıca isterseniz frontend'e seçilen branch bilgisini header'a kalıcı gösterme (veya kullanıcı profiline kaydetme) ekleyebilirim.

---

Raporu ürettim. Sonraki adımı (öncelik 1 veya başka) seçin, ben uygulamaya geçeyim.
