# Katana — Hızlı Durum Özeti ve Eylem Listesi

Kısa, madde madde: yapılanların yanına ✓ koydum; eksikleri kısa yazdım. Uzun açıklama çıkarıldı — isterseniz detaylı adımları tekrar eklerim.
Yapıldı (done):

- ✓ PendingStockAdjustment create merkezi hale getirildi (`PendingStockAdjustmentService.CreateAsync`).
- ✓ Approve akışı uygulandı (claim + transaction) ve stok güncellemesi yapılıyor.
- ✓ `IPendingNotificationPublisher` eklendi; API tarafında `SignalRNotificationPublisher` var ve publish loglanıyor.
  Eksikler (kısa, öncelik sırasına göre):

1. Frontend: SignalR client entegrasyonu ve admin pending list'in gerçek zamanlı güncellenmesi — (yüksek).
2. Güvenlik: Approve/Reject endpoint'lerine açık rol-based authorization kontrolü (Admin/StockManager) — (yüksek).
3. Dayanıklılık: Publish için durable retry/DLQ tasarımı — (orta).
   Hemen yapılacaklar (3 kısa adım):

1) (Şimdi) Dokümanı sadeleştirdim — bu dosya güncellendi.
2) Siz onaylarsanız hemen frontend SignalR scaffold ve client testlerini ekleyebilirim.
3) Ardından approve endpoint'lerine rol kontrolünü ve 1-2 unit testi ekleyeyim.
   Nasıl doğrularsınız (kısa):

- API çalıştırın ve e2e script'i çalıştırın: `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\admin-e2e.ps1`
- Loglarda şu satırları göreceksiniz: "Pending stock adjustment created ...", "Publishing PendingStockAdjustmentCreated ...", "Pending stock adjustment {Id} approved ...", "Publishing PendingStockAdjustmentApproved ...".
  Referans (kısa):

- Program / DI: `src/Katana.API/Program.cs`
- Pending service: `src/Katana.Business/Services/PendingStockAdjustmentService.cs`
- SignalR publisher: `src/Katana.API/Notifications/SignalRNotificationPublisher.cs`
- Admin controller: `src/Katana.API/Controllers/AdminController.cs`
- E2E script: `scripts/admin-e2e.ps1`
  Eğer başlamak isterseniz "frontend" veya "auth" yazın; hemen frontend SignalR scaffold veya approve rol kontrolü ile devam edeyim.

# Katana-Luca Entegrasyonu — Detaylı Proje İncelemesi ve Eylem Planı

Bu belge, proje kaynak kodu ve mevcut çalışma durumu temel alınarak profesyonel, hatasız bir admin paneli ve güvenli entegrasyon sağlamak için tespit ettiğim eksiklikleri, önceliklendirilmiş düzeltme/adım listesini ve uygulanabilir talimatları madde madde sunar.

