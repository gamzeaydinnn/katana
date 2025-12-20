# Requirements Document

## Introduction

Bu özellik, Katana ERP sisteminden Luca/Koza muhasebe sistemine stok kartı senkronizasyonunu iyileştirir. Mevcut sistemde stok kartları aktarılırken kategori, ölçü birimi, fiyat ve barkod bilgileri eksik veya yanlış aktarılmaktadır. Bu çözüm, Luca API'sinden dinamik olarak ölçü birimlerini çekerek, veritabanı tabanlı mapping sistemi kurarak ve mapper'ı güncelleyerek tam ve doğru stok kartı senkronizasyonu sağlayacaktır.

## Glossary

- **Katana**: Üretim ve envanter yönetim sistemi (kaynak sistem)
- **Luca/Koza**: Türk muhasebe yazılımı (hedef sistem)
- **Stok Kartı**: Luca'da ürün/malzeme tanım kaydı
- **OlcumBirimiId**: Luca'daki ölçü birimi kimlik numarası (örn: 5=ADET, 1=KG)
- **KategoriAgacKod**: Luca'daki kategori ağaç kodu (numerik format: "001", "220")
- **MappingTable**: Kaynak-hedef değer eşleştirmelerini tutan veritabanı tablosu
- **SKU**: Stock Keeping Unit - Ürün stok kodu
- **Sync Service**: Sistemler arası veri senkronizasyonu yapan servis

## Requirements

### Requirement 1

**User Story:** As a system administrator, I want to fetch measurement units from Luca API, so that I can create accurate unit mappings for stock card synchronization.

#### Acceptance Criteria

1. WHEN the system requests measurement units from Luca THEN the Sync_System SHALL call the `ListeleGnlOlcumBirimi.do` endpoint and return a list of measurement units with id, kod, ad, kisa, and aktif fields
2. WHEN the Luca API returns measurement units THEN the Sync_System SHALL parse the response and create LucaOlcumBirimiDto objects for each unit
3. WHEN the Luca API call fails THEN the Sync_System SHALL log the error with status code and response content and return an empty list
4. WHEN the Luca session is expired THEN the Sync_System SHALL re-authenticate before making the measurement units request

### Requirement 2

**User Story:** As a system administrator, I want to automatically create unit mappings from Luca measurement units, so that Katana units are correctly mapped to Luca unit IDs.

#### Acceptance Criteria

1. WHEN the sync-olcum-birimi-mappings endpoint is called THEN the Sync_System SHALL fetch measurement units from Luca and create UNIT type mappings in the MappingTable
2. WHEN a Katana unit string (e.g., "pcs", "kg", "m") is mapped THEN the Sync_System SHALL store the corresponding Luca OlcumBirimiId as the TargetValue
3. WHEN a mapping already exists for a Katana unit THEN the Sync_System SHALL skip creating a duplicate mapping
4. WHEN a Luca unit name is not found for a Katana unit THEN the Sync_System SHALL log a warning and continue with the next unit
5. WHEN the mapping creation completes THEN the Sync_System SHALL return the count of newly added mappings

### Requirement 3

**User Story:** As a system administrator, I want to manage product category mappings, so that Katana categories are correctly mapped to Luca category codes.

#### Acceptance Criteria

1. WHEN a product category mapping is created THEN the Sync_System SHALL store the Katana category name as SourceValue and Luca numeric category code as TargetValue with MappingType "PRODUCT_CATEGORY"
2. WHEN the mapper looks up a category THEN the Sync_System SHALL first check the database MappingTable, then fall back to appsettings.json CategoryMapping
3. WHEN no category mapping is found THEN the Sync_System SHALL use the DefaultKategoriKodu from LucaApiSettings or leave the field null
4. WHEN a category name contains only numeric characters THEN the Sync_System SHALL treat it as an internal ID and use the default category code instead

### Requirement 4

**User Story:** As a system administrator, I want the stock card mapper to use database mappings for unit conversion, so that unit mappings can be updated without code changes.

#### Acceptance Criteria

