# 🔧 Katana Sales Order 415 Error Fix - Test Guide

## 📋 Yapılan Değişiklikler

### 1. `CreateKatanaJsonContent` Metodu Güncellendi
**Dosya:** `src/Katana.Infrastructure/APIClients/KatanaService.cs` (satır 43-50)

**ÖNCE:**
```csharp
private static StringContent CreateKatanaJsonContent(string json)
{
    var content = new StringContent(json, Encoding.UTF8);
    content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
    {
        CharSet = null
    };
    return content;
}
```

**SONRA:**
```csharp
private static StringContent CreateKatanaJsonContent(string json)
{
    // Create StringContent without encoding parameter to avoid automatic charset addition
    var content = new StringContent(json);
    // Manually set Content-Type to exactly "application/json" without charset
    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
    return content;
}
```

### 2. Debug Logu Eklendi
**Dosya:** `src/Katana.Infrastructure/APIClients/KatanaService.cs` (CreateSalesOrderAsync metodu)

```csharp
// ✅ DEBUG: Content-Type'ı logla
_logger.LogInformation("🔍 Content-Type being sent: {ContentType}", 
    content.Headers.ContentType?.ToString());
```

---

## 🧪 Test Adımları

### Yöntem 1: Manuel Test (Admin Panel)

1. **Projeyi derle:**
   ```bash
   dotnet build
   ```

2. **Docker'ı yeniden başlat:**
   ```bash
   docker-compose down
   docker-compose up -d --build
   ```

3. **Admin paneline giriş yap:**
   - URL: http://localhost:3000
   - Email: admin@katana.com
   - Password: Admin123!

4. **PENDING sipariş bul:**
   - Sales Orders sayfasına git
   - SO-55, SO-53 gibi PENDING durumundaki bir sipariş seç

5. **Siparişi onayla:**
   - "Onayla" butonuna tıkla

6. **Logları kontrol et:**
   ```bash
   docker logs katana-backend 2>&1 | grep -A 5 "Content-Type being sent"
   ```

---

### Yöntem 2: Otomatik Test (Script)

```bash
./test-sales-order-approval.sh
```

Script otomatik olarak:
- ✅ Login yapar
- ✅ PENDING siparişleri listeler
- ✅ İlk PENDING siparişi onaylar
- ✅ Sonucu gösterir

---

## ✅ Başarı Kriterleri

### Loglarda görmek istediğiniz:

```
🔍 Content-Type being sent: application/json
✅ Sipariş durumu: APPROVED
✅ Katana Order ID: 123456
```

**ÖNEMLİ:** `Content-Type` değerinde `charset=utf-8` **OLMAMALI**

---

## ❌ Hata Durumu

Eğer hala 415 hatası alıyorsanız, loglarda şunu göreceksiniz:

```
🔍 Content-Type being sent: application/json; charset=utf-8
❌ Katana API hatası: 415 (Unsupported Media Type)
```

Bu durumda:
1. Değişikliklerin doğru uygulandığından emin olun
2. Docker container'ı tamamen yeniden build edin
3. Cache'i temizleyin: `docker-compose down -v`

---

## 🔍 Detaylı Log İnceleme

### Backend loglarını canlı izle:
```bash
docker logs -f katana-backend
```

### Sadece Content-Type loglarını filtrele:
```bash
docker logs katana-backend 2>&1 | grep "Content-Type being sent"
```

### Katana API hatalarını filtrele:
```bash
docker logs katana-backend 2>&1 | grep -i "katana api"
```

---

## 🎯 Beklenen Sonuç

- ✅ Sipariş durumu: `PENDING` → `APPROVED`
- ✅ Katana'da sipariş oluşturuldu
- ✅ Stok kartı güncellendi
- ✅ 415 hatası YOK
- ✅ Content-Type: `application/json` (charset YOK)

---

## 📝 Notlar

- Bu fix, .NET'in `StringContent` constructor'ına `Encoding.UTF8` parametresi verildiğinde otomatik olarak `charset=utf-8` eklemesini önler
- Katana API, Content-Type header'ında charset parametresi olmasını kabul etmiyor
- Debug logu test sonrası kaldırılabilir (production'da gereksiz)
