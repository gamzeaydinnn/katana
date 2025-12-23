# Katana Varyant Temizleme ve Sipariş Konsolidasyon Raporu

## Özet

Katana sisteminde oluşan varyant karmaşası ve sipariş karışıklığını çözmek için kapsamlı bir temizleme ve konsolidasyon stratejisi.

**Ana Sorun:**

- 1 sipariş geldi, içinde 4-5 ürün var
- Şu an: Her ürün ayrı varyant olarak oluşturuluyor (YANLIŞ)
- Doğru: 1 sipariş = 1 KatanaOrderId, ama sipariş içinde N satır (N ürün) olabilir
- Mevcut ürünler korunmalı, sipariş ürünleri silinmeli
- Yeniden onay akışında: 1 sipariş → 1 KatanaOrderId → N satır (N ürün)

---

## 1. Mevcut Durum Analizi

### 1.1 Yanlış Varyant Oluşturma (Şu Anki Sorun)

**Sorun:** Sipariş satırlarından her biri ayrı varyant oluşturuluyor

```
Sipariş SO-001 (1 sipariş):
  Satır 1: T-Shirt Kırmızı M (Qty: 2)
  Satır 2: T-Shirt Mavi L (Qty: 3)
  Satır 3: Pantolon Siyah 32 (Qty: 1)

ŞU AN (YANLIŞ):
  - Varyant 1: T-Shirt Kırmızı M → KatanaOrderId: KO-001
  - Varyant 2: T-Shirt Mavi L → KatanaOrderId: KO-002
  - Varyant 3: Pantolon Siyah 32 → KatanaOrderId: KO-003

  Sonuç: 1 sipariş → 3 ayrı KatanaOrderId (YANLIŞ!)
```

**Sonuç:**

- Aynı sipariş 3 farklı Katana siparişine bölünüyor
- Siparişler birbirine karışıyor
- Luca'ya 3 ayrı fatura gidiyor (1 fatura olması lazım)

### 1.2 Doğru Yapı (Hedef)

```
Sipariş SO-001 (1 sipariş):
  Satır 1: T-Shirt Kırmızı M (Qty: 2)
  Satır 2: T-Shirt Mavi L (Qty: 3)
  Satır 3: Pantolon Siyah 32 (Qty: 1)

DOĞRU (HEDEF):
  KatanaOrderId: KO-12345 (1 sipariş = 1 KatanaOrderId)
    - Satır 1: T-Shirt Kırmızı M (Qty: 2)
    - Satır 2: T-Shirt Mavi L (Qty: 3)
    - Satır 3: Pantolon Siyah 32 (Qty: 1)

  Luca'ya: 1 Fatura, 3 Satır
```

### 1.3 Veri Tabanı Yapısı

**Mevcut (Yanlış):**

```
SalesOrders (SO-001)
  ├─ SalesOrderLines
  │   ├─ Line 1: ProductId=101 (T-Shirt Kırmızı M) → KatanaOrderId=KO-001
  │   ├─ Line 2: ProductId=102 (T-Shirt Mavi L) → KatanaOrderId=KO-002
  │   └─ Line 3: ProductId=103 (Pantolon Siyah 32) → KatanaOrderId=KO-003

Products (Yanlış Varyantlar)
  ├─ Product 101: T-Shirt Kırmızı M (IsVariant=true, ParentId=null)
  ├─ Product 102: T-Shirt Mavi L (IsVariant=true, ParentId=null)
  └─ Product 103: Pantolon Siyah 32 (IsVariant=true, ParentId=null)
```

**Doğru (Hedef):**

```
SalesOrders (SO-001)
  └─ SalesOrderLines
      ├─ Line 1: ProductId=1 (T-Shirt Kırmızı M) → KatanaOrderId=KO-12345
      ├─ Line 2: ProductId=2 (T-Shirt Mavi L) → KatanaOrderId=KO-12345
      └─ Line 3: ProductId=3 (Pantolon Siyah 32) → KatanaOrderId=KO-12345

Products (Doğru Varyantlar)
  ├─ Product 1: T-Shirt (ParentId=null, IsVariant=false)
  │   └─ Variant 1a: T-Shirt Kırmızı M (ParentId=1, IsVariant=true)
  │   └─ Variant 1b: T-Shirt Mavi L (ParentId=1, IsVariant=true)
  └─ Product 3: Pantolon (ParentId=null, IsVariant=false)
      └─ Variant 3a: Pantolon Siyah 32 (ParentId=3, IsVariant=true)
```

