# Requirements Document

## Introduction

Bu özellik, satış siparişi onaylandığında sipariş satırlarındaki ürünler için Luca/Koza sisteminde otomatik olarak stok kartı oluşturulmasını sağlar. Mevcut sistemde stok kartı kontrolü yapılıyor ancak oluşturma işlemi güvenilir çalışmıyor. Bu geliştirme ile sipariş onay akışı daha sağlam hale getirilecek ve fatura oluşturma öncesi tüm ürünlerin Luca'da stok kartı olması garanti edilecektir.

## Glossary

- **Luca/Koza**: Muhasebe ve ERP sistemi (akozas.luca.com.tr)
- **Stok Kartı**: Luca sisteminde ürün tanımı (SKU, ad, KDV oranı, ölçü birimi vb.)
- **SKU**: Stock Keeping Unit - Ürün kodu
- **SalesOrder**: Satış siparişi
- **SalesOrderLine**: Sipariş satırı (ürün, miktar, fiyat)
- **ApproveOrder**: Sipariş onay işlemi
- **UpsertStockCardAsync**: Stok kartı varsa güncelle, yoksa oluştur metodu
- **FindStockCardBySkuAsync**: SKU ile stok kartı arama metodu

## Requirements

### Requirement 1

**User Story:** As an admin user, I want stock cards to be automatically created in Luca when I approve a sales order, so that invoice creation does not fail due to missing stock cards.

#### Acceptance Criteria

1. WHEN an admin approves a sales order THEN the system SHALL check each order line's SKU for existing stock card in Luca
2. WHEN a stock card does not exist for an SKU THEN the system SHALL create a new stock card in Luca before proceeding with invoice creation
3. WHEN stock card creation fails THEN the system SHALL log the error and continue with remaining SKUs
4. WHEN all stock cards are processed THEN the system SHALL proceed with Luca invoice creation
5. WHEN stock card already exists THEN the system SHALL skip creation and log the existing skartId

### Requirement 2

**User Story:** As a system administrator, I want detailed logging of stock card operations during order approval, so that I can troubleshoot issues.

#### Acceptance Criteria

1. WHEN stock card check starts THEN the system SHALL log the total number of lines being processed
2. WHEN a stock card is found THEN the system SHALL log the SKU and skartId
3. WHEN a stock card is created THEN the system SHALL log the SKU and creation result
4. WHEN a stock card creation fails THEN the system SHALL log the SKU and error message
5. WHEN all stock card operations complete THEN the system SHALL include results in the API response

### Requirement 3

**User Story:** As a developer, I want stock card creation to use proper data mapping, so that cards are created with correct values.

#### Acceptance Criteria

1. WHEN creating a stock card THEN the system SHALL use the SKU from the order line as kartKodu
2. WHEN creating a stock card THEN the system SHALL use the ProductName from the order line as kartAdi
3. WHEN creating a stock card THEN the system SHALL calculate KDV rate from the line's TaxRate field
4. WHEN creating a stock card THEN the system SHALL use default ölçü birimi (ADET=1) if not specified
5. WHEN creating a stock card THEN the system SHALL set kartTuru to 1 (Stok kartı)

### Requirement 4

**User Story:** As a system user, I want the approval process to be resilient, so that partial failures don't block the entire operation.

#### Acceptance Criteria

1. WHEN a single stock card creation fails THEN the system SHALL continue processing remaining SKUs
2. WHEN Luca API returns HTML error response THEN the system SHALL retry with session refresh
3. WHEN all retries fail THEN the system SHALL mark the SKU as failed and continue
4. WHEN invoice creation fails after stock card creation THEN the system SHALL preserve the created stock cards
5. WHEN approval completes THEN the system SHALL return detailed status for each SKU operation

### Requirement 5

**User Story:** As a developer, I want to ensure idempotency, so that repeated approval attempts don't create duplicate stock cards.

#### Acceptance Criteria

1. WHEN checking for existing stock card THEN the system SHALL use FindStockCardBySkuAsync method
2. WHEN stock card exists THEN the system SHALL NOT attempt to create a new one
3. WHEN UpsertStockCardAsync is called THEN the system SHALL handle duplicate SKU gracefully
4. WHEN approval is retried THEN the system SHALL skip already-created stock cards
5. WHEN stock card creation returns duplicate error THEN the system SHALL treat it as success
