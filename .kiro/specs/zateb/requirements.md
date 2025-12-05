# Requirements Document

## Introduction

Bu özellik, Katana sisteminden ZATEB (Zaman Damgalı Elektronik Belge) entegrasyonunu gerçekleştirir. ZATEB, Türkiye'deki e-fatura ve e-arşiv sistemleri için kullanılan bir entegrasyon platformudur. Kullanıcılar fatura, irsaliye ve diğer ticari belgeleri ZATEB üzerinden GİB'e (Gelir İdaresi Başkanlığı) iletebilir ve belge durumlarını takip edebilir.

## Glossary

- **ZATEB**: Zaman Damgalı Elektronik Belge - E-fatura/e-arşiv entegrasyon platformu
- **GİB**: Gelir İdaresi Başkanlığı - Türkiye'nin vergi otoritesi
- **e-Fatura**: Elektronik fatura - GİB'e kayıtlı mükellefler arası fatura
- **e-Arşiv**: Elektronik arşiv fatura - GİB'e kayıtlı olmayan mükelleflere kesilen fatura
- **UUID**: Faturanın benzersiz tanımlayıcısı
- **ETTN**: E-fatura takip numarası
- **Zarf (Envelope)**: Faturaların GİB'e gönderildiği paket
- **Durum Sorgusu**: Gönderilen belgenin GİB'deki durumunu sorgulama
- **Onay/Red**: Alınan faturanın kabul veya ret edilmesi

## Requirements

### Requirement 1

**User Story:** As a finance manager, I want to send e-invoices through ZATEB to GİB, so that I can comply with Turkish electronic invoicing regulations.

#### Acceptance Criteria

1. WHEN a user submits an invoice for e-fatura transmission THEN the ZatebService SHALL validate the invoice data and send it to ZATEB API
2. WHEN ZATEB API returns a successful response with UUID/ETTN THEN the ZatebService SHALL store the tracking information and return success
3. WHEN ZATEB API returns an error response THEN the ZatebService SHALL parse the error and return a failure result with the error message
4. WHEN the invoice recipient is not registered for e-fatura THEN the ZatebService SHALL automatically route to e-arşiv

### Requirement 2

**User Story:** As a finance manager, I want to check the status of sent invoices, so that I can track whether they were delivered and accepted.

#### Acceptance Criteria

1. WHEN a user queries invoice status by UUID THEN the ZatebService SHALL call ZATEB status API and return current status
2. WHEN the invoice status is "Kabul Edildi" (Accepted) THEN the ZatebService SHALL update local records accordingly
3. WHEN the invoice status is "Reddedildi" (Rejected) THEN the ZatebService SHALL return rejection reason and update local records
4. WHEN the invoice is still pending THEN the ZatebService SHALL return "Beklemede" status with estimated processing time

### Requirement 3

**User Story:** As a finance manager, I want to receive and process incoming e-invoices, so that I can manage supplier invoices electronically.

#### Acceptance Criteria

1. WHEN new incoming invoices are available THEN the ZatebService SHALL fetch them from ZATEB inbox
2. WHEN an incoming invoice is received THEN the ZatebService SHALL parse and store the invoice data locally
3. WHEN a user accepts an incoming invoice THEN the ZatebService SHALL send acceptance response to ZATEB
4. WHEN a user rejects an incoming invoice THEN the ZatebService SHALL send rejection response with reason to ZATEB

### Requirement 4

**User Story:** As a developer, I want ZATEB integration to handle authentication and session management, so that API requests are properly authorized.

#### Acceptance Criteria

1. WHEN connecting to ZATEB THEN the ZatebService SHALL authenticate using configured credentials
2. WHEN the session expires THEN the ZatebService SHALL re-authenticate and retry the request
3. WHEN authentication fails THEN the ZatebService SHALL return a clear error message indicating credential issues

### Requirement 5

**User Story:** As a developer, I want to validate invoice data before sending to ZATEB, so that invalid requests are caught early.

#### Acceptance Criteria

1. WHEN required fields (VKN/TCKN, invoice date, invoice number) are missing THEN the ZatebService SHALL return validation error
2. WHEN invoice total does not match line items THEN the ZatebService SHALL return calculation error
3. WHEN tax calculations are incorrect THEN the ZatebService SHALL return tax validation error
4. WHEN recipient VKN/TCKN format is invalid THEN the ZatebService SHALL return format validation error

</content>
</invoke>
