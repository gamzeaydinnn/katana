# Requirements Document

## Introduction

Bu özellik, Katana sisteminde aynı isme sahip ancak farklı SKU'lara sahip ürünlerin güvenli bir şekilde analiz edilmesini, konsolidasyonunu ve yönetilmesini sağlar. Sistem, yanlışlıkla gerekli ürünlerin silinmesini önlemek için kullanıcı onayı gerektiren akıllı bir deduplikasyon süreci sunar.

## Glossary

- **Product**: Katana sistemindeki ürün kaydı
- **SKU (Stock Keeping Unit)**: Ürün stok kodu
- **Duplicate Group**: Aynı isme sahip ürün grubu
- **Canonical Product**: Bir duplicate grup içinde ana/master olarak seçilen ürün
- **Merge**: İki veya daha fazla ürünü tek bir ürün altında birleştirme işlemi
- **System**: Katana entegrasyon sistemi
- **Admin User**: Sistem yöneticisi kullanıcı
- **Sales Order**: Satış siparişi
- **BOM (Bill of Materials)**: Ürün reçetesi
- **Stock Movement**: Stok hareketi

## Requirements

### Requirement 1

**User Story:** As an admin user, I want to analyze products with duplicate names, so that I can identify which products should be kept separate and which should be merged.

#### Acceptance Criteria

1. WHEN an admin user requests duplicate analysis THEN the System SHALL group all products by their exact name
2. WHEN products are grouped by name THEN the System SHALL display the count of products in each duplicate group
3. WHEN displaying duplicate groups THEN the System SHALL show product ID, SKU, code, category, and sales order ID for each product
4. WHEN a duplicate group has only one product THEN the System SHALL exclude it from the duplicate analysis results
5. WHEN displaying duplicate groups THEN the System SHALL sort them by count in descending order

### Requirement 2

**User Story:** As an admin user, I want to see detailed information about each product in a duplicate group, so that I can make informed decisions about which products to merge.

#### Acceptance Criteria

1. WHEN viewing a duplicate group THEN the System SHALL display all product attributes including name, SKU, code, category, and associated sales orders
2. WHEN a product has an associated sales order THEN the System SHALL highlight this information prominently
3. WHEN comparing products in a duplicate group THEN the System SHALL show differences in SKU, code, and category fields
4. WHEN a product is part of a BOM THEN the System SHALL indicate this relationship
5. WHEN a product has stock movements THEN the System SHALL display the count of related stock movements

### Requirement 3

**User Story:** As an admin user, I want to select a canonical product from a duplicate group, so that other duplicate products can be merged into it.

#### Acceptance Criteria

1. WHEN selecting a canonical product THEN the System SHALL allow selection of only one product per duplicate group
2. WHEN a product has active sales orders THEN the System SHALL prioritize it as a suggested canonical product
3. WHEN a product has the most complete data THEN the System SHALL suggest it as the canonical product
4. WHEN a canonical product is selected THEN the System SHALL display which products will be merged into it
5. WHEN no canonical product is selected THEN the System SHALL prevent the merge operation

### Requirement 4

**User Story:** As an admin user, I want to preview the impact of merging duplicate products, so that I can verify no critical data will be lost.

#### Acceptance Criteria

1. WHEN previewing a merge operation THEN the System SHALL display all sales orders that will be updated
2. WHEN previewing a merge operation THEN the System SHALL display all BOMs that will be updated
3. WHEN previewing a merge operation THEN the System SHALL display all stock movements that will be updated
4. WHEN a merge would affect active sales orders THEN the System SHALL display a warning message
5. WHEN a merge would delete products with unique data THEN the System SHALL display a critical warning

### Requirement 5

**User Story:** As an admin user, I want to execute a merge operation with confirmation, so that duplicate products are consolidated safely.

#### Acceptance Criteria

1. WHEN executing a merge operation THEN the System SHALL require explicit admin user confirmation
2. WHEN a merge is confirmed THEN the System SHALL update all sales order references to point to the canonical product
3. WHEN a merge is confirmed THEN the System SHALL update all BOM references to point to the canonical product
4. WHEN a merge is confirmed THEN the System SHALL update all stock movement references to point to the canonical product
5. WHEN a merge is confirmed THEN the System SHALL mark duplicate products as inactive and hide them from the system UI

