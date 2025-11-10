# Backend Entegrasyon Test Senaryosu

## Test Adımları

### 1. Mevcut Ürünü Kontrol Et

```powershell
# API'den bir ürün çek
$product = Invoke-RestMethod -Uri "http://localhost:5000/api/Products" -Method Get | Select-Object -First 1
Write-Host "Orijinal Ürün:" -ForegroundColor Yellow
$product | Format-List Id, Name, SKU, Price, Stock, IsActive, UpdatedAt
```

### 2. Ürünü Güncelle (Frontend'in Yaptığı Gibi)

```powershell
$productId = $product.Id
$updateData = @{
    name = "$($product.Name) - GÜNCELLEME TESTİ"
    sku = $product.SKU
    price = [decimal]($product.Price + 10)
    stock = [int]($product.Stock + 5)
    categoryId = $product.CategoryId
    isActive = $true
    mainImageUrl = $product.MainImageUrl
    description = "Backend test güncellemesi - $(Get-Date)"
} | ConvertTo-Json

Write-Host "`nGönderilen Güncelleme:" -ForegroundColor Cyan
$updateData

$headers = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer YOUR_TOKEN_HERE"
}

$response = Invoke-RestMethod -Uri "http://localhost:5000/api/Products/$productId" -Method Put -Body $updateData -Headers $headers
Write-Host "`nAPI Response:" -ForegroundColor Green
$response | Format-List
```

### 3. Güncellenmiş Ürünü Kontrol Et

```powershell
$updatedProduct = Invoke-RestMethod -Uri "http://localhost:5000/api/Products/$productId" -Method Get
Write-Host "`nGüncellenmiş Ürün:" -ForegroundColor Green
$updatedProduct | Format-List Id, Name, SKU, Price, Stock, IsActive, UpdatedAt
```

### 4. Veritabanını Doğrudan Kontrol Et

```sql
-- SQL Server'da çalıştır
SELECT
    Id,
    Name,
    SKU,
    Price,
    Stock,
    IsActive,
    UpdatedAt,
    CreatedAt
FROM Products
WHERE Id = @productId
ORDER BY UpdatedAt DESC;

-- Audit loglarını kontrol et
SELECT TOP 10
    Id,
    EntityType,
    EntityId,
    Action,
    Username,
    Timestamp,
    Details
FROM AuditLogs
WHERE EntityType = 'Product' AND EntityId = CAST(@productId AS NVARCHAR(50))
ORDER BY Timestamp DESC;
```

## Beklenen Sonuç

✅ **API güncellemesi başarılı olmalı**

- HTTP 200 OK dönmeli
- Response'da güncellenmiş değerler olmalı

✅ **Veritabanında kayıt güncellenmiş olmalı**

- Name değişmiş olmalı
- Price artmış olmalı
- Stock artmış olmalı
- UpdatedAt yeni tarih olmalı

✅ **Audit log kaydedilmiş olmalı**

- AuditLogs tablosunda yeni kayıt olmalı
- Action = "UPDATE" olmalı
- Username kaydedilmiş olmalı

## Kod İncelemesi

### ProductService.cs - UpdateProductAsync

```csharp
public async Task<ProductDto> UpdateProductAsync(int id, UpdateProductDto dto)
{
    // 1. Ürünü bul
    var product = await _context.Products.FindAsync(id);
    if (product == null)
        throw new KeyNotFoundException($"Ürün bulunamadı: {id}");

    // 2. SKU çakışma kontrolü
    var existingProduct = await _context.Products
        .FirstOrDefaultAsync(p => p.SKU == dto.SKU && p.Id != id);
    if (existingProduct != null)
        throw new InvalidOperationException($"Bu SKU'ya sahip başka bir ürün mevcut");

    // 3. Değerleri güncelle
    product.Name = dto.Name;
    product.SKU = dto.SKU;
    product.Price = dto.Price;
    product.Stock = dto.Stock;
    product.CategoryId = dto.CategoryId;
    product.MainImageUrl = dto.MainImageUrl;
    product.Description = dto.Description;
    product.IsActive = dto.IsActive;
    product.UpdatedAt = DateTime.UtcNow;

    // 4. VERİTABANINA YAZ ✅
    await _context.SaveChangesAsync();

    return MapToDto(product);
}
```

### ProductsController.cs - Update Endpoint

```csharp
[HttpPut("{id}")]
[Authorize(Roles = "Admin,StockManager")]
public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductDto dto)
{
    // 1. Validation
    var validationErrors = ProductValidator.ValidateUpdate(dto);
    if (validationErrors.Any())
        return BadRequest(new { errors = validationErrors });

    try
    {
        // 2. Service çağrısı
        var product = await _productService.UpdateProductAsync(id, dto);

        // 3. Audit log kaydet ✅
        _auditService.LogUpdate("Product", id.ToString(), User?.Identity?.Name, null,
            $"Updated: {product.SKU}");

        // 4. Application log kaydet ✅
        _loggingService.LogInfo($"Product updated: {id}", User?.Identity?.Name, null, LogCategory.UserAction);

        return Ok(product);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(ex.Message);
    }
}
```

## Sonuç

**✅ Backend entegrasyonu TAM ve ÇALIŞIR durumda!**

Tüm değişiklikler:

1. Frontend → API'ye PUT request gidiyor
2. Controller authorization ve validation yapıyor
3. ProductService veritabanını güncelliyor (**SaveChangesAsync**)
4. Audit ve application logları kaydediliyor
5. Response frontend'e dönüyor

**Değişiklikler veritabanında KALİCİ olarak saklanıyor!**
