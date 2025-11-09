# 🔍 Kod Tabanı — Açık Kalemler (Güncel)

Bu rapor, yalnızca tamamlanmamış ve takip edilmesi gereken başlıkları içerir. Tamamlanan tüm maddeler temizlenmiştir.

Güncelleme: 2025-11-09

---

## 🔒 Güvenlik ve Sırlar

- JWT Secret Key Management (Production)
  - appsettings içindeki sabit anahtar yerine ortam değişkeni/Key Vault kullanımı.
  - Öneri (Program.cs): `Environment.GetEnvironmentVariable("JWT_SECRET_KEY")` fallback.

---

## 🚀 CI/CD ve Operasyonlar

- CI/CD Pipeline (GitHub Actions)
  - Backend: restore → build → test → coverage upload
  - Frontend: install → test (coverage) → build
  - E2E (Playwright e2e/) opsiyonel job

- Docker/Container
  - Dockerfile (.NET 8) ve docker-compose.yml test/healthcheck güncellemesi.

---

## 📈 Performans ve Ölçeklenebilirlik

- Serilog DB Write Volümü
  - Varsayılan Information seviyesi yüksek; prod’da minimum gereken seviyeye indir, sampling/filtreleme uygula.

- SignalR Yayın Deseni
  - Geniş yayın (Clients.All) yerine kullanıcı/grup bazlı yayınların kurgulanması (ölçek için).

- Önbellekleme
  - Dağıtık cache (Redis) eklenmesi; çok instans için paylaşılabilir cache.

- Statik İçerik
  - CDN entegrasyonu (frontend statik dosyaları için).

---

## 📄 Dokümantasyon

- Swagger zenginleştirme (opsiyonel iyileştirmeler)
  - Seçili endpoint’lerde `ProducesResponseType` örnekleri ve DTO açıklamaları artırılabilir.

