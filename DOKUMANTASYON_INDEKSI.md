# Dokümantasyon İndeksi

## 📚 Tüm Dokümantasyon Dosyaları

### 1. GRUPLANDIRMA_MANTIGI_RAPORU.md

**Amaç**: Katana sisteminin gruplandırma mekanizmalarını detaylı olarak açıklamak

**İçerik**:

- Varyant Gruplandırması (1.1-1.5)
- SKU Gruplandırması ve Yönetimi (2.1-2.5)
- Duplicate Tespiti ve Yönetimi (3.1-3.4)
- Varyant Duplicate Tespiti (4.1-4.3)
- BOM Gruplandırması (5.1-5.3)
- Stok Hareketi Gruplandırması (6.1-6.2)
- Sistem Entegrasyonu (7.1-7.2)
- Performans Optimizasyonları (8.1-8.3)
- Hata Yönetimi (9.1-9.2)
- Raporlama ve İstatistikler (10.1-10.3)
- Best Practices (11.1-11.3)

**Okuma Süresi**: 30-45 dakika
**Hedef Kitle**: Sistem mimarı, teknik lider

---

### 2. VERI_TEMIZLIGI_VE_SOFT_RESET_STRATEJISI.md

**Amaç**: Hatalı veri temizliği ve soft reset stratejisini adım adım açıklamak

**İçerik**:

- **BÖLÜM 1**: Genel strateji ve felsefe
- **BÖLÜM 2**: Veritabanı şeması güncelleme
- **BÖLÜM 3**: Luca tarafında hatalı kartları silme
- **BÖLÜM 4**: Siparişleri "gönderilmemiş" olarak işaretleme
- **BÖLÜM 5**: Ürünleri inactive olarak işaretleme
- **BÖLÜM 6**: Header-Line mimarisi
- **BÖLÜM 7**: Benzerlik algoritması ve otomatik karar
- **BÖLÜM 8**: Admin dashboard ve preview
- **BÖLÜM 9**: Backup ve geri dönüş stratejisi
- **BÖLÜM 10**: Execution plan (adım adım yapılacaklar)
- **BÖLÜM 11**: Kod örneği - tüm bir akış
- **BÖLÜM 12**: Öğrenci olarak yapılacaklar
- **BÖLÜM 13**: Müşteriye sunuş stratejisi
- **BÖLÜM 14**: Özet ve kontrol listesi

**Okuma Süresi**: 60-90 dakika
**Hedef Kitle**: Geliştirici, proje yöneticisi

---

### 3. HIZLI_BASLANGIC_REHBERI.md

**Amaç**: Hızlı bir şekilde başlamak için temel bilgileri sunmak

**İçerik**:

- 5 dakikalık özet
- Yapılacaklar (sırasıyla)
- Kod şablonları
- Kritik noktalar
- Başarı göstergeleri
- Sorun giderme

**Okuma Süresi**: 10-15 dakika
**Hedef Kitle**: Acele eden geliştirici

---

### 4. UYGULAMA_OZETI.md

**Amaç**: Tüm uygulamanın bir özeti ve referans rehberi

**İçerik**:

- Sorun tanısı
- Çözüm stratejisi (4 aşama)
- Teknik implementasyon
- Levenshtein Distance algoritması
- Güvenlik mekanizmaları
- Header-Line mimarisi
- Execution plan
- Başarı kriterleri
- Sonraki adımlar
- Öğrenme çıktıları

**Okuma Süresi**: 20-30 dakika
**Hedef Kitle**: Herkes

---

## 🎯 Hangi Dosyayı Ne Zaman Okuyacaksın?

### Senin Durumun: Yeni Başlayan Geliştirici

```
1. HIZLI_BASLANGIC_REHBERI.md (10 dakika)
   └─ Temel bilgileri öğren

2. UYGULAMA_OZETI.md (20 dakika)
   └─ Genel resmi gör

3. VERI_TEMIZLIGI_VE_SOFT_RESET_STRATEJISI.md (60 dakika)
   └─ Detaylı implementasyon öğren

4. GRUPLANDIRMA_MANTIGI_RAPORU.md (30 dakika)
   └─ Sistem mimarisini anla
```

