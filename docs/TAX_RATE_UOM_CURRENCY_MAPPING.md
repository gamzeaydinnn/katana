# Tax Rate, UoM, and Currency Mapping Infrastructure

## Overview

This document describes the tax rate, unit of measure (UoM), and currency mapping infrastructure that enables proper conversion between Katana and Koza/Luca systems.

## Components

### 1. Tax Rate Mapping

Maps Katana `tax_rate_id` to Koza `kdvOran` (decimal format).

**Entity**: `TaxRateMapping`
- `KatanaTaxRateId` (long): Katana tax_rate_id from sales_order_rows, shipping_fees, etc.
- `KozaKdvOran` (decimal): Koza KDV oranı (0.18 = 18%, 0.20 = 20%, etc.)
- `Description` (string): Human-readable description
- Sync tracking fields: `LastSyncHash`, `SyncStatus`, `LastSyncAt`, `LastSyncError`

**Service**: `ITaxRateMappingService`
```csharp
// Get KDV oranı by tax_rate_id (with fallback)
decimal kdvOran = await _taxRateMappingService.GetKdvOranByTaxRateIdAsync(taxRateId, defaultRate: 0.20m);

// Create or update mapping
await _taxRateMappingService.CreateOrUpdateMappingAsync(taxRateId: 1, kozaKdvOran: 0.20m, description: "Standard VAT 20%");

// Get all mappings as dictionary for bulk operations
var taxRateMap = await _taxRateMappingService.GetTaxRateToKdvOranMapAsync();
```

**API Endpoints**:
- `GET /api/admin/tax-rate-mappings/mappings` - Get all mappings
- `GET /api/admin/tax-rate-mappings/by-tax-rate-id/{taxRateId}` - Get mapping by tax_rate_id
- `GET /api/admin/tax-rate-mappings/{taxRateId}/kdv-oran` - Get KDV oranı
- `POST /api/admin/tax-rate-mappings` - Create/update mapping
- `GET /api/admin/tax-rate-mappings/dictionary` - Get dictionary
- `DELETE /api/admin/tax-rate-mappings/{taxRateId}` - Delete mapping

**Default Mappings**:
| tax_rate_id | KDV Oranı | Description |
|-------------|-----------|-------------|
| 1 | 0.20 | Standard VAT 20% |
| 2 | 0.18 | Standard VAT 18% (old rate) |
| 3 | 0.10 | Reduced VAT 10% |
| 4 | 0.08 | Reduced VAT 8% |
| 5 | 0.01 | Super Reduced VAT 1% |
| 6 | 0.00 | Zero VAT 0% |

### 2. UoM (Unit of Measure) Mapping

Maps Katana UoM strings to Koza `olcumBirimiId`.

**Entity**: `UoMMapping`
- `KatanaUoMString` (string): Katana UoM string (stored in uppercase for case-insensitive matching)
- `KozaOlcumBirimiId` (long): Koza ölçüm birimi ID
- `Description` (string): Human-readable description
- Sync tracking fields: `LastSyncHash`, `SyncStatus`, `LastSyncAt`, `LastSyncError`

**Service**: `IUoMMappingService`
```csharp
// Get olcumBirimiId by UoM string (with fallback)
long olcumBirimiId = await _uomMappingService.GetOlcumBirimiIdByUoMStringAsync("pcs", defaultId: 5);

// Create or update mapping
await _uomMappingService.CreateOrUpdateMappingAsync("kg", kozaOlcumBirimiId: 1, description: "Kilogram");

// Get all mappings as dictionary for bulk operations
var uomMap = await _uomMappingService.GetUoMToOlcumBirimiIdMapAsync();
```

**API Endpoints**:
- `GET /api/admin/uom-mappings/mappings` - Get all mappings
- `GET /api/admin/uom-mappings/by-uom/{uomString}` - Get mapping by UoM string
- `GET /api/admin/uom-mappings/{uomString}/olcum-birimi-id` - Get olcumBirimiId
- `POST /api/admin/uom-mappings` - Create/update mapping
- `GET /api/admin/uom-mappings/dictionary` - Get dictionary
- `DELETE /api/admin/uom-mappings/{uomString}` - Delete mapping

**Default Mappings**:
| UoM String | olcumBirimiId | Description |
|------------|---------------|-------------|
| PCS, UNIT, ADET, EA | 5 | Pieces / Units (Adet) |
| KG | 1 | Kilogram |
| G, GR | 2 | Gram |
| M, METRE | 3 | Meter (Metre) |
| L, LITRE | 4 | Liter (Litre) |
| BOX, KUTU | 6 | Box (Kutu) |
| PKG, PAKET | 7 | Package (Paket) |

### 3. Currency Validation

Validates and normalizes currency codes (ISO 4217).

**Utility**: `CurrencyValidator`
```csharp
// Validate currency
bool isValid = CurrencyValidator.IsValidCurrency("TRY"); // true

// Normalize currency (handles variations)
string normalized = CurrencyValidator.NormalizeCurrency("turkish lira"); // "TRY"

// Validate and throw if invalid
string validated = CurrencyValidator.Validate("USD"); // "USD" or throws

// Get or default
string currency = CurrencyValidator.GetOrDefault("INVALID", "TRY"); // "TRY"

// Get supported currencies
var supported = CurrencyValidator.GetSupportedCurrencies(); // ["TRY", "USD", "EUR", ...]
```

**Supported Currencies**:
- TRY (Turkish Lira) - Default
- USD (US Dollar)
- EUR (Euro)
- GBP (British Pound)
- CHF (Swiss Franc)
- JPY (Japanese Yen)
- CNY (Chinese Yuan)
- RUB (Russian Ruble)
- AED (UAE Dirham)
- SAR (Saudi Riyal)

