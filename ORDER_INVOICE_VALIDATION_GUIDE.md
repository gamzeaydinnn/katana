# ✅ Fatura/Sipariş Doğrulama Rehberi

## 🎯 Amaç
Katana'dan Luca'ya gönderilen siparişlerin/faturaların doğru şekilde senkronize edildiğini kontrol etmek.

## 📊 Doğrulama Yöntemleri

### 1. **API Endpoint ile Doğrulama** (Önerilen)

#### Endpoint: `GET /api/orderinvoicesync/validate`

**Kullanım:**
```bash
curl -X GET "http://localhost:5055/api/orderinvoicesync/validate" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

**Response:**
```json
{
  "success": true,
  "data": {
    "orders": [
      {
        "orderId": 123,
        "orderNo": "SO-2024-001",
        "orderDate": "2024-01-15T10:30:00Z",
        "status": "Confirmed",
        "totalAmount": 1500.00,
        "isSynced": true,
        "lucaInvoiceId": 79409,
        "entityType": "Invoice",
        "mappingCreatedAt": "2024-01-15T11:00:00Z",
        "validationStatus": "✅ VAR"
      }
    ],
    "problematicOrders": [
      {
        "orderId": 456,
        "orderNo": "SO-2024-002",
        "orderDate": "2024-01-16T14:20:00Z",
        "status": "Confirmed",
        "updatedAt": "2024-01-16T15:00:00Z"
      }
    ],
    "statistics": {
      "totalOrders": 100,
      "syncedOrders": 95,
      "mappedOrders": 93,
      "problematicOrders": 2,
      "successRate": 93.0
    },
    "entityTypeDistribution": [
      {
        "entityType": "Invoice",
        "count": 80,
        "firstSync": "2024-01-01T00:00:00Z",
        "lastSync": "2024-01-16T15:00:00Z"
      }
    ],
    "recentLogs": [...]
  }
}
```

#### Duplicate Kontrolü: `GET /api/orderinvoicesync/validate/duplicates`

**Kullanım:**
```bash
curl -X GET "http://localhost:5055/api/orderinvoicesync/validate/duplicates" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 2. **SQL ile Doğrulama**

#### Dosya: `db/validation/check_order_invoice_sync.sql`

**Kullanım:**
```bash
# SQL Server Management Studio veya Azure Data Studio ile çalıştır
# Veya komut satırından:
sqlcmd -S localhost,1433 -U sa -P "Admin00!S" -d KatanaDB -i db/validation/check_order_invoice_sync.sql
```

**Temel Sorgular:**

#### 1. Genel Durum Kontrolü
```sql
SELECT 
    o.Id AS KatanaOrderId,
    o.OrderNo AS KatanaOrderNo,
    o.OrderDate,
    o.Status,
    o.IsSynced AS KatanaSyncFlag,
    om.LucaInvoiceId,
    CASE 
        WHEN om.LucaInvoiceId IS NOT NULL THEN '✅ VAR'
        WHEN o.IsSynced = 1 THEN '⚠️ SYNC FLAG VAR AMA MAPPING YOK'
        ELSE '❌ YOK'
    END AS LucaDurum
FROM Orders o
LEFT JOIN OrderMappings om ON o.Id = om.OrderId
WHERE o.Status IN ('Confirmed', 'Completed', 'Shipped')
ORDER BY o.OrderDate DESC;
```

#### 2. Sorunlu Siparişler
```sql
-- Sync edilmiş ama mapping olmayan
SELECT 
    o.Id,
    o.OrderNo,
    o.OrderDate,
    o.Status,
    o.IsSynced
FROM Orders o
LEFT JOIN OrderMappings om ON o.Id = om.OrderId
WHERE o.IsSynced = 1 
  AND om.LucaInvoiceId IS NULL;
```

