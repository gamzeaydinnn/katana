# Uygulama Özeti - Veri Temizliği ve Soft Reset

## 📌 Genel Bakış

Bu dokümantasyon, Katana-Luca entegrasyonundaki veri kalitesi sorunlarını çözmek için **4 aşamalı bir strateji** sunmaktadır.

---

## 🎯 Sorun Tanısı

### Mevcut Durum

```
Luca'da:
├─ 287 hatalı stok kartı
│  ├─ 156 encoding hatası (?, ü→?, ş→?)
│  ├─ 89 versioning hatası (-V2, -V3, -V4)
│  └─ 42 concatenation hatası (ABCABC, DEFDEF)
└─ Veri kalitesi: 94.7%

Katana'da:
├─ 45 sipariş bu kartlara bağlı
├─ 234 sipariş satırı etkilendi
└─ Yeni mantık test edilemiyor
```

### Neden Sorun?

```
1. Eski veriler sistemde dolaşıyor
2. Yeni mantık eski hatalı verilerle test edilemiyor
3. Müşteri "eksik var" diyor
4. Güven sorunu oluşuyor
```

---

## ✅ Çözüm Stratejisi

### 4 Aşama

```
AŞAMA 1: Veritabanı Şeması Güncelleme
├─ SalesOrderLines: IsSyncedToLuca, LukaErrorLog
├─ Products: IsMarkedForCleanup, CleanupReason
└─ DataCleanupAudit: Tüm işlemleri kaydet

AŞAMA 2: Luca Tarafında Temizlik
├─ Hatalı kartları tespit et
├─ Luca API'sini çağır
└─ Kartları sil

AŞAMA 3: Katana Tarafında Reset
├─ Siparişleri "gönderilmemiş" yap
├─ Ürünleri inactive yap
└─ Audit log'a kaydet

AŞAMA 4: Doğrulama ve Test
├─ Luca'da kontrol et
├─ Katana'da kontrol et
└─ Yeni mantığı test et
```

---

## 🔧 Teknik Implementasyon

### Yeni Servisler

```
DataCleanupService
├─ IdentifyBadStockCardsAsync()
├─ DeleteBadStockCardsAsync()
└─ MarkProductsForCleanupAsync()

SoftResetService
├─ ResetSalesOrderSyncAsync()
└─ FindOrdersWithBadProductsAsync()

LucaSyncTransformService
├─ TransformOrderToLucaFormatAsync()
└─ GetBOMComponentsForLucaAsync()

SmartDuplicateDetector
├─ MakeAutomaticDecisionAsync()
└─ CalculateSimilarity() [Levenshtein Distance]

RollbackService
└─ RollbackCleanupAsync()
```

### Yeni API Endpoint'leri

```
GET  /api/admin/cleanup/preview
     → Dashboard göster (hiçbir şey silme)

POST /api/admin/cleanup/execute
     → Temizliği başlat (Admin onayı gerekli)

POST /api/admin/cleanup/rollback
     → Geri dönüş yap (Acil durum)
```

---

## 📊 Levenshtein Distance Algoritması

### Matematiksel Formül

```
lev(a, b) = |a|                           if |b| = 0
          = |b|                           if |a| = 0
          = lev(tail(a), tail(b))         if a[0] = b[0]
          = 1 + min(lev(tail(a), b),
                     lev(a, tail(b)),
                     lev(tail(a), tail(b))) otherwise
```

### Örnek

```
ÜRÜN-KIRMIZI vs ÜR?N-KIRMIZI
Distance: 1 (bir karakter fark)
Similarity: 1 - (1/15) = 0.933 (93.3%)

Eşik: 0.90 (90%)
Sonuç: Otomatik olarak "Encoding Issue" kategorisine sok
```

### Implementasyon

```csharp
private int LevenshteinDistance(string s1, string s2)
{
    var n = s1.Length;
    var m = s2.Length;
    var d = new int[n + 1, m + 1];

    for (var i = 0; i <= n; i++) d[i, 0] = i;
    for (var j = 0; j <= m; j++) d[0, j] = j;

    for (var i = 1; i <= n; i++)
    {
        for (var j = 1; j <= m; j++)
        {
            var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
            d[i, j] = Math.Min(
                Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                d[i - 1, j - 1] + cost);
        }
    }

    return d[n, m];
}
```

---

## 🛡️ Güvenlik Mekanizmaları

### 1. Backup

```sql
BACKUP DATABASE [KatanaIntegration]
TO DISK = 'C:\Backups\PreCleanup.bak'
```

### 2. Soft Reset (Hard Delete Değil)

```csharp
// ❌ YAPMA
DELETE FROM Products WHERE SKU LIKE '%?%';

// ✅ YAP
product.IsActive = false;
product.IsMarkedForCleanup = true;
```

### 3. Audit Log

```sql
INSERT INTO DataCleanupAudit (
    OperationType, EntityType, EntityId,
    Reason, Status, ErrorMessage
) VALUES (...)
```

### 4. Geri Dönüş Mekanizması

```csharp
await _rollbackService.RollbackCleanupAsync(startTime);
```

---

## 📈 Header-Line Mimarisi

### Eski Yapı (Yanlış)