Not: Aşağıdaki dosya yolları ve sınıf isimleri repository içindeki gerçek dosyalara referans verir — uygulamaya başlamadan önce branch/commit üzerinde yedek ve kod incelemesi yapmanızı öneririm.

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

    # Katana-Luca Entegrasyonu — Durum Özeti, Yapılanlar ve Eylem Planı

    Bu belge, projede yapılan son değişiklikleri, bunların doğrulanmasını ve bir sonraki eylem listesini içerir. Özellikle admin-onaylı stok düzeltmeleri, SignalR bildirimleri, DI düzeltmeleri ve geliştirme/run notlarına odaklanır.

    Not: Dosya yolları repository içindeki gerçek dosyalara karşılık gelir. Değişiklik yapmadan önce ilgili branch/commit üzerinde yedek almanızı öneririm.

    ## Hızlı Özet (1–2 cümle)

    - Proje: ASP.NET Core (.NET 8), EF Core, SignalR, Serilog.
    - Recent changes: pending-stock workflow centralize edildi; create ve approve event'ları publish ediliyor (SignalR); business layer ASP.NET tiplerinden ayrıldı via `IPendingNotificationPublisher`; DI fix yapıldı (IOrderService); frontend/dev test script eklendi (`scripts/admin-e2e.ps1`).

    ***

    ## 1. Ne yapıldı (kısa, teknik özet)

    1. Pending workflow

    - Tüm pending oluşturma işleri `Katana.Business.Services.PendingStockAdjustmentService.CreateAsync` üzerinden yapılacak şekilde merkezileştirildi.
    - Approve işlemi `PendingStockAdjustmentService.ApproveAsync` ile gerçekleştiriliyor; işlem öncesi "claim" (conditional UPDATE) ile başka bir işlem tarafından kullanılması engelleniyor ve onay içinde DB transaction ile stok güncelleme + Stocks tablosuna kayıt eklendi.

    2. Bildirim/publish

    - İş katmanında `Katana.Core.Interfaces.IPendingNotificationPublisher` kullanılıyor. API, SignalR tabanlı `SignalRNotificationPublisher` ile bu arayüzü implemente ediyor.
    - Publish noktalarında (create ve approve) yayın giriş/çıkışları logger ile kaydediliyor; başarısız publish durumunda hata loglanıyor fakat işlem rollback edilmiyor (best-effort publish).

    3. DI ve controller düzeltmeleri

    - `IOrderService` için DI activation hatası çözüldü: concrete `OrderService` kayıt edilip `IOrderService` buna yönlendirildi (Program.cs içinde explicit AddScoped register).
    - `AdminController.GetProductStock` param tipi `long` → `int` olarak düzeltildi ve rota constraint eklendi (`{id:int}`) — EF Find tip eşleşmesi kaynaklı 500 hatası giderildi.

    4. Geliştirme ve test kolaylığı

    - PowerShell tabanlı `scripts/admin-e2e.ps1` eklendi — login → create pending → list → approve akışını otomatikleştirir ve PSReadLine yapıştırma çöküşü problemini önler.
    - JWT doğrulama hatalarının tespiti için Program.cs içinde token validation event'leri için diagnostic logging eklendi.

    5. Build/run notları

    - Geliştirme ortamında `dotnet` süreçleri DLL dosyalarını kilitleyebiliyor; build hatası alınırsa çalışan `dotnet` PID'lerini (ör. `Get-Process dotnet`) kontrol edip sonlandırmak çözüm olmuştur.

    ## 2. Doğrulama / nasıl test edilir (dev ortam)

    Aşağıdaki adımlar, API'yi çalıştırıp pending oluşturma ve onaylama ile publish loglarını doğrulamanız için yeterlidir.

    1. API derleyin ve çalıştırın (PowerShell):

    ````powershell
    # derleme
    dotnet build c:\Users\GAMZE\Desktop\katana\src\Katana.API

    # çalıştırma (örnek olarak 5055 portunu kullanabilirsiniz)
    # Katana — Özet & Kısa Eylem Listesi

    Yapıldı (✓):

    - ✓ PendingStockAdjustment create merkezi (PendingStockAdjustmentService.CreateAsync)
    - ✓ Approve akışı (claim + transaction) ve stok güncellemesi
    - ✓ SignalR publish + loglama (IPendingNotificationPublisher + SignalRNotificationPublisher)
    - ✓ DI fix: IOrderService kayıtlandı
    - ✓ Controller fix: GetProductStock param tipi düzeltildi
    - ✓ `scripts/admin-e2e.ps1` eklendi (login → create → approve)

    Eksik / Önceliklendirilmiş kısa liste:

    1. Frontend: SignalR client ve admin pending list canlı güncelleme — Yüksek
    2. Güvenlik: Approve/Reject için role-based authorization — Yüksek
    3. Dayanıklılık: Publish retry / DLQ (durable) — Orta
    4. Testler: Unit + integration (approve, concurrent) — Yüksek
    5. Performans: LogsController indeks ve keyset pagination — Orta

    Hızlı doğrulama:

    1) API'yi çalıştırın ve e2e script'i çalıştırın:

    ```powershell
    dotnet run --project src\Katana.API --urls "http://localhost:5055"
    powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\admin-e2e.ps1
    ````

    2. Loglarda bu satırları arayın: "Pending stock adjustment created", "Publishing PendingStockAdjustmentCreated", "Pending stock adjustment {Id} approved", "Publishing PendingStockAdjustmentApproved".

    Kısa referans:

    - `src/Katana.API/Program.cs`
    - `src/Katana.Business/Services/PendingStockAdjustmentService.cs`
    - `src/Katana.API/Notifications/SignalRNotificationPublisher.cs`
    - `src/Katana.API/Controllers/AdminController.cs`
    - `scripts/admin-e2e.ps1`

    Dosya kısaltıldı ve gereksiz tekrarlar kaldırıldı. İleri adım için "frontend" veya "auth" yazın — hemen başlıyorum.