#### 3. İstatistikler
```sql
SELECT 
    COUNT(*) AS TotalOrders,
    SUM(CASE WHEN o.IsSynced = 1 THEN 1 ELSE 0 END) AS SyncedOrders,
    SUM(CASE WHEN om.LucaInvoiceId IS NOT NULL THEN 1 ELSE 0 END) AS MappedOrders,
    SUM(CASE WHEN o.IsSynced = 1 AND om.LucaInvoiceId IS NULL THEN 1 ELSE 0 END) AS ProblematicOrders
FROM Orders o
LEFT JOIN OrderMappings om ON o.Id = om.OrderId
WHERE o.Status IN ('Confirmed', 'Completed', 'Shipped');
```

### 3. **Log Dosyası Kontrolü**

#### Script: `scripts/check_order_sync_logs.sh`

**Kullanım:**
```bash
./scripts/check_order_sync_logs.sh
```

**Çıktı:**
```
🔍 Fatura/Sipariş Sync Log Kontrolü Başlatılıyor...
==================================================

📊 1. ORDER/INVOICE Hata Sayısı
--------------------------------
ORDER hataları: 5
INVOICE hataları: 2

📊 2. Son 10 ORDER Hatası
--------------------------------
[2024-01-16 15:30:00] ERROR: Order SO-2024-002 failed: Session expired
...

📊 3. Başarılı ORDER Sync Sayısı (Son 24 saat)
--------------------------------
Başarılı sync: 93

📊 4. Duplicate Uyarıları
--------------------------------
Duplicate uyarı sayısı: 3
...
```

**Manuel Log Kontrolü:**
```bash
# ORDER hataları
grep -i "ORDER.*ERROR" logs/luca-raw.log

# INVOICE hataları
grep -i "INVOICE.*ERROR" logs/luca-raw.log

# Tüm hatalar
grep -i "ERROR\|FAIL" logs/luca-raw.log | tail -50

# Başarılı sync'ler
grep -i "ORDER.*SUCCESS\|Successfully sent.*order" logs/luca-raw.log | tail -20
```

## 🔍 Doğrulama Senaryoları

### ✅ Senaryo 1: Tüm Siparişler Sync Edilmiş
```
TotalOrders: 100
SyncedOrders: 100
MappedOrders: 100
ProblematicOrders: 0
SuccessRate: 100%

Durum: ✅ Mükemmel - Tüm siparişler Luca'da
```

### ⚠️ Senaryo 2: Bazı Siparişler Mapping'siz
```
TotalOrders: 100
SyncedOrders: 95
MappedOrders: 93
ProblematicOrders: 2
SuccessRate: 93%

Durum: ⚠️ Dikkat - 2 sipariş sync flag'i var ama mapping yok
Aksiyon: Problematic orders listesini kontrol et
```

### ❌ Senaryo 3: Çok Sayıda Hata
```
TotalOrders: 100
SyncedOrders: 50
MappedOrders: 45
ProblematicOrders: 5
SuccessRate: 45%

Durum: ❌ Sorunlu - Sync başarı oranı düşük
Aksiyon: 
1. Log dosyalarını kontrol et
2. Session/Auth sorunlarını kontrol et
3. Luca API erişimini test et
```

## 🛠️ Sorun Giderme

### Problem 1: Sync Flag Var Ama Mapping Yok

**Tespit:**
```sql
SELECT * FROM Orders o
LEFT JOIN OrderMappings om ON o.Id = om.OrderId
WHERE o.IsSynced = 1 AND om.LucaInvoiceId IS NULL;
```

**Olası Nedenler:**
1. Luca'ya gönderim başarılı ama response parse edilemedi
2. Transaction rollback oldu
3. Mapping kaydı oluşturulamadı

**Çözüm:**
```bash
# 1. Log'larda ilgili order'ı ara
grep "OrderNo: SO-2024-002" logs/luca-raw.log

# 2. Manuel mapping oluştur (gerekirse)
curl -X POST "http://localhost:5055/api/orderinvoicesync/manual-mapping" \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": 456,
    "lucaInvoiceId": 79410,
    "entityType": "Invoice"
  }'
```

