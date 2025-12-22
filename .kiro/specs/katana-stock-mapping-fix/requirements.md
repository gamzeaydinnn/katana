# Requirements Document

## Introduction

Bu özellik, Katana ERP sisteminden gelen ürün stok bilgisinin ExtractorService'de ProductDto'ya doğru şekilde aktarılmasını sağlar. Mevcut sistemde KatanaService.MapProductElement metodu InStock/OnHand/Available/Committed alanlarını KatanaProductDto'ya doğru şekilde dolduruyor, ancak ExtractorService.ExtractProductsAsync metodu bu stok bilgisini ProductDto'ya aktarmıyor. Bu durum, admin onayıyla Katana'da stok artışı oluşsa bile senkronizasyon sırasında yerel stok miktarının 0 olarak kalmasına neden oluyor.

## Glossary

- **Katana**: Üretim ve envanter yönetim sistemi (kaynak sistem)
- **ExtractorService**: Katana'dan veri çekip ProductDto'ya dönüştüren servis
- **KatanaProductDto**: Katana API'sinden gelen ham ürün verisi
- **ProductDto**: Sistem içinde kullanılan normalize edilmiş ürün verisi
- **InStock**: Katana'daki toplam stok miktarı
- **OnHand**: Katana'daki eldeki stok miktarı
- **Available**: Katana'daki kullanılabilir stok miktarı
- **Committed**: Katana'daki rezerve edilmiş stok miktarı
- **TransformerService**: ProductDto'yu Luca formatına dönüştüren servis
- **LoaderService**: Dönüştürülmüş veriyi Luca'ya gönderen servis

## Requirements

### Requirement 1

**User Story:** As a system administrator, I want Katana stock quantities to be correctly mapped to ProductDto, so that stock synchronization reflects actual inventory levels.

#### Acceptance Criteria

1. WHEN ExtractorService creates a ProductDto from KatanaProductDto THEN the Extractor_Service SHALL set ProductDto.Stock to the first non-null value from InStock, OnHand, or Available fields in that priority order
2. WHEN all stock fields (InStock, OnHand, Available) are null THEN the Extractor_Service SHALL set ProductDto.Stock to zero
3. WHEN a KatanaProductDto has InStock value THEN the Extractor_Service SHALL use InStock as the primary stock source
4. WHEN InStock is null but OnHand has value THEN the Extractor_Service SHALL use OnHand as the stock source
5. WHEN both InStock and OnHand are null but Available has value THEN the Extractor_Service SHALL use Available as the stock source

### Requirement 2

**User Story:** As a system administrator, I want stock mapping to be logged for debugging purposes, so that I can trace stock synchronization issues.

#### Acceptance Criteria

1. WHEN a product's stock is mapped THEN the Extractor_Service SHALL log the SKU, source field name, and stock value at Debug level
2. WHEN all stock fields are null for a product THEN the Extractor_Service SHALL log a warning with the SKU indicating no stock data was available
3. WHEN stock mapping completes for all products THEN the Extractor_Service SHALL log a summary with total products processed and products with non-zero stock

### Requirement 3

**User Story:** As a system administrator, I want the stock mapping to be testable, so that I can verify correct behavior through automated tests.

#### Acceptance Criteria

1. WHEN testing stock mapping with InStock=10, OnHand=5, Available=3 THEN the result SHALL be Stock=10
2. WHEN testing stock mapping with InStock=null, OnHand=5, Available=3 THEN the result SHALL be Stock=5
3. WHEN testing stock mapping with InStock=null, OnHand=null, Available=3 THEN the result SHALL be Stock=3
4. WHEN testing stock mapping with all null values THEN the result SHALL be Stock=0
5. WHEN testing stock mapping with InStock=0, OnHand=5, Available=3 THEN the result SHALL be Stock=0 (zero is a valid value, not null)
