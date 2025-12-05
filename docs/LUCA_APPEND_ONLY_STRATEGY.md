# Katana → Luca Append-Only Senkronizasyon Mimarisi

## 📋 Genel Bakış

Luca'da güncelleme endpoint'i olmadığından, her ürün değişikliğinde yeni stok kartı oluşturulur. Bu mimari, versiyonlu SKU'lar kullanarak değişiklik geçmişini takip eder.

```
KATANA (Ürün değişti)
    ↓
Değişiklik kontrolü (ProductMappingService)
    ↓
Değişiklik VAR → Yeni versiyonlu SKU üret (SKU-V2, SKU-V3...)
    ↓
Luca'ya yeni stok kartı gönder
    ↓
Eski mapping pasif (IsActive=false), yeni mapping aktif (IsActive=true)
```

## 🏗️ Oluşturulan Bileşenler

### 1. Veritabanı Tablosu

**Dosya:** `db/create_product_luca_mappings.sql`

```sql
CREATE TABLE ProductLucaMappings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    KatanaProductId NVARCHAR(100) NOT NULL,
    KatanaSku NVARCHAR(100) NOT NULL,
    LucaStockCode NVARCHAR(100) NOT NULL,  -- Versiyonlu: SKU-V2, SKU-V3...
    LucaStockId BIGINT NULL,
    Version INT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    SyncStatus NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    SyncedProductName NVARCHAR(500) NULL,
    SyncedPrice DECIMAL(18,2) NULL,
    SyncedVatRate INT NULL,
    SyncedBarcode NVARCHAR(100) NULL,
    LastSyncError NVARCHAR(MAX) NULL,
    SyncedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CreatedBy NVARCHAR(100) NULL
);
```

### 2. Domain Entity

**Dosya:** `src/Katana.Core/Entities/ProductLucaMapping.cs`

### 3. Repository Interface

**Dosya:** `src/Katana.Core/Interfaces/IProductMappingRepository.cs`

### 4. Repository Implementation

**Dosya:** `src/Katana.Data/Repositories/ProductMappingRepository.cs`

### 5. Service Interface

**Dosya:** `src/Katana.Business/Interfaces/IProductMappingService.cs`

### 6. Service Implementation

**Dosya:** `src/Katana.Business/Services/ProductMappingService.cs`

## 🔧 Kullanım

### Ana Metod: HandleProductUpdateAsync

```csharp
// Dependency Injection ile servis alın
private readonly IProductMappingService _productMappingService;

// Ürün güncellemesi kontrolü
var product = new KatanaProductDto { ... };
var result = await _productMappingService.HandleProductUpdateAsync(product);

if (result.ShouldSendToLuca)
{
    // Yeni veya değişen ürün - Luca'ya gönder
    var lucaResult = await _lucaService.CreateStockCardAsync(
        MapToLucaDto(product, result.LucaStockCode)  // VERSİYONLU SKU!
    );

    if (lucaResult.Success)
    {
        await _productMappingService.MarkAsSyncedAsync(result.MappingId, lucaResult.StockId);
    }
    else
    {
        await _productMappingService.MarkAsSyncFailedAsync(result.MappingId, lucaResult.ErrorMessage);
    }
}
else
{
    // Ürün değişmemiş, Luca'ya gönderilmedi
    Console.WriteLine($"Ürün {product.SKU} atlandı: {result.Message}");
}
```

### ProductUpdateResult Yapısı

| Property         | Tip    | Açıklama                      |
| ---------------- | ------ | ----------------------------- |
| Success          | bool   | İşlem başarılı mı?            |
| IsNewVersion     | bool   | Yeni versiyon oluşturuldu mu? |
| LucaStockCode    | string | Versiyonlu SKU (örn: SKU-V2)  |
| Version          | int    | Versiyon numarası             |
| MappingId        | int    | Veritabanı ID                 |
| ShouldSendToLuca | bool   | Luca'ya gönderilmeli mi?      |
| Message          | string | Durum mesajı                  |

## 📊 Değişiklik Kontrolü

Service aşağıdaki alanları karşılaştırır:

- **İsim** (SyncedProductName)
- **Fiyat** (SyncedPrice)
- **KDV Oranı** (SyncedVatRate)
- **Barkod** (SyncedBarcode)

Herhangi biri değiştiyse yeni versiyon oluşturulur.

## 🚀 Veritabanına Uygulama

```powershell
# SQL Server'da çalıştır
sqlcmd -S sunucu -d veritabani -i db/create_product_luca_mappings.sql
```

## 📝 Akış Özeti

| Durum                | Sonuç                                     |
| -------------------- | ----------------------------------------- |
| İlk kez eklenen ürün | V1 oluştur, Luca'ya gönder                |
| Değişiklik yok       | Skip, Luca'ya gönderme                    |
| Değişiklik var       | Yeni versiyon (V2, V3...), Luca'ya gönder |
| Sync başarılı        | SyncStatus = SYNCED                       |
| Sync başarısız       | SyncStatus = FAILED, hata kaydet          |

## ⚠️ Önemli Notlar

1. Her Katana ürünü için **sadece 1 aktif mapping** olur (IsActive=1)
2. Eski versiyonlar **silinmez**, sadece pasif yapılır (IsActive=0)
3. LucaStockCode **benzersiz** olmalı (UNIQUE constraint)
4. Değişiklik olmayan ürünler **Luca'ya gönderilmez** (performans)

## 🔗 İlgili Dosyalar

- `src/Katana.Business/UseCases/Sync/SyncService.cs` - Ana senkronizasyon servisi
- `src/Katana.Infrastructure/Mappers/KatanaToLucaMapper.cs` - DTO dönüşümleri
- `src/Katana.API/Program.cs` - DI kayıtları
