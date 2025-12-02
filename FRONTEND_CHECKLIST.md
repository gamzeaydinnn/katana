# Frontend Koza Entegrasyonu - Ön Kontrol Listesi

## ✅ Tamamlanan İşlemler

### 1. Backend API Hazır
- ✅ `KozaDepotsController` - `/api/admin/koza/depots`
  - GET: Depo listesi
  - POST `/create`: Yeni depo oluştur
- ✅ `KozaStockCardsController` - `/api/admin/koza/stocks`
  - GET: Stok kartı listesi
  - POST `/create`: Yeni stok kartı oluştur
- ✅ `[Authorize(Roles = "Admin")]` - Admin yetkisi gerekli

### 2. Backend DTO'lar Uyumlu
- ✅ `KozaDepoDto` ↔ Frontend `KozaStkDepo`
  - depoId, kod, tanim, kategoriKod ✓
- ✅ `KozaStokKartiDto` ↔ Frontend `KozaStokKarti`
  - kartKodu, kartAdi, kartTuru, kartTipi, olcumBirimiId ✓
  - JSON property names tamamen eşleşiyor ✓

### 3. Frontend Yapısı Düzenli
- ✅ `features/integrations/luca-koza/`
  - `cards/` - Kart tipleri, mapper'lar, servisler
  - `sync/` - Toplu senkronizasyon
  - `config.ts` - Varsayılan değerler
  - `README.md` - Dokümantasyon
- ✅ `services/api.ts` - Merkezi API yönetimi
  - `kozaAPI.depots.*`
  - `kozaAPI.stockCards.*`

### 4. Veritabanı Hazır
- ✅ `LocationKozaDepotMapping` entity oluşturuldu
- ✅ Migration uygulandı: `20251202181505_AddLocationKozaDepotMapping`
- ⚠️ **EKSİK**: `ProductKozaStockMapping` entity yok (henüz gerekmiyor, sadece senkronizasyon için)

## 🧪 Frontend'te Test Edilecekler

### Önce Backend Kontrolü
```bash
# Backend çalışıyor mu?
curl http://localhost:5055/api/health

# Admin login yapabiliyoruz mu?
# (Swagger UI'dan veya Postman'den test et)
POST http://localhost:5055/api/Auth/login
{
  "username": "admin",
  "password": "Admin123!"
}
```

### API Endpoint Testleri
```bash
# Token al (yukarıdaki login'den)
TOKEN="eyJhbGc..."

# Depo listesi
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5055/api/admin/koza/depots

# Stok kartı listesi
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:5055/api/admin/koza/stocks
```

### Frontend'te Kullanım
```typescript
// 1. Servisleri import et
import { depoService, stokService } from '@/features/integrations/luca-koza';

// 2. Depo listesi
const depolar = await depoService.listele();
console.log('Depolar:', depolar);

// 3. Stok kartı listesi
const stoklar = await stokService.listele();
console.log('Stok Kartları:', stoklar);

// 4. Yeni depo oluştur
const yeniDepo: KozaStkDepo = {
  kod: 'TEST-001',
  tanim: 'Test Deposu',
  kategoriKod: 'MERKEZ',
};
const sonuc = await depoService.ekle({ stkDepo: yeniDepo });

// 5. Mapper kullan
import { mapKatanaLocationToKozaDepo } from '@/features/integrations/luca-koza';

const katanaLocation = { /* ... */ };
const kozaDepo = mapKatanaLocationToKozaDepo(katanaLocation, {
  kategoriKod: 'MERKEZ',
});
```

## ⚠️ Dikkat Edilmesi Gerekenler

### 1. Authentication
- Frontend'de `authToken` localStorage'da olmalı
- Admin rolü gerekli (`[Authorize(Roles = "Admin")]`)
- Token expire olmuşsa yeni login gerekir

### 2. Backend Çalışıyor Olmalı
```bash
cd /Users/dilarasara/katana/src/Katana.API
dotnet run
```
Backend şu adreste çalışmalı: `http://localhost:5055`

### 3. Koza Session
- Backend'de Koza session cookie'si olmalı
- İlk çağrıda Koza'ya login yapılacak
- Session bilgisi `ILucaCookieJarStore` üzerinden yönetiliyor

### 4. CORS (Frontend development)
Frontend geliştirme sırasında (localhost:3000) CORS hatası alınırsa:
- Backend `Program.cs`'de CORS ayarları var
- `http://localhost:3000` ve `http://localhost:3001` allowed

## 🐛 Olası Hatalar ve Çözümleri

