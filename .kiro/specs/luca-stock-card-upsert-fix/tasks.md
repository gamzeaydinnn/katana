# Implementation Plan: Luca Stok Kartı UPSERT Düzeltmesi

## Overview

Bu plan, Luca'da aynı SKU'lu ürün güncellemesi için UPDATE işlemini düzgün şekilde yapılmasını sağlamak için gerekli kod değişikliklerini içerir.

---

## Tasks

- [x] 1. LucaUpdateStokKartiRequest DTO Oluştur

  - `src/Katana.Core/DTOs/LucaDtos.cs` dosyasına yeni DTO ekle
  - Alanlar: SkartId, KartKodu, KartAdi, UzunAdi, Barkod, KategoriAgacKod, PerakendeAlisBirimFiyat, PerakendeSatisBirimFiyat, GtipKodu
  - _Requirements: 2.1-2.8_

- [x] 2. UpdateStockCardAsync Metodunu Düzelt

  - `src/Katana.Infrastructure/APIClients/LucaService.StockCards.cs` dosyasında UpdateStockCardAsync metodunu iyileştir
  - Luca API'ye doğru request gönder
  - Response'u doğru parse et
  - Başarı/başarısızlık döndür
  - _Requirements: 1.2, 3.1-3.4_

- [x] 3. MapToUpdateRequest Helper Metodu Oluştur

  - `src/Katana.Business/Mappers/KatanaToLucaMapper.cs` dosyasına yeni metod ekle
  - LucaCreateStokKartiRequest → LucaUpdateStokKartiRequest dönüşümü
  - Tüm alanları doğru şekilde eşle
  - _Requirements: 2.1-2.8_

- [x] 4. UpsertStockCardAsync Metodunu Düzelt

  - `src/Katana.Infrastructure/APIClients/LucaService.Queries.cs` dosyasında UpsertStockCardAsync metodunu iyileştir
  - FindStockCardBySkuAsync() ile mevcut kartı ara
  - Bulunursa: UpdateStockCardAsync() çağır
  - Bulunamazsa: SendStockCardsAsync() çağır
  - Sonucu döndür
  - _Requirements: 1.1-1.5, 4.1-4.2_

- [x] 5. Unit Test: UpdateStockCardAsync Başarılı

  - `tests/Katana.Tests/Services/LucaServiceTests.cs` dosyasına test ekle
  - Luca API'ye başarılı UPDATE isteği gönderildiğinde test et
  - _Requirements: 1.2, 3.1_

- [x] 5.1 Property Test: Aynı SKU Güncelleme

  - **Property 1: Aynı SKU Güncelleme Garantisi**
  - **Validates: Requirements 1.1, 1.2, 1.3**

- [x] 6. Unit Test: UpdateStockCardAsync Başarısız

  - `tests/Katana.Tests/Services/LucaServiceTests.cs` dosyasına test ekle
  - Luca API'ye başarısız UPDATE isteği gönderildiğinde test et
  - _Requirements: 1.3, 3.1_

- [x] 6.1 Property Test: Yeni SKU Oluşturma

  - **Property 2: Yeni SKU Oluşturma Garantisi**
  - **Validates: Requirements 1.5**

- [x] 7. Unit Test: UpsertStockCardAsync UPDATE Path

  - `tests/Katana.Tests/Services/LucaServiceTests.cs` dosyasına test ekle
  - Mevcut SKU'lu ürün gönderildiğinde UPDATE yapıldığını test et
  - _Requirements: 1.1-1.3_

- [x] 7.1 Property Test: Alanlar Doğru Eşlenme

  - **Property 3: Alanlar Doğru Eşlenme**
  - **Validates: Requirements 2.1-2.8**

- [x] 8. Unit Test: UpsertStockCardAsync CREATE Path

  - `tests/Katana.Tests/Services/LucaServiceTests.cs` dosyasına test ekle
  - Yeni SKU'lu ürün gönderildiğinde CREATE yapıldığını test et
  - _Requirements: 1.5_

- [x] 8.1 Property Test: İdempotency

  - **Property 4: İdempotency**
  - **Validates: Requirements 4.1, 4.2**

- [x] 9. Unit Test: FindStockCardBySkuAsync

  - `tests/Katana.Tests/Services/LucaServiceTests.cs` dosyasına test ekle
  - SKU'ya göre kartı bulma/bulamama testleri
  - _Requirements: 1.1_

- [x] 9.1 Property Test: Hata Yönetimi

  - **Property 5: Hata Yönetimi**
  - **Validates: Requirements 3.1-3.4**

- [x] 10. Integration Test: Uçtan Uca Senkronizasyon

  - `tests/Katana.Tests/Integration/LucaSyncIntegrationTests.cs` dosyasına test ekle
  - Katana'dan Luca'ya ürün gönderilmesi testi
  - Aynı ürün 2 kez gönderilmesi testi
  - _Requirements: 1.1-1.5, 4.1-4.2_

- [x] 11. Checkpoint - Tüm Testler Geçiyor

  - Ensure all tests pass, ask the user if questions arise.

- [x] 12. Code Review ve Cleanup

  - Kod kalitesi kontrol et
  - Logging'i kontrol et
  - Exception handling'i kontrol et
  - _Requirements: 3.1-3.4_

- [x] 13. Dokümantasyon Güncelle

  - `KATANA_LUCA_ANALIZ_OZETI.md` dosyasını güncelle
  - Yeni UPSERT mantığını dokümante et
  - _Requirements: Tümü_
