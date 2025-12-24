# Requirements Document

## Introduction

Katana entegrasyon sisteminde admin onay süreci yanlış çalışmaktadır. Şu anda her sipariş satırı ayrı bir ürün olarak Katana'ya gönderilmekte, bu da gereksiz ürün çoğalmasına neden olmaktadır.

Doğru yaklaşım: Bir sipariş onaylandığında, tüm sipariş satırları (varyantlar) tek bir Katana order olarak gönderilmeli ve her satır ayrı bir line item olarak işlenmelidir. Siparişler daha sonra Luca'ya tek bir fatura olarak aktarılmalıdır.

## Glossary

- **System**: Katana entegrasyon sistemi
- **SalesOrder**: Satış siparişi entity'si
- **SalesOrderLine**: Sipariş satırı/kalemi
- **Admin**: Sistem yöneticisi rolü
- **Katana**: Üretim ERP sistemi
- **Luca**: Muhasebe/ERP sistemi (Koza)
- **Variant**: Ürün varyantı (renk, beden vb.)
- **KatanaOrderId**: Katana'daki sipariş ID'si
- **ApprovalProcess**: Sipariş onay süreci

## Requirements

### Requirement 1

**User Story:** As a system administrator, I want to approve a sales order and send it to Katana as a single order with multiple lines, so that products are not duplicated in Katana.

#### Acceptance Criteria

1. WHEN an admin approves a sales order THEN the system SHALL send exactly one Katana order for that sales order
2. WHEN a sales order has multiple lines THEN the system SHALL send each line as a separate row in the same Katana order
3. WHEN sending to Katana THEN the system SHALL NOT create separate products for each order line
4. WHEN the Katana order is created THEN the system SHALL store the returned KatanaOrderId in the SalesOrder record
5. WHEN the Katana order is created THEN the system SHALL update all SalesOrderLines with the same KatanaOrderId

### Requirement 2

**User Story:** As a system administrator, I want order lines to be sent as variants within a single order, so that the order structure is preserved correctly.

#### Acceptance Criteria

1. WHEN building the Katana order payload THEN the system SHALL include all order lines as separate rows
2. WHEN an order line has variant information THEN the system SHALL preserve the variant details in the Katana order row
3. WHEN an order line has a SKU THEN the system SHALL use that SKU in the Katana order row
4. WHEN an order line has quantity and price THEN the system SHALL include those values in the Katana order row
5. WHEN the order has location information THEN the system SHALL include the location in each order row

### Requirement 3

**User Story:** As a system administrator, I want the system to validate order data before sending to Katana, so that invalid orders are rejected early.

#### Acceptance Criteria

1. WHEN validating an order THEN the system SHALL verify that the order has at least one line
2. WHEN validating order lines THEN the system SHALL verify that each line has a non-empty SKU
3. WHEN validating order lines THEN the system SHALL verify that each line has a positive quantity
4. WHEN validation fails THEN the system SHALL return a clear error message and prevent approval
5. WHEN all validations pass THEN the system SHALL proceed with sending the order to Katana

### Requirement 4

**User Story:** As a system administrator, I want the approved order to be sent to Luca as a single invoice with multiple lines, so that the invoice matches the Katana order structure.

#### Acceptance Criteria

1. WHEN an order is approved and sent to Katana THEN the system SHALL also send it to Luca as a single invoice
2. WHEN creating the Luca invoice THEN the system SHALL include all order lines as separate invoice lines
3. WHEN the Luca invoice is created THEN the system SHALL store the returned LucaOrderId in the SalesOrder record
4. WHEN the Luca sync fails THEN the system SHALL log the error but NOT rollback the Katana order creation
5. WHEN the Luca sync succeeds THEN the system SHALL mark the order as IsSyncedToLuca

### Requirement 5

**User Story:** As a system administrator, I want the system to handle approval errors gracefully, so that partial failures can be recovered.

#### Acceptance Criteria

1. WHEN Katana order creation fails THEN the system SHALL log the error and return it to the admin
2. WHEN Katana order creation fails THEN the system SHALL NOT mark the order as approved
3. WHEN Luca invoice creation fails THEN the system SHALL log the error but keep the order as approved
4. WHEN Luca invoice creation fails THEN the system SHALL allow manual retry of Luca sync
5. WHEN any error occurs THEN the system SHALL provide detailed error information to the admin

### Requirement 6

**User Story:** As a system administrator, I want to prevent duplicate order submissions, so that the same order is not sent to Katana multiple times.

#### Acceptance Criteria

1. WHEN an order is already approved THEN the system SHALL reject further approval attempts
2. WHEN an order has a KatanaOrderId THEN the system SHALL reject approval attempts
3. WHEN checking for duplicates THEN the system SHALL verify the order status is Pending
4. WHEN an order is being approved THEN the system SHALL use database transactions to prevent race conditions
5. WHEN duplicate approval is detected THEN the system SHALL return a clear error message

### Requirement 7

**User Story:** As a system administrator, I want the system to log all approval operations, so that I can audit and troubleshoot issues.

#### Acceptance Criteria

1. WHEN an approval starts THEN the system SHALL log the order ID, user, and timestamp
2. WHEN sending to Katana THEN the system SHALL log the request payload and response
3. WHEN sending to Luca THEN the system SHALL log the request payload and response
4. WHEN an error occurs THEN the system SHALL log the full error message and stack trace
5. WHEN approval completes THEN the system SHALL log the final status and any warnings

### Requirement 8

**User Story:** As a system administrator, I want to see the approval status in the UI, so that I can track which orders have been processed.

#### Acceptance Criteria

1. WHEN viewing orders THEN the system SHALL display the approval status (Pending, Approved, Error)
2. WHEN an order is approved THEN the system SHALL display the KatanaOrderId
3. WHEN an order is synced to Luca THEN the system SHALL display the LucaOrderId
4. WHEN an error occurs THEN the system SHALL display the error message in the UI
5. WHEN viewing order details THEN the system SHALL show all order lines with their Katana sync status
