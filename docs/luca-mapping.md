# Katana → Luca/Koza Kavram Eşleştirme ve Akış (Kısa Özet)

## Önemli Kavram Eşleştirmeleri
| # | Katana Kavramı | Luca/Koza Karşılığı | Endpoint | Açıklama |
|---|----------------|---------------------|----------|----------|
| 1 | Product (variant) | Stok Kartı | `EkleStkWsSkart.do` | Ürün bilgileri |
| 2 | OnHand stock | Diğer Stok Hareketi (Giriş) | `EkleStkWsDshBaslik.do` | Başlangıç stoğu |
| 3 | Sales Order | Satış Siparişi | `EkleStsWsSiparisBaslik.do` | Sipariş |
| 4 | Sales Order (invoiced) | Satış Faturası | `EkleFtrWsFaturaBaslik.do` | Fatura |
| 5 | Customer | Müşteri Kartı | `EkleFinMusteriWS.do` | Müşteri |
| 6 | `unit` (pcs, kg) | `olcumBirimiId` | - | Ölçü birimi ID mapping |
| 7 | `CategoryId` | `kategoriAgacKod` | - | Kategori kodu |

## Senkronizasyon Akış Şeması (Katana Products → Luca Stok Kartları)
1. `KatanaService.GetProductsAsync()` → `KatanaProductDto[]`
2. `MappingHelper.MapToLucaStockCard()` → `LucaCreateStokKartiRequest`
3. `LucaService.SendStockCardsAsync()` → `EkleStkWsSkart.do` (POST)
4. `MappingHelper.MapToLucaInitialStock()` → `LucaCreateDshBaslikRequest`
5. `LucaService.CreateOtherStockMovementAsync()` → `EkleStkWsDshBaslik.do` (POST)
