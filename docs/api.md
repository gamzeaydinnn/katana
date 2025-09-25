# API Dokümantasyonu

## Genel Bakış

Katana-Luca Integration API, Katana MRP/ERP sistemi ile Luca Koza muhasebe sistemi arasında veri entegrasyonu sağlar.

## Kimlik Doğrulama

API, JWT Bearer token kimlik doğrulaması kullanır.

### Token Alma

```http
POST /api/auth/token
Content-Type: application/json

{
  "username": "your-username",
  "password": "your-password"
}
```

### Token Kullanımı

```http
Authorization: Bearer <your-jwt-token>
```

## API Endpoints

### Sync Controller

#### Complete Sync
Tüm veri tiplerini senkronize eder.

```http
POST /api/sync/run?fromDate=2024-01-01
Authorization: Bearer <token>
```

#### Stock Sync
Sadece stok verilerini senkronize eder.

```http
POST /api/sync/stock?fromDate=2024-01-01
Authorization: Bearer <token>
```

#### Invoice Sync
Sadece fatura verilerini senkronize eder.

```http
POST /api/sync/invoices?fromDate=2024-01-01
Authorization: Bearer <token>
```

#### Customer Sync
Sadece müşteri verilerini senkronize eder.

```http
POST /api/sync/customers?fromDate=2024-01-01
Authorization: Bearer <token>
```

#### Sync Status
Tüm senkronizasyon durumlarını getirir.

```http
GET /api/sync/status
Authorization: Bearer <token>
```

**Response:**
```json
[
  {
    "syncType": "STOCK",
    "lastSyncTime": "2024-01-15T10:30:00Z",
    "isRunning": false,
    "currentStatus": "SUCCESS",
    "pendingRecords": 5,
    "nextScheduledSync": "2024-01-15T16:30:00Z"
  }
]
```

### Reports Controller

#### Integration Logs
Entegrasyon loglarını getirir.

```http
GET /api/reports/logs?page=1&pageSize=50&syncType=STOCK&status=SUCCESS
Authorization: Bearer <token>
```

#### Last Sync Reports
Her senkronizasyon tipi için son raporu getirir.

```http
GET /api/reports/last
Authorization: Bearer <token>
```

#### Failed Records
Başarısız kayıtları getirir.

```http
GET /api/reports/failed?page=1&pageSize=50&recordType=INVOICE
Authorization: Bearer <token>
```

#### Statistics
Senkronizasyon istatistiklerini getirir.

```http
GET /api/reports/statistics?days=7
Authorization: Bearer <token>
```

### Mapping Controller

#### Get All Mappings
Tüm mapping kayıtlarını getirir.

```http
GET /api/mapping?mappingType=SKU_ACCOUNT&page=1&pageSize=50
Authorization: Bearer <token>
```

#### Get SKU Account Mappings
SKU - hesap kodu eşleştirmelerini getirir.

```http
GET /api/mapping/sku-accounts
Authorization: Bearer <token>
```

#### Get Location Mappings
Lokasyon - depo eşleştirmelerini getirir.

```http
GET /api/mapping/locations
Authorization: Bearer <token>
```

#### Create Mapping
Yeni mapping kaydı oluşturur.

```http
POST /api/mapping
Authorization: Bearer <token>
Content-Type: application/json

{
  "mappingType": "SKU_ACCOUNT",
  "sourceValue": "PRD-123",
  "targetValue": "600.10",
  "description": "Mobile Phone Category",
  "isActive": true
}
```

#### Update Mapping
Mevcut mapping kaydını günceller.

```http
PUT /api/mapping/1
Authorization: Bearer <token>
Content-Type: application/json

{
  "targetValue": "600.11",
  "description": "Updated Mobile Phone Category",
  "isActive": true
}
```

#### Delete Mapping
Mapping kaydını siler.

```http
DELETE /api/mapping/1
Authorization: Bearer <token>
```

## Response Formatları

### Başarılı Response
```json
{
  "isSuccess": true,
  "syncType": "STOCK",
  "message": "Stock sync completed successfully",
  "processedRecords": 150,
  "successfulRecords": 148,
  "failedRecords": 2,
  "errors": [],
  "syncTime": "2024-01-15T10:30:00Z",
  "duration": "00:02:15"
}
```

### Hata Response
```json
{
  "message": "An error occurred while processing your request.",
  "details": "Connection timeout to Katana API",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## HTTP Status Codes

- `200 OK` - İşlem başarılı
- `201 Created` - Kaynak oluşturuldu
- `400 Bad Request` - Geçersiz istek
- `401 Unauthorized` - Kimlik doğrulama hatası
- `404 Not Found` - Kaynak bulunamadı
- `409 Conflict` - Çakışma hatası
- `500 Internal Server Error` - Sunucu hatası

## Rate Limiting

API'de rate limiting uygulanmaktadır:
- Dakikada maksimum 100 istek
- Saatte maksimum 1000 istek

## Health Check

Sistem durumunu kontrol etmek için:

```http
GET /health
```

Response:
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:01.234",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.567"
    }
  }
}
```