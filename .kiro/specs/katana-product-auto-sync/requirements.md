# Requirements Document

## Introduction

Bu özellik, Katana API'den ürünlerin otomatik olarak yerel veritabanına senkronize edilmesini sağlar. Şu anda ürünler sadece manuel olarak `sync=true` parametresi ile çağrıldığında kaydediliyor, bu da kullanıcı deneyimini olumsuz etkiliyor ve Luca'ya stok kartı oluşturulmasını engelliyor.

## Glossary

- **Katana API**: Harici ürün yönetim sistemi API'si
- **Local Database**: Uygulama veritabanı (PostgreSQL)
- **Background Worker**: Arka planda periyodik olarak çalışan servis
- **Luca**: Muhasebe sistemi entegrasyonu
- **Stock Card**: Luca sistemindeki stok kartı kaydı
- **Product Sync**: Ürün senkronizasyon işlemi

## Requirements

### Requirement 1

**User Story:** Sistem yöneticisi olarak, Katana'daki yeni ürünlerin otomatik olarak yerel veritabanına kaydedilmesini istiyorum, böylece manuel senkronizasyon yapmama gerek kalmasın.

#### Acceptance Criteria

1. WHEN the system starts THEN the Background Worker SHALL initialize and begin periodic synchronization
2. WHEN the Background Worker runs THEN the system SHALL fetch all products from Katana API
3. WHEN new products are detected THEN the system SHALL save them to the local database with default category
4. WHEN products are saved THEN the system SHALL log the operation with created/updated/skipped counts
5. IF Katana API is unavailable THEN the system SHALL log the error and retry on the next scheduled run

### Requirement 2

**User Story:** Sistem yöneticisi olarak, senkronizasyon sıklığını yapılandırabilmek istiyorum, böylece sistem yükünü kontrol edebilirim.

#### Acceptance Criteria

1. WHEN the application starts THEN the system SHALL read sync interval from configuration
2. WHERE sync interval is not configured THEN the system SHALL use a default interval of 5 minutes
3. WHEN sync interval is changed THEN the system SHALL apply the new interval on next application restart
4. WHEN sync interval is less than 1 minute THEN the system SHALL reject the configuration and use default

### Requirement 3

**User Story:** Geliştirici olarak, senkronize edilen ürünlerin Luca'ya otomatik olarak stok kartı olarak gönderilmesini istiyorum, böylece manuel işlem gerekmez.

#### Acceptance Criteria

1. WHEN a new product is created in local database THEN the system SHALL trigger stock card creation in Luca
2. WHEN stock card creation succeeds THEN the system SHALL update the product with LucaId
3. IF stock card creation fails THEN the system SHALL log the error and mark the product for retry
4. WHEN a product is updated THEN the system SHALL update the corresponding stock card in Luca
5. IF Luca API is unavailable THEN the system SHALL queue the operation for later retry

### Requirement 4

**User Story:** Sistem yöneticisi olarak, senkronizasyon durumunu ve hatalarını görebilmek istiyorum, böylece sorunları hızlıca tespit edebilirim.

#### Acceptance Criteria

1. WHEN sync operation completes THEN the system SHALL log summary with counts and duration
2. WHEN sync errors occur THEN the system SHALL log detailed error information
3. WHEN sync runs THEN the system SHALL update last sync timestamp in database
4. WHERE admin panel is accessed THEN the system SHALL display last sync status and timestamp
5. WHEN sync is running THEN the system SHALL prevent concurrent sync operations

### Requirement 5

**User Story:** Geliştirici olarak, mevcut manuel senkronizasyon endpoint'inin çalışmaya devam etmesini istiyorum, böylece gerektiğinde manuel tetikleme yapabilirim.

#### Acceptance Criteria

1. WHEN manual sync endpoint is called THEN the system SHALL perform immediate synchronization
2. WHEN manual sync is triggered THEN the system SHALL return sync results to the caller
3. IF automatic sync is running THEN the system SHALL queue manual sync request
4. WHEN manual sync completes THEN the system SHALL return created/updated/skipped counts
5. WHERE sync parameter is true THEN the system SHALL maintain backward compatibility with existing behavior
