# Cari Kart (Müşteri/Tedarikçi) Modülü - Mevcut Durum Analizi

## ✅ Mevcut Durum

### 1. Customer (Müşteri) Entity ✅

**Dosya:** `src/Katana.Core/Entities/Customer.cs`

#### Kritik Alanlar Kontrolü:

| Koza Gereksinimi | Customer Entity | Durum |
|-----------------|----------------|-------|
| **CariKodu** (benzersiz) | `LucaCode` (CK-{Id} formatında) | ✅ Var |
| **FinansalNesneId** | `LucaFinansalNesneId` | ✅ Var |
| **Unvan/Ad Soyad** | `Title` | ✅ Var |
| **VKN/TCKN** | `TaxNo` + `Type` (1=Şirket/VKN, 2=Şahıs/TCKN) | ✅ Var |
| **Adres** | `Address` | ✅ Var |
| **İl** | `City` | ✅ Var |
| **İlçe** | `District` | ✅ Var |
| **Ülke** | `Country` (default: "Turkey") | ✅ Var |
| **Vergi Dairesi** | `TaxOffice` | ✅ Var |
| **E-belge bayrakları** | ❌ Yok | ❌ Eksik |

#### Customer Entity Detayları:

```csharp
public class Customer
{
    public int Id { get; set; }
    public int Type { get; set; } = 1;              // 1=Şirket (VKN), 2=Şahıs (TCKN)
    public string TaxNo { get; set; }               // VKN/TCKN (max 11)
    public string? TaxOffice { get; set; }          // Vergi Dairesi
    public string Title { get; set; }               // Unvan/Ad Soyad
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Country { get; set; } = "Turkey";
    
    // Koza Entegrasyon Alanları
    public string? LucaCode { get; set; }           // CariKodu (CK-{Id})
    public long? LucaFinansalNesneId { get; set; }  // FinansalNesneId
    
    // Diğer alanlar...
    public string? GroupCode { get; set; }
    public decimal? DefaultDiscountRate { get; set; }
    public string? Currency { get; set; } = "TRY";
    public bool IsActive { get; set; } = true;
    public bool IsSynced { get; set; } = false;
    public string? LastSyncError { get; set; }
}
```

**Not:** Customer için ayrı bir `CustomerKozaCariMapping` tablosu yok. Mapping bilgileri direkt `Customer` entity'sinde (`LucaCode`, `LucaFinansalNesneId`).

---

### 2. Supplier (Tedarikçi) Entity ⚠️

**Dosya:** `src/Katana.Core/Entities/Supplier.cs`

#### Kritik Alanlar Kontrolü:

| Koza Gereksinimi | Supplier Entity | Durum |
|-----------------|----------------|-------|
| **CariKodu** (benzersiz) | `LucaCode` (TED-{Id} formatında) | ✅ Var |
| **FinansalNesneId** | `LucaFinansalNesneId` | ✅ Var |
| **Unvan/Ad Soyad** | `Name` | ✅ Var |
| **VKN/TCKN** | `TaxNo` | ✅ Var |
| **Adres** | `Address` | ✅ Var |
| **İl** | `City` | ✅ Var |
| **İlçe** | ❌ Yok | ❌ Eksik |
| **Ülke** | ❌ Yok | ❌ Eksik |
| **Vergi Dairesi** | ❌ Yok | ❌ Eksik |
| **E-belge bayrakları** | ❌ Yok | ❌ Eksik |

#### Supplier Entity Detayları:

```csharp
public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; }                // Unvan/Ad Soyad
    public string? Code { get; set; }
    public string? TaxNo { get; set; }              // VKN/TCKN
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    // ❌ District yok
    // ❌ Country yok
    // ❌ TaxOffice yok
    
    // Koza Entegrasyon Alanları
    public string? LucaCode { get; set; }           // CariKodu (TED-{Id})
    public long? LucaFinansalNesneId { get; set; }  // FinansalNesneId
    
    public bool IsActive { get; set; } = true;
    public string? LastSyncError { get; set; }
}
```

**Not:** Supplier için `SupplierKozaCariMapping` tablosu var (mapping tablosu).

---

### 3. SupplierKozaCariMapping ✅

**Dosya:** `src/Katana.Core/Entities/SupplierKozaCariMapping.cs`

