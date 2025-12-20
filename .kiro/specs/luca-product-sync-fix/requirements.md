# Requirements Document

## Introduction

Bu özellik, Luca/Koza muhasebe sisteminden ürün senkronizasyonunu düzeltir. Mevcut sistemde "Koza'dan Çek" işlemi yapıldığında, frontend'de yapılan değişiklikler korunmaya çalışılıyor ancak bu yanlış bir yaklaşım. Luca her zaman "single source of truth" (tek doğru kaynak) olmalı. Frontend'de yapılan değişiklikler önce Luca'ya gönderilmeli, sonra "Koza'dan Çek" yapıldığında Luca'daki güncel veri alınmalı.

## Glossary

- **Luca/Koza**: Türk muhasebe yazılımı (tek doğru kaynak - single source of truth)
- **Local DB**: Katana uygulamasının yerel veritabanı (Products tablosu)
- **Koza'dan Çek**: Luca'dan ürünleri çekip local DB'ye senkronize etme işlemi
- **UpdateStockCardAsync**: Luca'da stok kartı güncelleme API metodu
- **Single Source of Truth**: Verilerin tek bir kaynaktan yönetilmesi prensibi

## Requirements

### Requirement 1

**User Story:** As a system administrator, I want product updates from frontend to be sent to Luca immediately, so that Luca always has the latest product data.

#### Acceptance Criteria

1. WHEN a product is updated via PUT /api/products/{id} endpoint THEN the Sync_System SHALL first update the local database and then send the update to Luca via UpdateStockCardAsync
2. WHEN the Luca update succeeds THEN the Sync_System SHALL log a success message with the product SKU
3. WHEN the Luca update fails THEN the Sync_System SHALL log a warning but still return success for the local update
4. WHEN the product has a LucaId THEN the Sync_System SHALL use that ID for the Luca update request

### Requirement 2

**User Story:** As a system administrator, I want "Koza'dan Çek" to always use Luca data as the source of truth, so that local database reflects the actual Luca state.

#### Acceptance Criteria

1. WHEN the sync-products-from-luca endpoint is called THEN the Sync_System SHALL fetch all products from Luca and overwrite local database records
2. WHEN a product exists in both Luca and local DB THEN the Sync_System SHALL update local record with Luca data without any timestamp comparison
3. WHEN a product exists in Luca but not in local DB THEN the Sync_System SHALL create a new local record
4. WHEN the sync completes THEN the Sync_System SHALL return counts of created and updated products

### Requirement 3

**User Story:** As a system administrator, I want the UpdateProductRequest DTO to include all Luca-editable fields, so that frontend can send complete product updates.

#### Acceptance Criteria

1. WHEN UpdateProductRequest is received THEN the DTO SHALL include Name, UzunAdi, Barcode, CategoryId, UnitId, Quantity, PurchasePrice, SalesPrice, KdvRate, and GtipCode fields
2. WHEN mapping to LucaUpdateStokKartiRequest THEN the Mapper SHALL convert CategoryId to KategoriAgacKod using database mappings
3. WHEN mapping to LucaUpdateStokKartiRequest THEN the Mapper SHALL convert UnitId to OlcumBirimiId using database mappings
4. WHEN a field is null in the request THEN the Mapper SHALL preserve the existing value in local DB

### Requirement 4

**User Story:** As a system administrator, I want the frontend product edit form to show only Luca-editable fields, so that users don't try to edit non-syncable fields.

#### Acceptance Criteria

1. WHEN the product edit modal opens THEN the Frontend SHALL display only these editable fields: Name, UzunAdi, Barcode, Category, Unit, Quantity, PurchasePrice, SalesPrice, KdvRate, GtipCode
2. WHEN displaying the SKU field THEN the Frontend SHALL show it as read-only
3. WHEN the user saves changes THEN the Frontend SHALL call PUT /api/products/{id} with the UpdateProductRequest
4. WHEN the save succeeds THEN the Frontend SHALL show a success message indicating the product was updated in both local DB and Luca

### Requirement 5

**User Story:** As a system administrator, I want the LucaService to have an UpdateStockCardAsync method, so that products can be updated in Luca.

#### Acceptance Criteria

1. WHEN UpdateStockCardAsync is called THEN the LucaService SHALL call the Luca GuncelleStokKarti.do endpoint with the provided request
2. WHEN the Luca session is expired THEN the LucaService SHALL re-authenticate before making the update request
3. WHEN the Luca API returns success THEN the LucaService SHALL return true
4. WHEN the Luca API returns an error THEN the LucaService SHALL log the error and return false

### Requirement 6

**User Story:** As a system administrator, I want proper error handling for product sync operations, so that I can diagnose issues.

#### Acceptance Criteria

1. WHEN a product update to Luca fails THEN the Sync_System SHALL log the error with product SKU and error details
2. WHEN "Koza'dan Çek" fails for a specific product THEN the Sync_System SHALL log the error and continue with the next product
3. WHEN the entire sync operation fails THEN the Sync_System SHALL return an error response with the failure reason
4. WHEN partial sync occurs THEN the Sync_System SHALL return success with counts of successful and failed operations
