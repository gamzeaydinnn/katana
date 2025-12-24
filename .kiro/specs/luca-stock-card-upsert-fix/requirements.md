# Requirements: Luca Stok Kartı UPSERT Düzeltmesi

## Introduction

Katana'dan Luca'ya ürün senkronizasyonu sırasında, aynı SKU'lu ürün geldiğinde gereksiz yeni stok kartı oluşturuluyor. Luca API'de `GuncelleStkWsSkart.do` endpoint'i mevcut ve çalışıyor. Sistem bu endpoint'i kullanarak mevcut kartları güncellemeli, yeni kart açmamalı.

## Glossary

- **SKU**: Stok Tutma Ünitesi (Stock Keeping Unit) - Ürünün benzersiz kodu
- **Luca**: Muhasebe ve İş Yönetimi Sistemi
- **Stok Kartı**: Luca'da ürünü temsil eden kayıt (skartId ile tanımlanır)
- **UPSERT**: UPDATE + INSERT - Varsa güncelle, yoksa oluştur
- **GuncelleStkWsSkart.do**: Luca API'de stok kartı güncelleme endpoint'i
- **EkleStkWsKart.do**: Luca API'de stok kartı oluşturma endpoint'i

## Requirements

### Requirement 1: Mevcut Stok Kartını Güncelle

**User Story:** Katana'dan aynı SKU'lu ürün geldiğinde, sistem Luca'da mevcut stok kartını güncellemeli, yeni kart açmamalı.

#### Acceptance Criteria

1. WHEN aynı SKU'lu ürün Luca'ya gönderilirse THEN sistem önce `FindStockCardBySkuAsync` ile mevcut kartı aramalı
2. WHEN mevcut stok kartı bulunursa THEN sistem `UpdateStockCardAsync` ile kartı güncellemeli
3. WHEN güncelleme başarılı olursa THEN sistem "başarılı" döndürmeli ve yeni kart açmamalı
4. WHEN güncelleme başarısız olursa THEN sistem hata mesajı döndürmeli ve yeni kart açmamalı
5. WHEN stok kartı bulunamazsa THEN sistem `CreateStockCardAsync` ile yeni kart açmalı

### Requirement 2: Güncellenecek Alanları Doğru Şekilde Eşle

**User Story:** Ürün bilgileri Luca'ya gönderilirken, tüm güncellenebilir alanlar doğru şekilde eşlenmelidir.

#### Acceptance Criteria

1. WHEN ürün güncellenirse THEN `kartKodu` (SKU) alanı eşlenmelidir
2. WHEN ürün güncellenirse THEN `kartAdi` (ürün adı) alanı eşlenmelidir
3. WHEN ürün güncellenirse THEN `uzunAdi` (uzun ad) alanı eşlenmelidir
4. WHEN ürün güncellenirse THEN `barkod` alanı eşlenmelidir
5. WHEN ürün güncellenirse THEN `kategoriAgacKod` (kategori) alanı eşlenmelidir
6. WHEN ürün güncellenirse THEN `perakendeAlisBirimFiyat` (alış fiyatı) alanı eşlenmelidir
7. WHEN ürün güncellenirse THEN `perakendeSatisBirimFiyat` (satış fiyatı) alanı eşlenmelidir
8. WHEN ürün güncellenirse THEN `gtipKodu` alanı eşlenmelidir

### Requirement 3: Hata Yönetimi

**User Story:** Güncelleme işlemi sırasında oluşan hatalar düzgün şekilde yönetilmeli ve loglama yapılmalı.

#### Acceptance Criteria

1. WHEN güncelleme başarısız olursa THEN sistem hata mesajını loglayıp döndürmeli
2. WHEN Luca API HTML döndürürse (session expired) THEN sistem session'ı yenileyip tekrar denemeli
3. WHEN tüm denemeler başarısız olursa THEN sistem "güncelleme başarısız" mesajı döndürmeli
4. WHEN güncelleme sırasında exception oluşursa THEN sistem exception'ı loglayıp işlemi güvenli şekilde sonlandırmalı

### Requirement 4: İdempotency (Tekrar Çalıştırılabilirlik)

**User Story:** Aynı ürün birden fazla kez gönderilse bile sistem tutarlı şekilde çalışmalı.

#### Acceptance Criteria

1. WHEN aynı ürün 2 kez gönderilirse THEN sistem 2. kez güncelleme yapmalı, yeni kart açmamalı
2. WHEN güncelleme başarılı olursa THEN sistem "başarılı" döndürmeli
3. WHEN güncelleme başarısız olursa THEN sistem "başarısız" döndürmeli, tutarsız durum oluşmamalı
