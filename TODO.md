# Katana Integration — Sadece Açık İşler (TODO)

Son güncelleme: 2025-11-09

Bu dosya yalnızca devam eden/eksik işleri içerir. Tamamlanan kalemler temizlenmiştir.

---

## 🟠 Yüksek Öncelik (1-2 Hafta)

### 1) Test Coverage – Kritik Senaryolar
- Concurrent approval testleri (gerçek eşzamanlılık doğrulaması)
  - `tests/Katana.Tests/Services/ConcurrentApprovalTests.cs` içeriğini genişlet
  - 10 paralel onay denemesi → sadece 1 başarı beklenir
- SignalR yayın testleri (hub çağrısı doğrulama)
  - HubContext mock’layıp `SendAsync("PendingCreated"|"PendingApproved")` çağrılarını doğrula

---

## 🟡 Orta Öncelik (2-4 Hafta)

### 2) Monitoring & Alerting
- Application Insights (veya alternatif) entegrasyonu
- Uyarılar: yavaş sorgu (>5s), DLQ birikimi (>10), hata oranı eşiği
- Dashboard metrikleri için görseller/raporlama

### 3) API Documentation (Swagger) İyileştirmeleri
- XML comment kapsamını artır (DTO + controller özetleri)
- `ProducesResponseType` ve örnek gövdeler

### 4) Production Security Sertleştirme
- JWT Secret → ortam değişkeni/secret store (Key Vault)
- Rate limiting (AspNetCoreRateLimit veya eşdeğeri)

---

## 🚀 Sonraki Adımlar (Kısa Liste)
1. ConcurrentApprovalTests’i gerçek yük ile tamamla
2. SignalR hub publish testlerini ekle
3. Monitoring/alerting temelini kur (AI/alerts)
4. Swagger response örnekleri ve `ProducesResponseType` eklemeleri
5. JWT Key’i prod’da secret store’a taşı

