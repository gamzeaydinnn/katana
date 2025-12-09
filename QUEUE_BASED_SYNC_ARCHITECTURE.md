# Katana Queue-Based Sync Architecture

## 🎯 Özet

Bu güncelleme ile sync işlemleri **senkron (blocking)** yapıdan **asenkron queue-based** yapıya taşındı.

### Temel Değişiklikler:

1. **Hangfire** ile job queue sistemi
2. **Redis** ile persistent cache (session restart'larda bile veri korunur)
3. **Paralel batch processing** (5 batch aynı anda işlenir)
4. **Retry policy** (başarısız batch'ler otomatik tekrar denenir)

---

## 🏗️ Mimari

```
┌─────────────────┐
│  POST /api/sync │
│      /start     │  ← User request (returns JobId)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Hangfire Queue │  ← Job kuyruğa eklenir
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   SyncWorker    │  ← Background'da işlenir
│  (5 paralel)    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Redis Cache    │  ← SKU → StockCardId mapping
│  (Persistent)   │
└─────────────────┘
```

---

## 📦 Yeni Bileşenler

### 1. **IStockCardCache** (Redis Cache)

**Lokasyon:** `Katana.Business/Interfaces/IStockCardCache.cs`

Stok kartı cache'i artık Redis'te tutuluyor. Session restart olsa bile veri korunur.

```csharp
public interface IStockCardCache
{
    Task<long?> GetStockCardIdAsync(string sku);
    Task SetStockCardIdAsync(string sku, long stockCardId);
    Task<bool> IsCacheWarmedAsync();
    Task WarmupCacheAsync(Dictionary<string, long> stockCards);
}
```

### 2. **RedisStockCardCache** (Implementation)

**Lokasyon:** `Katana.Infrastructure/Caching/RedisStockCardCache.cs`

- 7 gün TTL (sliding: 1 gün)
- Thread-safe operations
- Bulk get/set desteği

### 3. **ISyncWorker** (Background Worker Interface)

**Lokasyon:** `Katana.Business/Interfaces/ISyncWorker.cs`

```csharp
public interface ISyncWorker
{
    Task<SyncResultDto> ProcessStockCardsAsync(int? limit, bool dryRun);
    Task<SyncResultDto> ProcessCustomersAsync(int? limit, bool dryRun);
    Task<SyncResultDto> ProcessInvoicesAsync(int? limit, bool dryRun);
}
```

### 4. **SyncWorker** (Worker Implementation)

**Lokasyon:** `Katana.Infrastructure/Workers/SyncWorker.cs`

**Özellikler:**

- ✅ Paralel batch processing (MaxDegreeOfParallelism=5)
- ✅ Batch size: 20 (configurable)
- ✅ Retry policy: 3 deneme, exponential backoff
- ✅ Thread-safe counter'lar (ConcurrentBag, Interlocked)
- ✅ Detailed logging

**Akış:**

1. Cache'i warmup et (Luca'dan tüm stock card'ları çek)
2. Katana'dan ürünleri getir
3. 20'lik batch'lere ayır
4. 5 batch'i paralel işle
5. Her batch kendi retry policy'sine sahip

---

## 🔧 Konfigürasyon

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=db;Database=KatanaDB;User=sa;Password=***;",
    "HangfireConnection": "Server=db;Database=KatanaDB;User=sa;Password=***;",
    "Redis": "localhost:6379,abortConnect=false"
  }
}
```

### Docker Compose (Redis Ekleme)

```yaml
services:
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes

  katana-api:
    depends_on:
      - redis
      - db

volumes:
  redis-data:
```

---

## 🚀 Kullanım

### 1. Sync Job Başlatma (Async)

```bash
POST /api/sync/start
{
  "syncType": "PRODUCT",
  "limit": 100,
  "dryRun": false
}
```

**Response:**

```json
{
  "success": true,
  "message": "Sync job queued successfully: PRODUCT",
  "jobId": "12345",
  "syncType": "PRODUCT",
  "syncMode": "async",
  "dashboardUrl": "/hangfire/jobs/details/12345"
}
```

### 2. Job Status Kontrolü

```bash
GET /api/sync/job/{jobId}
```

**Response:**

```json
{
  "jobId": "12345",
  "state": "Processing",
  "createdAt": "2025-12-07T19:00:00Z",
  "job": "ProcessStockCardsAsync",
  "history": [
    {
      "stateName": "Enqueued",
      "createdAt": "2025-12-07T19:00:00Z"
    }
  ]
}
```

### 3. Hangfire Dashboard

Tarayıcıda: `http://localhost:5055/hangfire`

- Aktif job'ları görüntüle
- Job geçmişini incele
- Retry failed jobs
- Job queue monitoring

---

## 📊 Performans İyileştirmeleri

### Önceki Mimari (Senkron)

- 50 ürün → **5+ dakika**
- Her ürün için session refresh
- Duplicate API calls
- Blocking operation

### Yeni Mimari (Queue + Paralel)

- 50 ürün → **<1 dakika** (beklenen)
- Cache warmup 1 kez
- 5 batch paralel
- Non-blocking (API hemen response döner)

### Paralel İşleme Örneği