**Toplam Okuma Süresi**: ~2 saat

---

### Müşteri Sunuşu Yapacaksan

```
1. UYGULAMA_OZETI.md (Sorun Tanısı bölümü)
   └─ Sorunları açıkla

2. VERI_TEMIZLIGI_VE_SOFT_RESET_STRATEJISI.md (BÖLÜM 13)
   └─ Müşteri sunuş stratejisini oku

3. HIZLI_BASLANGIC_REHBERI.md (Başarı Göstergeleri)
   └─ Sonuçları göster
```

**Hazırlık Süresi**: ~30 dakika

---

### Kod Yazacaksan

```
1. HIZLI_BASLANGIC_REHBERI.md (Kod Şablonları)
   └─ Temel şablonları kopyala

2. VERI_TEMIZLIGI_VE_SOFT_RESET_STRATEJISI.md (BÖLÜM 2-11)
   └─ Detaylı implementasyon oku

3. GRUPLANDIRMA_MANTIGI_RAPORU.md (İlgili bölümler)
   └─ Sistem mimarisini anla
```

**Kodlama Süresi**: ~8 saat

---

## 📊 Dosya Haritası

```
DOKUMANTASYON/
├─ GRUPLANDIRMA_MANTIGI_RAPORU.md
│  ├─ Varyant Gruplandırması
│  ├─ SKU Yönetimi
│  ├─ Duplicate Tespiti
│  ├─ BOM Gruplandırması
│  └─ Sistem Entegrasyonu
│
├─ VERI_TEMIZLIGI_VE_SOFT_RESET_STRATEJISI.md
│  ├─ Genel Strateji
│  ├─ Veritabanı Şeması
│  ├─ Luca Temizliği
│  ├─ Soft Reset
│  ├─ Header-Line Mimarisi
│  ├─ Benzerlik Algoritması
│  ├─ Dashboard
│  ├─ Backup & Rollback
│  ├─ Execution Plan
│  ├─ Kod Örneği
│  ├─ Öğrenci Rehberi
│  └─ Müşteri Sunuşu
│
├─ HIZLI_BASLANGIC_REHBERI.md
│  ├─ 5 Dakikalık Özet
│  ├─ Yapılacaklar
│  ├─ Kod Şablonları
│  ├─ Kritik Noktalar
│  ├─ Başarı Göstergeleri
│  └─ Sorun Giderme
│
├─ UYGULAMA_OZETI.md
│  ├─ Sorun Tanısı
│  ├─ Çözüm Stratejisi
│  ├─ Teknik Implementasyon
│  ├─ Algoritma
│  ├─ Güvenlik
│  ├─ Mimarisi
│  ├─ Execution Plan
│  ├─ Başarı Kriterleri
│  └─ Öğrenme Çıktıları
│
└─ DOKUMANTASYON_INDEKSI.md (Bu dosya)
   ├─ Dosya Açıklamaları
   ├─ Okuma Rehberi
   ├─ Dosya Haritası
   ├─ Anahtar Kavramlar
   └─ Hızlı Referans
```

---

## 🔑 Anahtar Kavramlar

### Soft Reset

```
Tanım: Veriyi silmeden "gönderilmemiş" olarak işaretleme
Avantaj: Geri dönüş mümkün
Kullanım: IsSyncedToLuca = false
```

### Header-Line Mimarisi

```
Tanım: Sipariş satırlarını ana ürün altında gruplandırma
Avantaj: Luca'ya doğru format ile gönderim
Kullanım: Canonical ürün başlık, varyantlar satır
```

### Levenshtein Distance

```
Tanım: İki metin arasındaki benzerliği ölçme
Formül: 1 - (distance / maxLength)
Eşik: 0.90 (90%)
```

### Duplicate Kategorileri

```
1. Versioning: -V2, -V3, -V4
2. Concatenation: ABCABC, DEFDEF
3. Encoding: ?, ü→?, ş→?
4. Mixed: Birden fazla sorun
```

