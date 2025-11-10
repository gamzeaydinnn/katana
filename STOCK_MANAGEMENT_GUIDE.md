# ✅ Stok Yönetimi Sistemi - Kullanım Kılavuzu

## 🎯 Genel Bakış

Sistemde **iki farklı stok görünümü** bulunmaktadır:

### 1. 📊 Stok Görünümü (Herkes İçin)

**Yol:** Sidebar → "Stok Görünümü" `/stock-view`

**Özellikler:**

- ✅ Anlık stok durumu görüntüleme
- ✅ Otomatik yenileme (30 saniye)
- ✅ Düşük stok uyarıları
- ✅ Kritik stok bildirimleri
- ✅ Toplam stok değeri
- ✅ Arama ve filtreleme
- ❌ **Düzenleme YOK** (sadece görüntüleme)

**Göstergeler:**

- 🟢 **Normal:** Stok yeterli
- 🟡 **Düşük Stok:** ≤10 adet kaldı
- 🔴 **Stokta Yok:** Tükendi

**Statistikler:**

1. Toplam Ürün Sayısı
2. Aktif Ürün Sayısı
3. Düşük Stok Uyarısı (badge ile)
4. Stokta Olmayan Ürünler (badge ile)
5. Toplam Envanter Değeri

### 2. 🛠️ Stok Yönetimi (Sadece Admin)

**Yol:** Admin Paneli → "Stok Yönetimi" sekmesi `/admin` (Tab 3)

**Özellikler:**

- ✅ Tüm "Stok Görünümü" özellikleri
- ✅ **Satın alma işlemi** 🛒
- ✅ Stok güncelleme
- ✅ Backend entegrasyonu
- ✅ Veritabanı kalıcılığı
- ✅ Audit logging

**Satın Alma Süreci:**

1. Ürünün yanındaki 🛒 butonuna tıkla
2. Satın alınacak miktarı gir
3. Toplam tutarı görüntüle
4. "Satın Al" butonuna tıkla
5. Stok otomatik güncellenir

## 📍 Navigasyon

### Genel Kullanıcılar:

```
Login → Dashboard → Sidebar → Stok Görünümü
```

### Admin Kullanıcılar:

```
Login → Admin Paneli → Stok Yönetimi sekmesi
```

## 🔔 Bildirim Sistemi

### Kritik Uyarı (Kırmızı)

```
KRİTİK UYARI: X ürün stokta yok! Lütfen yöneticiye bildiriniz.
```

- Stok = 0 olan ürünler
- Tabloda kırmızı arka plan
- Badge ile gösterilir

### Düşük Stok Uyarısı (Sarı)

```
DİKKAT: X ürün düşük stokta. Yakında tükenebilir.
```

- Stok ≤ 10 olan ürünler
- Tabloda sarı arka plan
- Badge ile gösterilir

## 🔄 Backend Entegrasyonu

### API Endpoint'leri

#### 1. Ürün Listesi

```http
GET /api/Products
```

**Response:**

```json
[
  {
    "id": 1,
    "sku": "PRD001",
    "name": "Ürün Adı",
    "stock": 15,
    "price": 100.0,
    "isActive": true
  }
]
```

#### 2. İstatistikler

```http
GET /api/Products/statistics
```

**Response:**

```json
{
  "totalProducts": 50,
  "activeProducts": 45,
  "lowStockProducts": 8,
  "outOfStockProducts": 3,
  "totalInventoryValue": 15000.0
}
```

#### 3. Stok Güncelleme (Admin Only)

```http
PATCH /api/Products/{id}/stock
Content-Type: application/json
Authorization: Bearer {token}

15  // Yeni stok miktarı
```

**Authorization:**

- `[Authorize(Roles = "Admin,StockManager")]`
- Sadece Admin ve StockManager rolleri

## 💡 Kullanım Senaryoları

### Senaryo 1: Normal Kullanıcı Stok Kontrolü

1. Kullanıcı sisteme giriş yapar
2. Sidebar'dan "Stok Görünümü"ne tıklar
3. Tüm ürünlerin anlık stok durumunu görür
4. Düşük stoklu ürünleri tespit eder
5. Yöneticiye bildirim yapar

### Senaryo 2: Admin Satın Alma İşlemi

1. Admin sisteme giriş yapar
2. "Admin Paneli" → "Stok Yönetimi" sekmesine gider
3. Düşük stoklu ürünü tespit eder
4. 🛒 Satın Al butonuna tıklar
5. Miktar girer (örn: 50 adet)
6. Toplam tutar hesaplanır
7. "Satın Al" butonuna basar
8. Backend'e istek gider:
   ```
   PATCH /api/Products/123/stock
   Body: 65  (mevcut 15 + yeni 50)
   ```
9. Veritabanı güncellenir ✅
10. Audit log kaydedilir ✅
11. Success mesajı gösterilir
12. Tablo otomatik yenilenir

### Senaryo 3: Otomatik İzleme

1. Kullanıcı "Stok Görünümü" sayfasını açar
2. Sayfa her 30 saniyede bir otomatik yenilenir
3. Yeni düşük stok uyarıları otomatik görünür
4. Anlık takip sağlanır

## 🎨 Görsel Özellikler

### Renkli Kartlar

- 🟣 Mor Gradient: Toplam Ürün
- 🟢 Yeşil Gradient: Aktif Ürün
- 🟠 Turuncu Gradient: Düşük Stok
- 🔴 Kırmızı Gradient: Stokta Yok
- 🔵 Mavi Gradient: Toplam Değer

### Tablo Göstergeleri

- Renkli stok sayıları
- Durum chip'leri
- Icon'lu uyarılar
- Hover efektleri
- Sticky header

## 🔒 Güvenlik

### Roller ve Yetkiler

```
Normal User:
- ✅ Stok görüntüleme
- ❌ Stok düzenleme
- ❌ Satın alma

Admin/StockManager:
- ✅ Stok görüntüleme
- ✅ Stok düzenleme
- ✅ Satın alma
- ✅ Audit logları
```

### Authorization Flow

```
Frontend Request
    ↓
JWT Token Kontrolü
    ↓
Role Validation (Admin/StockManager)
    ↓
Backend Update
    ↓
Database SaveChangesAsync()
    ↓
Audit Log
    ↓
Success Response
```

## 📊 Özet

| Özellik           | Stok Görünümü | Stok Yönetimi (Admin) |
| ----------------- | ------------- | --------------------- |
| Görüntüleme       | ✅            | ✅                    |
| Arama/Filtre      | ✅            | ✅                    |
| İstatistikler     | ✅            | ✅                    |
| Uyarılar          | ✅            | ✅                    |
| Otomatik Yenileme | ✅ (30s)      | ✅                    |
| Düzenleme         | ❌            | ✅                    |
| Satın Alma        | ❌            | ✅                    |
| Veritabanı Yazma  | ❌            | ✅                    |
| Authorization     | Hayır         | Gerekli               |

## 🚀 Sonuç

- ✅ İki ayrı sayfa: Görüntüleme ve Yönetim
- ✅ Role-based access control
- ✅ Backend entegrasyonu TAM
- ✅ Veritabanı kalıcılığı SAĞLANDı
- ✅ Anlık bildirimler AKTIF
- ✅ Otomatik yenileme ÇALIŞIYOR
- ✅ Modern ve profesyonel tasarım

**Herkes stok durumunu görebilir, sadece admin düzenleyebilir!** 🎉
