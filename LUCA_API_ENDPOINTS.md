# Luca API Endpoints & Usage Examples

## Stock Card Sync Endpoint

### Start Sync
```
POST /api/sync/start
Content-Type: application/json

{
  "syncType": "STOCK_CARD"
}
```

**Response**:
```json
{
  "syncType": "PRODUCT_STOCK_CARD",
  "processedRecords": 150,
  "successfulRecords": 145,
  "failedRecords": 2,
  "skippedRecords": 3,
  "isSuccess": true,
  "message": "Sync completed: 145 successful, 2 failed, 3 skipped",
  "errors": ["PRD-002: Error message", "PRD-003: Error message"]
}
```

## Stock Card Creation (Direct)

### Create Single Stock Card
```
POST /api/luca/stock-cards
Content-Type: application/json

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
  "satilabilirFlag": 1,
  "satinAlinabilirFlag": 1,
  "lotNoFlag": 1,
  "minStokKontrol": 0,
  "maliyetHesaplanacakFlag": true
}
```

**Response**:
```json
{
  "skartId": 79409,
  "error": false,
  "message": "00013225 - Test Ürünü stok kartı başarılı bir şekilde kaydedilmiştir."
}
```

## Stock Card Listing

### List All Stock Cards
```
POST /api/luca/stock-cards/list
Content-Type: application/json

{}
```

**Response**:
```json
{
  "list": [
    {
      "skartId": 79409,
      "kod": "00013225",
      "kartAdi": "Test Ürünü",
      "kartTuru": 1,
      "olcumBirimiId": 1,
      "kartAlisKdvOran": 1,
      "kartSatisKdvOran": 1,
      "kategoriAgacKod": "001.001",
      "barkod": "8888888",
      "perakendeSatisBirimFiyat": 100.00,
      "perakendeAlisBirimFiyat": 50.00
    }
  ]
}
```

## Debugging Endpoints

### Check Luca Connection
```
GET /api/luca/health
```

### Get Session Status
```
GET /api/luca/session-status
```

**Response**:
```json
{
  "isAuthenticated": true,
  "sessionCookie": "JSESSIONID=ABC123...",
  "cookieExpiry": "2025-12-04T14:30:45Z",
  "branchId": 1,
  "lastAuthAt": "2025-12-04T13:30:45Z"
}
```

## Error Responses

### 401 Unauthorized
```json
{
  "error": true,
  "message": "Session expired or invalid credentials"
}
```

### 409 Conflict (Duplicate)
```json
{
  "error": true,
  "message": "Kart kodu daha önce kullanılmış"
}
```

### 500 Server Error
```json
{
  "error": true,
  "message": "Internal server error"
}
```

## cURL Examples

### Create Stock Card
```bash
curl -X POST http://localhost:5000/api/luca/stock-cards \
  -H "Content-Type: application/json" \
  -d '{
    "kartAdi": "Test Ürünü",
    "kartKodu": "00013225",
    "kartTipi": 1,
    "kartAlisKdvOran": 1,
    "olcumBirimiId": 1,
    "baslangicTarihi": "06/04/2022",
    "kartTuru": 1,
    "kategoriAgacKod": null,
    "barkod": "8888888",
    "satilabilirFlag": 1,
    "satinAlinabilirFlag": 1,
    "lotNoFlag": 1,
    "minStokKontrol": 0,
    "maliyetHesaplanacakFlag": true
  }'
```

### Start Sync
```bash
curl -X POST http://localhost:5000/api/sync/start \
  -H "Content-Type: application/json" \
  -d '{"syncType": "STOCK_CARD"}'
```

### List Stock Cards
```bash
curl -X POST http://localhost:5000/api/luca/stock-cards/list \
  -H "Content-Type: application/json" \
  -d '{}'
```

## PowerShell Examples

### Create Stock Card
```powershell
$body = @{
    kartAdi = "Test Ürünü"
    kartKodu = "00013225"
    kartTipi = 1
    kartAlisKdvOran = 1
    olcumBirimiId = 1
    baslangicTarihi = "06/04/2022"
    kartTuru = 1
    kategoriAgacKod = $null
    barkod = "8888888"
    satilabilirFlag = 1
    satinAlinabilirFlag = 1
    lotNoFlag = 1
    minStokKontrol = 0
    maliyetHesaplanacakFlag = $true
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/luca/stock-cards" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body
```

### Start Sync
```powershell
$body = @{
    syncType = "STOCK_CARD"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/sync/start" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body
```

