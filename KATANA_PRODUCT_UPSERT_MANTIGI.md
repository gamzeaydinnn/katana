# Katana Product Upsert Mantığı

## 🎯 Hedef

Siparişten gelen ürünleri akıllıca yönet:

- **Ürün varsa**: Stok miktarını güncelle (tekrar oluşturma)
- **Ürün yoksa**: Yeni ürün ve varyantları oluştur
- **Varyant varsa**: Stok miktarını güncelle
- **Varyant yoksa**: Yeni varyant oluştur

---

## 📊 Mantık Akışı

```
Sipariş Geldi
    ↓
Sipariş Satırlarını Oku (4-5 ürün)
    ↓
Her Ürün İçin:
    ├─ SKU'ya göre Katana'da ara
    │
    ├─ BULUNDU:
    │  ├─ Stok miktarını güncelle
    │  ├─ Varyantları kontrol et
    │  └─ Varyant varsa stok güncelle, yoksa oluştur
    │
    └─ BULUNAMADI:
       ├─ Yeni ürün oluştur
       ├─ Varyantları oluştur
       └─ Stok miktarını ayarla
    ↓
Sipariş Onaylandı
```

---

## 🔧 Upsert Service Implementasyonu

```csharp
// File: src/Katana.Business/Services/ProductUpsertService.cs

public class ProductUpsertService : IProductUpsertService
{
    private readonly IntegrationDbContext _context;
    private readonly ILogger<ProductUpsertService> _logger;

    /// <summary>
    /// Sipariş satırlarından ürünleri upsert et
    /// Var olan ürünleri güncelle, yeni olanları oluştur
    /// </summary>
    public async Task<ProductUpsertResult> UpsertOrderProductsAsync(
        int salesOrderId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Upserting products for order {OrderId}", salesOrderId);

        var result = new ProductUpsertResult
        {
            SalesOrderId = salesOrderId,
            StartedAt = DateTime.UtcNow
        };

        using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            // Adım 1: Sipariş satırlarını getir
            var orderLines = await _context.SalesOrderLines
                .Where(l => l.SalesOrderId == salesOrderId)
                .ToListAsync(ct);

            if (!orderLines.Any())
            {
                result.Success = true;
                result.Message = "No order lines found";
                return result;
            }

            // Adım 2: Her satır için ürünü upsert et
            foreach (var line in orderLines)
            {
                var upsertResult = await UpsertSingleProductAsync(line, ct);

                if (upsertResult.IsCreated)
                    result.CreatedProducts++;
                else if (upsertResult.IsUpdated)
                    result.UpdatedProducts++;

                result.ProcessedLines++;
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            result.Success = true;

            _logger.LogInformation(
                "Upsert complete: {Created} created, {Updated} updated, {Processed} processed",
                result.CreatedProducts, result.UpdatedProducts, result.ProcessedLines);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Failed to upsert products for order {OrderId}", salesOrderId);
        }

        result.CompletedAt = DateTime.UtcNow;
        return result;
    }

    /// <summary>
    /// Tek bir ürünü upsert et
    /// </summary>
    private async Task<SingleProductUpsertResult> UpsertSingleProductAsync(
        SalesOrderLine orderLine,
        CancellationToken ct)
    {
        var result = new SingleProductUpsertResult
        {
            SKU = orderLine.SKU,
            ProductName = orderLine.ProductName
        };

        // Adım 1: SKU'ya göre ürünü ara
        var existingProduct = await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.SKU == orderLine.SKU && p.IsActive, ct);

        if (existingProduct != null)
        {
            // GÜNCELLE: Ürün var
            result.IsUpdated = true;
            result.ProductId = existingProduct.Id;

            // Stok miktarını güncelle
            existingProduct.Stock = orderLine.Quantity;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Updated existing product {SKU} (ID: {Id}), stock: {Stock}",
                existingProduct.SKU, existingProduct.Id, existingProduct.Stock);

            // Varyantları kontrol et
            await UpsertVariantsAsync(existingProduct, orderLine, ct);
        }
        else
        {
            // OLUŞTUR: Ürün yok
            result.IsCreated = true;

            var newProduct = new Product
            {
                SKU = orderLine.SKU,
                Name = orderLine.ProductName ?? orderLine.SKU,
                Stock = orderLine.Quantity,
                Price = orderLine.UnitPrice ?? 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync(ct);

            result.ProductId = newProduct.Id;

            _logger.LogInformation(
                "Created new product {SKU} (ID: {Id}), stock: {Stock}",
                newProduct.SKU, newProduct.Id, newProduct.Stock);

            // Varyantları oluştur
            await UpsertVariantsAsync(newProduct, orderLine, ct);
        }

        return result;
    }

    /// <summary>
    /// Ürünün varyantlarını upsert et
    /// </summary>
    private async Task UpsertVariantsAsync(
        Product product,
        SalesOrderLine orderLine,
        CancellationToken ct)
    {
        // Eğer sipariş satırında varyant bilgisi varsa
        if (string.IsNullOrWhiteSpace(orderLine.VariantCode))
            return;

        // Adım 1: Varyantı ara
        var existingVariant = await _context.ProductVariants
            .FirstOrDefaultAsync(
                v => v.ProductId == product.Id &&
                     v.SKU == orderLine.VariantCode &&
                     v.IsActive,
                ct);

        if (existingVariant != null)
        {
            // GÜNCELLE: Varyant var
            existingVariant.Stock = orderLine.Quantity;
            existingVariant.Price = orderLine.UnitPrice ?? 0;
            existingVariant.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation(
                "Updated existing variant {SKU} for product {ProductId}",
                existingVariant.SKU, product.Id);
        }
        else
        {
            // OLUŞTUR: Varyant yok
            var newVariant = new ProductVariant
            {
                ProductId = product.Id,
                SKU = orderLine.VariantCode,
                Name = orderLine.VariantName ?? orderLine.VariantCode,
                Stock = orderLine.Quantity,
                Price = orderLine.UnitPrice ?? 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ProductVariants.Add(newVariant);

            _logger.LogInformation(
                "Created new variant {SKU} for product {ProductId}",
                newVariant.SKU, product.Id);
        }

        await _context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Toplu upsert işlemi (birden fazla sipariş)
    /// </summary>
    public async Task<BulkProductUpsertResult> UpsertMultipleOrdersAsync(
        List<int> orderIds,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Bulk upserting products for {Count} orders", orderIds.Count);

        var result = new BulkProductUpsertResult
        {
            TotalOrders = orderIds.Count,
            StartedAt = DateTime.UtcNow
        };

        foreach (var orderId in orderIds)
        {
            var upsertResult = await UpsertOrderProductsAsync(orderId, ct);

            if (upsertResult.Success)
            {
                result.SuccessfulOrders++;
                result.TotalCreatedProducts += upsertResult.CreatedProducts;
                result.TotalUpdatedProducts += upsertResult.UpdatedProducts;
            }
            else
            {
                result.FailedOrders++;
                result.Errors.Add($"Order {orderId}: {upsertResult.ErrorMessage}");
            }
        }

        result.CompletedAt = DateTime.UtcNow;
        result.Success = result.FailedOrders == 0;

        _logger.LogInformation(
            "Bulk upsert complete: {Success}/{Total} successful, {Created} created, {Updated} updated",
            result.SuccessfulOrders, result.TotalOrders,
            result.TotalCreatedProducts, result.TotalUpdatedProducts);

        return result;
    }

    /// <summary>
    /// Ürün var mı kontrol et (SKU'ya göre)
    /// </summary>
    public async Task<bool> ProductExistsAsync(string sku, CancellationToken ct = default)
    {
        return await _context.Products
            .AnyAsync(p => p.SKU == sku && p.IsActive, ct);
    }

    /// <summary>
    /// Ürünü SKU'ya göre getir
    /// </summary>
    public async Task<Product> GetProductBySkuAsync(string sku, CancellationToken ct = default)
    {
        return await _context.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.SKU == sku && p.IsActive, ct);
    }
}
```