```
Sipariş: SO-001
├─ Satır 1: TSHIRT-RED-M × 10
├─ Satır 2: TSHIRT-RED-L × 5
└─ Satır 3: TSHIRT-BLUE-M × 8

Luca'ya gönder:
├─ TSHIRT-RED-M (Hatalı: varyant olarak gönderiliyor)
├─ TSHIRT-RED-L
└─ TSHIRT-BLUE-M
```

### Yeni Yapı (Doğru)

```
Sipariş: SO-001
Header: TSHIRT (Canonical)
├─ Toplam Miktar: 23
├─ BOM Bileşenleri:
│  ├─ PAMUK: 34.5 kg
│  ├─ DÜĞME: 115 adet
│  └─ İPLİK: 2,300 m
└─ Satırlar:
   ├─ TSHIRT-RED-M × 10
   ├─ TSHIRT-RED-L × 5
   └─ TSHIRT-BLUE-M × 8

Luca'ya gönder:
└─ TSHIRT (Ana ürün olarak gönderiliyor)
```

---

## 🎬 Execution Plan

### Gün 1: Hazırlık (2 saat)

```
1. Backup al
2. Migration'ları oluştur
3. Servisleri implement et
4. API endpoint'lerini ekle
```

### Gün 2: Analiz (1 saat)

```
1. Dashboard'u aç
2. İstatistikleri incele
3. Müşteriye rapor sun
4. Onay al
```

### Gün 3: Temizlik (2 saat)

```
1. Luca'da kartları sil
2. Siparişleri reset et
3. Ürünleri inactive yap
4. Audit log'u kontrol et
```

### Gün 4: Doğrulama (1 saat)

```
1. Luca'da kontrol et
2. Katana'da kontrol et
3. Yeni mantığı test et
4. Başarı kriterleri kontrol et
```

---

## 📋 Başarı Kriterleri

```
✓ Hatalı kartlar Luca'dan silindi
✓ Siparişler "gönderilmemiş" olarak işaretlendi
✓ Ürünler inactive yapıldı
✓ Yeni mantık temiz verilerle çalışıyor
✓ Veri kalitesi skoru 100% oldu
✓ Müşteri memnun
✓ Sistem stabil
```

---

## 🚀 Sonraki Adımlar

### Kısa Vadede (Bu Hafta)

```
1. Veritabanı migration'larını oluştur
2. DataCleanupService'i implement et
3. API endpoint'lerini ekle
4. Test et
```

### Orta Vadede (Sonraki Hafta)

```
1. LucaSyncTransformService'i implement et
2. SmartDuplicateDetector'ı implement et
3. RollbackService'i implement et
4. Müşteriye sunumu hazırla
```

### Uzun Vadede (Sonraki Ay)

```
1. Monitoring sistemi kur
2. Otomatik temizlik scripti oluştur
3. Veri kalitesi dashboard'u geliştir
4. Müşteri eğitimi yap
```

---

## 📚 Referans Dosyalar

```
VERI_TEMIZLIGI_VE_SOFT_RESET_STRATEJISI.md
├─ BÖLÜM 1-5: Temel strateji ve veritabanı
├─ BÖLÜM 6-7: Header-Line mimarisi ve algoritma
├─ BÖLÜM 8-9: Dashboard ve backup
├─ BÖLÜM 10-11: Execution plan ve kod örneği
├─ BÖLÜM 12-13: Öğrenci rehberi ve müşteri sunuşu
└─ BÖLÜM 14: Özet ve kontrol listesi

HIZLI_BASLANGIC_REHBERI.md
├─ 5 dakikalık özet
├─ Yapılacaklar (sırasıyla)
├─ Kod şablonları
├─ Kritik noktalar
├─ Başarı göstergeleri
└─ Sorun giderme

GRUPLANDIRMA_MANTIGI_RAPORU.md
├─ Varyant gruplandırması
├─ SKU yönetimi
├─ Duplicate tespiti
├─ BOM gruplandırması
└─ Sistem entegrasyonu
```

---

## 💡 Önemli Notlar

### Felsefe

```
"Veri temizliği, ERP projelerinde Go-Live öncesi standarttır.
Korkma, bu normal bir süreçtir."
```

### Temel İlkeler

```
1. Backup almadan hiçbir şey silme
2. Soft reset ile başla (hard delete değil)
3. Admin onayını her zaman al
4. Audit log'u tut
5. Geri dönüş mekanizması hazırla
```

### Başarı Sırrı

```
1. Adım adım ilerlemek
2. Her aşamada doğrulama yapmak
3. Müşteri ile iletişim kurmak
4. Şeffaflık sağlamak
5. Sabırlı olmak
```

---

## 🎓 Öğrenme Çıktıları

Bu proje tamamlandığında şunları öğrenmiş olacaksın:

```
✓ Veritabanı migration'ları nasıl oluşturulur
✓ Soft reset mekanizması nasıl çalışır
✓ Levenshtein Distance algoritması nasıl uygulanır
✓ Header-Line mimarisi nasıl tasarlanır
✓ Audit log sistemi nasıl kurulur
✓ Geri dönüş mekanizması nasıl oluşturulur
✓ Admin dashboard'u nasıl tasarlanır
✓ Müşteri sunuşu nasıl yapılır
✓ ERP projelerinde veri temizliği nasıl yönetilir
```

---

## 📞 İletişim

Sorularınız varsa:

1. Audit log'u kontrol edin
2. Backup'tan geri dönün
3. Rollback service'i çalıştırın
4. Müşteriye bilgi verin

**Başarılar!** 🚀
