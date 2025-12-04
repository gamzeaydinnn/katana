# ✅ Kategori Mapping Sorunu - ÇÖZÜLDÜ

## 🔥 Sorun
- Katana'dan gelen `category_name` (örn: "1MAMUL", "3YARI MAMUL") doğrudan Luca'nın `KategoriAgacKod` alanına yazılamıyor
- Luca sadece numeric kodları kabul ediyor (örn: "001", "220")
- Mapping olmadığında stok kartı oluşturulamıyor

## ✅ Çözüm - 3 Katmanlı Mapping Sistemi

### 1. **Database Mapping (Öncelikli)**
```sql
-- MappingTables tablosu
MappingType: "PRODUCT_CATEGORY"
SourceValue: "1MAMUL" → TargetValue: "001"
SourceValue: "3YARI MAMUL" → TargetValue: "220"
```

**Kullanım:**
- `/api/mapping/category-mappings` endpoint'i ile yönetiliyor
- `MappingService.GetCategoryMappingAsync()` ile çekiliyor
- Runtime'da güncellenebilir

### 2. **appsettings.json Mapping (Fallback)**
```json
{
  "LucaApi": {
    "CategoryMapping": {
      "1MAMUL": "001",
      "2HAMMADDE": "002",
      "3YARI MAMUL": "220",
      "4YARDIMCI MALZEME": "004",
      "5AMBALAJ": "005",
      "default": "01"
    }
  }
}
```

**Kullanım:**
- Database'de mapping yoksa appsettings'ten bakılıyor
- `lucaSettings.CategoryMapping` dictionary'den çekiliyor
- Deployment'ta sabit mapping'ler için

### 3. **DefaultKategoriKodu (Son Çare)**
```json
{
  "LucaApi": {
    "DefaultKategoriKodu": "01"
  }
}
```

**Kullanım:**
- Hiçbir mapping bulunamazsa kullanılıyor
- Tüm ürünler aynı kategoriye düşer

## 🎯 Mapping Öncelik Sırası

```
1. Database MappingTables (PRODUCT_CATEGORY)
   ↓ (bulunamadı)
2. appsettings.json CategoryMapping[category_name]
   ↓ (bulunamadı)
3. appsettings.json CategoryMapping["default"]
   ↓ (bulunamadı)
4. DefaultKategoriKodu
```

## 📝 Kod Değişiklikleri

### `LucaApiSettings.cs`
```csharp
public Dictionary<string, string> CategoryMapping { get; set; } = new();
```

### `KatanaToLucaMapper.cs`
```csharp
// 1. Database mapping
if (productCategoryMappings?.TryGetValue(lookupKey, out var mapped))
    category = mapped;

// 2. appsettings mapping
if (string.IsNullOrWhiteSpace(category))
    if (lucaSettings.CategoryMapping?.TryGetValue(lookupKey, out var configMapped))
        category = configMapped;

// 3. Default fallback
if (string.IsNullOrWhiteSpace(category))
    if (lucaSettings.CategoryMapping?.TryGetValue("default", out var defaultCategory))
        category = defaultCategory;
    else
        category = lucaSettings.DefaultKategoriKodu;
```

## 🚀 Kullanım Örnekleri

### Örnek 1: Database'e Mapping Ekleme
```http
POST /api/mapping/category-mappings
{
  "sourceValue": "1MAMUL",
  "targetValue": "001",
  "description": "Mamul ürünler"
}
```

### Örnek 2: Katana'dan Gelen Ürün
```json
{
  "sku": "PROD-001",
  "name": "Test Ürün",
  "category_name": "1MAMUL"  // ← Katana'dan gelen
}
```

**Mapping Sonucu:**
```json
{
  "kartKodu": "PROD-001",
  "kartAdi": "Test Ürün",
  "kategoriAgacKod": "001"  // ← Luca'ya giden
}
```

### Örnek 3: Mapping Bulunamadığında
```json
{
  "category_name": "YENİ_KATEGORİ"  // ← Mapping yok
}
```

**Fallback Sonucu:**
```json
{
  "kategoriAgacKod": "01"  // ← default veya DefaultKategoriKodu
}
```

## ⚠️ Önemli Notlar

1. **Category NAME asla KOD olarak kullanılmaz**
   - ❌ `kategoriAgacKod: "1MAMUL"` (YANLIŞ)
   - ✅ `kategoriAgacKod: "001"` (DOĞRU)

2. **Numeric ID'ler de mapping gerektirir**
   - Katana bazen internal ID döner (örn: "1", "2")
   - Bunlar da Luca kodlarına map edilmeli

3. **Case-insensitive mapping**
   - "1MAMUL", "1mamul", "1Mamul" → hepsi aynı

4. **Luca Kategori Kodları**
   - `ListeleStkSkartKategoriAgac.do` endpoint'inden çekilebilir
   - Format: "001", "001.001", "220" gibi

## 📊 Test Senaryoları

### ✅ Senaryo 1: Database Mapping Var
```
Input: category_name = "1MAMUL"
Database: "1MAMUL" → "001"
Output: kategoriAgacKod = "001"
```

### ✅ Senaryo 2: Sadece appsettings Mapping Var
```
Input: category_name = "2HAMMADDE"
Database: (yok)
appsettings: "2HAMMADDE" → "002"
Output: kategoriAgacKod = "002"
```

### ✅ Senaryo 3: Hiçbir Mapping Yok
```
Input: category_name = "UNKNOWN"
Database: (yok)
appsettings: (yok)
Fallback: CategoryMapping["default"] = "01"
Output: kategoriAgacKod = "01"
```

### ✅ Senaryo 4: Category Boş
```
Input: category_name = null
Output: kategoriAgacKod = "01" (DefaultKategoriKodu)
```

## 🔧 Bakım ve Güncelleme

### Yeni Kategori Ekleme
1. Katana'dan yeni `category_name` geldiğinde
2. Luca'dan uygun kategori kodunu bul
3. Database'e veya appsettings'e ekle

### Toplu Kategori Güncelleme
```sql
-- Tüm kategorileri listele
SELECT DISTINCT category_name 
FROM Products 
WHERE category_name IS NOT NULL;

-- Mapping'leri kontrol et
SELECT * FROM MappingTables 
WHERE MappingType = 'PRODUCT_CATEGORY';
```

## 📈 İyileştirmeler

- ✅ 3 katmanlı fallback mekanizması
- ✅ Database + appsettings hybrid yaklaşım
- ✅ Case-insensitive mapping
- ✅ Default fallback desteği
- ✅ Runtime güncellenebilir mapping
- ✅ Category NAME'lerin KOD olarak kullanılmasını engelleme

## 🎉 Sonuç

Artık Katana'dan gelen herhangi bir `category_name` değeri güvenli şekilde Luca'nın `KategoriAgacKod` formatına dönüştürülüyor. Mapping bulunamadığında bile sistem fallback mekanizması ile çalışmaya devam ediyor.