100 ürün → 5 batch (20'şer ürün):

```
Batch 1 (20 ürün)  ─┐
Batch 2 (20 ürün)  ─┼─→ Paralel (5 thread)
Batch 3 (20 ürün)  ─┤
Batch 4 (20 ürün)  ─┤
Batch 5 (20 ürün)  ─┘
```

---

## 🛠️ Geliştirme Notları

### Cache Warmup

```csharp
// Cache warmup artık Redis'e yazıyor
var cacheWarmed = await _lucaService.WarmupCacheWithRetryAsync(3);

// Cache status kontrolü
var (isHealthy, count, status) = await _stockCardCache.GetCacheStatusAsync();
```

### Retry Policy

```csharp
// Polly ile exponential backoff
_retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(
        3,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (exception, timeSpan, retryCount, context) =>
        {
            _logger.LogWarning("Retry {RetryCount}/3 after {Delay}s",
                retryCount, timeSpan.TotalSeconds);
        });
```

### Thread-Safe Counter

```csharp
// Interlocked for atomic operations
System.Threading.Interlocked.Add(ref successCount, batchResult.SuccessCount);
System.Threading.Interlocked.Add(ref failedCount, batchResult.FailedCount);

// ConcurrentBag for errors
var errors = new ConcurrentBag<string>();
errors.Add(errorMsg);
```

---

## 🔍 Debugging

### Hangfire Logs

```bash
docker logs katana-api-1 | grep Hangfire
```

### Redis Cache Kontrolü

```bash
docker exec -it katana-redis-1 redis-cli

> KEYS luca:stockcard:*
> GET luca:stockcard:SKU123
> GET luca:stockcard:count
```

### Sync Worker Logs

```bash
docker logs katana-api-1 | grep "🚀\|✅\|❌\|⚡"
```

---

## 📚 API Endpoints

| Endpoint                | Method | Açıklama                |
| ----------------------- | ------ | ----------------------- |
| `/api/sync/start`       | POST   | Sync job başlat (async) |
| `/api/sync/job/{jobId}` | GET    | Job durumunu getir      |
| `/api/sync/history`     | GET    | Sync geçmişi            |
| `/hangfire`             | GET    | Hangfire dashboard      |

---

## ⚙️ Configuration Parameters

### SyncWorker

```csharp
private const int BATCH_SIZE = 20;              // Batch başına ürün sayısı
private const int MAX_DEGREE_OF_PARALLELISM = 5; // Paralel batch sayısı
private const int MAX_RETRY_ATTEMPTS = 3;        // Retry deneme sayısı
```

### Redis Cache

```csharp
AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7);  // 7 gün TTL
SlidingExpiration = TimeSpan.FromDays(1);                // 1 gün sliding
```

### Hangfire

```csharp
WorkerCount = 5;                                 // Max 5 paralel worker
QueuePollInterval = TimeSpan.FromSeconds(15);   // Queue polling interval
Queues = new[] { "default", "sync", "critical" }; // Queue priority
```

---

## 🎓 Örnek Senaryolar

### Senaryo 1: 1000 Ürün Sync

```bash
POST /api/sync/start
{
  "syncType": "PRODUCT",
  "limit": 1000,
  "dryRun": false
}
```

**Beklenen Süre:** ~5-10 dakika (50 batch × 20 ürün, 5 paralel)

### Senaryo 2: Dry Run Test

```bash
POST /api/sync/start
{
  "syncType": "PRODUCT",
  "limit": 50,
  "dryRun": true  # Gerçek API call yok, sadece simülasyon
}
```

### Senaryo 3: Cache Warmup Kontrolü

```csharp
var status = await _stockCardCache.GetCacheStatusAsync();
Console.WriteLine($"Cache: {status.Status}");
// Output: "Cache: Healthy: 1234 entries cached"
```

---

## 🚨 Troubleshooting

### Problem: Job Enqueue Edilemiyor

**Çözüm:**

```bash
# Hangfire DB'yi kontrol et
docker exec -it katana-db-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -C -d KatanaDB -Q "SELECT * FROM Hangfire.Job ORDER BY Id DESC;"
```

### Problem: Redis Bağlantı Hatası

**Çözüm:**

```bash
# Redis'in çalıştığını kontrol et
docker ps | grep redis

# Redis connection test
docker exec -it katana-redis-1 redis-cli PING
```

### Problem: Paralel Batch'ler Çalışmıyor

**Çözüm:**

```csharp
// Hangfire worker count'ı artır
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 10; // 5 → 10
});
```

---

## 📝 Migration Checklist

- [x] Hangfire NuGet paketleri eklendi
- [x] Redis NuGet paketleri eklendi
- [x] IStockCardCache interface oluşturuldu
- [x] RedisStockCardCache implementation
- [x] ISyncWorker interface
- [x] SyncWorker paralel batch engine
- [x] LucaService Redis cache entegrasyonu
- [x] Program.cs Hangfire configuration
- [x] Program.cs Redis configuration
- [x] SyncController queue-based endpoints
- [x] Polly retry policy eklendi
- [ ] Docker Compose Redis service ekle
- [ ] appsettings.json Redis connection string
- [ ] Integration test yazılacak

---

## 🔗 Kaynaklar

- [Hangfire Documentation](https://docs.hangfire.io)
- [Redis StackExchange](https://stackexchange.github.io/StackExchange.Redis/)
- [Polly Retry Policies](https://github.com/App-vNext/Polly)
- [Parallel.ForEachAsync](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.parallel.foreachasync)

---

**Son Güncelleme:** 2025-12-07  
**Versiyon:** 2.0.0 - Queue-Based Sync Architecture