---

## 📋 DTO Modelleri

```csharp
// File: src/Katana.Core/DTOs/ProductUpsertDtos.cs

public class ProductUpsertResult
{
    public int SalesOrderId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; }
    public string ErrorMessage { get; set; }

    public int ProcessedLines { get; set; }
    public int CreatedProducts { get; set; }
    public int UpdatedProducts { get; set; }

    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class SingleProductUpsertResult
{
    public string SKU { get; set; }
    public string ProductName { get; set; }
    public long ProductId { get; set; }

    public bool IsCreated { get; set; }
    public bool IsUpdated { get; set; }
}

public class BulkProductUpsertResult
{
    public bool Success { get; set; }
    public int TotalOrders { get; set; }
    public int SuccessfulOrders { get; set; }
    public int FailedOrders { get; set; }

    public int TotalCreatedProducts { get; set; }
    public int TotalUpdatedProducts { get; set; }

    public List<string> Errors { get; set; } = new();

    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}
```

---

## 🔌 API Endpoint

```csharp
// File: src/Katana.API/Controllers/ProductUpsertController.cs

[ApiController]
[Route("api/products/upsert")]
[Authorize(Roles = "Admin")]
public class ProductUpsertController : ControllerBase
{
    private readonly IProductUpsertService _upsertService;

    /// <summary>
    /// Sipariş ürünlerini upsert et
    /// </summary>
    [HttpPost("order/{orderId}")]
    public async Task<ActionResult<ProductUpsertResult>> UpsertOrderProducts(int orderId)
    {
        var result = await _upsertService.UpsertOrderProductsAsync(orderId);
        return Ok(result);
    }

    /// <summary>
    /// Birden fazla siparişin ürünlerini upsert et
    /// </summary>
    [HttpPost("orders")]
    public async Task<ActionResult<BulkProductUpsertResult>> UpsertMultipleOrders(
        [FromBody] List<int> orderIds)
    {
        var result = await _upsertService.UpsertMultipleOrdersAsync(orderIds);
        return Ok(result);
    }

    /// <summary>
    /// Ürün var mı kontrol et
    /// </summary>
    [HttpGet("exists/{sku}")]
    public async Task<ActionResult<bool>> ProductExists(string sku)
    {
        var exists = await _upsertService.ProductExistsAsync(sku);
        return Ok(exists);
    }

    /// <summary>
    /// Ürünü SKU'ya göre getir
    /// </summary>
    [HttpGet("by-sku/{sku}")]
    public async Task<ActionResult<Product>> GetProductBySku(string sku)
    {
        var product = await _upsertService.GetProductBySkuAsync(sku);
        if (product == null)
            return NotFound();
        return Ok(product);
    }
}
```

