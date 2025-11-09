# Katana Projesi — Açık Kalemler (Güncel)

Bu belge, yalnızca devam eden ve tamamlanmamış maddeleri içerir. Tamamlanan tüm bulgular ve eski notlar kaldırılmıştır.

Güncelleme: 2025-11-09

---

## 1) Güvenlik ve Sırlar

- JWT Secret Yönetimi (Prod)
  - appsettings’teki sabit anahtar yerine ortam değişkeni/secret store (Key Vault) kullanımı.
  - Öneri: `Environment.GetEnvironmentVariable("JWT_SECRET_KEY")` fallback ve prod konfigürasyonunda zorunlu hale getirme.

- Prod DB Erişim/Yetkilendirme
  - Uzak SQL Server’da kullanıcı mapping/izinlerinin doğrulanması (CONNECT/db_owner veya granular roller).
  - Connection stringlerin güvenli saklanması ve maskelenmiş logging.

---

## 2) CI/CD ve Operasyonlar

- GitHub Actions Pipeline
  - Backend: restore → build → test → coverage upload.
  - Frontend: install → test (coverage) → build.
  - E2E (e2e/ Playwright) opsiyonel job ve raporlama.

- Docker/Container
  - .NET 8 multi-stage Dockerfile gözden geçirme, `docker-compose.yml` healthcheck ve env ayrımı.

---

## 3) Performans ve Gözlemlenebilirlik

- Serilog Log Hacmi
  - Production’da minimum gerekli seviyeye indirme, sampling/filtreleme kuralları ekleme.

- SignalR Yayın Stratejisi
  - `Clients.All` yerine kullanıcı/grup bazlı yayın ile ölçeklenebilirlik iyileştirme.

- Önbellek/Katmanlı Cache
  - Dağıtık cache (Redis) ile cache paylaştırma; response caching politikalarını gözden geçirme.

---

## 4) Dokümantasyon ve Mimari Notlar

- DB Migrations/Seed Belgeleri
  - Prod/dev ayrımı, otomatik migration politikası ve manuel migration yönergeleri.

- Swagger Zenginleştirme (Opsiyonel)
  - Seçili endpoint’lerde `ProducesResponseType` örnekleri ve DTO açıklamalarını genişletme.

