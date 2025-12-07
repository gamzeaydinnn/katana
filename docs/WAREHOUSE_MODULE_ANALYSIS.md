# Warehouse Module Analysis

## 2) Depo / Lokasyon eşleşmesi (Katana Location ↔ Koza Depo)
- `LocationKozaDepotMapping` entity'si, Katana lokasyon ID'sini Koza depo koduna/ID'sine kayıt ediyor (arkaplanda `LocationKozaDepotMappings` tablosu).  
  * `LocationMappingService` veri çekiyor, MappingTable'daki `LOCATION_WAREHOUSE` fallback'ine dönüyor, yeni eşleşmeler yaratıp validasyon yapıyor (`src/Katana.Business/Services/LocationMappingService.cs:12-246`).  
  * `LocationMappingController` admin panelinden bu mapping'leri listelemeye, doğrulamaya ve güncellemeye izin veriyor (`src/Katana.API/Controllers/Admin/LocationMappingController.cs:1-200`).  
  * DI kayıtları `Program.cs` içinde sağlanmış (ILocationMappingService + repository), böylece servis/worker tarafında bu mapping'ler kolaylıkla kullanılıyor (`src/Katana.API/Program.cs:178-220`).

## 4) Ürün / Malzeme / Hizmet → Koza Stok/Hizmet Kartı
- `VariantMappings` tablosu (`Migration 20251207230000...`) variant_id'leri lokal ürün/SKU ile eşleştiriyor ve çakışmayı önlüyor (`src/Katana.Data/Migrations/20251207230000_AddVariantMappingAndOrderMappingDocs.cs:13-72`).  
- `VariantMappingRepository` + `VariantMappingService` iş birliğiyle `variant_id → (ProductId, SKU)` dönüşümü sağlanıyor; SKU yoksa `Product`/`ProductVariant` tabloları sorgulanıyor (`src/Katana.Data/Repositories/VariantMappingRepository.cs:8-55`, `src/Katana.Business/Services/VariantMappingService.cs:8-45`).  
- `KatanaSalesOrderSyncWorker` sipariş satırlarını işlerken `ResolveVariantMappingAsync` yardımıyla `variant_id`'yi `productId/SKU`'ya çeviriyor, cache'liyor ve mapping tablosuna yazıyor (`src/Katana.API/Workers/KatanaSalesOrderSyncWorker.cs:77-210`, `:343-368`).  
- `KatanaService` API istemcisi `GetVariantSkuAsync` üzerinden Koza'da SKU'nun alınmasını kolaylaştırıyor, böylece yeni mapping'ler yakalanabiliyor (`src/Katana.Infrastructure/APIClients/KatanaService.cs:250-310`).  
- DI kayıtlarında `IVariantMappingRepository`/`IVariantMappingService` ve ilgili servisler hazır (`src/Katana.API/Program.cs:182-220`), dolayısıyla variant tablosu senkronizasyona entegre durumda.

## 5) Satış Siparişi / Satınalma Siparişi eşleşmesi
- `OrderMapping` entity'sine `BelgeSeri`, `BelgeNo` ve `BelgeTakipNo` alanları eklendi; kayıtlar artık sipariş bazlı idempotency ve Koza belge kimliği saklamaya hazır (`src/Katana.Data/Models/OrderMapping.cs:32-86`).  
- `OrderMappingRepository.SaveLucaInvoiceIdAsync` bu alanları da güncelliyor; bir sipariş tekrar gönderilirse `GetMappingAsync` ile aynı belge numarası/seri yeniden kullanılabiliyor (`src/Katana.Data/Repositories/OrderMappingRepository.cs:60-130`).  
- `OrderInvoiceSyncService` gönderimden önce mapping'e bakıyor, daha önce sync edilmiş siparişi tekrar göndermiyor; başarılı gönderimde `BelgeSeri/No/TakipNo`'yu `OrderMappingRepository`'e yazıyor (`src/Katana.Business/UseCases/Sync/OrderInvoiceSyncService.cs:120-259`).  
- `BuildSalesInvoiceRequestAsync` içinde Katana order_no, tarih ve müşteri kodu validasyonları tutuluyor; gerektiğinde `OrderMapping`'ten alınmış `BelgeSeri`/No kullanılıyor (`src/Katana.Business/UseCases/Sync/OrderInvoiceSyncService.cs:284-460`).  
- `KatanaSalesOrderSyncWorker` onaylanan pending stock adjustment'ları okuyor, her bir satırın `ExternalOrderId` ile `OrderInvoiceSyncService`'e iletilmesini sağlıyor; böylece Koza belge numarası sabit kalıyor ve duplicate oluşmuyor (`src/Katana.API/Workers/KatanaSalesOrderSyncWorker.cs:216-335`).  

## Devam eden adımlar
- Veri migrasyonlarının (`VariantMappings`, `OrderMappings`'daki yeni alanlar) doğru şekilde veri tabanına uygulandığını teyit etmek için `dotnet ef migrations list`/`dotnet ef database update` sonrası schema kontrol edilebilir.  
- Mapping tablolarının (kategori ağacı, ölçü birimi) Koza'dan çekilip güncellendiğini görmek için ilgili servisler tarafından sağlanan cache'ler ve API uç noktaları incelenebilir.
