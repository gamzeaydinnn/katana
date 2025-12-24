# Requirements Document

## Introduction

Katana entegrasyon sisteminde bazı siparişler tekrar tekrar oluşmuş durumda (örn: SO-SO-84, SO-SO-SO-56). Bu duplike siparişlerin tespit edilip silinmesi gerekmektedir. Ayrıca admin onay akışının doğru çalıştığından emin olunmalıdır: Admin yeşil başparmak emojisine tıkladığında sipariş Katana'ya gitmeli, oradan da Luca'ya aktarılmalıdır. Bir siparişte birden fazla ürün varsa bunlar tek bir satır ve ID olarak Katana'ya gönderilmelidir.

## Glossary

- **System**: Katana entegrasyon sistemi
- **SalesOrder**: Satış siparişi entity'si
- **DuplicateOrder**: Aynı sipariş numarasına sahip tekrar eden kayıtlar
- **Admin**: Sistem yöneticisi rolü
- **Katana**: Üretim ERP sistemi
- **Luca**: Muhasebe/ERP sistemi (Koza)
- **ApprovalFlow**: Admin onay → Katana → Luca akışı
- **OrderNo**: Sipariş numarası (benzersiz olmalı)

## Requirements

### Requirement 1

**User Story:** As a system administrator, I want to identify and delete duplicate sales orders, so that the order list is clean and accurate.

#### Acceptance Criteria

1. WHEN the system analyzes orders THEN the system SHALL identify orders with identical OrderNo values
2. WHEN duplicate orders are found THEN the system SHALL keep the oldest order and mark newer ones for deletion
3. WHEN deleting duplicate orders THEN the system SHALL preserve the original order with all its data
4. WHEN deleting duplicate orders THEN the system SHALL log all deletion operations for audit
5. WHEN duplicate orders have different statuses THEN the system SHALL keep the one with the most advanced status (APPROVED > PENDING)

### Requirement 2

**User Story:** As a system administrator, I want to see a preview of duplicate orders before deletion, so that I can verify the cleanup is correct.

#### Acceptance Criteria

1. WHEN requesting duplicate analysis THEN the system SHALL return a list of all duplicate groups
2. WHEN displaying duplicate groups THEN the system SHALL show which order will be kept and which will be deleted
3. WHEN displaying duplicate groups THEN the system SHALL show order details (OrderNo, CustomerName, Total, Status)
4. WHEN the admin confirms deletion THEN the system SHALL proceed with the cleanup
5. WHEN the admin cancels THEN the system SHALL not delete any orders

### Requirement 3

**User Story:** As a system administrator, I want the admin approval flow to work correctly, so that approved orders go to Katana and then to Luca.

#### Acceptance Criteria

1. WHEN an admin clicks the green thumbs-up button THEN the system SHALL send the order to Katana
2. WHEN the Katana order is created THEN the system SHALL automatically send it to Luca
3. WHEN an order has multiple products THEN the system SHALL send them as a single Katana order with multiple rows
4. WHEN the approval succeeds THEN the system SHALL update the order status to APPROVED
5. WHEN the approval fails THEN the system SHALL display a clear error message

### Requirement 4

**User Story:** As a system administrator, I want to prevent future duplicate orders, so that the system maintains data integrity.

#### Acceptance Criteria

1. WHEN syncing orders from Katana THEN the system SHALL check for existing orders with the same OrderNo
2. WHEN a duplicate OrderNo is detected THEN the system SHALL update the existing order instead of creating a new one
3. WHEN updating an existing order THEN the system SHALL preserve the original ID and sync status
4. WHEN an order is already approved THEN the system SHALL not allow re-sync from Katana
5. WHEN logging sync operations THEN the system SHALL indicate whether an order was created or updated

### Requirement 5

**User Story:** As a system administrator, I want to clean up orders with malformed OrderNo values, so that the order list is consistent.

#### Acceptance Criteria

1. WHEN analyzing orders THEN the system SHALL identify orders with malformed OrderNo (e.g., SO-SO-84, SO-SO-SO-56)
2. WHEN malformed OrderNo is detected THEN the system SHALL extract the correct base OrderNo
3. WHEN cleaning up malformed orders THEN the system SHALL merge them with the correct base order if it exists
4. WHEN no base order exists THEN the system SHALL rename the malformed order to the correct format
5. WHEN cleanup is complete THEN the system SHALL report the number of orders fixed

</content>
</invoke>