```csharp
public class SupplierKozaCariMapping
{
    public int Id { get; set; }
    public string KatanaSupplierId { get; set; }    // Katana Supplier ID (string)
    public string KozaCariKodu { get; set; }        // Koza cari kodu
    public long? KozaFinansalNesneId { get; set; } // Koza finansal nesne ID
    public string? KatanaSupplierName { get; set; }
    public string? KozaCariTanim { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**Kullanım:** Katana Supplier → Koza Cari mapping için.

---

### 4. Customer Mapping Yapısı

**Customer için ayrı mapping tablosu yok.** Mapping bilgileri direkt `Customer` entity'sinde:

- `Customer.LucaCode` → Koza CariKodu
- `Customer.LucaFinansalNesneId` → Koza FinansalNesneId

**Neden?**
- Customer entity'si zaten mevcut ve mapping alanları eklenmiş
- Supplier için mapping tablosu var çünkü Katana'dan gelen supplier'lar için kullanılıyor

---

## ❌ Eksikler ve İyileştirmeler

### 1. Supplier Entity Eksik Alanlar

```csharp
// Mevcut Supplier entity'sine eklenmesi gerekenler:
public class Supplier
{
    // ... mevcut alanlar ...
    
    // ❌ Eksik alanlar:
    [MaxLength(100)]
    public string? District { get; set; }           // İlçe
    
    [MaxLength(50)]
    public string? Country { get; set; } = "Turkey"; // Ülke
    
    [MaxLength(100)]
    public string? TaxOffice { get; set; }          // Vergi Dairesi
    
    // E-belge bayrakları
    public int? EfaturaTuru { get; set; }            // E-fatura türü
    public bool? EfaturaMukellefi { get; set; }      // E-fatura mükellefi mi?
    public bool? EarsivMukellefi { get; set; }       // E-arşiv mükellefi mi?
}
```

### 2. Customer Entity Eksik Alanlar

```csharp
// Mevcut Customer entity'sine eklenmesi gerekenler:
public class Customer
{
    // ... mevcut alanlar ...
    
