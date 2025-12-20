# Koza Depo Entegrasyonu

## Mimari Kararlar

### ✅ Backend-First Yaklaşım
Tüm Koza API çağrıları **backend üzerinden** yapılır. Frontend Koza API'ye **asla direkt erişmez**.

**Neden?**
- 🔐 Koza cookie/session auth (JSESSIONID + şube seçimi) backend'de yönetilir
- 🔒 Koza URL'leri/kimlik bilgileri frontend'e açılmaz
- 📊 Retry/log/NO_JSON (HTML döndü) gibi hatalar backend loglarında yakalanır
- 💾 depoId gibi Koza internal id'leri DB'de mapping olarak saklanır

### Dosya Yapısı

```
Backend (C#)
├── src/Katana.Business/
│   ├── DTOs/Koza/
│   │   └── KozaDepoDtos.cs              # Koza depo tipleri
│   └── Interfaces/
│       └── ILucaService.cs               # Depo metodları eklendi
│
├── src/Katana.Infrastructure/
│   └── APIClients/
│       ├── LucaService.cs                # partial class yapıldı
│       └── LucaService.Depots.cs         # Depo implementasyonu
│
├── src/Katana.Core/
│   └── Entities/
│       └── LocationKozaDepotMapping.cs   # Mapping entity
│
└── src/Katana.API/
    └── Controllers/Admin/
        └── KozaDepotsController.cs       # REST API

Frontend (TypeScript)
└── src/features/integrations/luca-koza/
    ├── cards/
    │   ├── DepoKarti.ts                  # Tipler
    │   ├── DepoMapper.ts                 # Katana → Koza dönüşümü
    │   ├── DepoService.ts                # Backend API çağrıları
    │   └── index.ts
    ├── sync/
    │   └── LocationSync.ts               # Toplu senkronizasyon
    └── config.ts
```

## Backend API Endpoint'leri

### 1. Depoları Listele
```http
GET /api/admin/koza/depots
Authorization: Bearer {token}
```

**Response:**
```json
[
  {
    "depoId": 123,
    "kod": "0001",
    "tanim": "Ana Depo",
    "kategoriKod": "GENEL",
    "ulke": "Türkiye",
    "il": "İstanbul",
    "ilce": null,
    "adresSerbest": "Sanayi Mahallesi, İstanbul, Türkiye"
  }
]
```

### 2. Depo Oluştur
```http
POST /api/admin/koza/depots/create
Authorization: Bearer {token}
Content-Type: application/json

{
  "stkDepo": {
    "kod": "0002",
    "tanim": "Yan Depo",
    "kategoriKod": "GENEL",
    "ulke": "Türkiye",
    "il": "Ankara"
  }
}
```

**Response:**
```json
{
  "success": true,
  "message": "OK"
}
```

## Koza Payload Formatı (Düzeltildi ✅)

### ❌ YANLIŞ (Copilot'un önerisi)
```json
{
  "depoKodu": "0001",
  "depoAdi": "Ana Depo",
  "adres": "...",
  "sorumluKisi": "..."
}
```

### ✅ DOĞRU (Koza formatı)
```json
{
  "stkDepo": {
    "kod": "0001",
    "tanim": "Ana Depo",
    "kategoriKod": "GENEL",
    "ulke": "Türkiye",
    "il": "İstanbul",
    "ilce": null,
    "adresSerbest": "Sanayi Mahallesi, İstanbul"
  }
}
```

## Katana Location → Koza Depo Mapping

### Katana Location Formatı (Düzeltildi ✅)

```typescript
interface KatanaLocation {
  id: number | string;
  name: string;
  legal_name?: string | null;
  address?: {
    line_1?: string | null;    // ✅ line_1 (line1 değil!)
    line_2?: string | null;    // ✅ line_2 (line2 değil!)
    city?: string | null;
    state?: string | null;
    zip?: string | null;       // ✅ zip
    country?: string | null;
  } | null;
  deleted_at?: string | null;  // ✅ aktiflik kontrolü (archived değil!)
}
```

### Dönüşüm Stratejisi

```typescript
// Depo kodu üretimi
function makeDepoKodu(id: number | string): string {
  if (typeof id === "number" || /^\d+$/.test(String(id))) {
    return String(id).padStart(4, "0");  // 2 → "0002"
  }
  // string id → "LOC_ABC123" (max 20)
  return String(id).toUpperCase().replace(/[^A-Z0-9]/g, "_").slice(0, 20);
}

// Aktiflik kontrolü
function isActive(location: KatanaLocation): boolean {
  return !location.deleted_at;  // deleted_at doluysa "silinmiş"
}
```

