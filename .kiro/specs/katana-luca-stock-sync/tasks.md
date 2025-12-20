# Implementation Plan

- [x] 1. Veri Modelleri ve DTO'ları Oluştur

  - [x] 1.1 LucaOlcumBirimiDto ve LucaOlcumBirimiResponse sınıflarını oluştur

    - `src/Katana.Core/DTOs/LucaDtos.cs` dosyasına ekle
    - Id, Kod, Ad, Kisa, Aktif alanlarını JsonPropertyName ile tanımla
    - _Requirements: 1.1, 1.2_

  - [x] 1.2 Property test: Measurement Unit Response Parsing

    - **Property 1: Measurement Unit Response Parsing**
    - **Validates: Requirements 1.1, 1.2**

- [x] 2. LucaService'e Ölçü Birimi Endpoint'i Ekle

  - [x] 2.1 GetMeasurementUnitsAsync metodunu LucaService.Queries.cs'e ekle

    - `ListeleGnlOlcumBirimi.do` endpoint'ini çağır
    - Session expired durumunda re-authenticate yap
    - Hata durumunda boş liste dön ve logla
    - _Requirements: 1.1, 1.3, 1.4_

  - [x] 2.2 Unit test: LucaService GetMeasurementUnitsAsync

    - Mock response ile başarılı çağrı testi
    - Hata durumu testi
    - _Requirements: 1.1, 1.3_

- [x] 3. MappingService'e Unit Mapping Desteği Ekle

  - [x] 3.1 GetUnitMappingAsync metodunu MappingService'e ekle

    - MappingType='UNIT' olan kayıtları getir
    - Dictionary<string, string> olarak dön
    - _Requirements: 2.1, 4.1_

  - [x] 3.2 UpdateUnitMappingAsync metodunu ekle

    - Yeni UNIT mapping ekle veya güncelle
    - Duplicate kontrolü yap
    - _Requirements: 2.3_

  - [ ] 3.3 Property test: Unit Mapping Idempotency
    - **Property 2: Unit Mapping Idempotency**
    - **Validates: Requirements 2.3**

- [x] 4. OlcumBirimiSyncService Oluştur

  - [x] 4.1 IOlcumBirimiSyncService interface'ini oluştur

    - `src/Katana.Business/Interfaces/` altına ekle
    - SyncOlcumBirimiMappingsAsync ve GetLucaOlcumBirimleriAsync metodları
    - _Requirements: 2.1, 8.1_

  - [x] 4.2 OlcumBirimiSyncService implementasyonunu oluştur

    - Luca'dan ölçü birimlerini çek
    - Katana unit'lerini Luca ID'lerine eşle
    - Duplicate mapping oluşturmayı engelle
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5_

  - [ ] 4.3 Property test: Unit Mapping Count Correctness
    - **Property 3: Unit Mapping Count Correctness**
    - **Validates: Requirements 2.5**

- [x] 5. Checkpoint - Tüm testlerin geçtiğinden emin ol

  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. KatanaToLucaMapper'ı Güncelle - Unit Mapping

  - [x] 6.1 MapKatanaProductToStockCard metoduna dbUnitMappings parametresi ekle

    - Önce DB mapping'e bak
    - Sonra config mapping'e bak
    - Son olarak AutoMapUnit fallback kullan
    - _Requirements: 4.1, 4.2, 4.3, 4.4_

  - [x] 6.2 Property test: Unit Mapping Priority Chain

    - **Property 4: Unit Mapping Priority Chain**
    - **Validates: Requirements 4.1, 4.2, 4.3, 4.4**

- [x] 7. KatanaToLucaMapper'ı Güncelle - Category Mapping

  - [x] 7.1 Kategori mapping fallback zincirini implement et

    - Önce DB PRODUCT_CATEGORY mapping'e bak
    - Sonra appsettings CategoryMapping'e bak
    - Son olarak DefaultKategoriKodu kullan
    - _Requirements: 3.2, 3.3_

  - [x] 7.2 Numeric category detection ekle

    - Sadece rakamlardan oluşan kategori adlarını tespit et
    - Bu durumda default kategori kodunu kullan

    - _Requirements: 3.4_

  - [x] 7.3 Property test: Category Mapping Fallback Chain

    - **Property 5: Category Mapping Fallback Chain**
    - **Validates: Requirements 3.2, 3.3**

  - [ ] 7.4 Property test: Numeric Category Detection

    - **Property 6: Numeric Category Detection**

    - **Validates: Requirements 3.4**

