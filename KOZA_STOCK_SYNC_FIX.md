# Koza Stok Kartı Senkronizasyon Hatası Çözümü

## Sorun
Stok kartı ekranında ürünleri Koza'ya senkronize ederken 400 Bad Request hataları alınıyor:
```
Product X senkronize edilemedi: AxiosError
Failed to load resource: the server responded with a status of 400 (Bad Request)
```

## Kök Neden
Frontend'de kullanılan `kategoriAgacKod: "URUNLER"` değeri Koza API'sinde geçerli bir kategori kodu değil.

## Uygulanan Çözümler

### 1. Kategori Kodunu Düzeltme
**Dosya:** `frontend/katana-web/src/components/Admin/KozaIntegration.tsx`

Değişiklik:
```typescript
// ÖNCE:
kategoriAgacKod: "URUNLER"

// SONRA:
kategoriAgacKod: "001"  // Koza'da geçerli bir kategori kodu
```

### 2. Daha İyi Hata Mesajları
**Dosya:** `src/Katana.API/Controllers/Admin/KozaStockCardsController.cs`

- Backend'den dönen hata mesajlarına daha fazla detay eklendi
- Kategori kodu, kart kodu ve kart adı bilgileri hata yanıtına dahil edildi

**Dosya:** `src/Katana.Infrastructure/APIClients/LucaService.StockCards.cs`

- HTTP hata kodları ve yanıt gövdeleri daha detaylı loglanıyor
- Her deneme (ATTEMPT 1, 2, 3) için başarısızlık nedenleri loglanıyor

**Dosya:** `frontend/katana-web/src/components/Admin/KozaIntegration.tsx`

- Console'da daha detaylı hata mesajları gösteriliyor
- Kullanıcıya kategori kodu kontrolü yapması öneriliyor

## Geçerli Kategori Kodlarını Bulma

Koza'daki geçerli kategori kodlarını bulmak için:

1. **Koza Admin Paneline Giriş Yapın**
2. **Stok Kartları > Kategoriler** menüsüne gidin
3. Mevcut kategorilerin kodlarını not edin (örn: "001", "001.001", "002", vb.)

Alternatif olarak, backend loglarını kontrol edin:
```bash
# Backend loglarında kategori hatalarını ara
grep -i "kategori" logs/app-*.log
```

## Kategori Kodunu Değiştirme

Eğer "001" de çalışmazsa, doğru kategori kodunu bulduktan sonra:

**Dosya:** `frontend/katana-web/src/components/Admin/KozaIntegration.tsx`

```typescript
const kozaStok = mapKatanaProductToKozaStokKarti(product, {
  kategoriAgacKod: "DOĞRU_KATEGORİ_KODU",  // Buraya Koza'dan aldığınız kodu yazın
  olcumBirimiId: 1, // Adet
});
```

## Test Etme

1. Frontend ve backend'i yeniden derleyin:
```bash
# Backend
cd src/Katana.API
dotnet build

# Frontend
cd frontend/katana-web
npm run build
```

2. Uygulamayı yeniden başlatın:
```bash
docker-compose down
docker-compose up -d
```

3. Stok kartı ekranına gidin ve senkronizasyonu tekrar deneyin

4. Hata alırsanız:
   - Browser console'u açın (F12)
   - Network tab'ında `/api/admin/koza/stocks/create` endpoint'ine yapılan istekleri inceleyin
   - Response body'de dönen hata mesajını kontrol edin
   - Backend loglarını kontrol edin: `logs/app-$(date +%Y%m%d).log`

## Ek Notlar

- Koza API'si kategori kodlarını strict olarak kontrol eder
- Geçersiz kategori kodu kullanıldığında 400 Bad Request döner
- Her Koza kurulumunda farklı kategori kodları olabilir
- Kategori kodları genellikle "001", "001.001" gibi hiyerarşik yapıdadır

## İlgili Dosyalar

- `frontend/katana-web/src/components/Admin/KozaIntegration.tsx` - Senkronizasyon UI
- `frontend/katana-web/src/features/integrations/luca-koza/cards/StokMapper.ts` - Mapping logic
- `src/Katana.API/Controllers/Admin/KozaStockCardsController.cs` - API endpoint
- `src/Katana.Infrastructure/APIClients/LucaService.StockCards.cs` - Koza API client

## Sorun Devam Ederse

1. Backend loglarını kontrol edin ve tam hata mesajını bulun
2. Koza API dokümantasyonunu kontrol edin
3. Koza destek ekibiyle iletişime geçin ve geçerli kategori kodlarını sorun
4. Test için önce manuel olarak Koza'da bir stok kartı oluşturun ve hangi değerleri kullandığınızı not edin