### Audit Log

```
Tanım: Tüm işlemleri kaydetme
Amaç: Geri dönüş ve denetim
Bilgiler: OperationType, EntityType, Status, ErrorMessage
```

---

## ⚡ Hızlı Referans

### Kritik SQL Komutları

```sql
-- Backup al
BACKUP DATABASE [KatanaIntegration]
TO DISK = 'C:\Backups\PreCleanup.bak'

-- Hatalı kartları bul
SELECT * FROM SalesOrderLines
WHERE IsSyncedToLuca = 0

-- Audit log'u kontrol et
SELECT * FROM DataCleanupAudit
WHERE Status = 'FAILED'

-- Geri dönüş yap
RESTORE DATABASE [KatanaIntegration]
FROM DISK = 'C:\Backups\PreCleanup.bak'
```

### Kritik API Endpoint'leri

```
GET  /api/admin/cleanup/preview
     → Dashboard göster

POST /api/admin/cleanup/execute
     → Temizliği başlat

POST /api/admin/cleanup/rollback
     → Geri dönüş yap
```

### Kritik Servisler

```
DataCleanupService
├─ IdentifyBadStockCardsAsync()
└─ DeleteBadStockCardsAsync()

SoftResetService
├─ ResetSalesOrderSyncAsync()
└─ FindOrdersWithBadProductsAsync()

RollbackService
└─ RollbackCleanupAsync()
```

---

## 📈 Başarı Göstergeleri

```
Başlamadan Önce:
- Hatalı Kartlar: 287
- Etkilenen Siparişler: 45
- Veri Kalitesi: 94.7%

Temizlikten Sonra:
- Hatalı Kartlar: 0
- Etkilenen Siparişler: 0
- Veri Kalitesi: 100%
```

---

## 🎓 Öğrenme Yolu

```
Hafta 1:
├─ Dokümantasyonu oku (2 saat)
├─ Veritabanı migration'larını oluştur (1 saat)
├─ Servisleri implement et (3 saat)
└─ API endpoint'lerini ekle (2 saat)

Hafta 2:
├─ Dashboard'u tasarla (2 saat)
├─ Müşteri sunuşunu hazırla (1 saat)
├─ Test et (2 saat)
└─ Temizliği yap (2 saat)

Hafta 3:
├─ Doğrulama yap (1 saat)
├─ Yeni mantığı test et (2 saat)
└─ Müşteriye rapor sun (1 saat)
```

---

## 💡 İpuçları

### Okuma Sırasında

```
1. Başlık ve özeti oku
2. Kod örneklerini incele
3. Diyagramları anla
4. Notlar al
5. Sorular yaz
```

### Implementasyon Sırasında

```
1. Backup al
2. Migration'ları çalıştır
3. Servisleri test et
4. API endpoint'lerini test et
5. Müşteri sunuşunu yap
6. Temizliği başlat
```

### Sorun Giderme Sırasında

```
1. Audit log'u kontrol et
2. Hata mesajını oku
3. Backup'tan geri dön
4. Rollback service'i çalıştır
5. Müşteriye bilgi ver
```

---

## 📞 Yardım Gerekirse

1. **Hızlı Cevap İçin**: HIZLI_BASLANGIC_REHBERI.md
2. **Detaylı Bilgi İçin**: VERI_TEMIZLIGI_VE_SOFT_RESET_STRATEJISI.md
3. **Sistem Mimarisi İçin**: GRUPLANDIRMA_MANTIGI_RAPORU.md
4. **Genel Bakış İçin**: UYGULAMA_OZETI.md

---

## ✨ Son Söz

Bu dokümantasyon, Katana-Luca entegrasyonundaki veri kalitesi sorunlarını çözmek için kapsamlı bir rehber sunmaktadır.

**Başarılar!** 🚀

---

**Dokümantasyon Tarihi**: Aralık 2024
**Versiyon**: 1.0
**Durum**: Tamamlandı
