# Implementation Plan

- [x] 1. DTO'ları Oluştur

  - [x] 1.1 UpdateProductRequest DTO'yu oluştur

    - `src/Katana.Core/DTOs/ProductDtos.cs` dosyasına ekle
    - Name, UzunAdi, Barcode, CategoryId, UnitId, Quantity, PurchasePrice, SalesPrice, KdvRate, GtipCode alanları
    - _Requirements: 3.1_

  - [x] 1.2 LucaUpdateStokKartiRequest DTO'yu oluştur

    - `src/Katana.Core/DTOs/LucaDtos.cs` dosyasına ekle
    - SkartId, KartKodu, KartAdi, UzunAdi, Barkod, KategoriAgacKod, OlcumBirimiId, PerakendeAlisBirimFiyat, PerakendeSatisBirimFiyat, GtipKodu alanları
    - JsonPropertyName attribute'ları ekle
    - _Requirements: 3.1, 5.1_

- [x] 2. LucaService'e UpdateStockCardAsync Ekle

  - [x] 2.1 UpdateStockCardAsync metodunu LucaService.Operations.cs'e ekle

    - `GuncelleStokKarti.do` endpoint'ini çağır
    - Session expired durumunda re-authenticate yap
    - Başarılı ise true, hata ise false dön
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [x] 2.2 Property test: Luca API Success Returns True

    - **Property 8: Luca API Success Returns True**
    - **Validates: Requirements 5.3**

  - [x] 2.3 Property test: Luca API Error Returns False

    - **Property 9: Luca API Error Returns False**
    - **Validates: Requirements 5.4**

- [x] 3. ProductsController'ı Güncelle

  - [x] 3.1 UpdateProduct endpoint'ini güncelle

    - Local DB'yi güncelle
    - MapProductToLucaUpdateRequest helper metodu ekle
    - LucaService.UpdateStockCardAsync çağır
    - Luca hatası olsa bile success dön
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [x] 3.2 Property test: Luca Update Contains LucaId

    - **Property 1: Luca Update Contains LucaId**
    - **Validates: Requirements 1.4**

  - [x] 3.3 Property test: Null Field Preservation

    - **Property 7: Null Field Preservation**
    - **Validates: Requirements 3.4**

  - [x] 3.4 Property test: Category Mapping Correctness

    - **Property 5: Category Mapping Correctness**
    - **Validates: Requirements 3.2**

  - [x] 3.5 Property test: Unit Mapping Correctness

    - **Property 6: Unit Mapping Correctness**
    - **Validates: Requirements 3.3**

- [x] 4. Checkpoint - Tüm testlerin geçtiğinden emin ol

  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. SyncService'i Güncelle - Koza'dan Çek

  - [x] 5.1 SyncProductsFromLucaAsync metodunu güncelle

    - Timestamp karşılaştırmasını KALDIR
    - Luca verisi her zaman local'in üzerine yazılsın
    - Yeni ürünler oluşturulsun
    - Created ve updated count'ları dön
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x] 5.2 Property test: Sync Overwrites Local Data

    - **Property 2: Sync Overwrites Local Data**
    - **Validates: Requirements 2.1, 2.2**

  - [x] 5.3 Property test: Sync Creates Missing Products

    - **Property 3: Sync Creates Missing Products**
    - **Validates: Requirements 2.3**

  - [x] 5.4 Property test: Sync Count Accuracy

    - **Property 4: Sync Count Accuracy**
    - **Validates: Requirements 2.4**

- [x] 6. Checkpoint - Tüm testlerin geçtiğinden emin ol

  - Ensure all tests pass, ask the user if questions arise.

- [x] 7. Frontend ProductEditModal Oluştur

  - [x] 7.1 ProductEditModal.tsx komponentini oluştur

    - `frontend/katana-web/src/components/Admin/ProductEditModal.tsx`
    - Sadece Luca-editable alanları göster
    - SKU read-only olsun
    - PUT /api/products/{id} çağırsın
    - _Requirements: 4.1, 4.2, 4.3, 4.4_

  - [x] 7.2 Products.tsx'i güncelle

    - ProductEditModal'ı import et ve kullan
    - handleEdit ve handleSaveProduct fonksiyonları ekle
    - _Requirements: 4.3_

- [x] 8. Error Handling ve Logging

  - [x] 8.1 ProductsController'da hata yönetimini güncelle

    - Luca hatası durumunda warning logla
    - Local update başarılı ise success dön
    - _Requirements: 6.1_

  - [x] 8.2 SyncService'de hata yönetimini güncelle

    - Tek ürün hatası durumunda devam et
    - Partial sync sonuçlarını dön
    - _Requirements: 6.2, 6.3, 6.4_

- [ ] 9. Final Checkpoint - Tüm testlerin geçtiğinden emin ol

  - Ensure all tests pass, ask the user if questions arise.
