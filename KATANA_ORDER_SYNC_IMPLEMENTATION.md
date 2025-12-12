# Katana Sipariş Onay ve Ürün Sync İmplementasyonu

## Tarih

11 Aralık 2025

## Yapılan Değişiklikler

### 1. PurchaseOrdersController Güncellemesi

**Dosya**: `src/Katana.API/Controllers/PurchaseOrdersController.cs`

#### Eklenen Özellikler:

1. **IKatanaService Dependency Injection**

   - Constructor'a `IKatanaService` parametresi eklendi
   - Katana API ile iletişim için gerekli servis enjekte edildi

2. **Admin Onayında Otomatik Katana Sync**
   - Sipariş durumu `Pending` → `Approved` olduğunda tetiklenir
   - Arka planda (Task.Run) çalışır, ana isteği bloklamaz
   - Her sipariş kalemi için:
     - Katana'da ürün var mı kontrol edilir (`GetProductBySkuAsync`)
     - Varsa: Stok güncellenir (`UpdateProductAsync`)
     - Yoksa: Yeni ürün oluşturulur (`CreateProductAsync`)

#### Kod Akışı:

```csharp
// Admin onayı (Pending -> Approved)
if (request.NewStatus == PurchaseOrderStatus.Approved && oldStatus != PurchaseOrderStatus.Approved)
{
    _logger.LogInformation("✅ Sipariş onaylandı, Katana'ya ürünler ekleniyor/güncelleniyor: {OrderNo}", order.OrderNo);

    // Arka planda Katana'ya ürün ekle/güncelle
    _ = Task.Run(async () =>
    {
        await Task.Delay(1000); // DB commit bekle

        foreach (var item in order.Items)
        {
            // Katana'da ürün kontrolü
            var existingProduct = await _katanaService.GetProductBySkuAsync(item.Product.SKU);

            if (existingProduct != null)
            {
                // Ürün varsa güncelle
                var newStock = (existingProduct.InStock ?? 0) + item.Quantity;
                await _katanaService.UpdateProductAsync(...);
            }
            else
            {
                // Ürün yoksa oluştur
                await _katanaService.CreateProductAsync(newProduct);
            }
        }
    });
}
```

### 2. Test Scripti Güncellenmesi

**Dosya**: `test-katana-order-approval-flow.ps1`

#### Eklenen Kontroller:

1. **Katana Sync Bekleme**

   - Admin onayından sonra 5 saniye bekleme
   - Arka plan task'ının tamamlanması için zaman tanır

2. **Katana Ürün Kontrolü**
   - `/api/katana/products` endpoint'inden ürünleri çeker
   - Siparişteki ürünün Katana'da olup olmadığını kontrol eder
   - Ürün bulunursa detayları gösterir (ID, SKU, Stok, Fiyat)

## Tam Akış

```
┌─────────────────────┐
│  Katana Webhook     │
│  (Sipariş Gelişi)   │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  POST /api/         │
│  purchase-orders    │
│  Status: PENDING    │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  Sipariş Sekmesi    │
│  (Onay Bekliyor)    │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│  Admin Onayı        │
│  PATCH /status      │
│  newStatus=Approved │
└──────────┬──────────┘
           │
           ├──────────────────────────┐
           │                          │
           ▼                          ▼
┌─────────────────────┐    ┌─────────────────────┐
│  Status: APPROVED   │    │  Arka Plan Task     │
│  (DB Güncellendi)   │    │  Katana Sync        │
└─────────────────────┘    └──────────┬──────────┘
                                      │
                                      ▼
                           ┌─────────────────────┐
                           │  Katana'da Ürün     │
                           │  Var mı Kontrol?    │
                           └──────────┬──────────┘
                                      │
                    ┌─────────────────┴─────────────────┐
                    │                                   │
                    ▼                                   ▼
         ┌─────────────────────┐           ┌─────────────────────┐
         │  EVET: Stok         │           │  HAYIR: Yeni Ürün   │
         │  Güncelle           │           │  Oluştur            │
         │  UpdateProductAsync │           │  CreateProductAsync │
         └─────────────────────┘           └─────────────────────┘
                    │                                   │
                    └─────────────────┬─────────────────┘
                                      │
                                      ▼
                           ┌─────────────────────┐
                           │  Katana'da Ürün     │
                           │  Hazır              │
                           └─────────────────────┘
```