---

## 2. Temizleme Stratejisi

### 2.1 Faz 1: Veri Analizi ve Raporlama

**Adım 1: Yanlış Varyantları Tespit Et**

```sql
-- Sipariş satırlarından oluşturulan yanlış varyantları bul
SELECT
  so.Id as OrderId,
  so.OrderNumber,
  COUNT(sol.Id) as LineCount,
  STRING_AGG(p.Name, ', ') as ProductNames,
  STRING_AGG(CAST(p.Id AS VARCHAR), ', ') as ProductIds,
  so.Status
FROM SalesOrders so
JOIN SalesOrderLines sol ON so.Id = sol.SalesOrderId
JOIN Products p ON sol.ProductId = p.Id
WHERE p.IsVariant = 1 AND p.ParentProductId IS NULL
GROUP BY so.Id, so.OrderNumber, so.Status
HAVING COUNT(sol.Id) > 1
ORDER BY so.Id DESC
```

**Adım 2: Mevcut Ürünleri Tespit Et (Korunacak)**

```sql
-- Luca stok kartı olan ürünleri bul (mevcut ürünler)
SELECT
  p.Id,
  p.Name,
  p.SKU,
  p.LucaStockCardId,
  COUNT(sol.Id) as OrderLineCount
FROM Products p
LEFT JOIN SalesOrderLines sol ON p.Id = sol.ProductId
WHERE p.LucaStockCardId IS NOT NULL
GROUP BY p.Id, p.Name, p.SKU, p.LucaStockCardId
ORDER BY OrderLineCount DESC
```

**Adım 3: Siparişlerdeki Ürünleri Listele**

```sql
-- Her sipariş için satırları göster
SELECT
  so.OrderNumber,
  sol.Id as LineId,
  p.Name,
  p.SKU,
  sol.Quantity,
  sol.KatanaOrderId,
  p.LucaStockCardId,
  CASE
    WHEN p.LucaStockCardId IS NOT NULL THEN 'Mevcut Ürün (Koru)'
    ELSE 'Sipariş Ürünü (Sil)'
  END as Action
FROM SalesOrders so
JOIN SalesOrderLines sol ON so.Id = sol.SalesOrderId
JOIN Products p ON sol.ProductId = p.Id
WHERE so.Status IN ('Pending', 'Approved')
ORDER BY so.OrderNumber, sol.Id
```

### 2.2 Faz 2: Varyant Temizliği

**Adım 1: Sipariş Ürünlerini İşaretle**

```csharp
public class OrderProductCleanupService
{
    public async Task MarkOrderProductsForDeletion()
    {
        // Siparişlerdeki ürünleri bul (LucaStockCardId olmayan)
        var orderProducts = await _context.Products
            .Where(p => p.LucaStockCardId == null &&
                        p.IsVariant == true &&
                        p.ParentProductId == null)
            .ToListAsync();

        foreach (var product in orderProducts)
        {
            product.IsOrderProduct = true;
            product.MarkedForDeletionDate = DateTime.UtcNow;
            product.DeletionReason = "Created from order line, no Luca stock card";
        }

        await _context.SaveChangesAsync();
    }
}
```

**Adım 2: Mevcut Ürünleri Koru**

```csharp
public async Task ProtectExistingProducts()
{
    // Luca stok kartı olan ürünleri koru
    var existingProducts = await _context.Products
        .Where(p => p.LucaStockCardId != null)
        .ToListAsync();

    foreach (var product in existingProducts)
    {
        product.IsProtected = true;
        product.ProtectionReason = "Has Luca stock card";
    }

    await _context.SaveChangesAsync();
}
```

**Adım 3: Sipariş Ürünlerini Sil**