---

## 📊 Örnek Akış

### Senaryo 1: Ürün Var, Varyant Yok

```
Sipariş Satırı:
├─ SKU: TSHIRT-RED-M
├─ Quantity: 50
└─ Price: 100

Katana'da:
├─ TSHIRT-RED-M (ID: 1001, Stock: 30)
└─ Varyantlar: Yok

İşlem:
1. TSHIRT-RED-M bulundu
2. Stock: 30 → 50 (güncelle)
3. Varyant yok → Yeni varyant oluştur

Sonuç:
├─ Ürün güncellendi (Stock: 50)
└─ Varyant oluşturuldu
```

### Senaryo 2: Ürün Yok

```
Sipariş Satırı:
├─ SKU: SHIRT-BLUE-L
├─ Quantity: 25
└─ Price: 80

Katana'da:
└─ SHIRT-BLUE-L: Yok

İşlem:
1. SHIRT-BLUE-L bulunamadı
2. Yeni ürün oluştur
3. Varyant oluştur

Sonuç:
├─ Ürün oluşturuldu (ID: 2001, Stock: 25)
└─ Varyant oluşturuldu
```

### Senaryo 3: Ürün Var, Varyant Var

```
Sipariş Satırı:
├─ SKU: TSHIRT-RED-M
├─ Quantity: 75
└─ Price: 100

Katana'da:
├─ TSHIRT-RED-M (ID: 1001, Stock: 50)
└─ Varyant: TSHIRT-RED-M (Stock: 50)

İşlem:
1. TSHIRT-RED-M bulundu
2. Stock: 50 → 75 (güncelle)
3. Varyant bulundu
4. Varyant Stock: 50 → 75 (güncelle)

Sonuç:
├─ Ürün güncellendi (Stock: 75)
└─ Varyant güncellendi (Stock: 75)
```