- [x] 8. KatanaToLucaMapper'ı Güncelle - Price Mapping

  - [x] 8.1 Fiyat alanlarını doğru şekilde map et
    - PerakendeAlisBirimFiyat = CostPrice ?? PurchasePrice ?? 0
    - PerakendeSatisBirimFiyat = SalesPrice ?? Price ?? 0
    - Her iki fiyat sıfırsa warning logla
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
  - [ ] 8.2 Property test: Price Mapping Correctness
    - **Property 7: Price Mapping Correctness**
    - **Validates: Requirements 5.1, 5.2, 5.4**

- [x] 9. KatanaToLucaMapper'ı Güncelle - Barcode Handling

  - [x] 9.1 Versioned SKU barcode handling ekle

    - `-V\d+$` pattern'ine uyan SKU'lar için Barkod = null
    - Diğer SKU'lar için Barcode ?? SKU kullan
    - Versioned SKU tespit edildiğinde info log yaz
    - _Requirements: 6.1, 6.2, 6.3_

  - [ ] 9.2 Property test: Versioned SKU Barcode Handling

    - **Property 8: Versioned SKU Barcode Handling**

    - **Validates: Requirements 6.1**

  - [ ] 9.3 Property test: Non-Versioned SKU Barcode Handling
    - **Property 9: Non-Versioned SKU Barcode Handling**
    - **Validates: Requirements 6.2**

- [x] 10. Checkpoint - Tüm testlerin geçtiğinden emin ol

  - Ensure all tests pass, ask the user if questions arise.

- [x] 11. Stock Card Validation Ekle

  - [x] 11.1 ValidateLucaStockCard metodunu implement et

    - KartKodu boş kontrolü
    - KartAdi boş kontrolü
    - OlcumBirimiId > 0 kontrolü
    - Tüm hataları birleştirip ValidationException fırlat
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_

  - [x] 11.2 Property test: Stock Card Validation Completeness

    - **Property 10: Stock Card Validation Completeness**
    - **Validates: Requirements 10.1, 10.2, 10.3, 10.4**

- [x] 12. SyncController Endpoint'lerini Ekle

  - [x] 12.1 list-luca-olcum-birimleri endpoint'ini ekle

    - GET /api/sync/list-luca-olcum-birimleri
    - Luca'dan tüm ölçü birimlerini getir
    - _Requirements: 8.1_

  - [x] 12.2 sync-olcum-birimi-mappings endpoint'ini ekle

    - POST /api/sync/sync-olcum-birimi-mappings
    - Mapping'leri oluştur ve eklenen sayıyı dön

    - _Requirements: 8.2_

  - [x] 12.3 test-single-product endpoint'ini ekle

    - GET /api/sync/test-single-product/{sku}
    - Katana product ve mapped Luca request'i dön
    - 404 döndür eğer ürün bulunamazsa

    - _Requirements: 7.1, 7.2, 7.3_

  - [ ] 12.4 Property test: Test Endpoint Response Structure
    - **Property 11: Test Endpoint Response Structure**
    - **Validates: Requirements 7.1, 7.2**

- [x] 13. Database Migration'ları Oluştur

  - [x] 13.1 MappingTable tablosu için migration oluştur (eğer yoksa)

    - Id, MappingType, SourceValue, TargetValue, Description, IsActive, audit fields
    - _Requirements: 9.1_

  - [x] 13.2 Seed data migration'ı oluştur

    - Yaygın UNIT mapping'leri ekle (pcs, kg, m, l, etc.)
    - Bilinen PRODUCT_CATEGORY mapping'leri ekle
    - _Requirements: 9.2, 9.3_

- [x] 14. DI Registration ve Entegrasyon

  - [x] 14.1 Yeni servisleri DI container'a kaydet

    - IOlcumBirimiSyncService -> OlcumBirimiSyncService
    - Program.cs'e ekle
    - _Requirements: 2.1, 8.1_

  - [x] 14.2 Mevcut SyncService'i güncelle
    - Stok kartı sync'te yeni mapper parametrelerini kullan
    - DB mapping'leri çekip mapper'a geçir
    - _Requirements: 4.1, 3.2_

- [x] 15. Final Checkpoint - Tüm testlerin geçtiğinden emin ol
  - Build başarılı, tüm implementasyonlar tamamlandı
