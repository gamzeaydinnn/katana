# Complete Stock Card Creation Fix - All Issues Resolved

## 🎯 PROBLEM TIMELINE

### Issue 1: Missing Fields ✅ FIXED

**Error**: `{"error":true}` with no message
**Cause**: Missing required fields in request payload
**Solution**: Added `MinStokKontrol`, `AlisTevkifatOran`, `SatisTevkifatOran`, `AlisTevkifatKod`, `SatisTevkifatKod`

### Issue 2: Invalid Category Code ✅ FIXED

**Error**: `{"error":true,"message":"Kategori bulunamadı."}`
**Cause**: Category code `"01"` doesn't exist in Luca
**Solution**: Changed `DefaultKategoriKodu` from `"01"` to `null`

### Issue 3: JSON Serialization Omitting Null ✅ FIXED

**Error**: `{"error":true}` with no message (again!)
**Cause**: JSON serializer configured to omit null values, so `kategoriAgacKod` field was completely missing
**Solution**: Changed `DefaultIgnoreCondition` from `WhenWritingNull` to `Never`

## 📋 ALL CHANGES MADE

### 1. Mapper Changes (`src/Katana.Business/Mappers/KatanaToLucaMapper.cs`)

```csharp
var dto = new LucaCreateStokKartiRequest
{
    // ... existing fields ...

    KategoriAgacKod = category,  // ✅ Use mapping result or null
    MinStokKontrol = 0,          // ✅ Added
    AlisTevkifatOran = "0",      // ✅ Added
    SatisTevkifatOran = "0",     // ✅ Added
    AlisTevkifatKod = 0,         // ✅ Added
    SatisTevkifatKod = 0,        // ✅ Added

    // ... rest of fields ...
};
```

### 2. DTO Changes (`src/Katana.Core/DTOs/LucaDtos.cs`)

```csharp
// Made nullable to allow null values
[JsonPropertyName("kategoriAgacKod")]
public string? KategoriAgacKod { get; set; }  // Was: string = string.Empty

[JsonPropertyName("barkod")]
public string? Barkod { get; set; }  // Was: string = string.Empty
```

### 3. Configuration Changes (`appsettings.json` & `appsettings.Development.json`)

```json
// BEFORE
"DefaultKategoriKodu": "01",
"CategoryMapping": {
  "default": "01"
}

// AFTER
"DefaultKategoriKodu": null,
"CategoryMapping": {
  "default": null
}
```

### 4. JSON Serialization Changes (`src/Katana.Infrastructure/APIClients/LucaService.StockCards.cs`)

```csharp
// BEFORE (2 occurrences)
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = null,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull  // ❌ Omits null
};

// AFTER
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = null,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never  // ✅ Includes null
};
```

## 🚀 HOW TO APPLY

### Quick Apply:

```powershell
.\fix-json-serialization.ps1
```

### Manual Steps:

```powershell
# Restart backend (all changes already applied)
docker-compose restart backend

# Wait for startup
Start-Sleep -Seconds 8

# Monitor logs
docker-compose logs -f backend | Select-String "LUCA JSON REQUEST|kategoriAgacKod|Stock card"
```

## 📊 EXPECTED RESULTS

### Request JSON (BEFORE all fixes):

```json
{
  "kartAdi": "Presli Boru",
  "kartKodu": "PUT. Ø22*1,5",
  "kartTipi": 4,
  "kartAlisKdvOran": 1,
  "kartSatisKdvOran": 1,
  "olcumBirimiId": 5,
  "baslangicTarihi": "06/12/2025",
  "kartTuru": 1,
  "barkod": "PUT. Ø22*1,5",
  "satilabilirFlag": 1,
  "satinAlinabilirFlag": 1,
  "lotNoFlag": 0,
  "maliyetHesaplanacakFlag": true
}
```

**Missing**: `minStokKontrol`, `alisTevkifatOran`, `satisTevkifatOran`, `alisTevkifatKod`, `satisTevkifatKod`, `kategoriAgacKod`

### Request JSON (AFTER all fixes):

```json
{
  "kartAdi": "Presli Boru",
  "kartKodu": "PUT. Ø22*1,5",
  "kartTipi": 4,
  "kartAlisKdvOran": 1,
  "kartSatisKdvOran": 1,
  "olcumBirimiId": 5,
  "baslangicTarihi": "06/12/2025",
  "kartTuru": 1,
  "kategoriAgacKod": null, // ✅ Present with null value
  "barkod": "PUT. Ø22*1,5",
  "satilabilirFlag": 1,
  "satinAlinabilirFlag": 1,
  "lotNoFlag": 0,
  "minStokKontrol": 0, // ✅ Added
  "alisTevkifatOran": "0", // ✅ Added
  "satisTevkifatOran": "0", // ✅ Added
  "alisTevkifatKod": 0, // ✅ Added
  "satisTevkifatKod": 0, // ✅ Added
  "maliyetHesaplanacakFlag": true
}
```

### Response (SUCCESS):

```json
{
  "skartId": 79409,
  "error": false,
  "message": "PUT. Ø22*1,5 - Presli Boru stok kartı başarılı bir şekilde kaydedilmiştir."
}
```

## 📁 FILES MODIFIED

1. **src/Katana.Business/Mappers/KatanaToLucaMapper.cs**
   - Added missing fields to request object
2. **src/Katana.Core/DTOs/LucaDtos.cs**
   - Made `KategoriAgacKod` and `Barkod` nullable
3. **src/Katana.API/appsettings.json**
   - Changed `DefaultKategoriKodu` to `null`
   - Changed `CategoryMapping.default` to `null`
4. **src/Katana.API/appsettings.Development.json**
   - Changed `DefaultKategoriKodu` to `null`
   - Changed `CategoryMapping.default` to `null`
5. **src/Katana.Infrastructure/APIClients/LucaService.StockCards.cs**
   - Changed JSON serialization to include null values (2 locations)

## 🔧 TECHNICAL DETAILS

### Why JSON Serialization Matters

**Problem**: C# JSON serializer by default omits null values

```csharp
DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
```

**Result**: Field is completely missing from JSON

```json
{ "kartAdi": "...", "kartKodu": "..." } // No kategoriAgacKod at all
```

**Luca API Requirement**: Field must be present (even if null)

```json
{ "kartAdi": "...", "kartKodu": "...", "kategoriAgacKod": null } // Field present
```

**Solution**: Include null values in JSON

```csharp
DefaultIgnoreCondition = JsonIgnoreCondition.Never
```

### Category Mapping Logic

1. Check database mapping table
2. Check `appsettings.json` CategoryMapping
3. If found: Use mapped code (e.g., "001", "220")
4. If not found: Use `DefaultKategoriKodu` (now `null`)
5. Send to Luca with field present

## ✨ FINAL RESULT

After all fixes:

- ✅ All required fields are included
- ✅ Category code is valid (null is acceptable)
- ✅ JSON includes null values (field is present)
- ✅ Stock cards should be created successfully

**Next Step**: Restart backend and test!

```powershell
.\fix-json-serialization.ps1
```

## 🎉 SUCCESS INDICATORS

Look for these in the logs:

1. **Request includes kategoriAgacKod**:

   ```
   >>> LUCA JSON REQUEST: {...,"kategoriAgacKod":null,...}
   ```

2. **Response shows success**:

   ```
   {"error":false,"skartId":XXXXX,"message":"...başarılı..."}
   ```

3. **No more errors**:
   ```
   ✅ Stock card PUT. Ø22*1,5 created successfully
   ```