---

## 🔄 Sipariş Onaylandığında Upsert Çalışması

```csharp
// File: src/Katana.API/Controllers/SalesOrdersController.cs

[HttpPost("{id}/approve")]
public async Task<ActionResult> ApproveSalesOrder(int id)
{
    var order = await _context.SalesOrders
        .Include(o => o.Lines)
        .FirstOrDefaultAsync(o => o.Id == id);

    if (order == null)
        return NotFound();

    // Adım 1: Ürünleri upsert et
    var upsertResult = await _upsertService.UpsertOrderProductsAsync(id);

    if (!upsertResult.Success)
        return BadRequest(new { error = upsertResult.ErrorMessage });

    // Adım 2: Siparişi onayla
    order.Status = "APPROVED";
    order.ApprovedAt = DateTime.UtcNow;
    order.ApprovedBy = User.Identity.Name;

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "Order approved successfully",
        upsertResult = new
        {
            created = upsertResult.CreatedProducts,
            updated = upsertResult.UpdatedProducts,
            processed = upsertResult.ProcessedLines
        }
    });
}
```

---

## 📊 Stok Güncelleme Mantığı

```
Sipariş Onaylandığında:
    ↓
Her Satır İçin:
    ├─ Ürün var mı? (SKU'ya göre)
    │
    ├─ EVET:
    │  ├─ Stok = Sipariş Miktarı (güncelle)
    │  ├─ Varyant var mı?
    │  │  ├─ EVET: Varyant Stok = Sipariş Miktarı
    │  │  └─ HAYIR: Yeni Varyant Oluştur
    │  └─ Güncelleme Kaydı Tut
    │
    └─ HAYIR:
       ├─ Yeni Ürün Oluştur
       ├─ Stok = Sipariş Miktarı
       ├─ Varyant Oluştur
       └─ Oluşturma Kaydı Tut
    ↓
Sonuç Raporu Döndür
```

---

## ✅ Avantajları

```
1. Tekrar Eden Ürün Oluşturma Yok
   └─ SKU'ya göre kontrol et, varsa güncelle

2. Stok Miktarı Otomatik Güncelleme
   └─ Her sipariş onaylandığında stok güncellenir

3. Varyant Yönetimi
   └─ Varyant varsa güncelle, yoksa oluştur

4. Toplu İşlem Desteği
   └─ Birden fazla siparişi aynı anda işle

5. Hata Yönetimi
   └─ Transaction ile atomik işlem

6. Audit Trail
   └─ Oluşturma/Güncelleme kaydı tut
```

---

## 🚀 Hızlı Başlangıç

```bash
# 1. Service'i implement et
# ProductUpsertService.cs

# 2. API endpoint'lerini ekle
# ProductUpsertController.cs

# 3. Sipariş onaylandığında upsert çalıştır
# SalesOrdersController.cs → ApproveSalesOrder()

# 4. Test et
POST /api/products/upsert/order/123

# 5. Sonuç
{
  "success": true,
  "processedLines": 5,
  "createdProducts": 2,
  "updatedProducts": 3
}
```

---

## 💡 Önemli Notlar

```
1. SKU Benzersiz Olmalı
   └─ Aynı SKU'ya sahip ürün tekrar oluşturulmaz

2. Stok Miktarı Sipariş Miktarı Olur
   └─ Her sipariş onaylandığında güncellenir

3. Varyantlar Otomatik Yönetilir
   └─ Varyant varsa güncelle, yoksa oluştur

4. Transaction Kullan
   └─ Tüm işlem başarılı veya hiçbiri

5. Logging Yap
   └─ Oluşturma/Güncelleme işlemlerini kaydet
```

**Başarılar!** 🚀
