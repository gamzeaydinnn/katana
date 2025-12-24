# Sales Order Approval API Documentation

## Overview

Bu dokümantasyon, satış siparişi onay sürecini açıklar. Yeni implementasyon ile sipariş onaylandığında:

1. Tüm sipariş satırları **tek bir Katana order** olarak gönderilir
2. Her satır, Katana order içinde ayrı bir `sales_order_row` olarak yer alır
3. Sipariş daha sonra Luca'ya tek bir fatura olarak senkronize edilir

## API Endpoint

### POST /api/sales-orders/{id}/approve

Siparişi onaylar ve Katana'ya gönderir.

#### Request

```http
POST /api/sales-orders/123/approve
Authorization: Bearer {token}
```

#### Response (Success)

```json
{
  "success": true,
  "message": "Sipariş onaylandı ve Katana'ya gönderildi",
  "orderNo": "SO-12345",
  "orderStatus": "APPROVED",
  "katanaOrderId": 98765,
  "lineCount": 3,
  "lucaSync": {
    "attempted": true,
    "isSuccess": true,
    "lucaOrderId": 54321,
    "message": "Luca'ya başarıyla senkronize edildi"
  }
}
```

#### Response (Validation Error - 400)

```json
{
  "success": false,
  "message": "Sipariş doğrulama hatası",
  "errors": [
    "Satır 1: SKU eksik",
    "Satır 2 (SKU-001): Geçersiz miktar (0)",
    "Satır 3 (SKU-002): VariantId eksik"
  ],
  "orderNo": "SO-12345"
}
```

#### Response (Duplicate Approval - 400)

```json
{
  "success": false,
  "message": "Bu sipariş zaten onaylanmış",
  "katanaOrderId": 98765
}
```

#### Response (Katana Error - 500)

```json
{
  "success": false,
  "message": "Katana API hatası",
  "error": "Connection timeout"
}
```

## Validation Rules

Sipariş onaylanmadan önce aşağıdaki kontroller yapılır:

1. **Sipariş Durumu**: `APPROVED` veya `SHIPPED` durumundaki siparişler reddedilir
2. **KatanaOrderId**: Zaten KatanaOrderId'si olan siparişler reddedilir
3. **Satır Kontrolü**: En az bir satır olmalı
4. **SKU Kontrolü**: Her satırda SKU olmalı
5. **Miktar Kontrolü**: Her satırda pozitif miktar olmalı
6. **VariantId Kontrolü**: Her satırda VariantId olmalı

## Data Flow

```
1. Admin "Onayla" butonuna tıklar
   ↓
2. Validation kontrolleri yapılır
   ↓
3. Katana order payload oluşturulur:
   {
     order_no: "SO-12345",
     customer_id: 123,
     sales_order_rows: [
       { variant_id: 1, quantity: 10, price_per_unit: 100 },
       { variant_id: 2, quantity: 5, price_per_unit: 150 }
     ]
   }
   ↓
4. Katana API'ye POST /sales_orders gönderilir
   ↓
5. Dönen KatanaOrderId veritabanına kaydedilir:
   - SalesOrder.KatanaOrderId = returned ID
   - SalesOrder.Status = "APPROVED"
   - Tüm SalesOrderLine.KatanaOrderId = same ID
   ↓
6. Luca'ya fatura olarak senkronize edilir (non-blocking)
   ↓
7. Sonuç döndürülür
```

## Database Changes

### SalesOrder Table

| Column        | Type      | Description                    |
| ------------- | --------- | ------------------------------ |
| KatanaOrderId | long      | Katana'daki sipariş ID'si      |
| Status        | string    | APPROVED, NOT_SHIPPED, SHIPPED |
| ApprovedDate  | DateTime? | Onay tarihi                    |
| ApprovedBy    | string?   | Onaylayan kullanıcı            |

### SalesOrderLine Table

| Column        | Type  | Description              |
| ------------- | ----- | ------------------------ |
| KatanaOrderId | long? | Parent order ile aynı ID |
| VariantId     | long? | Katana variant ID        |

## Error Handling

| Error Type         | HTTP Status | Behavior                         |
| ------------------ | ----------- | -------------------------------- |
| Validation Error   | 400         | Sipariş onaylanmaz               |
| Duplicate Approval | 400         | Sipariş onaylanmaz               |
| Katana API Error   | 500         | Sipariş onaylanmaz, rollback     |
| Luca Sync Error    | 200         | Sipariş onaylanır, hata loglanır |

## Luca Sync Behavior

- Luca senkronizasyonu **non-blocking**'dir
- Luca hatası sipariş onayını engellemez
- Hata durumunda `LastSyncError` alanına kaydedilir
- Manuel retry için `/api/sales-orders/{id}/sync` endpoint'i kullanılabilir

## Migration from Old Flow

### Eski Akış (YANLIŞ)

```
Her satır için:
  → SyncProductStockAsync(SKU, Quantity)
  → Ayrı ürün oluşturulur
```

### Yeni Akış (DOĞRU)

```
Tek API çağrısı:
  → CreateSalesOrderAsync(order with all rows)
  → Tek sipariş, N satır
```

## Test Scripts

- `test-order-approval-new.ps1` - Yeni approval flow'u test eder
- `test-duplicate-approval.ps1` - Duplicate approval kontrolünü test eder

## Related Endpoints

- `GET /api/sales-orders` - Sipariş listesi
- `GET /api/sales-orders/{id}` - Sipariş detayı
- `POST /api/sales-orders/{id}/sync` - Manuel Luca senkronizasyonu
- `GET /api/sales-orders/{id}/sync-status` - Senkronizasyon durumu