## Usage in Mapping Code

### Before (Hardcoded Values)
```csharp
// ❌ OLD: Hardcoded values
var dto = new LucaCreateStokKartiRequest
{
    OlcumBirimiId = 5, // Hardcoded "Adet"
    KartAlisKdvOran = 0.18, // Hardcoded 18%
    KartSatisKdvOran = 0.18
};
```

### After (Using Mapping Services)
```csharp
// ✅ NEW: Using mapping services
var olcumBirimiId = await _uomMappingService.GetOlcumBirimiIdByUoMStringAsync(
    product.Unit, 
    defaultId: _lucaSettings.DefaultOlcumBirimiId);

var kdvOran = await _taxRateMappingService.GetKdvOranByTaxRateIdAsync(
    salesOrderRow.TaxRateId ?? 0, 
    defaultRate: (decimal)_lucaSettings.DefaultKdvOran);

var dto = new LucaCreateStokKartiRequest
{
    OlcumBirimiId = olcumBirimiId,
    KartAlisKdvOran = (double)kdvOran,
    KartSatisKdvOran = (double)kdvOran
};
```

## Database Schema

### TaxRateMappings Table
```sql
CREATE TABLE TaxRateMappings (
    Id INT PRIMARY KEY IDENTITY,
    KatanaTaxRateId BIGINT NOT NULL UNIQUE,
    KozaKdvOran DECIMAL(5,4) NOT NULL,
    Description NVARCHAR(200),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    LastSyncHash NVARCHAR(64),
    SyncStatus NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    LastSyncAt DATETIME2,
    LastSyncError NVARCHAR(MAX)
);

CREATE INDEX IX_TaxRateMappings_SyncStatus ON TaxRateMappings(SyncStatus);
CREATE INDEX IX_TaxRateMappings_LastSyncAt ON TaxRateMappings(LastSyncAt);
```

### UoMMappings Table
```sql
CREATE TABLE UoMMappings (
    Id INT PRIMARY KEY IDENTITY,
    KatanaUoMString NVARCHAR(50) NOT NULL UNIQUE,
    KozaOlcumBirimiId BIGINT NOT NULL,
    Description NVARCHAR(200),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    LastSyncHash NVARCHAR(64),
    SyncStatus NVARCHAR(20) NOT NULL DEFAULT 'PENDING',
    LastSyncAt DATETIME2,
    LastSyncError NVARCHAR(MAX)
);

CREATE INDEX IX_UoMMappings_SyncStatus ON UoMMappings(SyncStatus);
CREATE INDEX IX_UoMMappings_LastSyncAt ON UoMMappings(LastSyncAt);
```

## Seeding Default Data

Run the seed script to populate default mappings:

```bash
# Using sqlcmd
sqlcmd -S localhost,1433 -d KatanaDB -U sa -P "Admin00!S" -C -i db/migrations/seed_tax_rate_uom_mappings.sql

# Using docker exec
docker exec -i katana-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -d KatanaDB -U sa -P "Admin00!S" -C < db/migrations/seed_tax_rate_uom_mappings.sql
```

Or use the API to create mappings programmatically:

```bash
# Create tax rate mapping
curl -X POST http://localhost:5000/api/admin/tax-rate-mappings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "katanaTaxRateId": 1,
    "kozaKdvOran": 0.20,
    "description": "Standard VAT 20%"
  }'

# Create UoM mapping
curl -X POST http://localhost:5000/api/admin/uom-mappings \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "katanaUoMString": "pcs",
    "kozaOlcumBirimiId": 5,
    "description": "Pieces (Adet)"
  }'
```

## Migration

Migration created: `20251206225909_AddTaxRateAndUoMMappin gs`

Apply migration:
```bash
dotnet ef database update --project src/Katana.Data --startup-project src/Katana.API --context IntegrationDbContext
```

## Testing

### Test Tax Rate Mapping
```bash
# Get KDV oranı for tax_rate_id 1
curl http://localhost:5000/api/admin/tax-rate-mappings/1/kdv-oran

# Get all tax rate mappings
curl http://localhost:5000/api/admin/tax-rate-mappings/mappings
```

### Test UoM Mapping
```bash
# Get olcumBirimiId for "pcs"
curl http://localhost:5000/api/admin/uom-mappings/pcs/olcum-birimi-id

# Get all UoM mappings
curl http://localhost:5000/api/admin/uom-mappings/mappings
```

### Test Currency Validation
```csharp
[Test]
public void CurrencyValidator_ValidatesTRY()
{
    Assert.IsTrue(CurrencyValidator.IsValidCurrency("TRY"));
    Assert.AreEqual("TRY", CurrencyValidator.NormalizeCurrency("turkish lira"));
}
```

## Notes

- **Case-Insensitive**: UoM strings are stored in uppercase for case-insensitive matching
- **Fallback Values**: All services provide default fallback values if mapping not found
- **Sync Tracking**: All mappings include sync tracking fields for monitoring
- **Leading Zeros**: Currency codes are always 3 characters (ISO 4217)
- **Decimal Precision**: KDV oranı stored as DECIMAL(5,4) for precision (0.1800)

## Related Files

- Entities: `src/Katana.Core/Entities/TaxRateMapping.cs`, `UoMMapping.cs`
- Services: `src/Katana.Business/Services/TaxRateMappingService.cs`, `UoMMappingService.cs`
- Utilities: `src/Katana.Core/Utilities/CurrencyValidator.cs`
- Controllers: `src/Katana.API/Controllers/Admin/TaxRateMappingController.cs`, `UoMMappingController.cs`
- Migration: `src/Katana.Data/Migrations/20251206225909_AddTaxRateAndUoMMappings.cs`
- Seed Script: `db/migrations/seed_tax_rate_uom_mappings.sql`