1. WHEN mapping a Katana product to Luca stock card THEN the Mapper SHALL look up the unit in database UNIT mappings first
2. WHEN the unit is not found in database mappings THEN the Mapper SHALL fall back to LucaApiSettings.UnitMapping configuration
3. WHEN the unit is not found in any mapping THEN the Mapper SHALL use the AutoMapUnit fallback method and log a warning
4. WHEN no unit is provided from Katana THEN the Mapper SHALL use DefaultOlcumBirimiId from LucaApiSettings and log a warning

### Requirement 5

**User Story:** As a system administrator, I want price information to be included in stock card synchronization, so that purchase and sales prices are transferred to Luca.

#### Acceptance Criteria

1. WHEN mapping a Katana product THEN the Mapper SHALL set PerakendeAlisBirimFiyat from CostPrice or PurchasePrice fields
2. WHEN mapping a Katana product THEN the Mapper SHALL set PerakendeSatisBirimFiyat from SalesPrice or Price fields
3. WHEN both purchase and sales prices are zero THEN the Mapper SHALL log a warning with the SKU
4. WHEN price values are null THEN the Mapper SHALL default to zero

### Requirement 6

**User Story:** As a system administrator, I want barcode handling to prevent duplicate barcode errors, so that versioned SKUs do not cause synchronization failures.

#### Acceptance Criteria

1. WHEN a SKU ends with a version suffix (e.g., "-V2", "-V3") THEN the Mapper SHALL set the Barkod field to null
2. WHEN a SKU does not have a version suffix THEN the Mapper SHALL use the product's Barcode field or fall back to SKU
3. WHEN a versioned SKU is detected THEN the Mapper SHALL log an informational message about the barcode being set to null

### Requirement 7

**User Story:** As a system administrator, I want to test single product mapping before bulk synchronization, so that I can verify mapping configuration is correct.

#### Acceptance Criteria

1. WHEN the test-single-product endpoint is called with a SKU THEN the Sync_System SHALL return both the Katana product data and the mapped Luca request
2. WHEN testing a product THEN the Sync_System SHALL include mapping details showing whether category and unit mappings were found
3. WHEN the product is not found THEN the Sync_System SHALL return a 404 error with an appropriate message

### Requirement 8

**User Story:** As a system administrator, I want API endpoints to list Luca measurement units and sync mappings, so that I can manage mappings through the API.

#### Acceptance Criteria

1. WHEN the list-luca-olcum-birimleri endpoint is called THEN the Sync_System SHALL return all measurement units from Luca with success status and count
2. WHEN the sync-olcum-birimi-mappings endpoint is called THEN the Sync_System SHALL create mappings and return success status with added count
3. WHEN an API error occurs THEN the Sync_System SHALL return a BadRequest response with the error message

### Requirement 9

**User Story:** As a system administrator, I want database migrations for the mapping system, so that the required tables and seed data are properly created.

#### Acceptance Criteria

1. WHEN the migration is applied THEN the Database SHALL have a MappingTables table with columns for Id, MappingType, SourceValue, TargetValue, Description, IsActive, and audit fields
2. WHEN the seed migration is applied THEN the Database SHALL contain initial UNIT mappings for common units (pcs, kg, m, l, etc.)
3. WHEN the seed migration is applied THEN the Database SHALL contain initial PRODUCT_CATEGORY mappings for known Katana categories

### Requirement 10

**User Story:** As a system administrator, I want stock card validation before sending to Luca, so that invalid data is caught early and logged.

#### Acceptance Criteria

1. WHEN a stock card request is created THEN the Mapper SHALL validate that KartKodu is not empty
2. WHEN a stock card request is created THEN the Mapper SHALL validate that KartAdi is not empty
3. WHEN a stock card request is created THEN the Mapper SHALL validate that OlcumBirimiId is greater than zero
4. WHEN validation fails THEN the Mapper SHALL throw a ValidationException with all error messages concatenated
5. WHEN validation fails THEN the Mapper SHALL log the validation errors with the SKU
