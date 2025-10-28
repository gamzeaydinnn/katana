# Katana-Luca Entegrasyonu — Detaylı Proje İncelemesi ve Eylem Planı

Bu belge, proje kaynak kodu ve mevcut çalışma durumu temel alınarak profesyonel, hatasız bir admin paneli ve güvenli entegrasyon sağlamak için tespit ettiğim eksiklikleri, önceliklendirilmiş düzeltme/adım listesini ve uygulanabilir talimatları madde madde sunar.

Not: Aşağıdaki dosya yolları ve sınıf isimleri repository içindeki gerçek dosyalara referans verir — uygulamaya başlamadan önce branch/commit üzerinde yedek ve kod incelemesi yapmanızı öneririm.

## Hızlı Özet (1–2 cümle)

- Mevcut backend: ASP.NET Core (.NET 8), EF Core, Katana/Luca API client'ları, bir pending write queue ve RetryPendingDbWritesService bulunuyor.
- Yapılması gerekenler: SQL Server üretim hedefi olarak sabitlenmiş durumda (SQLite fallback kaldırıldı), admin-onaylı stok düşümü workflow'u tamamlanmalı, logging/telemetri, performans (log sorguları) ve güvenlik iyileştirmeleri uygulanmalı.

---

## 1. Kritik (High) — hemen uygulanmalı

1.1 Admin-onaylı sipariş → bekleyen stok düzeltmesi workflow'u (zorunlu)

