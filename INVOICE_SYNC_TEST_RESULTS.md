# Luca Fatura Sync Test Sonuçları

## Test Tarihi

11 Aralık 2025 - 10:52

## Test Edilen Özellik

Satınalma siparişlerinin Luca'ya fatura olarak gönderilmesi

## Test Sonuçları

### ❌ Fatura Sync Başarısız

**Sipariş Bilgileri:**

- Sipariş ID: 4003
- Sipariş No: PO-20251211-5DC998E0
- Durum: Received
- Tedarikçi: Test Supplier
- Toplam: 1005.00 TL

**Hata Detayları:**

```
'<' is an invalid start of a value. Path: $ | LineNumber: 3 | BytePositionInLine: 0.
```

**Backend Log Analizi:**

- İstek süresi: 786,306 ms (~13 dakika)
- HTTP Status: 499 (Client Closed Request - Timeout)
- Sorun: Luca session refresh sırasında HTML response alınıyor

## Sorun Analizi

### 1. Luca Session Sorunu

- `ForceSessionRefreshAsync()` çağrısı çok uzun sürüyor
- Luca API'den JSON yerine HTML response geliyor
- Bu durum JSON parse hatası veriyor

### 2. Timeout Sorunu

- İstek 120 saniye timeout'a uğruyor
- Backend'de işlem devam ediyor ama client bağlantıyı kesiyor
- 499 status code: Client bağlantıyı kapattı

### 3. HTML Response Sorunu

- Luca API bazen JSON yerine HTML login sayfası döndürüyor
- Session expire olmuş veya geçersiz olabilir
- Session refresh mekanizması düzgün çalışmıyor

## Mevcut Kod Akışı

```csharp
[HttpPost("{id}/sync")]
public async Task<ActionResult<PurchaseOrderSyncResultDto>> SyncToLuca(int id)
{
    // 1. Siparişi ve ilişkili verileri yükle
    var order = await _context.PurchaseOrders
        .Include(p => p.Supplier)
        .Include(p => p.Items)
            .ThenInclude(i => i.Product)
        .FirstOrDefaultAsync(p => p.Id == id);

    // 2. Luca invoice request'e map et
    var lucaInvoiceRequest = MappingHelper.MapToLucaInvoiceFromPurchaseOrder(order, order.Supplier);

    // 3. Session refresh (SORUN BURADA!)
    try
    {
        await _lucaService.ForceSessionRefreshAsync();  // ⚠️ Çok uzun sürüyor
    }
    catch (Exception refreshEx)
    {
        _logger.LogWarning(refreshEx, "Session refresh hatası");
    }

    // 4. Fatura gönder
    var syncResult = await _lucaService.SendInvoiceAsync(lucaInvoiceRequest);

    // ...
}
```

## Önerilen Çözümler

### 1. Session Refresh Optimizasyonu

```csharp
// Session refresh'i daha akıllı yap
// Sadece gerektiğinde refresh et
if (!await _lucaService.IsSessionValidAsync())
{
    await _lucaService.ForceSessionRefreshAsync();
}
```

### 2. Timeout Ayarları

```csharp
// Luca API çağrıları için timeout ayarla
var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(30)  // Daha kısa timeout
};
```

### 3. Retry Mekanizması

```csharp
// HTML response alınırsa retry yap
for (int i = 0; i < 3; i++)
{
    try
    {
        var result = await _lucaService.SendInvoiceAsync(request);
        if (result.IsSuccess) break;
    }
    catch (JsonException)
    {
        // HTML response alındı, session refresh gerekli
        await _lucaService.ForceSessionRefreshAsync();
        continue;
    }
}
```

### 4. Async Background Job

```csharp
// Sync işlemini background job olarak çalıştır
// Kullanıcı beklemez, işlem arka planda devam eder
[HttpPost("{id}/sync")]
public async Task<ActionResult> SyncToLuca(int id)
{
    // Job kuyruğuna ekle
    _backgroundJobClient.Enqueue(() =>
        SyncPurchaseOrderToLucaAsync(id));

    return Accepted(new {
        message = "Sync işlemi başlatıldı",
        orderId = id
    });
}
```

## Test Scriptleri

### 1. Sipariş Onay Akışı Testi

- Script: `test-katana-order-approval-flow.ps1`
- Durum: ✅ BAŞARILI
- Test edilen: Katana → Sipariş Sekmesi → Admin Onayı

### 2. Fatura Sync Testi

- Script: `test-invoice-sync-only.ps1`
- Durum: ❌ BAŞARISIZ
- Test edilen: Sipariş → Luca Fatura

### 3. Tam Akış Testi

- Script: `test-purchase-order-invoice.ps1`
- Durum: ⚠️ KISMEN BAŞARILI
- Sipariş oluşturma: ✅
- Onaylama: ✅
- Teslim alma: ✅
- Luca sync: ❌

## Sonraki Adımlar

### Acil (P0)

1. [ ] Luca session refresh mekanizmasını düzelt
2. [ ] HTML response kontrolü ekle
3. [ ] Timeout ayarlarını optimize et

### Önemli (P1)

4. [ ] Retry mekanizması ekle
5. [ ] Background job ile async sync
6. [ ] Session validation kontrolü

### İyileştirme (P2)

7. [ ] Sync durumu için real-time bildirim
8. [ ] Detaylı hata loglama
9. [ ] Monitoring ve alerting

## Geçici Çözüm

Şu an için fatura sync çalışmıyor. Geçici olarak:

1. **Manuel Luca Girişi**: Siparişler manuel olarak Luca'ya girilebilir
2. **Batch Sync**: Gece saatlerinde toplu sync denenebilir
3. **Session Pre-warm**: Uygulama başlangıcında session oluştur

## Sonuç

✅ **Çalışan Kısımlar:**

- Katana'dan sipariş gelişi
- Sipariş sekmesinde görünürlük
- Admin onayı
- Sipariş teslim alma
- Stok artışı

❌ **Çalışmayan Kısımlar:**

- Luca'ya fatura gönderimi
- Session refresh mekanizması
- Timeout yönetimi

⚠️ **Kritik Sorun:**
Luca session management düzgün çalışmıyor. Bu düzeltilmeden fatura sync çalışmayacak.
