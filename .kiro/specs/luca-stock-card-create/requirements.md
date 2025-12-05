# Requirements Document

## Introduction

Bu özellik, Katana sisteminden Luca API'ye stok kartı oluşturma işlemini gerçekleştirir. Kullanıcılar yeni ürünler tanımlayabilir ve bu ürünleri Luca muhasebe sistemine senkronize edebilir. API, belirli zorunlu alanlar (kartAdi, kartKodu, baslangicTarihi, kartTuru) ve opsiyonel alanlar (barkod, KDV oranları, tevkifat bilgileri vb.) ile stok kartı oluşturmayı destekler.

## Glossary

- **Stok Kartı**: Luca sisteminde bir ürün veya hizmeti temsil eden kayıt
- **kartKodu**: Ürünün benzersiz kodu (örn: "00013225")
- **kartAdi**: Ürünün adı/tanımı
- **kartTuru**: Kart türü (1=Stok, 2=Hizmet)
- **kartTipi**: Kart tipi kategorisi
- **olcumBirimiId**: Ölçüm birimi ID'si (Adet, Kg, Lt vb.)
- **baslangicTarihi**: Kartın geçerlilik başlangıç tarihi (dd/mm/yyyy formatında)
- **kategoriAgacKod**: Ürün kategori ağacı kodu
- **tevkifat**: Vergi kesintisi oranları ve tipleri
- **KDV Oranı**: Katma Değer Vergisi oranı
- **skartId**: Luca tarafından döndürülen stok kartı ID'si
- **LucaService**: Luca API ile iletişim kuran servis katmanı
- **Koza**: Luca'nın web arayüzü/API sistemi

## Requirements

### Requirement 1

**User Story:** As a warehouse manager, I want to create new stock cards in Luca, so that I can track inventory and integrate with the accounting system.

#### Acceptance Criteria

1. WHEN a user submits a stock card creation request with valid required fields (kartAdi, kartKodu, baslangicTarihi, kartTuru) THEN the LucaService SHALL send the request to Luca API and return the created skartId
2. WHEN a user submits a stock card creation request with missing required fields THEN the LucaService SHALL reject the request with a validation error before sending to Luca
3. WHEN Luca API returns a successful response with skartId THEN the LucaService SHALL parse the response and return a success result containing the skartId and confirmation message
4. WHEN Luca API returns an error response THEN the LucaService SHALL parse the error and return a failure result with the error message

### Requirement 2

**User Story:** As a product manager, I want to specify product details like barcode, VAT rates, and category, so that the stock card contains complete information for accounting purposes.

#### Acceptance Criteria

1. WHEN a user provides optional fields (barkod, kartAlisKdvOran, olcumBirimiId, kategoriAgacKod) THEN the LucaService SHALL include these fields in the API request
2. WHEN a user provides tevkifat (withholding tax) information THEN the LucaService SHALL format the tevkifat rates correctly (e.g., "7/10", "2/10") and include tevkifatTipId values
3. WHEN a user does not provide optional boolean flags (satilabilirFlag, satinAlinabilirFlag, lotNoFlag) THEN the LucaService SHALL use default values (satilabilirFlag=1, satinAlinabilirFlag=1, lotNoFlag=0)

### Requirement 3

**User Story:** As a developer, I want the stock card creation request to be properly serialized, so that Luca API can parse and process it correctly.

#### Acceptance Criteria

1. WHEN serializing the stock card request THEN the LucaService SHALL format the baslangicTarihi field as "dd/MM/yyyy" string
2. WHEN serializing the stock card request THEN the LucaService SHALL use JSON format with correct property names matching Luca API expectations
3. WHEN the LucaService receives a response THEN the system SHALL deserialize the JSON response to extract skartId, error flag, and message fields

### Requirement 4

**User Story:** As a system administrator, I want stock card creation to handle authentication and session management, so that requests are properly authorized.

#### Acceptance Criteria

1. WHEN creating a stock card THEN the LucaService SHALL ensure authentication is valid before sending the request
2. WHEN the session expires during stock card creation THEN the LucaService SHALL re-authenticate and retry the request
3. WHEN branch selection is required THEN the LucaService SHALL ensure the correct branch is selected before creating the stock card

### Requirement 5

**User Story:** As a developer, I want to validate stock card data before sending to Luca, so that invalid requests are caught early and meaningful error messages are provided.

#### Acceptance Criteria

1. WHEN kartKodu is empty or null THEN the LucaService SHALL return a validation error with message "kartKodu is required"
2. WHEN kartAdi is empty or null THEN the LucaService SHALL return a validation error with message "kartAdi is required"
3. WHEN kartTuru is not 1 or 2 THEN the LucaService SHALL return a validation error with message "kartTuru must be 1 (Stok) or 2 (Hizmet)"
4. WHEN baslangicTarihi is not in valid date format THEN the LucaService SHALL return a validation error with message "baslangicTarihi must be in dd/MM/yyyy format"