### Requirement 11

**User Story:** As a system, I want to automatically group products by Katana order ID, so that products from the same order are treated as variants of a single product.

#### Acceptance Criteria

1. WHEN products share the same Katana order ID THEN the System SHALL group them as variants of a single product
2. WHEN displaying products THEN the System SHALL show only one product per Katana order ID group
3. WHEN a product group has multiple variants THEN the System SHALL display the variant count
4. WHEN viewing a product with variants THEN the System SHALL allow expanding to see all variants
5. WHEN merging products with the same Katana order ID THEN the System SHALL automatically select the first created product as canonical

### Requirement 6

**User Story:** As an admin user, I want to exclude certain duplicate groups from merging, so that products that should remain separate are not accidentally merged.

#### Acceptance Criteria

1. WHEN viewing duplicate groups THEN the System SHALL allow marking groups as "keep separate"
2. WHEN a group is marked as "keep separate" THEN the System SHALL exclude it from merge operations
3. WHEN a group is marked as "keep separate" THEN the System SHALL store the reason for exclusion
4. WHEN viewing excluded groups THEN the System SHALL display the exclusion reason and timestamp
5. WHEN an excluded group needs to be reconsidered THEN the System SHALL allow removing the "keep separate" flag

### Requirement 7

**User Story:** As an admin user, I want to see a history of merge operations, so that I can audit changes and rollback if necessary.

#### Acceptance Criteria

1. WHEN a merge operation completes THEN the System SHALL create an audit log entry
2. WHEN viewing merge history THEN the System SHALL display the canonical product, merged products, and timestamp
3. WHEN viewing merge history THEN the System SHALL display the admin user who performed the operation
4. WHEN viewing merge history THEN the System SHALL display the count of updated references
5. WHEN a merge needs to be rolled back THEN the System SHALL provide a rollback function that restores original product references

### Requirement 8

**User Story:** As an admin user, I want to filter and search duplicate groups, so that I can focus on specific product categories or patterns.

#### Acceptance Criteria

1. WHEN filtering duplicate groups THEN the System SHALL allow filtering by product category
2. WHEN filtering duplicate groups THEN the System SHALL allow filtering by minimum duplicate count
3. WHEN searching duplicate groups THEN the System SHALL allow searching by product name pattern
4. WHEN searching duplicate groups THEN the System SHALL allow searching by SKU pattern
5. WHEN filters are applied THEN the System SHALL update the duplicate group list in real-time

### Requirement 9

**User Story:** As an admin user, I want to export duplicate analysis results, so that I can review them offline or share with team members.

#### Acceptance Criteria

1. WHEN exporting duplicate analysis THEN the System SHALL generate a CSV file with all duplicate groups
2. WHEN exporting duplicate analysis THEN the System SHALL include all product attributes in the export
3. WHEN exporting duplicate analysis THEN the System SHALL include reference counts for sales orders, BOMs, and stock movements
4. WHEN exporting duplicate analysis THEN the System SHALL include the export timestamp and admin user
5. WHEN the export is complete THEN the System SHALL provide a download link to the admin user

### Requirement 10

**User Story:** As a system, I want to validate merge operations before execution, so that data integrity is maintained.

#### Acceptance Criteria

1. WHEN validating a merge operation THEN the System SHALL verify the canonical product exists and is active
2. WHEN validating a merge operation THEN the System SHALL verify all products to be merged exist
3. WHEN validating a merge operation THEN the System SHALL verify no circular references exist in BOMs
4. WHEN validating a merge operation THEN the System SHALL verify the canonical product is not already part of another pending merge
5. WHEN validation fails THEN the System SHALL display specific error messages and prevent the merge operation

### Requirement 11

**User Story:** As a system, I want to automatically group products by Katana order ID, so that products from the same order are treated as variants of a single product.

#### Acceptance Criteria

1. WHEN products share the same Katana order ID THEN the System SHALL group them as variants of a single product
2. WHEN displaying products THEN the System SHALL show only one product per Katana order ID group
3. WHEN a product group has multiple variants THEN the System SHALL display the variant count
4. WHEN viewing a product with variants THEN the System SHALL allow expanding to see all variants
5. WHEN merging products with the same Katana order ID THEN the System SHALL automatically select the first created product as canonical