## Database Mapping

### LocationKozaDepotMapping Entity

```csharp
public class LocationKozaDepotMapping
{
    public int Id { get; set; }
    public string KatanaLocationId { get; set; }  // Katana Location ID
    public string KozaDepoKodu { get; set; }      // Koza depo kodu (transfer için)
    public long? KozaDepoId { get; set; }         // Koza depo ID (eldeki miktar için)
    public DateTime UpdatedAt { get; set; }
    public string? KatanaLocationName { get; set; }
    public string? KozaDepoTanim { get; set; }
}
```

**Neden bu mapping şart?**

1. **Eldeki miktar endpoint'i** → `depoId` istiyor
2. **Depo transferi endpoint'i** → `girisDepoKodu`/`cikisDepoKodu` istiyor

## Kullanım Örnekleri

### Backend (C#)

```csharp
// Depoları listele
var depots = await _lucaService.ListDepotsAsync();

// Yeni depo oluştur
var result = await _lucaService.CreateDepotAsync(new KozaCreateDepotRequest
{
    StkDepo = new KozaDepoDto
    {
        Kod = "0001",
        Tanim = "Ana Depo",
        KategoriKod = "GENEL",
        Ulke = "Türkiye",
        Il = "İstanbul"
    }
});
```

### Frontend (TypeScript)

```typescript
import { depoService } from './features/integrations/luca-koza';

// Depoları listele
const depots = await depoService.listele();

// Yeni depo oluştur
const result = await depoService.ekle({
  stkDepo: {
    kod: "0001",
    tanim: "Ana Depo",
    kategoriKod: "GENEL",
    ulke: "Türkiye",
    il: "İstanbul"
  }
});

// Varsa getir, yoksa oluştur
const depot = await depoService.getirVeyaOlustur({
  kod: "0001",
  tanim: "Ana Depo",
  kategoriKod: "GENEL"
});
```

### Toplu Senkronizasyon

```typescript
import { LocationSyncService } from './features/integrations/luca-koza';

const syncService = new LocationSyncService({
  defaultKategoriKod: "GENEL",
});

// Katana'dan location'ları çek (varsayalım)
const katanaLocations = await fetchKatanaLocations();

// Senkronize et
const results = await syncService.syncLocations(katanaLocations);

// Mapping'leri oluştur
const depoIdMap = syncService.buildDepoIdMapping(results);
const depoKodMap = syncService.buildDepoKodMapping(results);

// Eldeki miktar için depoId kullan
const depoId = depoIdMap.get(katanaLocationId);

// Depo transferi için depoKodu kullan
const girisDepoKodu = depoKodMap.get(targetLocationId);
const cikisDepoKodu = depoKodMap.get(sourceLocationId);
```

## Hata Yönetimi

### NO_JSON Hatası (HTML döndü)

Backend otomatik tespit eder ve loglara yazar:

```csharp
if (body.TrimStart().StartsWith("<"))
{
    _logger.LogError("Koza NO_JSON (HTML döndü). Auth/şube/cookie kırık olabilir.");
    throw new InvalidOperationException("Koza NO_JSON hatası");
}
```

**Çözüm:**
- Koza auth/session/branch selection kontrol et
- Backend loglarında detaylı hata mesajları var

### Retry Mekanizması

Backend `SendWithAuthRetryAsync` kullanarak otomatik retry yapar:
- Unauthorized → re-auth + retry
- Branch selection hatası → branch selection + retry
- Maksimum 2 deneme

## Güvenlik

✅ **Frontend Koza API'ye direkt erişmez**
- Tüm çağrılar backend üzerinden
- JSESSIONID cookie backend'de tutulur
- Koza kimlik bilgileri environment variable'larda

✅ **Admin yetkisi gerekli**
```csharp
[Authorize(Roles = "Admin")]
```

## Next Steps

1. **Migration oluştur** → `LocationKozaDepotMapping` tablosu için
2. **Sync service** → Katana locations → Koza depots otomatik sync
3. **UI** → Admin panel'de depo yönetimi sayfası
4. **Cari Kart** → Customer/Supplier için benzer implementasyon

## Önemli Notlar

⚠️ **firmaKodu gönderilmez** → Koza zaten cookie/şube seçimiyle bağlı
⚠️ **Katana field adları** → `line_1`, `line_2`, `zip`, `deleted_at`
⚠️ **Koza field adları** → `kod`, `tanim`, `kategoriKod`, `ulke`, `il`, `ilce`, `adresSerbest`
