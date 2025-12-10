# Requirements Document

## Introduction

Bu özellik, Koza Entegrasyon sayfasındaki Stok Kartları sekmesini, Admin Paneldeki "Luca Ürünleri" sayfasıyla aynı bilgi ve işlevselliğe kavuşturmayı amaçlar. Mevcut durumda Koza Entegrasyon'daki stok kartları sadece temel bilgileri (ID, Kod, Ad, Kategori, KDV) gösterirken, Luca Ürünleri sayfası daha zengin bir veri seti sunmaktadır.

## Glossary

- **Koza Entegrasyon**: Admin panelindeki Koza sistemine entegrasyon yönetim sayfası
- **Stok Kartı**: Ürün bilgilerini içeren kayıt (kod, ad, barkod, kategori, birim, fiyat vb.)
- **LucaProducts**: Admin panelindeki mevcut Luca ürünleri listesi bileşeni
- **KozaIntegration**: Koza entegrasyon yönetim bileşeni
- **kozaAPI**: Frontend'den backend'e Koza işlemleri için API servisi

## Requirements

### Requirement 1

**User Story:** As an admin user, I want to see detailed stock card information in Koza Integration page, so that I can manage stock cards without switching to Luca Products page.

#### Acceptance Criteria

1. WHEN the Koza Integration stock cards tab is displayed THEN the system SHALL show the following columns: Ürün Kodu, Ürün Adı, Barkod, Kategori, Ölçü Birimi, Miktar, Birim Fiyat, KDV Oranı, Durum, Son Güncelleme
2. WHEN stock cards are loaded THEN the system SHALL display all available stock card data from the backend API
3. WHEN a stock card has missing optional fields THEN the system SHALL display "-" as placeholder

### Requirement 2

**User Story:** As an admin user, I want to search and filter stock cards in Koza Integration page, so that I can quickly find specific products.

#### Acceptance Criteria

1. WHEN the stock cards tab is active THEN the system SHALL display a search input field
2. WHEN a user types in the search field THEN the system SHALL filter stock cards by product code or product name
3. WHEN search results are displayed THEN the system SHALL show the count of filtered items

### Requirement 3

**User Story:** As an admin user, I want to see stock card statistics in Koza Integration page, so that I can have an overview of the inventory.

#### Acceptance Criteria

1. WHEN the stock cards tab is displayed THEN the system SHALL show total stock card count
2. WHEN stock cards are filtered THEN the system SHALL show filtered count alongside total count

### Requirement 4

**User Story:** As an admin user, I want responsive mobile support for stock cards in Koza Integration, so that I can manage stock cards on mobile devices.

#### Acceptance Criteria

1. WHEN the page is viewed on mobile devices THEN the system SHALL display stock cards in card layout instead of table
2. WHEN mobile card layout is displayed THEN the system SHALL show all essential stock card information in a readable format
