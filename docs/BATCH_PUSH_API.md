# Luca Batch Push API Kullanım Kılavuzu

## 🎯 Genel Bakış

Bu sistem, Luca'ya toplu ürün gönderimini arka planda (background) ve **paralel** olarak işleyerek:

- **Timeout riski yok** - İşlem arka planda devam eder
- **10x hız artışı** - 10 paralel thread ile eş zamanlı gönderim
- **Kullanıcı beklemez** - 202 Accepted ile anında yanıt
- **Gerçek zamanlı ilerleme** - SignalR ile canlı progress bar
- **İptal edilebilir** - İstediğiniz zaman durdurabilirsiniz
- **Luca'yı yormaz** - SemaphoreSlim ile kontrollü paralel işlem

## ⚡ Performans

| Senaryo       | Eski Sistem          | Yeni Sistem            |
| ------------- | -------------------- | ---------------------- |
| 1142 ürün     | ~19 dakika (tek tek) | ~2 dakika (10 paralel) |
| CPU kullanımı | Yüksek (bekleme)     | Düşük (async)          |
| Timeout riski | Yüksek               | Yok                    |
| Hız           | ~1 ürün/sn           | ~10 ürün/sn            |

## 📡 API Endpoint'leri

### 1. Toplu Ürün Gönderimi Başlat

```http
POST /api/luca/push-products-batch
Content-Type: application/json
Authorization: Bearer {token}

{
    "productIds": [1, 2, 3],       // Opsiyonel - boş ise tüm ürünler
    "batchSize": 100,              // Varsayılan: 100
    "delayBetweenBatchesMs": 1000, // Batch arası bekleme (ms)
    "onlyUpdated": false,          // Sadece güncellenmiş ürünler
    "updatedWithinHours": 24       // Son X saat (onlyUpdated=true ise)
}
```

**Yanıt (202 Accepted):**

```json
{
  "jobId": "batch_20251202103000_abc12345",
  "message": "Batch job başarıyla oluşturuldu. 1142 ürün 12 batch halinde işlenecek.",
  "totalProducts": 1142,
  "totalBatches": 12,
  "batchSize": 100,
  "createdAt": "2025-12-02T10:30:00Z",
  "statusUrl": "/api/luca/batch-status/batch_20251202103000_abc12345"
}
```

### 2. Job Durumu Sorgula

```http
GET /api/luca/batch-status/{jobId}
```

**Yanıt:**

```json
{
  "jobId": "batch_20251202103000_abc12345",
  "status": "InProgress",
  "jobType": "ProductPush",
  "totalItems": 1142,
  "processedItems": 450,
  "successfulItems": 448,
  "failedItems": 2,
  "currentBatch": 5,
  "totalBatches": 12,
  "progressPercentage": 39.4,
  "createdAt": "2025-12-02T10:30:00Z",
  "startedAt": "2025-12-02T10:30:01Z",
  "estimatedTimeRemaining": "00:08:30",
  "errors": [],
  "failedItemDetails": [
    {
      "itemId": 123,
      "itemCode": "SKU-123",
      "itemName": "Ürün Adı",
      "success": false,
      "errorMessage": "Kategori eşleşmesi bulunamadı",
      "processedAt": "2025-12-02T10:32:15Z"
    }
  ]
}
```

### 3. Aktif Job'ları Listele

```http
GET /api/luca/batch-jobs
Authorization: Bearer {token}
```

### 4. Job'u İptal Et

```http
POST /api/luca/batch-cancel/{jobId}
Authorization: Bearer {token}
Content-Type: application/json

{
    "reason": "Yanlış ürünler seçildi"
}
```

### 5. Önizleme (Preview)

```http
GET /api/luca/preview-push?onlyUpdated=false&updatedWithinHours=24
```

**Yanıt:**

```json
{
  "totalProducts": 1142,
  "estimatedBatches": 12,
  "estimatedTimeMinutes": 18.0,
  "batchSize": 100
}
```

### 6. Tek Ürün Gönder (Test)

```http
POST /api/luca/push-product/{id}
Authorization: Bearer {token}
```

### 7. Bağlantı Testi

```http
GET /api/luca/test-connection
```

## 🔔 SignalR Bildirimleri

Hub URL: `/hubs/notifications`

### Event: `BatchJobProgress`

```javascript
connection.on("BatchJobProgress", (data) => {
  console.log(`Job ${data.jobId}: ${data.message} (${data.progress}%)`);

  // Detaylı ilerleme bilgisi
  if (data.details) {
    console.log(`Hız: ${data.details.itemsPerSecond}/sn`);
    console.log(`Kalan süre: ${data.details.estimatedSecondsRemaining} sn`);
    console.log(`Başarılı: ${data.details.successfulItems}`);
    console.log(`Başarısız: ${data.details.failedItems}`);
  }

  // data.status: "InProgress", "Completed", "Failed", "Cancelled", "PartiallyCompleted"
});
```

