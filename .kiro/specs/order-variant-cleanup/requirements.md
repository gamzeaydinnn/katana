# Requirements Document

## Introduction

Katana sisteminde siparişlerden gelen ürünler için yanlış varyant yapısı oluşturulmuş durumda. Her sipariş satırı ayrı bir KatanaOrderId almış ve Katana'ya gereksiz ürün kayıtları gönderilmiştir. Bu durum veri karmaşasına ve stok yönetimi sorunlarına yol açmaktadır.

Bu özellik, Katana'ya daha önce siparişlerle gönderilmiş yanlış verileri temizleyecek ve tüm siparişlerdeki admin onaylarını geri çekecektir. Luca tarafındaki temizlik manuel olarak yapılacaktır.

## Glossary

- **System**: Katana entegrasyon sistemi
- **SalesOrder**: Satış siparişi entity'si
- **SalesOrderLine**: Sipariş satırı entity'si
- **KatanaOrderId**: Katana ERP sistemindeki sipariş ID'si
- **KatanaProduct**: Katana'da oluşturulmuş ürün
- **OrderProduct**: Siparişten Katana'ya gönderilmiş ürün
- **Admin**: Sistem yöneticisi rolü
- **Katana**: Üretim ERP sistemi
- **ApprovedOrder**: Admin tarafından onaylanmış sipariş

## Requirements

### Requirement 1

**User Story:** As a system administrator, I want to identify products that were sent to Katana from orders, so that I can clean them up from Katana system.

#### Acceptance Criteria

1. WHEN the system scans order history THEN the system SHALL identify all products that were sent to Katana during order approval
2. WHEN order products are identified THEN the system SHALL collect their SKU codes and Katana product IDs
3. WHEN the system generates a cleanup report THEN the system SHALL display product count, SKU list, and affected order count
4. WHEN the report is generated THEN the system SHALL group products by order to show which products came from which orders
5. WHEN duplicate SKUs exist THEN the system SHALL identify them and show consolidation opportunities

### Requirement 2

**User Story:** As a system administrator, I want to delete order-generated products from Katana, so that incorrect product records are removed from the ERP system.

#### Acceptance Criteria

1. WHEN order products are deleted THEN the system SHALL call Katana API to delete each product by SKU
2. WHEN Katana deletion succeeds THEN the system SHALL log the successful deletion with product details
3. WHEN Katana deletion fails THEN the system SHALL log the error and continue with remaining products
4. WHEN all deletions complete THEN the system SHALL generate a summary report with success count, fail count, and error details
5. WHEN deletion is requested THEN the system SHALL support dry-run mode to preview changes without executing them

### Requirement 3

**User Story:** As a system administrator, I want to reset all approved sales orders to pending status, so that they can be re-approved with the correct grouping logic.

#### Acceptance Criteria

1. WHEN orders are reset THEN the system SHALL change Status from Approved to Pending for all affected orders
2. WHEN orders are reset THEN the system SHALL clear ApprovedDate, ApprovedBy, and SyncStatus fields
3. WHEN orders are reset THEN the system SHALL set all KatanaOrderId values to null in SalesOrderLines
4. WHEN orders are reset THEN the system SHALL delete all OrderMapping records for pending orders
5. WHEN reset completes THEN the system SHALL generate a report showing count of orders reset, lines affected, and mappings cleared

### Requirement 4

**User Story:** As a system administrator, I want orders to be grouped by their original Katana order ID during future approvals, so that one sales order creates one Katana order with multiple lines instead of separate orders.

#### Acceptance Criteria

1. WHEN an admin approves a sales order in the future THEN the system SHALL generate exactly one KatanaOrderId for all lines in that order
2. WHEN multiple order lines exist THEN the system SHALL assign the same KatanaOrderId to all lines
3. WHEN grouping orders THEN the system SHALL use the original KatanaOrderId from Katana sync as the grouping key
4. WHEN no KatanaOrderId exists THEN the system SHALL use the local SalesOrder ID as fallback grouping key
5. WHEN sending to Katana THEN the system SHALL create one order with N lines for N products

### Requirement 5

**User Story:** As a system administrator, I want the approval process to validate order data before sending to Katana, so that invalid data does not cause sync failures.

#### Acceptance Criteria

1. WHEN validating an order THEN the system SHALL verify that order lines exist and are not empty
2. WHEN validating order lines THEN the system SHALL verify that each line has a non-empty SKU and positive quantity
3. WHEN validation fails THEN the system SHALL return a clear error message and prevent approval
4. WHEN customer data is missing THEN the system SHALL return an error indicating which fields are required
5. WHEN all validations pass THEN the system SHALL proceed with the approval workflow

### Requirement 6

**User Story:** As a system administrator, I want detailed logging and audit trails for cleanup operations, so that I can track what was changed and troubleshoot issues.

#### Acceptance Criteria

1. WHEN any cleanup operation executes THEN the system SHALL log the operation start time, user, and parameters
2. WHEN products are deleted THEN the system SHALL log each product ID, name, and deletion result
3. WHEN orders are reset THEN the system SHALL log each order ID, previous status, and new status
4. WHEN errors occur THEN the system SHALL log the full error message, stack trace, and context
5. WHEN operations complete THEN the system SHALL log summary statistics and total duration

### Requirement 7

**User Story:** As a system administrator, I want a rollback capability for cleanup operations, so that I can restore the system if something goes wrong.

#### Acceptance Criteria

1. WHEN starting cleanup operations THEN the system SHALL create a database backup
2. WHEN a critical error occurs THEN the system SHALL provide instructions for restoring from backup
3. WHEN rollback is requested THEN the system SHALL restore the database from the most recent backup
4. WHEN rollback completes THEN the system SHALL verify data integrity and log the restoration result
5. WHEN backup fails THEN the system SHALL prevent cleanup operations from proceeding

### Requirement 8

**User Story:** As a system administrator, I want to monitor the health of the cleanup process, so that I can ensure operations complete successfully and track system improvements.

#### Acceptance Criteria

1. WHEN cleanup runs THEN the system SHALL track and display progress percentage
2. WHEN operations complete THEN the system SHALL display total duration, success rate, and error count
3. WHEN errors occur THEN the system SHALL display them in real-time in the admin interface
4. WHEN monitoring metrics THEN the system SHALL track orders per KatanaOrderId ratio before and after cleanup
5. WHEN future approvals occur THEN the system SHALL verify that one order creates one KatanaOrderId
