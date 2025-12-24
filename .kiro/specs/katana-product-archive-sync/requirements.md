# Requirements Document

## Introduction

Bu özellik, yerel veritabanımızdaki ürünleri kaynak (source of truth) olarak kabul ederek Katana API'sindeki ürünleri senkronize eder. Katana'da var olup yerel veritabanımızda bulunmayan ürünler `{"is_archived": true}` ile arşivlenir. Bu sayede iki sistem arasındaki ürün tutarlılığı sağlanır.

## Glossary

- **Local Database (Yerel Veritabanı)**: Sistemimizin ana veritabanı, ürünlerin kaynak kaynağı (source of truth)
- **Katana API**: Harici üretim yönetim sistemi API'si
- **Archive (Arşivleme)**: Katana'da ürünü `is_archived: true` olarak işaretleme
- **SKU**: Ürün stok kodu, benzersiz tanımlayıcı
- **Sync Service**: Senkronizasyon işlemlerini yöneten servis

## Requirements

### Requirement 1

**User Story:** As a system administrator, I want to archive products in Katana that don't exist in our local database, so that both systems remain consistent with our database as the source of truth.

#### Acceptance Criteria

1. WHEN the archive sync process starts THEN the System SHALL fetch all products from both local database and Katana API
2. WHEN comparing products THEN the System SHALL use SKU as the unique identifier for matching
3. WHEN a product exists in Katana but not in local database THEN the System SHALL mark that product as archived in Katana using `{"is_archived": true}`
4. WHEN archiving a product THEN the System SHALL use Katana's PATCH endpoint to update the product
5. WHEN the archive operation completes THEN the System SHALL return a summary with counts of archived products and any errors

### Requirement 2

**User Story:** As a system administrator, I want to preview which products will be archived before executing the operation, so that I can verify the changes are correct.

#### Acceptance Criteria

1. WHEN requesting a preview THEN the System SHALL return a list of products that would be archived without making any changes
2. WHEN displaying preview results THEN the System SHALL show product ID, SKU, and name for each product to be archived
3. WHEN preview is complete THEN the System SHALL display the total count of products to be archived

### Requirement 3

**User Story:** As a system administrator, I want to have an API endpoint to trigger the archive sync, so that I can execute it manually or schedule it.

#### Acceptance Criteria

1. WHEN calling the archive sync endpoint THEN the System SHALL require authentication
2. WHEN the endpoint is called with preview mode THEN the System SHALL return preview results without archiving
3. WHEN the endpoint is called with execute mode THEN the System SHALL perform the archive operation
4. WHEN the operation fails for some products THEN the System SHALL continue with remaining products and report failures

### Requirement 4

**User Story:** As a developer, I want proper error handling and logging, so that I can troubleshoot issues with the sync process.

#### Acceptance Criteria

1. WHEN an error occurs during Katana API call THEN the System SHALL log the error with product details
2. WHEN rate limiting is encountered THEN the System SHALL implement retry logic with exponential backoff
3. WHEN the sync completes THEN the System SHALL log a summary of the operation