### SignalR Bağlantı Örneği (JavaScript)

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/notifications", {
    accessTokenFactory: () => localStorage.getItem("token"),
  })
  .withAutomaticReconnect()
  .build();

connection.on("BatchJobProgress", (data) => {
  updateProgressBar(data.progress);
  updateStatusMessage(data.message);

  if (data.details) {
    updateStats({
      processed: data.details.processedItems,
      total: data.details.totalItems,
      speed: data.details.itemsPerSecond,
      eta: data.details.estimatedSecondsRemaining,
    });
  }
});

await connection.start();
```

## 📊 Job Durumları

| Durum                | Açıklama                                 |
| -------------------- | ---------------------------------------- |
| `Pending`            | Kuyrukta bekliyor                        |
| `InProgress`         | İşleniyor                                |
| `Completed`          | Başarıyla tamamlandı                     |
| `Failed`             | Tamamen başarısız                        |
| `PartiallyCompleted` | Kısmen başarılı (bazı ürünler hata aldı) |
| `Cancelled`          | Kullanıcı tarafından iptal edildi        |

## 🚀 Örnek Kullanım Senaryoları

### Tüm Ürünleri Gönder

```bash
curl -X POST "https://api.example.com/api/luca/push-products-batch" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}'
```

### Son 24 Saatte Güncellenen Ürünleri Gönder

```bash
curl -X POST "https://api.example.com/api/luca/push-products-batch" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"onlyUpdated": true, "updatedWithinHours": 24}'
```

### Belirli Ürünleri Gönder

```bash
curl -X POST "https://api.example.com/api/luca/push-products-batch" \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"productIds": [1, 2, 3, 4, 5]}'
```

## ⚙️ Yapılandırma

### Request Parametreleri

- `batchSize`: 1-500 arası (varsayılan: 100)
- `delayBetweenBatchesMs`: 0-10000 ms arası (varsayılan: 1000)

### Paralel İşlem Ayarları (Worker)

- `MaxParallelism`: 10 (Luca API'yi yormadan optimum değer)
- `ProgressNotifyInterval`: 10 (Her 10 üründe bir SignalR bildirimi)

### Nginx Timeout Ayarları (Production)

```nginx
# /etc/nginx/sites-available/katana
location /api/ {
    proxy_pass http://localhost:5000;
    proxy_read_timeout 300;
    proxy_send_timeout 300;
    proxy_connect_timeout 60;

    # WebSocket desteği (SignalR için)
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
}

location /hubs/ {
    proxy_pass http://localhost:5000;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_read_timeout 86400;  # 24 saat (SignalR bağlantısı için)
}
```

## 🔧 Mimari

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  LucaController │────▶│  BatchJobService │────▶│ In-Memory Queue │
└─────────────────┘     └──────────────────┘     └────────┬────────┘
                                                          │
                        ┌─────────────────────────────────┘
                        │
                        ▼
┌─────────────────┐     ┌──────────────────────────────────────┐
│   SignalR Hub   │◀────│     LucaBatchPushWorker              │
│  (Notifications)│     │  ┌─────────────────────────────────┐ │
└─────────────────┘     │  │   SemaphoreSlim (MaxParallel=10) │ │
                        │  ├─────────────────────────────────┤ │
                        │  │ Thread 1 ──▶ Luca API           │ │
                        │  │ Thread 2 ──▶ Luca API           │ │
                        │  │ Thread 3 ──▶ Luca API           │ │
                        │  │    ...                          │ │
                        │  │ Thread 10 ──▶ Luca API          │ │
                        │  └─────────────────────────────────┘ │
                        └──────────────────────────────────────┘
```

## 📁 Oluşturulan Dosyalar

| Dosya                                                | Açıklama                        |
| ---------------------------------------------------- | ------------------------------- |
| `src/Katana.Core/DTOs/BatchDtos.cs`                  | Batch işlem DTO'ları            |
| `src/Katana.Business/Interfaces/IBatchJobService.cs` | Servis interface'i              |
| `src/Katana.Business/Services/BatchJobService.cs`    | Job yönetim servisi (Singleton) |
| `src/Katana.API/Workers/LucaBatchPushWorker.cs`      | Paralel background worker       |
| `src/Katana.API/Controllers/LucaController.cs`       | API endpoint'leri               |

## 🔒 Yetkilendirme

- `push-products-batch`: Admin, StokYonetici rolleri gerekli
- `batch-status/{jobId}`: Herkese açık (AllowAnonymous)
- `batch-jobs`: Admin, StokYonetici rolleri gerekli
- `batch-cancel/{jobId}`: Admin, StokYonetici rolleri gerekli

## 🛡️ Hata Yönetimi

- **Tek batch hatası**: Diğer batch'ler çalışmaya devam eder
- **Cookie expire**: Her batch için yeni scope oluşturulur
- **Network timeout**: Batch bazında retry yapılabilir
- **Luca 1001/1003 hataları**: FailedItemDetails'te detaylı kayıt
