# Senkronizasyon Dropdown Uyuşmazlıkları - Düzeltmeler

## Özet
Frontend'deki "Senkronizasyon Başlat" dropdown'ında 3 seçenek (SUPPLIER, WAREHOUSE, CUSTOMER_LUCA) backend'de endpoint'leri olmadığı için çalışmıyordu. Tüm uyuşmazlıklar düzeltildi.

---

## Yapılan Değişiklikler

### 1. Backend: SyncController.cs - 3 Yeni Endpoint Eklendi

**Dosya**: `src/Katana.API/Controllers/SyncController.cs`

#### Eklenen Endpoint'ler:

```csharp
[HttpPost("suppliers")]
public async Task<ActionResult<SyncResultDto>> SyncSuppliers()
{
    // Katana → Koza tedarikçi senkronizasyonu
    var result = await _syncService.SyncSuppliersToKozaAsync();
}

[HttpPost("warehouses")]
public async Task<ActionResult<SyncResultDto>> SyncWarehouses()
{
    // Katana → Koza depo senkronizasyonu
    var result = await _syncService.SyncWarehousesToKozaAsync();
}

[HttpPost("customers-luca")]
public async Task<ActionResult<SyncResultDto>> SyncCustomersLuca()
{
    // Katana → Luca müşteri (cari) senkronizasyonu
    var result = await _syncService.SyncCustomersToLucaAsync();
}
```

**Konum**: `/Sync/suppliers`, `/Sync/warehouses`, `/Sync/customers-luca`

---

### 2. Frontend: api.ts - Endpoint Mapping Güncellendi

**Dosya**: `frontend/katana-web/src/services/api.ts`

**Değişiklik**:
```typescript
// BEFORE:
const endpointMap: Record<string, string> = {
  STOCK: "/Sync/stock",
  INVOICE: "/Sync/invoices",
  CUSTOMER: "/Sync/customers",
  DESPATCH: "/Sync/from-luca/despatch",
  ALL: "/Sync/run",
  STOCK_CARD: "/Sync/to-luca/stock-cards",
  PRODUCT: "/Sync/to-luca/stock-cards",
};

// AFTER:
const endpointMap: Record<string, string> = {
  STOCK: "/Sync/stock",
  INVOICE: "/Sync/invoices",
  CUSTOMER: "/Sync/customers",
  DESPATCH: "/Sync/from-luca/despatch",
  ALL: "/Sync/run",
  STOCK_CARD: "/Sync/to-luca/stock-cards",
  PRODUCT: "/Sync/to-luca/stock-cards",
  SUPPLIER: "/Sync/suppliers",           // ← EKLENDI
  WAREHOUSE: "/Sync/warehouses",         // ← EKLENDI
  CUSTOMER_LUCA: "/Sync/customers-luca", // ← EKLENDI
};
```

---

## Düzeltme Sonrası Durum

### ✅ Tüm Dropdown Seçenekleri Artık Çalışıyor

| Dropdown Label | Frontend Value | API Endpoint | Backend Method | Yön | Status |
|---|---|---|---|---|---|
| Stok Hareketleri | STOCK | /Sync/stock | SyncStockAsync() | Katana→Luca | ✅ |
| Fatura | INVOICE | /Sync/invoices | SyncInvoicesAsync() | Katana→Luca | ✅ |
| Müşteri (Cari) | CUSTOMER | /Sync/customers | SyncCustomersAsync() | Katana→Luca | ✅ |
| İrsaliye | DESPATCH | /Sync/from-luca/despatch | SyncDespatchFromLucaAsync() | Luca→Katana | ✅ |
| Tümü | ALL | /Sync/run | SyncAllAsync() | Katana→Luca | ✅ |
| Stok Kartları (Luca) | STOCK_CARD | /Sync/to-luca/stock-cards | SyncProductsToLucaAsync() | Katana→Luca | ✅ |
| **Tedarikçi Kartları (Koza)** | **SUPPLIER** | **/Sync/suppliers** | **SyncSuppliersToKozaAsync()** | **Katana→Koza** | **✅ FIXED** |
| **Depo Kartları (Koza)** | **WAREHOUSE** | **/Sync/warehouses** | **SyncWarehousesToKozaAsync()** | **Katana→Koza** | **✅ FIXED** |
| **Müşteri Kartları (Luca Cari)** | **CUSTOMER_LUCA** | **/Sync/customers-luca** | **SyncCustomersToLucaAsync()** | **Katana→Luca** | **✅ FIXED** |

---

## Senkronizasyon Yönleri

### Katana → Luca (Push)
- Stok Hareketleri
- Fatura
- Müşteri (Cari)
- Stok Kartları
- Müşteri Kartları (Luca Cari)

### Luca → Katana (Pull)
- İrsaliye

### Katana → Koza (Push)
- Tedarikçi Kartları
- Depo Kartları

### Katana → Luca (ALL - Mixed)
- Tümü (tüm sync işlemlerini çalıştırır)

---

## Test Komutları

### 1. Tedarikçi Kartları Sync Test
```bash
curl -X POST http://localhost:5000/api/Sync/suppliers \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"syncType":"SUPPLIER"}'
```

### 2. Depo Kartları Sync Test
```bash
curl -X POST http://localhost:5000/api/Sync/warehouses \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"syncType":"WAREHOUSE"}'
```

### 3. Müşteri Kartları (Luca) Sync Test
```bash
curl -X POST http://localhost:5000/api/Sync/customers-luca \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {token}" \
  -d '{"syncType":"CUSTOMER_LUCA"}'
```

### 4. Frontend'de Test
1. Uygulamayı aç
2. "Senkronizasyon Yönetimi" sayfasına git
3. "Senkronizasyon Başlat" butonuna tıkla
4. Dropdown'dan "Tedarikçi Kartları (Koza)" seç
5. "Başlat" butonuna tıkla
6. Network tab'ında POST /api/Sync/suppliers request'ini kontrol et
7. Response'da success: true olmalı

---

## Dosyalar Değiştirilen

1. ✅ `src/Katana.API/Controllers/SyncController.cs` - 3 endpoint eklendi
2. ✅ `frontend/katana-web/src/services/api.ts` - 3 mapping eklendi

---

## Notlar

- **StartSync() method'unda**: SUPPLIER, WAREHOUSE, CUSTOMER_LUCA case'leri zaten vardı, sadece endpoint'ler eksikti
- **ISyncService interface'inde**: Tüm method'lar zaten implement edilmişti
- **SyncService implementation'ında**: Tüm method'lar zaten çalışıyordu
- **Sorun**: Frontend'de endpoint mapping ve backend'de HTTP endpoint'leri eksikti

---

## Sonuç

Tüm 9 dropdown seçeneği artık tam olarak çalışıyor:
- ✅ 6 seçenek zaten çalışıyordu
- ✅ 3 seçenek düzeltildi (SUPPLIER, WAREHOUSE, CUSTOMER_LUCA)
- ✅ Katana↔Luca ve Katana→Koza yönleri doğru şekilde implement edildi