    // ❌ Eksik alanlar:
    // E-belge bayrakları
    public int? EfaturaTuru { get; set; }            // E-fatura türü
    public bool? EfaturaMukellefi { get; set; }      // E-fatura mükellefi mi?
    public bool? EarsivMukellefi { get; set; }       // E-arşiv mükellefi mi?
}
```

### 3. CustomerKozaCariMapping Tablosu (Opsiyonel)

Şu anda Customer için mapping tablosu yok. İsterseniz ekleyebilirsiniz:

```csharp
// src/Katana.Core/Entities/CustomerKozaCariMapping.cs
public class CustomerKozaCariMapping
{
    public int Id { get; set; }
    public int CustomerId { get; set; }              // ERP Customer ID
    public string KozaCariKodu { get; set; }          // Koza cari kodu
    public long? KozaFinansalNesneId { get; set; }   // Koza finansal nesne ID
    public string? CustomerName { get; set; }
    public string? KozaCariTanim { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public virtual Customer Customer { get; set; }
}
```

**Avantajları:**
- Supplier ile tutarlı yapı
- Mapping geçmişi tutulabilir
- Birden fazla Koza cari'ye map edilebilir (nadir durum)

**Dezavantajları:**
- Mevcut yapı zaten çalışıyor
- Customer entity'sinde zaten `LucaCode` ve `LucaFinansalNesneId` var

**Öneri:** Mevcut yapıyı koruyun, sadece eksik alanları ekleyin.

---

## 📋 Koza Entegrasyonu İçin Kullanım

### Customer → Koza Cari Mapping

```csharp
// MappingHelper.cs'de kullanım:
var customer = await _context.Customers.FindAsync(customerId);
var cariKodu = customer.LucaCode ?? customer.GenerateLucaCode(); // CK-{Id}
var finansalNesneId = customer.LucaFinansalNesneId;

// Koza'ya fatura gönderirken:
var faturaDto = new KozaFaturaDto
{
    CariKodu = cariKodu,
    FinansalNesneId = finansalNesneId,
    // ...
};
```

### Supplier → Koza Cari Mapping

```csharp
// SupplierKozaCariMapping kullanımı:
var mapping = await _context.SupplierKozaCariMappings
    .FirstOrDefaultAsync(m => m.KatanaSupplierId == supplierId);

if (mapping != null)
{
    var cariKodu = mapping.KozaCariKodu;
    var finansalNesneId = mapping.KozaFinansalNesneId;
    
    // Koza'ya alış faturası gönderirken:
    var faturaDto = new KozaFaturaDto
    {
        CariKodu = cariKodu,
        FinansalNesneId = finansalNesneId,
        // ...
    };
}
```

---

## 🔄 Mevcut Servisler

### CustomerService

**Dosya:** `src/Katana.Business/Services/CustomerService.cs`

- ✅ `GetAllCustomersAsync()`
- ✅ `GetCustomerByIdAsync(int id)`
- ✅ `GetCustomerByTaxNoAsync(string taxNo)`
- ✅ `CreateCustomerAsync(CreateCustomerDto dto)`
- ✅ `UpdateCustomerAsync(int id, UpdateCustomerDto dto)`
- ✅ `DeleteCustomerAsync(int id)`
- ✅ `UpdateLastSyncErrorAsync(int customerId, string? errorMessage, long? lucaFinansalNesneId)`

### SupplierService

**Dosya:** `src/Katana.Business/Services/SupplierService.cs`

- ✅ `GetAllAsync()`
- ✅ `GetByIdAsync(int id)`
- ✅ `CreateAsync(CreateSupplierDto dto)`
- ✅ `UpdateAsync(int id, UpdateSupplierDto dto)`
- ✅ `DeleteAsync(int id)`

---

## ✅ Sonuç ve Öneriler

### Mevcut Durum Özeti

| Özellik | Customer | Supplier |
|---------|----------|----------|
| **CariKodu** | ✅ `LucaCode` | ✅ `LucaCode` |
| **FinansalNesneId** | ✅ `LucaFinansalNesneId` | ✅ `LucaFinansalNesneId` |
| **Unvan/Ad Soyad** | ✅ `Title` | ✅ `Name` |
| **VKN/TCKN** | ✅ `TaxNo` + `Type` | ✅ `TaxNo` |
| **Adres/İl/İlçe/Ülke** | ✅ Tümü var | ⚠️ Sadece Address, City |
| **Vergi Dairesi** | ✅ `TaxOffice` | ❌ Yok |
| **E-belge bayrakları** | ❌ Yok | ❌ Yok |
| **Mapping Tablosu** | ❌ Yok (entity'de) | ✅ `SupplierKozaCariMapping` |

### Yapılması Gerekenler

1. **Supplier Entity Güncelleme** (Öncelik: Yüksek)
   - `District` (İlçe) ekle
   - `Country` (Ülke) ekle
   - `TaxOffice` (Vergi Dairesi) ekle

2. **E-belge Bayrakları Ekleme** (Öncelik: Orta)
   - Customer entity'sine: `EfaturaTuru`, `EfaturaMukellefi`, `EarsivMukellefi`
   - Supplier entity'sine: `EfaturaTuru`, `EfaturaMukellefi`, `EarsivMukellefi`

3. **CustomerKozaCariMapping Tablosu** (Öncelik: Düşük)
   - İsteğe bağlı, mevcut yapı çalışıyor
   - Supplier ile tutarlılık için eklenebilir

### Önerilen Yaklaşım

**Model-1 Yaklaşımı (Mevcut):**
- Customer: Entity'de mapping (`LucaCode`, `LucaFinansalNesneId`)
- Supplier: Mapping tablosu (`SupplierKozaCariMapping`)

**Bu yaklaşım çalışıyor, sadece eksik alanları ekleyin!**

---

## 📝 Migration Örneği

```csharp
// Supplier entity'sine eksik alanları eklemek için:
public partial class AddSupplierMissingFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "District",
            table: "Suppliers",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Country",
            table: "Suppliers",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: true,
            defaultValue: "Turkey");

        migrationBuilder.AddColumn<string>(
            name: "TaxOffice",
            table: "Suppliers",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "EfaturaTuru",
            table: "Suppliers",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "EfaturaMukellefi",
            table: "Suppliers",
            type: "bit",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "EarsivMukellefi",
            table: "Suppliers",
            type: "bit",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "District", table: "Suppliers");
        migrationBuilder.DropColumn(name: "Country", table: "Suppliers");
        migrationBuilder.DropColumn(name: "TaxOffice", table: "Suppliers");
        migrationBuilder.DropColumn(name: "EfaturaTuru", table: "Suppliers");
        migrationBuilder.DropColumn(name: "EfaturaMukellefi", table: "Suppliers");
        migrationBuilder.DropColumn(name: "EarsivMukellefi", table: "Suppliers");
    }
}
```

---

## ✅ Sonuç

**Cari Kart yapısı büyük ölçüde mevcut!**

- ✅ Customer: Tüm kritik alanlar var (E-belge bayrakları hariç)
- ⚠️ Supplier: Bazı alanlar eksik (District, Country, TaxOffice, E-belge bayrakları)
- ✅ Mapping yapıları çalışıyor
- ✅ Koza entegrasyonu mevcut

**Yapılacaklar:**
1. Supplier entity'sine eksik alanları ekleyin
2. Customer ve Supplier'a E-belge bayrakları ekleyin
3. Migration oluşturun ve uygulayın

Bu değişiklikleri yapmamı ister misiniz? 🚀

