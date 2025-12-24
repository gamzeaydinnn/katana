# Katana Sipariş Ürünleri Analiz Özeti

**Analiz Tarihi:** 23 Aralık 2025

## 📊 Genel İstatistikler

- **Toplam Sipariş:** 69 adet
- **Toplam Sipariş Satırı:** 300+ adet
- **Benzersiz SKU:** 136 adet
- **Products Tablosunda OLAN:** 80 ürün ✅
- **Products Tablosunda OLMAYAN:** 56 ürün ❌

## 🔍 Önemli Bulgular

### 1. Eksik Ürünler

56 ürün Products tablosunda bulunmuyor ve senkronize edilmesi gerekiyor.

### 2. Sipariş Durumu

- Tüm siparişler **"Pending"** durumunda
- Tarih aralığı: 5 Aralık 2025 - 22 Aralık 2025

### 3. Ürün Tipleri

- Tüm ürünler **VARIANT-** prefix'i ile başlıyor
- Katana API'den gelen varyant ürünleri
- Bazı siparişlerde "GENEL" SKU kullanılmış (SO-56)

### 4. En Çok Sipariş Edilen Ürünler

1. **VARIANT-36652542** - 8 siparişte
2. **VARIANT-37563692** - 6 siparişte
3. **VARIANT-36652282** - 4 siparişte
4. **VARIANT-36652313** - 4 siparişte

## ⚠️ Kritik Sorunlar

### 1. Katana Product ID Eksikliği

```
katana_product_id: NULL (tüm ürünlerde)
```

Hiçbir üründe Katana API product ID'si yok. Bu, Katana API ile ürün senkronizasyonunun yapılmadığını gösteriyor.

### 2. Eksik Ürün Senkronizasyonu

56 ürün Products tablosunda yok:

- VARIANT-37476540
- VARIANT-37476542
- VARIANT-37707875
- VARIANT-36651972 - VARIANT-36651987 (16 ürün)
- Ve diğerleri...

### 3. Tarih Tutarsızlığı

- Bir sipariş (SO-79) gelecek tarihli: **01/12/2026** ⚠️
- Diğer siparişler Aralık 2025 tarihlerinde

## 📋 Yapılması Gerekenler

### 1. Acil: Eksik Ürünleri Senkronize Et

```sql
-- 56 eksik ürünü Products tablosuna ekle
-- Katana API'den ürün bilgilerini çek
-- SKU, Name, Price, Stock bilgilerini doldur
```

### 2. Katana Product ID'lerini Güncelle

```sql
-- Mevcut ürünlerin katana_product_id alanlarını doldur
-- Katana API'den product ID'leri al
-- Products tablosunu güncelle
```

### 3. Katana Order ID'lerini Güncelle

```sql
-- katana_order_id alanlarını doldur
-- Aynı siparişten gelen varyantları grupla
```

### 4. Ürün Senkronizasyon Mekanizması

- Katana API'den otomatik ürün çekme
- Sipariş geldiğinde eksik ürünleri otomatik oluşturma
- Periyodik senkronizasyon (günlük/saatlik)

## 🔧 Teknik Detaylar

### Database Schema

```csharp
public class Product
{
    public int? KatanaProductId { get; set; }      // NULL - Doldurulmalı
    public long? KatanaOrderId { get; set; }       // NULL - Doldurulmalı
    public long? LucaId { get; set; }              // Luca sync için
}
```

### Eksik Alanlar

- `katana_product_id`: Tüm ürünlerde NULL
- `katana_order_id`: Tüm ürünlerde NULL
- `LucaId`: Bazı ürünlerde NULL

## 📈 Sonraki Adımlar

1. **Katana API Integration**

   - Product endpoint'ini kullan
   - Eksik 56 ürünü çek ve kaydet
   - Mevcut ürünleri güncelle

2. **Sync Service Geliştir**

   - KatanaProductSyncService oluştur
   - Otomatik senkronizasyon
   - Hata yönetimi

3. **Data Migration**

   - Mevcut ürünlere Katana ID'leri ekle
   - Order ID'leri ile varyantları grupla
   - Luca senkronizasyonu için hazırla

4. **Monitoring & Logging**
   - Senkronizasyon logları
   - Hata bildirimleri
   - Dashboard metrikleri

## 📝 Notlar

- Tüm siparişler "Pending" durumunda - onay bekliyor
- Ürün fiyatları bazı siparişlerde 0.00 (test siparişleri?)
- GENEL SKU kullanımı düzeltilmeli
- Gelecek tarihli sipariş kontrol edilmeli (SO-79)