### Problem 2: Duplicate Mapping

**Tespit:**
```bash
curl -X GET "http://localhost:5055/api/orderinvoicesync/validate/duplicates"
```

**Çözüm:**
```sql
-- En son mapping'i tut, diğerlerini sil
DELETE FROM OrderMappings
WHERE Id NOT IN (
    SELECT MAX(Id)
    FROM OrderMappings
    GROUP BY OrderId
);
```

### Problem 3: Session Expired Hataları

**Tespit:**
```bash
grep -i "session.*expired\|unauthorized" logs/luca-raw.log
```

**Çözüm:**
1. Luca session cookie'sini yenile
2. `ManualSessionCookie` ayarını güncelle
3. Auth mekanizmasını kontrol et

### Problem 4: HTTP 4xx/5xx Hataları

**Tespit:**
```bash
grep -i "HTTP [45][0-9][0-9]" logs/luca-raw.log | tail -20
```

**Çözüm:**
- 400 Bad Request: Request payload'ını kontrol et
- 401 Unauthorized: Auth token'ı yenile
- 404 Not Found: Endpoint URL'ini kontrol et
- 500 Internal Server Error: Luca API'yi kontrol et

## 📈 Monitoring ve Alerting

### Günlük Kontroller
```bash
# Cron job ile günlük kontrol
0 9 * * * /path/to/scripts/check_order_sync_logs.sh > /var/log/order-sync-daily.log 2>&1
```

### Metrikler
- **Success Rate**: > 95% olmalı
- **Problematic Orders**: < 5 olmalı
- **Response Time**: < 5 saniye olmalı
- **Error Rate**: < 2% olmalı

### Alert Koşulları
```bash
# Success rate < 90%
if [ "$SUCCESS_RATE" -lt 90 ]; then
    echo "⚠️ ALERT: Success rate düşük: $SUCCESS_RATE%"
    # Send notification
fi

# Problematic orders > 10
if [ "$PROBLEMATIC_COUNT" -gt 10 ]; then
    echo "⚠️ ALERT: Çok fazla sorunlu sipariş: $PROBLEMATIC_COUNT"
    # Send notification
fi
```

## 🎯 Best Practices

1. **Günlük Doğrulama**: Her gün en az 1 kez validation endpoint'ini çağır
2. **Log Rotation**: Log dosyalarını düzenli temizle (7 gün retention)
3. **Backup**: OrderMappings tablosunu düzenli yedekle
4. **Monitoring**: Grafana/Prometheus ile metrik toplama
5. **Alerting**: Slack/Email ile otomatik bildirim

## 📝 Checklist

- [ ] API validation endpoint çalışıyor mu?
- [ ] SQL sorguları doğru sonuç veriyor mu?
- [ ] Log dosyaları okunabilir mi?
- [ ] Success rate > 95% mi?
- [ ] Problematic orders < 5 mi?
- [ ] Duplicate mapping var mı?
- [ ] Session/Auth hataları var mı?
- [ ] HTTP hataları var mı?

## 🔗 İlgili Dosyalar

- API Controller: `src/Katana.API/Controllers/OrderInvoiceSyncController.cs`
- SQL Queries: `db/validation/check_order_invoice_sync.sql`
- Log Check Script: `scripts/check_order_sync_logs.sh`
- Entity Models: `src/Katana.Core/Entities/Order.cs`, `src/Katana.Data/Models/OrderMapping.cs`

## 📞 Destek

Sorun devam ederse:
1. Log dosyalarını incele: `logs/luca-raw.log`
2. Database'i kontrol et: OrderMappings tablosu
3. API endpoint'i test et: `/api/orderinvoicesync/validate`
4. Luca API erişimini test et: `/api/luca/health`