### Hata: "401 Unauthorized"
**Sebep**: Token yok veya geçersiz
**Çözüm**: 
```typescript
// Login yap, token'ı al
const response = await authAPI.login('admin', 'Admin123!');
localStorage.setItem('authToken', response.token);
```

### Hata: "403 Forbidden"
**Sebep**: Admin rolü yok
**Çözüm**: Kullanıcının Admin rolü olmalı

### Hata: "404 Not Found - /api/admin/koza/depots"
**Sebep**: Backend route yanlış veya controller kayıtlı değil
**Çözüm**: Backend'i kontrol et, controllers Auto-discover olmalı

### Hata: "Network Error" veya "ERR_CONNECTION_REFUSED"
**Sebep**: Backend çalışmıyor
**Çözüm**: 
```bash
cd /Users/dilarasara/katana/src/Katana.API
dotnet run
```

### Hata: Koza API'den "NO_JSON" veya HTML response
**Sebep**: Koza session geçersiz
**Çözüm**: Backend otomatik yeniden login yapacak, retry et

### Hata: "kartKodu zorunludur" veya validation error
**Sebep**: Request body eksik alanlar içeriyor
**Çözüm**: 
```typescript
// Zorunlu alanlar:
const stokKarti: KozaStokKarti = {
  kartKodu: 'PROD-001',           // Zorunlu
  kartAdi: 'Test Ürün',           // Zorunlu
  kartTuru: 1,                     // Zorunlu
  kartTipi: 1,                     // Zorunlu
  olcumBirimiId: 1,                // Zorunlu (Koza'dan alınmalı)
  kategoriAgacKod: 'KATEGORI-01',  // Zorunlu (Koza'dan alınmalı)
  kartAlisKdvOran: 0.18,          // Zorunlu
  kartSatisKdvOran: 0.18,         // Zorunlu
};
```

## 📊 Senkronizasyon Akışı

### Location → Depo Sync
```typescript
import { LocationSyncService } from '@/features/integrations/luca-koza';

const locationSync = new LocationSyncService();

// Tüm location'ları Koza'ya sync et
const katanaLocations = await api.get('/api/locations');
const results = await locationSync.senkronize(katanaLocations, {
  kategoriKod: 'MERKEZ',
  ulke: 'TÜRKİYE',
  il: 'İSTANBUL',
});

// Mapping oluştur (ID bazlı)
const depoIdMapping = locationSync.buildDepoIdMapping(results);
// Location ID 5 → Koza depoId 123
console.log(depoIdMapping.get(5)); // 123

// Kod bazlı mapping
const depoKodMapping = locationSync.buildDepoKodMapping(results);
// "LOC-5" → "LOC-5"
console.log(depoKodMapping.get('LOC-5'));
```

### Product → Stok Kartı Sync
```typescript
import { ProductSyncService } from '@/features/integrations/luca-koza';

const productSync = new ProductSyncService();

// Tüm product'ları Koza'ya sync et
const katanaProducts = await api.get('/api/products');
const results = await productSync.senkronize(katanaProducts, {
  kategoriAgacKod: 'URUNLER',
  olcumBirimiId: 1, // Adet
  kartAlisKdvOran: 0.18,
  kartSatisKdvOran: 0.18,
});

// Mapping oluştur
const stokIdMapping = productSync.buildStokKartIdMapping(results);
const stokKodMapping = productSync.buildStokKartKodMapping(results);
```

## 🎯 Bir Sonraki Adımlar

1. ✅ Frontend'i başlat: `npm start`
2. ✅ Backend'i çalıştır: `dotnet run`
3. ✅ Admin login yap
4. ✅ Browser console'da test et:
   ```javascript
   // Test servisleri
   import { depoService } from './features/integrations/luca-koza';
   const depolar = await depoService.listele();
   console.log(depolar);
   ```
5. ⏭️ UI component'leri oluştur (Admin panelinde)
6. ⏭️ Toplu senkronizasyon butonu ekle
7. ⏭️ Mapping tablosunu veritabanında sakla

## 📝 Notlar

- ✅ Tüm API çağrıları `kozaAPI` üzerinden yapılıyor
- ✅ Backend proxy kullanılıyor (güvenlik)
- ✅ DTO'lar frontend-backend arası uyumlu
- ✅ Validation backend'de yapılıyor
- ✅ Error handling mevcut
- ✅ TypeScript tipleri tam
- ⚠️ `ProductKozaStockMapping` entity'si henüz yok (mapping DB'ye kaydedilmiyor)