- Amaç: Dış kaynaklı bir sipariş geldiğinde stok ANINDA düşürülmemeli; önce `PendingStockAdjustments` kaydı oluşturulacak, admin paneli üzerinden onaylandıktan sonra gerçek stoğa yansıtılacak.
- Gerekenler (backend):
  - Entity: `Katana.Data.Models.PendingStockAdjustment` (mevcut, kontrol et: tekil tanım)
  - DbSet ve EF konfigürasyonu: `IntegrationDbContext` (index'ler ve unique constraint oluşturulmuş).
  - API endpoints:
    - POST `/api/admin/pending-stock-adjustments` — yeni pending kaydı oluştur (işleme alınan siparişin external ID'si, SKU, quantity, requestedBy).
    - GET `/api/admin/pending-stock-adjustments` — filtrelenebilir liste (status, tarih aralığı, sku, externalOrderId).
    - POST `/api/admin/pending-stock-adjustments/{id}/approve` — approve (onaylayan kullanıcı, timestamp) → onaylanınca:
      - transactional olarak: decrement stock (Stock table veya uygun servis), mark ApprovedAt/ApprovedBy, create AuditLog; publish domain event / notification.
    - POST `/api/admin/pending-stock-adjustments/{id}/reject` — reddetme sebebi ile işaretle.
  - Business logic:
    - İzin kontrolü: yalnızca Admin/StockManager rolü approve/reject yapabilmeli.
    - Concurrency: Approve işlemi sırasında optimistic concurrency veya DB transaction kullanarak duplicate apply önlenmeli.
    - Error handling: Eğer stoğun düşülmesi sırasında hata oluşursa status "Failed" ve detaylı ErrorLog/Notification oluştur.
- UI (admin panel):
  - Pending list view: hızlı filtreler, satır içi onay/reject butonları, detay modalı.
  - Onay workflow: onaylamadan önce basit doğrulama (stokta yeterli mi?), onay sonrası görsel geri bildirim.
- Testler: unit test (approve happy path + reject + concurrent approve) ve e2e (admin approves flow).
- Acceptance criteria: Yeni sipariş geldiğinde hemen stoğu değiştirmeyen, admin onayı sonrası stok değişikliğini garanti eden, tekrar uygulanamayacak bir süreç.

- Admin bildirimleri (zorunlu): Katana'dan gelen inventory event'leri (ör. stok azalması, stok artışı veya diğer stok değişiklikleri) admin paneline bildirim olarak iletilecek; kalıcı stok değişikliği admin onayıyla uygulanacak.

- Gereksinimler:
  - Backend: `PendingStockAdjustments` kaydı oluşturulduğunda bir domain event publish edilecek (ör. `PendingStockAdjustmentCreated` veya genel `InventoryEventCreated`). Event payload'ında eventType (Decrease/Increase/Adjustment) belirtilecek.
  - Bildirim yolları: anlık in-app (SignalR) öncelikli; ayrıca e-posta ve webhook opsiyonel olarak desteklenecek.
  - Bildirim içerikleri: EventType, ExternalOrderId (varsa), SKU, Quantity, RequestedBy, RequestedAt, link to admin pending detail.
  - UI: admin panelinde bildirim tepsisi / badge, toast/alert ve pending listesine doğrudan yönlendirme.
  - Güvenlik/İzin: yalnızca uygun rollere bildirim gösterilecek; detay erişimi role-based kontrol ile korunacak.
  - Dayanıklılık: bildirim gönderimi başarısız olursa retry mekanizması, DLQ ve audit log olacak.
- Acceptance criteria (bildirimler):

  - Katana'dan gelen her inventory event (azalış/artış/düzeltme vb.) için admin panelinde görünür bir bildirim oluşturuluyor.
  - Bildirim tıklandığında ilgili pending kaydın detay sayfası açılıyor ve event tipi net görülebiliyor.
  - Admin onaylamadan kalıcı stok değişikliği uygulanmıyor; onay mekanizması eventType'a uyumlu (ör. artışlar için hızlı onay seçeneği) olmalı.
  - Bildirim transient olsa dahi pending kayıt admin listesinde persistent olarak görünmeye devam ediyor.

    1.2 Veritabanı migration uyumu ve prod readiness

- Mevcut durum: DB (katanaluca-db) zaten tablolar içeriyor ancak `__EFMigrationsHistory` boştu; manuel olarak migration id'leri eklenerek senkronize edildi.
- Yapılacaklar:

  - (Short-term) `__EFMigrationsHistory`'e eklenen kayıtların bir dokümantasyonunu tut (hangi migration id, kim ekledi, neden).
  - (Long-term) Üretim DB'si için migration strategy belirle: migration'ları doğrudan çalıştırmadan önce yedek al ve migration'ları stage ortamında test et.
  - Otomasyon: CI/CD pipeline'a `dotnet ef migrations script` ve `sqlpackage`/run-sql adımı ekle — rollback planı hazır olsun.

    1.3 Logging ve log hacmi kontrolü

- Mevcut: LoggingService, AuditLogs ve ErrorLogs tabloları mevcut; şu an DB'ye log persist'i configurable fakat yoğun log yazımı performans sorunlarına sebep oluyor.
- Yapılacaklar:

  - Varsayılan olarak yalnızca Warning+ veya Error seviyelerini DB'ye yaz (config: `LoggingOptions:PersistMinimumLevel`).
  - Error/Audit tabloları için uygun indexler oluştur (zaten migration'da bazı indexler var — doğrula): `ErrorLogs(Level, CreatedAt)`, `AuditLogs(EntityName, ActionType, Timestamp)`.
  - Retention policy: eski logları purge eden bir arka plan görevi ekle (örn. 90 gün).
  - Monitoring: Slow query loglarını capture et (Application Insights/Elastic) ve LogsController sorgularını optimize et (keyset pagination yerine OFFSET/FETCH yerine cursor veya indexed queries).

    1.4 Pending DB write queue + retry worker resilientify

- Mevcut: `PendingDbWriteQueue` ve `RetryPendingDbWritesService` var. İyi.
- Yapılacaklar:
  - Ensure durability: очередь içeriğini (özellikle önemli audit/failed writes) kısa süreli process crash'lerinden sonra kaybetmemek için opsiyon: küçük lokal SQLite/SQL table backstore veya persistent queue (e.g., Azure Storage Queue, RabbitMQ). Eğer in-memory ise restart kayıpları olabilir.
  - Retry policy: exponential backoff, max attempts, DLQ (dead letter) ve alerting.

---

## 2. Yüksek (Medium) — kısa vadede yapılmalı

2.1 LogsController performans

- Mevcut: OFFSET/FETCH, GROUP BY sorguları zaman zaman 15–60s. Optimize edilmesi gerek.
- Öneriler:

  - Add indexes used by WHERE/OREDR BY clauses (CreatedAt DESC, Level, Category) — migration'ı doğrula.
  - Replace OFFSET pagination for large pages with keyset pagination (WHERE CreatedAt < @cursor ORDER BY CreatedAt DESC LIMIT @pageSize).
  - Pre-aggregate heavy stats in a scheduled job (e.g., daily counts) for dashboard.

    2.2 Security hardening

- Yapılacaklar:

  - Secret management: appsettings.Development.json içinde gerçek API keys ve DB passwords olmamalı. Use user secrets or environment variables for dev.
  - JWT: Ensure secret key is stored securely, rotate keys periodically.
  - Validate all external API inputs and sanitize logging to avoid log injection.
  - Rate limiting on public endpoints.

    2.3 Telemetry & health

- Add Application Insights or another APM.
- Health checks: extend to check external Katana/Luca endpoints and queue health.

  2.4 Tests

- Expand unit tests for new admin approval flow and existing critical services.
- Add integration tests that run against a disposable SQL Server (mssql container) in CI.

---

## 3. Orta / Düşük (Low) — planlı olarak

3.1 Frontend admin panel polish

- Improve UX for pending approvals: bulk approve, search, sorting, per-row actions, optimistic UI updates.
- Show audit trail per pending record.

  3.2 Documentation

- README: dev run steps, migration commands, how to seed `__EFMigrationsHistory` if restoring from an existing DB.
- Operational runbook: backup, restore, failover steps for RDS.

  3.3 Localization

- Logging messages are changed to Turkish in LoggingService; decide whether the entire app should be localized (frontend + API messages) and implement resource files.

---

## 4. Mühendislik kabul kriterleri / Test checklist

- Admin-onay akışı:
  - [ ] Yeni sipariş kaydı pending olarak oluşuyor.
  - [ ] Admin listede görüp approve/reject yapabiliyor.
  - [ ] Approve sonrası stock miktarı DB'de doğru şekilde azaldı.
  - [ ] Approve işlemi idempotent: ikinci approve denemesi hata vermez ve tekrar stok düşmez.
- Migrations & DB:
  - [ ] Production DB yedeği alındı.
  - [ ] `dotnet ef database update` başarıyla çalıştı (stage ortamında test edildi).
- Logging & performance:
  - [ ] Error/Warning only persisted when config açık.
  - [ ] LogsController sorguları 1s altına düştü (P95 hedefi).
- Security:
  - [ ] Secrets not in repo.
  - [ ] JWT validation and role-based authorization enforced on admin endpoints.

---

## 5. Dosya / Kod referansları (başlamanız gereken yerler)

- Backend entry / DI: `src/Katana.API/Program.cs`
- DbContext: `src/Katana.Data/Context/IntegrationDbContext.cs`
- Pending model: `src/Katana.Data/Models/PendingStockAdjustment.cs`
- Logging service: `src/Katana.Infrastructure/Logging/LoggingService.cs`
- Retry worker: `src/Katana.Infrastructure/Workers/RetryPendingDbWritesService.cs`
- Logs endpoints: `src/Katana.API/Controllers/LogsController.cs`
- Existing admin logic/service: `src/Katana.Business/Services/AdminService.cs` (incele, yoksa oluştur)
- Frontend admin panel: `frontend/katana-web/src/...` (Admin components)

---

## 6. Örnek migration / SQL (kısa not)

- `AddPendingStockAdjustmentsAndLogIndexes` migration dosyası zaten oluşturulmuş. Deploy sırasında EF `database update` çalıştırıldığında migration'lar uygulanacak (veya `__EFMigrationsHistory` ile senkronize edilecek).

---

## 7. Tahmini iş yükü (rough estimates)

- Admin approval backend (endpoints + business logic + tests): 1–2 iş günü
- Admin frontend UI (list, approve/reject, tests): 1–2 iş günü
- Logging/perf tuning + index verification: 0.5–1 gün
- CI/CD + migration safe-run + runbook: 0.5–1 gün
- Total (MVP production-ready admin flow): 3–6 iş günü (1–2 geliştiriciyle)

---

## 8. Riskler ve dikkat edilmesi gerekenler

- Production DB üzerinde migration çalıştırmadan önce tam yedek alın.
- Manual seeding of `__EFMigrationsHistory` (yaptığınız gibi) güvenli ama dokümante edilmeli ve onay süreçleriyle eşleştirilmeli.
- Concurrency: online satışlarda dış sistem aynı siparişi birkaç kez gönderebilir — unique constraint on `ExternalOrderId` + idempotency checks zorunlu.

---

## 9. Hemen yapılacak 5 maddelik checklist (ilk 2 gün)

1. Implement API endpoints ve business logic for pending adjustments (approve/reject). (High)
2. Implement frontend admin pending list with approve/reject buttons. (High)
3. Add unit + integration tests for approve flow and concurrency. (High)
4. Verify / create DB indexes for logs and replace OFFSET pagination with keyset pagination in LogsController. (Medium)
5. Add retention job for logs and configure monitoring (APM). (Medium)

---

## 10. Sonraki adımlar (benim tarafımdan yapılabilecekler)

- İsterseniz hemen backend approve/reject endpoint'larını oluşturup unit testlerini yazabilirim.
- İsterseniz admin panel için React bileşenleri scaffoldingini oluşturup API ile entegrasyon sağlayabilirim.
- Ayrıca LogsController sorgularını optimize etmek için örnek refactor sunabilirim.

---

Bu dosya, projenin üretime hazırlanması için takip edilecek yol haritasıdır. Hangi maddeden başlamamı istersiniz? (Benim önerim: önce admin approval backend + tests, sonra frontend.)