```csharp
public async Task DeleteOrderProducts()
{
    var productsToDelete = await _context.Products
        .Where(p => p.IsOrderProduct && !p.IsProtected)
        .ToListAsync();

    // Luca'dan sil
    foreach (var product in productsToDelete)
    {
        if (product.LucaStockCardId.HasValue)
        {
            await _lucaService.DeleteStockCard(product.LucaStockCardId.Value);
        }
    }

    // Veritabanından sil
    _context.Products.RemoveRange(productsToDelete);
    await _context.SaveChangesAsync();
}
```

### 2.3 Faz 3: Sipariş Durumunu Sıfırla

**Adım 1: Siparişleri Onaylanmamış Yap**

```csharp
public async Task ResetOrdersToUnapproved()
{
    var approvedOrders = await _context.SalesOrders
        .Where(o => o.Status == OrderStatus.Approved)
        .ToListAsync();

    foreach (var order in approvedOrders)
    {
        order.Status = OrderStatus.Pending;
        order.ApprovedDate = null;
        order.ApprovedBy = null;
        order.SyncStatus = SyncStatus.NotSynced;
    }

    await _context.SaveChangesAsync();
}
```

**Adım 2: KatanaOrderId'leri Sıfırla**

```csharp
public async Task ResetKatanaOrderIds()
{
    var orderLines = await _context.SalesOrderLines
        .Where(ol => ol.KatanaOrderId != null)
        .ToListAsync();

    foreach (var line in orderLines)
    {
        line.KatanaOrderId = null;
    }

    await _context.SaveChangesAsync();
}
```

**Adım 3: Senkronizasyon Kayıtlarını Temizle**

```csharp
public async Task ClearSyncRecords()
{
    var syncRecords = await _context.OrderMappings
        .Where(om => om.Order.Status == OrderStatus.Pending)
        .ToListAsync();

    _context.OrderMappings.RemoveRange(syncRecords);
    await _context.SaveChangesAsync();
}
```

---

## 3. Yeniden Onay Akışı (Doğru Yapı)

### 3.1 Sipariş Onay Süreci

```
1. Admin siparişi görüntüler (SO-001)
   ├─ Satır 1: T-Shirt Kırmızı M (Qty: 2)
   ├─ Satır 2: T-Shirt Mavi L (Qty: 3)
   └─ Satır 3: Pantolon Siyah 32 (Qty: 1)

2. Admin "Onayla" butonuna tıklar

3. Sistem: 1 KatanaOrderId oluştur (KO-12345)

4. Tüm satırlara aynı KatanaOrderId ata
   ├─ Satır 1: KatanaOrderId = KO-12345
   ├─ Satır 2: KatanaOrderId = KO-12345
   └─ Satır 3: KatanaOrderId = KO-12345

5. Luca'ya gönder: 1 Fatura, 3 Satır
```

### 3.2 Kod Implementasyonu

```csharp
public class OrderApprovalService
{
    public async Task ApproveOrder(int orderId)
    {
        var order = await _context.SalesOrders
            .Include(o => o.OrderLines)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        // 1 KatanaOrderId oluştur (tüm satırlar için)
        var katanaOrderId = await _katanaService.CreateOrder(
            customerId: order.CustomerId,
            deliveryDate: order.DeliveryDate,
            totalLines: order.OrderLines.Count
        );

        // Tüm satırlara aynı KatanaOrderId ata
        foreach (var line in order.OrderLines)
        {
            line.KatanaOrderId = katanaOrderId;
        }

        order.Status = OrderStatus.Approved;
        order.ApprovedDate = DateTime.UtcNow;
        order.ApprovedBy = _currentUser.Id;

        await _context.SaveChangesAsync();

        // Luca'ya gönder (1 fatura, N satır)
        await _orderInvoiceSyncService.SyncOrderToLuca(orderId);
    }
}
```

---

## 4. Luca Senkronizasyonu (Doğru Yapı)

### 4.1 Fatura Yapısı

```
Fatura (1 per Sipariş):
  FaturaNo: F-2025-001
  Müşteri: ABC Ltd
  Tarih: 2025-01-15
  KatanaOrderId: KO-12345

  Satırlar (N per Sipariş):
    1. T-Shirt Kırmızı M - Qty: 2 - Birim Fiyat: 100 - Toplam: 200
    2. T-Shirt Mavi L - Qty: 3 - Birim Fiyat: 100 - Toplam: 300
    3. Pantolon Siyah 32 - Qty: 1 - Birim Fiyat: 150 - Toplam: 150

  Toplam: 650
```

