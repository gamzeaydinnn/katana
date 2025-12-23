# Requirements Document

## Introduction

Bu özellik, Katana sipariş sistemindeki varyant yönetimi ve BOM (Bill of Materials) entegrasyonunu iyileştirmeyi amaçlamaktadır. Mevcut durumda her varyant ayrı SKU olarak yönetilmekte ve Luca'ya ayrı stok kartı olarak gönderilmektedir. Bu özellik ile varyantlar gruplandırılacak, siparişler konsolide edilecek ve BOM entegrasyonu sağlanacaktır.

## Glossary

- **Variant (Varyant)**: Bir ürünün satılabilir/stoklanabilir en küçük birimi (örn: T-Shirt Kırmızı M Beden)
- **SKU**: Stock Keeping Unit - Benzersiz stok kodu
- **BOM (Bill of Materials)**: Bir ürünün üretimi için gereken hammadde ve bileşenlerin listesi
- **Sales Order**: Müşteriye satış siparişi
- **Manufacturing Order**: Üretim emri
- **Luca/Koza**: Muhasebe ve ERP sistemi
- **Katana**: Üretim ve stok yönetim sistemi
- **Consolidation (Konsolidasyon)**: Birden fazla varyantı tek sipariş/fatura altında birleştirme
- **Deduplication**: Tekrarlanan ürünleri tespit edip birleştirme

## Requirements

### Requirement 1

**User Story:** As a warehouse manager, I want to see variants grouped by their parent product, so that I can better understand product inventory across all variations.

#### Acceptance Criteria

1. WHEN a user views the product list THEN the System SHALL display variants grouped under their parent product with variant count indicator
2. WHEN a user expands a product group THEN the System SHALL show all variants with their SKU, attributes (color, size), and individual stock levels
3. WHEN a product has only one variant THEN the System SHALL display it as a single product without grouping
4. IF a variant has no parent product association THEN the System SHALL display it as a standalone product with a warning indicator

### Requirement 2

**User Story:** As an admin, I want to consolidate duplicate products that were created as separate SKUs, so that I can clean up the product catalog.

#### Acceptance Criteria

1. WHEN an admin initiates duplicate detection THEN the System SHALL identify products with similar names using fuzzy matching algorithm
2. WHEN duplicates are detected THEN the System SHALL present them in groups with similarity score and recommend a canonical (primary) product
3. WHEN an admin confirms merge operation THEN the System SHALL transfer all order history and stock movements to the canonical product
4. WHEN merge is complete THEN the System SHALL mark obsolete products as inactive with a reference to the canonical product
5. IF a product has active orders THEN the System SHALL prevent deletion and display a warning with order references

### Requirement 3

**User Story:** As a sales representative, I want to create orders with multiple variants in a single order, so that customers can order different variations together.

#### Acceptance Criteria

1. WHEN a user creates a sales order THEN the System SHALL allow adding multiple variants from the same or different products
2. WHEN a sales order is saved THEN the System SHALL store each variant as a separate order line with variant-specific attributes
3. WHEN a sales order is synced to Luca THEN the System SHALL send all variants as separate invoice lines within a single invoice document
4. WHEN displaying order summary THEN the System SHALL group variants by parent product and show subtotals per product group

### Requirement 4

**User Story:** As a production manager, I want to see BOM requirements when a sales order is placed, so that I can plan material procurement.

#### Acceptance Criteria

1. WHEN a sales order contains products with BOM THEN the System SHALL calculate total raw material requirements based on order quantity
2. WHEN displaying BOM requirements THEN the System SHALL show each component with required quantity, current stock, and shortage amount
3. WHEN raw materials are insufficient THEN the System SHALL highlight shortage items and suggest purchase order creation
4. IF a product has no BOM defined THEN the System SHALL skip BOM calculation and proceed with standard order processing

### Requirement 5

**User Story:** As an admin, I want to sync manufacturing orders to Luca, so that production costs are properly tracked in the accounting system.

#### Acceptance Criteria

1. WHEN a manufacturing order is completed THEN the System SHALL create a production document in Luca with consumed materials
2. WHEN syncing production THEN the System SHALL decrease raw material stock and increase finished product stock in Luca
3. WHEN production has scrap/waste THEN the System SHALL record scrap quantity separately with appropriate cost allocation
4. IF Luca sync fails THEN the System SHALL retry with exponential backoff and notify admin after 3 failed attempts

### Requirement 6

**User Story:** As a system administrator, I want to standardize SKU format across all products, so that product identification is consistent.

#### Acceptance Criteria

1. WHEN a new product is created THEN the System SHALL validate SKU format against pattern: PRODUCT-VARIANT-ATTRIBUTE (e.g., TSHIRT-RED-M)
2. WHEN SKU format is invalid THEN the System SHALL display validation error with correct format example
3. WHEN migrating existing products THEN the System SHALL provide a bulk SKU rename tool with preview before applying changes
4. WHEN SKU is changed THEN the System SHALL update all related order lines, stock movements, and Luca mappings

### Requirement 7

**User Story:** As a finance manager, I want variant sales to be properly reflected in Luca invoices, so that revenue is accurately tracked per product variation.

#### Acceptance Criteria

1. WHEN a sales order with variants is synced to Luca THEN the System SHALL create invoice lines with variant-specific stock codes
2. WHEN invoice is created THEN the System SHALL include variant attributes in the line description field
3. WHEN querying sales reports THEN the System SHALL allow filtering by parent product or specific variant
4. IF variant stock code is missing in Luca THEN the System SHALL auto-create the stock card before invoice creation
