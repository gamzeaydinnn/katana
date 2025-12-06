# Stock Card Creation Fix - Complete Summary

## 🎯 PROBLEM

Luca API was returning `{"error":true}` for all stock card creation requests without providing error messages.

## 🔍 ROOT CAUSE

After comparing our request payload with the user's working example, we identified **6 missing fields**:

1. `kategoriAgacKod` - Was always sending `string.Empty` instead of using mapping result or `null`
2. `minStokKontrol` - Not being sent at all
3. `alisTevkifatOran` - Not being sent at all
4. `satisTevkifatOran` - Not being sent at all
5. `alisTevkifatKod` - Not being sent at all
6. `satisTevkifatKod` - Not being sent at all

## ✅ SOLUTION IMPLEMENTED

### Changes Made:

#### 1. **Mapper Fix** (`src/Katana.Business/Mappers/KatanaToLucaMapper.cs`)

Added missing fields to the `LucaCreateStokKartiRequest` object:

```csharp
var dto = new LucaCreateStokKartiRequest
{
    // ... existing fields ...

    // ✅ FIX 1: Use category mapping result (null or code like "001", "220")
    KategoriAgacKod = category,  // Was: string.Empty

    // ✅ FIX 2: Add minStokKontrol
    MinStokKontrol = 0,

    // ✅ FIX 3-6: Add tevkifat (withholding tax) fields
    AlisTevkifatOran = "0",
    SatisTevkifatOran = "0",
    AlisTevkifatKod = 0,
    SatisTevkifatKod = 0,

    // ... rest of fields ...
};
```

#### 2. **DTO Fix** (`src/Katana.Core/DTOs/LucaDtos.cs`)

Made fields nullable to allow sending `null` values:

```csharp
// ✅ Changed from: public string KategoriAgacKod { get; set; } = string.Empty;
[JsonPropertyName("kategoriAgacKod")]
public string? KategoriAgacKod { get; set; }

// ✅ Changed from: public string Barkod { get; set; } = string.Empty;
[JsonPropertyName("barkod")]
public string? Barkod { get; set; }
```

**Why nullable?**

- Luca API accepts `null` for optional fields (as shown in user's example)
- Sending empty strings (`""`) may cause validation errors
- Versioned SKUs need `null` barcode to avoid duplicate barcode errors

## 📊 COMPARISON

### User's Working Example:

```json
{
  "kartAdi": "Test Ürünü",
  "kartKodu": "00013225",
  "kartTipi": 1,
  "kartAlisKdvOran": 1,
  "olcumBirimiId": 1,
  "baslangicTarihi": "06/04/2022",
  "kartTuru": 1,
  "kategoriAgacKod": null,
  "barkod": "8888888",
  "alisTevkifatOran": "7/10",
  "satisTevkifatOran": "2/10",
  "alisTevkifatTipId": 1,
  "satisTevkifatTipId": 1,
  "satilabilirFlag": 1,
  "satinAlinabilirFlag": 1,
  "lotNoFlag": 1,
  "minStokKontrol": 0,
  "maliyetHesaplanacakFlag": true
}
```

### Our Request (After Fix):

```json
{
  "kartAdi": "Presli Boru",
  "kartKodu": "cliplok1",
  "kartTipi": 4,
  "kartAlisKdvOran": 1,
  "kartSatisKdvOran": 1,
  "olcumBirimiId": 5,
  "baslangicTarihi": "06/12/2025",
  "kartTuru": 1,
  "kategoriAgacKod": null, // ✅ Now uses mapping or null
  "barkod": "cliplok1",
  "alisTevkifatOran": "0", // ✅ Added
  "satisTevkifatOran": "0", // ✅ Added
  "alisTevkifatKod": 0, // ✅ Added
  "satisTevkifatKod": 0, // ✅ Added
  "satilabilirFlag": 1,
  "satinAlinabilirFlag": 1,
  "lotNoFlag": 0,
  "minStokKontrol": 0, // ✅ Added
  "maliyetHesaplanacakFlag": true
}
```

## 🚀 HOW TO APPLY

### Quick Apply:

```powershell
.\apply-stock-card-fix.ps1
```

### Manual Steps:

```powershell
# 1. Restart backend
docker-compose restart backend

# 2. Wait for backend to be ready
Start-Sleep -Seconds 8

# 3. Monitor logs
docker-compose logs -f backend | Select-String "LUCA|Stock card|error"

# 4. Trigger sync (via API or frontend)
# API: POST http://localhost:5055/api/sync/trigger
# Frontend: Click "Sync Now" button

# 5. Check results
.\check-luca-simple.ps1
```

## 🎯 EXPECTED RESULTS

### Success Indicators:

✅ Luca API returns: `{"error":false, "skartId":XXXXX, "message":"...başarılı..."}`
✅ New products (cliplok1, Ø38x1,5-2, etc.) are created in Luca
✅ No more `{"error":true}` responses
✅ No unnecessary -V2, -V3 versions for existing products

### Success Log Example:

```
[INF] >>> LUCA JSON REQUEST (cliplok1): {"kartAdi":"Presli Boru","kartKodu":"cliplok1",...}
[INF] Luca stock card response for cliplok1 => HTTP OK, BODY={"skartId":79409,"error":false,"message":"cliplok1 - Presli Boru stok kartı başarılı bir şekilde kaydedilmiştir."}
[INF] ✅ Stock card cliplok1 created successfully
```

### If Still Failing:

- Check logs for actual error message (should now be provided by Luca API)
- Verify category mappings in `appsettings.json`
- Check if category codes exist in Luca (e.g., "001", "220")
- Verify Turkish character encoding (ISO-8859-9)

## 📁 FILES MODIFIED

1. `src/Katana.Business/Mappers/KatanaToLucaMapper.cs` (lines ~477-510)
   - Added missing fields to request object
2. `src/Katana.Core/DTOs/LucaDtos.cs` (lines ~1566, ~1578)
   - Made `KategoriAgacKod` nullable
   - Made `Barkod` nullable

## 🔧 TECHNICAL DETAILS

### Category Mapping Logic:

1. First checks database mapping table (`productCategoryMappings`)
2. Then checks `appsettings.json` CategoryMapping
3. If not found, uses `DefaultKategoriKodu` (if valid numeric code)
4. Otherwise sends `null` (Luca API accepts null)

### Barcode Logic:

- Normal SKUs: Send barcode (or SKU if barcode is empty)
- Versioned SKUs (ending with `-V2`, `-V3`, etc.): Send `null` to avoid duplicate barcode errors

### Encoding:

- Turkish characters are converted to ISO-8859-9 (Windows-1254) format
- Applied to `KartAdi` and `UzunAdi` fields

## 📝 NOTES

- Luca API does NOT support updates, only creates new stock cards
- Versioning is intentional for products with changes
- Empty Luca cache (from previous session issue) prevented duplicate detection
- After this fix, both issues should be resolved:
  1. New products will be created successfully
  2. Existing products won't create unnecessary versions (cache is now populated)

## ✨ CONCLUSION

All required fields are now included in the request payload, matching the user's working example. The fix has been applied and is ready for testing.

**Next Step**: Restart backend and test!