### 4.2 Kod Implementasyonu

```csharp
public async Task SyncOrderToLuca(int orderId)
{
    var order = await _context.SalesOrders
        .Include(o => o.OrderLines)
        .ThenInclude(l => l.Product)
        .FirstOrDefaultAsync(o => o.Id == orderId);

    // 1 Fatura oluştur (tüm satırlar için)
    var invoiceId = await _lucaService.CreateInvoice(
        customerId: order.CustomerId,
        invoiceDate: DateTime.UtcNow,
        katanaOrderId: order.OrderLines.First().KatanaOrderId
    );

    // Her satır için fatura satırı ekle
    foreach (var line in order.OrderLines)
    {
        var product = line.Product;

        // Stok kartı yoksa oluştur
        if (!product.LucaStockCardId.HasValue)
        {
            var stockCardId = await _lucaService.CreateStockCard(
                name: product.Name,
                sku: product.SKU
            );

            product.LucaStockCardId = stockCardId;
        }

        // Fatura satırı ekle
        await _lucaService.AddInvoiceLine(
            invoiceId: invoiceId,
            stockCardId: product.LucaStockCardId.Value,
            quantity: line.Quantity,
            unitPrice: line.UnitPrice
        );
    }

    order.SyncStatus = SyncStatus.Synced;
    await _context.SaveChangesAsync();
}
```

---

## 5. Uygulama Adımları

### Faz 1: Hazırlık (1 gün)

- [ ] Veritabanı yedekle
- [ ] Mevcut varyantları analiz et
- [ ] Siparişleri kontrol et
- [ ] Rapor oluştur

### Faz 2: Temizleme (1 gün)

- [ ] Sipariş ürünlerini işaretle
- [ ] Mevcut ürünleri koru
- [ ] Sipariş ürünlerini sil
- [ ] Luca'dan sil

### Faz 3: Sıfırlama (1 gün)

- [ ] Siparişleri onaylanmamış yap
- [ ] KatanaOrderId'leri sıfırla
- [ ] Senkronizasyon kayıtlarını temizle

### Faz 4: Test (1 gün)

- [ ] Sipariş onay akışını test et
- [ ] 1 sipariş → 1 KatanaOrderId doğrulaması
- [ ] Luca senkronizasyonunu test et
- [ ] Fatura satırlarını kontrol et

### Faz 5: Canlıya Alma (1 gün)

- [ ] Üretim ortamında çalıştır
- [ ] Monitoring başlat
- [ ] Hata loglarını kontrol et

---

## 6. Kontrol Listeleri

### Temizleme Öncesi

- [ ] Tüm siparişler yedeklendi
- [ ] Luca'daki stok kartları kontrol edildi
- [ ] Mevcut ürünler tanımlandı
- [ ] Sipariş ürünleri tanımlandı

### Temizleme Sonrası

- [ ] Sipariş ürünleri silindi
- [ ] Luca stok kartları güncellendi
- [ ] Siparişler onaylanmamış duruma alındı
- [ ] KatanaOrderId'ler sıfırlandı

### Yeniden Onay Sonrası

- [ ] 1 sipariş = 1 KatanaOrderId
- [ ] Tüm satırlar aynı KatanaOrderId'ye sahip
- [ ] 1 Fatura, N Satır
- [ ] Muhasebe verileri tutarlı

---

## 7. Rollback Planı

Sorun oluşursa:

1. Veritabanı yedeğinden geri yükle
2. Luca'daki işlemleri geri al
3. Siparişleri orijinal durumuna döndür
4. Hata analizi yap

---

## 8. Monitoring

Temizlemeden sonra izlenecek metrikler:

- Sipariş başına KatanaOrderId sayısı (1 olmalı)
- Sipariş başına satır sayısı (N olabilir)
- Luca senkronizasyon başarı oranı
- Fatura satır sayısı doğruluğu
- Stok hareket doğruluğu
