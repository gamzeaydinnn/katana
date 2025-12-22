# Implementation Plan

- [x] 1. ExtractorService'de Stock Mapping Ekle

  - [x] 1.1 ExtractProductsAsync metodunda ProductDto.Stock alanını doldur

    - `src/Katana.Business/Services/ExtractorService.cs` dosyasını güncelle
    - ProductDto oluşturulurken `Stock = product.InStock ?? product.OnHand ?? product.Available ?? 0` ekle
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

  - [ ]\* 1.2 Property test: Stock Mapping Priority Chain
    - **Property 1: Stock Mapping Priority Chain**
    - **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5**

- [x] 2. Logging Ekle

  - [x] 2.1 Stock mapping için debug logging ekle

    - Her ürün için SKU ve stock değerini debug seviyesinde logla
    - Tüm stok alanları null olan ürünler için warning logla
    - _Requirements: 2.1, 2.2_

  - [x] 2.2 İşlem özeti logging ekle

    - Toplam ürün sayısı ve sıfır olmayan stok sayısını logla
    - _Requirements: 2.3_

- [x] 3. Checkpoint - Build ve Test

  - Ensure all tests pass, ask the user if questions arise.
