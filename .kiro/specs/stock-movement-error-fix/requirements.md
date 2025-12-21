# Requirements Document

## Introduction

Stok hareketleri ekranında çok sayıda "Hata" durumunda kayıt bulunmaktadır. Bu kayıtlar Luca'ya senkronize edilirken çeşitli nedenlerle başarısız olmuştur. Bu özellik, hatalı stok hareketlerini (transfer ve düzeltme kayıtları) tespit edip, hata nedenlerini analiz edip, otomatik düzeltme mekanizmaları sağlayacak ve kullanıcıların hatalı kayıtları toplu olarak yeniden işlemesine olanak tanıyacaktır.

## Glossary

- **StockMovement**: Stok hareketi - Transfer veya Adjustment kayıtları
- **Transfer**: Depolar arası stok transferi
- **Adjustment**: Stok düzeltme işlemi (Fire, Sarf, Sayım Fazlası, vb.)
- **Luca**: Hedef muhasebe sistemi
- **SyncStatus**: Senkronizasyon durumu (PENDING, SYNCED, ERROR)
- **ErrorMessage**: Hata mesajı - Senkronizasyon başarısız olduğunda kaydedilen hata açıklaması
- **Retry**: Yeniden deneme - Hatalı kaydı tekrar Luca'ya gönderme işlemi
- **BulkRetry**: Toplu yeniden deneme - Birden fazla hatalı kaydı aynı anda yeniden işleme
- **ErrorAnalysis**: Hata analizi - Hata nedenlerini kategorize etme ve raporlama

## Requirements

### Requirement 1

**User Story:** As a system administrator, I want to view all failed stock movements with their error messages, so that I can understand why synchronization failed.

#### Acceptance Criteria

1. WHEN the system queries failed movements THEN the System SHALL return all records with SyncStatus = "ERROR" including their error messages
2. WHEN displaying failed movements THEN the System SHALL show DocumentNo, MovementType, MovementDate, ErrorMessage, and FailedAt timestamp
3. WHEN no error message exists THEN the System SHALL display "Bilinmeyen hata" as the error message
4. WHEN filtering by movement type THEN the System SHALL support filtering by "TRANSFER" or "ADJUSTMENT"
5. WHEN filtering by date range THEN the System SHALL return only movements within the specified date range

### Requirement 2

**User Story:** As a system administrator, I want to categorize error types, so that I can identify common failure patterns.

#### Acceptance Criteria

1. WHEN analyzing an error message THEN the System SHALL categorize it into one of: MAPPING_ERROR, VALIDATION_ERROR, API_ERROR, AUTHENTICATION_ERROR, or UNKNOWN_ERROR
2. WHEN the error contains "mapping" or "bulunamadı" THEN the System SHALL categorize it as MAPPING_ERROR
3. WHEN the error contains "validation" or "geçersiz" THEN the System SHALL categorize it as VALIDATION_ERROR
4. WHEN the error contains "API" or "timeout" or "connection" THEN the System SHALL categorize it as API_ERROR
5. WHEN the error contains "authentication" or "session" or "oturum" THEN the System SHALL categorize it as AUTHENTICATION_ERROR
6. WHEN the error does not match any pattern THEN the System SHALL categorize it as UNKNOWN_ERROR

### Requirement 3

**User Story:** As a system administrator, I want to see error statistics grouped by category, so that I can prioritize which issues to fix first.

#### Acceptance Criteria

1. WHEN requesting error statistics THEN the System SHALL return counts grouped by error category
2. WHEN calculating statistics THEN the System SHALL include total error count, errors by category, and errors by movement type
3. WHEN displaying statistics THEN the System SHALL show percentage of each error category relative to total errors
4. WHEN no errors exist THEN the System SHALL return zero counts for all categories

### Requirement 4

**User Story:** As a system administrator, I want to retry a single failed movement, so that I can fix individual errors after resolving the underlying issue.

#### Acceptance Criteria