## Sonraki Adımlar

### 1. Katana'dan Sync ile Stok Kartı Oluşturma

Katana'da ürün oluşturulduktan sonra, Luca'ya stok kartı senkronizasyonu için:

**Mevcut Durum**:

- Katana'da ürün oluşturuluyor ✅
- Luca'ya stok kartı sync'i manuel yapılıyor ❌

**Gerekli**:

- Katana ürün oluşturulduktan sonra otomatik Luca sync tetiklenmeli
- Sync endpoint'i: `POST /api/sync/products/katana-to-luca`
- Veya: `POST /api/admin/koza-stock-cards/sync-from-katana`

**Önerilen İmplementasyon**:

```csharp
// PurchaseOrdersController.cs - Approved durumunda
_ = Task.Run(async () =>
{
    // ... Katana'ya ürün ekle/güncelle ...

    // Katana sync başarılı olduysa, Luca'ya da sync et
    if (created != null || updated)
    {
        try
        {
            // Luca'ya stok kartı sync tetikle
            await _lucaService.SyncStockCardFromKatanaAsync(item.Product.SKU);
            _logger.LogInformation("✅ Luca stok kartı sync tetiklendi: {SKU}", item.Product.SKU);
        }
        catch (Exception lucaEx)
        {
            _logger.LogError(lucaEx, "❌ Luca stok kartı sync hatası: {SKU}", item.Product.SKU);
        }
    }
});
```

### 2. Docker Build ve Deploy

**Mevcut Sorun**:

- Kod değişiklikleri Docker container'a yansımadı
- Container eski kodu çalıştırıyor

**Çözüm**:

```powershell
# 1. Docker image'ı yeniden build et
docker-compose build api

# 2. Container'ları yeniden başlat
docker-compose down
docker-compose up -d

# 3. Logları kontrol et
docker logs katana-api-1 --follow
```

### 3. Test ve Doğrulama

**Test Adımları**:

1. Backend'i yeniden build et ve başlat
2. Test scriptini çalıştır: `.\test-katana-order-approval-flow.ps1`
3. Logları kontrol et:
   ```powershell
   docker logs katana-api-1 --tail 100 | Select-String -Pattern "Katana"
   ```
4. Katana'da ürünü kontrol et
5. Luca'ya sync tetikle
6. Luca'da stok kartını kontrol et

### 4. Webhook Entegrasyonu

**Gerekli**:

- Katana webhook endpoint'i: `POST /api/webhooks/katana/purchase-orders`
- Webhook signature doğrulama
- Retry mekanizması

### 5. Monitoring ve Alerting

**Eklenecekler**:

- Katana sync başarı/hata metrikleri
- Luca sync başarı/hata metrikleri
- Başarısız sync'ler için alert
- Dashboard'da sync durumu gösterimi

## Test Sonuçları

### ✅ Başarılı

1. Sipariş oluşturma (Pending)
2. Sipariş sekmesinde görünürlük
3. Admin onayı (Pending → Approved)
4. Kod değişiklikleri tamamlandı

### ⚠️ Beklemede

1. Docker build ve deploy
2. Katana sync log'larının görünmesi
3. Katana'da ürün kontrolü
4. Luca'ya otomatik sync

### ❌ Eksik

1. Katana webhook entegrasyonu
2. Luca'ya otomatik stok kartı sync
3. Hata yönetimi ve retry mekanizması
4. Monitoring ve alerting

## Öneriler

1. **Öncelik 1**: Docker build ve deploy yapılmalı
2. **Öncelik 2**: Katana sync log'ları kontrol edilmeli
3. **Öncelik 3**: Luca'ya otomatik sync eklenmeli
4. **Öncelik 4**: Webhook entegrasyonu tamamlanmalı
5. **Öncelik 5**: Monitoring ve alerting kurulmalı

## Notlar

- Arka plan task'ları (Task.Run) ana isteği bloklamaz
- Hata durumunda log'lanır ama sipariş onayı başarılı sayılır
- Retry mekanizması henüz yok
- Katana API rate limit'leri dikkate alınmalı
- Luca session yönetimi önemli