1. WHEN retrying a failed movement THEN the System SHALL fetch the movement data from database
2. WHEN the movement is a TRANSFER THEN the System SHALL call SyncTransferToLucaAsync with the transfer ID
3. WHEN the movement is an ADJUSTMENT THEN the System SHALL call SyncAdjustmentToLucaAsync with the adjustment ID
4. WHEN retry succeeds THEN the System SHALL update SyncStatus to "SYNCED" and clear the error message
5. WHEN retry fails THEN the System SHALL update the error message with the new failure reason and increment retry count
6. WHEN movement is not found THEN the System SHALL return a 404 error with appropriate message

### Requirement 5

**User Story:** As a system administrator, I want to retry multiple failed movements at once, so that I can efficiently process many errors.

#### Acceptance Criteria

1. WHEN bulk retry is requested THEN the System SHALL accept a list of movement IDs and their types
2. WHEN processing bulk retry THEN the System SHALL retry each movement sequentially
3. WHEN bulk retry completes THEN the System SHALL return total count, success count, and failed count
4. WHEN a movement fails during bulk retry THEN the System SHALL continue processing remaining movements
5. WHEN bulk retry is requested with empty list THEN the System SHALL return zero counts

### Requirement 6

**User Story:** As a system administrator, I want to automatically fix common mapping errors, so that movements can be retried successfully.

#### Acceptance Criteria

1. WHEN a MAPPING_ERROR is detected for missing product code THEN the System SHALL attempt to fetch the Luca stock code from mapping repository
2. WHEN a MAPPING_ERROR is detected for missing warehouse code THEN the System SHALL use default warehouse code "002"
3. WHEN a MAPPING_ERROR is detected for missing unit THEN the System SHALL use default unit ID from settings
4. WHEN auto-fix succeeds THEN the System SHALL log the fix and mark the movement as ready for retry
5. WHEN auto-fix fails THEN the System SHALL log the failure and keep the error status

### Requirement 7

**User Story:** As a system administrator, I want to clear error status for movements that should be retried, so that I can reset failed movements to pending state.

#### Acceptance Criteria

1. WHEN clearing error status THEN the System SHALL update SyncStatus from "ERROR" to "PENDING"
2. WHEN clearing error status THEN the System SHALL clear the ErrorMessage field
3. WHEN clearing error status THEN the System SHALL reset RetryCount to zero
4. WHEN clearing error status for multiple movements THEN the System SHALL process all movements in a single transaction
5. WHEN clearing error status fails THEN the System SHALL rollback all changes

### Requirement 8

**User Story:** As a system administrator, I want to export failed movements to CSV, so that I can analyze errors offline or share with team members.

#### Acceptance Criteria

1. WHEN exporting failed movements THEN the System SHALL generate a CSV file with columns: DocumentNo, MovementType, MovementDate, ErrorCategory, ErrorMessage, RetryCount
2. WHEN no failed movements exist THEN the System SHALL return an empty CSV with headers only
3. WHEN export is requested THEN the System SHALL include all movements with SyncStatus = "ERROR"
4. WHEN export completes THEN the System SHALL return the CSV file with appropriate content-type header

### Requirement 9

**User Story:** As a system administrator, I want to view detailed error logs for a specific movement, so that I can debug complex synchronization issues.

#### Acceptance Criteria

1. WHEN requesting error details for a movement THEN the System SHALL return the movement data, error message, retry history, and related mapping data
2. WHEN the movement is a TRANSFER THEN the System SHALL include source warehouse, destination warehouse, product details, and quantity
3. WHEN the movement is an ADJUSTMENT THEN the System SHALL include warehouse, product details, quantity, and adjustment reason
4. WHEN retry history exists THEN the System SHALL show all previous retry attempts with timestamps and error messages
5. WHEN movement is not found THEN the System SHALL return a 404 error

### Requirement 10

**User Story:** As a system administrator, I want to prevent duplicate retries, so that the same movement is not processed multiple times simultaneously.

#### Acceptance Criteria

1. WHEN a retry is initiated THEN the System SHALL check if the movement is already being processed
2. WHEN a movement is being processed THEN the System SHALL set a processing flag in database
3. WHEN retry completes THEN the System SHALL clear the processing flag
4. WHEN a retry is requested for a movement already being processed THEN the System SHALL return an error indicating the movement is locked
5. WHEN processing flag is older than 5 minutes THEN the System SHALL consider it stale and allow retry

