# Test Planı: Temiz Sync Testi (Frontend Gürültüsü Olmadan)

## 🔍 Sorun Analizi

### Mevcut Durum (Sunucu Kilitlenme Sebepleri)

1. **Frontend DDoS**: Admin paneli saniyede onlarca istek atıyor

   - `/api/adminpanel/statistics` → 46 saniye timeout
   - `/api/adminpanel/products` → 46 saniye timeout
   - `GetProductsAsync` → TaskCanceledException (30s timeout)

2. **Session Trashing**: 9 saniyede (23:53:39-23:53:48) onlarca login/logout

   - Her admin paneli isteği yeni login tetikliyor
   - CPU/Network tüm gücünü session management'a harcıyor
   - Asıl Sync işine sıra gelmiyor

3. **Defansif Kod Henüz Test Edilemedi**:
   - `[CACHE WARMING]` logu YOK
   - `[STRUTS SYNC]` logu YOK
   - `ListStockCardsSimpleAsync` çağrısı YOK
   - Kod sahneye çıkmadı bile!

### Neden Test Başarısız Oldu?

```
Frontend (React Admin) → İstatistik/Ürün API'leri → Login/ChangeBranch → Session Chaos
                                                              ↓
                                                    Sunucu CPU %100
                                                              ↓
                                                    Sync İsteği İşlenemiyor
                                                              ↓
                                                    Defansif Kod Çalışmıyor
```

---

## ✅ ÇÖZÜM: İzole Test Prosedürü

### Adım 1: Frontend'i Tamamen Kapat

**Aksiyon**: Tüm tarayıcı sekmelerini kapat

- React Admin Panel (`http://localhost:3000`)
- Swagger UI (`http://localhost:5178/swagger`)
- Diğer Luca/Katana sekmeleri

**Amaç**: Background polling isteklerini durdurmak

```powershell
# Opsiyonel: Tarayıcı process'lerini kontrol et
Get-Process chrome,msedge,firefox -ErrorAction SilentlyContinue | Stop-Process -Force
```

---

### Adım 2: Backend'i Temiz Restart

**Aksiyon**: Mevcut process'i durdur ve cache'i temizle

```powershell
# Terminal'de çalışan dotnet run'ı durdur
# Ctrl+C

# Bin/obj klasörlerini temizle (eski DLL'leri sil)
cd c:\Users\GAMZE\Desktop\katana\src\Katana.API
dotnet clean
dotnet build --no-incremental

# Temiz başlat
dotnet run
```

**Beklenen İlk Loglar**:

```
info: Katana.API[0]
      ✅ Application started at: http://localhost:5178
info: Katana.Infrastructure[0]
      🔐 Luca API initialized with BaseUrl: https://...
```

---

### Adım 3: Postman ile İzole Test

**Önemli**: Tarayıcı açma! Sadece Postman/curl kullan.

#### Postman Request

```http
POST http://localhost:5178/api/sync/products-to-luca
Content-Type: application/json

{
  "limit": 5,
  "dryRun": false
}
```

#### Alternatif: PowerShell ile Test

```powershell
$body = @{
    limit = 5
    dryRun = $false
} | ConvertTo-Json

$response = Invoke-RestMethod `
    -Uri "http://localhost:5178/api/sync/products-to-luca" `
    -Method POST `
    -Body $body `
    -ContentType "application/json"

$response | ConvertTo-Json -Depth 5
```

---

### Adım 4: Log Analizi - Aradığımız Patterns

#### ✅ Başarı Senaryosu (Defansif Kod Çalışıyor)

**1. Cache Warming Başlangıcı**:

```log
[HH:mm:ss INF] ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[HH:mm:ss INF] 🔥 [CACHE WARMING] İlk batch başlangıcında cache doldurulacak
[HH:mm:ss INF] 🔥 [CACHE WARMING] ListStockCardsSimpleAsync çağrılıyor...
```

**2. Struts Timing Fix**:

```log
[HH:mm:ss DBG] ⏳ [STRUTS SYNC] Waiting 500ms after ChangeBranch...
[HH:mm:ss DBG] ✅ [STRUTS SYNC] Delay complete - ready for ListStockCards
```

**3. Cookie Verification**:

```log
[HH:mm:ss DBG] 🍪 [COOKIE PRESENT] Cookie header verified: JSESSIONID=...
```

**4. Cache Success**:

```log
[HH:mm:ss INF] ✅ CACHE HAZIR: 12847 SKU → StokKartId mapping
[HH:mm:ss INF] ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**5. Double Check Logic**:

```log
[HH:mm:ss INF] 🔍 [1/3] Cache kontrolü: 81.06301-8211
[HH:mm:ss INF] 📦 [CACHE HIT] Stok kartı bulundu: 81.06301-8211 (skartId: 12345)
```

veya

```log
[HH:mm:ss INF] ✨ [CACHE MISS] Yeni stok kartı: NEW-PRODUCT-001
[HH:mm:ss WRN] ⚠️ [2/3] Cache MISS - SAFETY CHECK: Canlı API'den tekrar sorgulanıyor...
[HH:mm:ss INF] ✅ [SAFETY CHECK PASSED] SKU gerçekten yok - CREATE yapılacak
[HH:mm:ss INF] ➕ [3/3] Yeni stok kartı POST ediliyor: NEW-PRODUCT-001
```

---

#### ❌ Fail-Fast Senaryosu (Cache Warming Patladı)

```log
[HH:mm:ss ERR] ❌ [CRITICAL] JSON parse failed for ListStockCards. Body: Unable to instantiate Action...
[HH:mm:ss ERR] ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[HH:mm:ss ERR] 🚨 CRITICAL: Cache warming FAILED with InvalidOperationException!
[HH:mm:ss ERR]    Cache Warming ZORUNLU - Fuzzy Search için SKU → StokKartId mapping lazım
[HH:mm:ss ERR]    SYNC DURDURULDU - Duplicate creation risk var!
[HH:mm:ss ERR] ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

**Beklenen Response**:

```json
{
  "isSuccess": false,
  "message": "Cache warming failed critically: JSON parse failed for ListStockCards...",
  "sent": 0,
  "skipped": 0,
  "duplicates": 0
}
```

---

#### ⚠️ Cache Integrity Error (Double Check Buldu)

```log
[HH:mm:ss ERR] 🚨 [CACHE INTEGRITY ERROR] SKU: Ø38x1,5-2
[HH:mm:ss ERR]    Cache: BULUNAMADI (null)
[HH:mm:ss ERR]    Live API: BULUNDU (skartId: 67890)
[HH:mm:ss ERR]    Duplicate oluşturma ÖNLENDİ!
```

**Anlam**: Cache warming kısmen başarılı ama bazı kartlar eksik. Double check sayesinde duplicate önlendi.

---

### Adım 5: Sonuç Değerlendirmesi

#### Başarı Kriterleri

| Kriter                                       | Beklenen | Gerçekleşti? |
| -------------------------------------------- | -------- | ------------ |
| `[CACHE WARMING]` logu görüldü               | ✅       | ⬜           |
| `[STRUTS SYNC]` 500ms delay logu görüldü     | ✅       | ⬜           |
| `[COOKIE PRESENT]` verification logu görüldü | ✅       | ⬜           |
| Cache başarıyla doldu (12k+ SKU)             | ✅       | ⬜           |
| Frontend gürültüsü olmadı                    | ✅       | ⬜           |
| Sync 5 ürünü işledi                          | ✅       | ⬜           |

---

## 🔧 Troubleshooting

### Problem: Hala Timeout Alıyorum

**Çözüm**: Luca API'nin yavaş olması normal. 5 ürün yerine 2 ürün dene:

```json
{ "limit": 2, "dryRun": false }
```

### Problem: "Branch not selected" Hatası

**Çözüm**: İlk login sonrası branch seçimi manuel yapılmalı. Tarayıcıda bir kez Luca'ya gir, branch seç, sonra tekrar Postman'den test et.

### Problem: Session Cookie Yok

**Kontrol**:

```log
[HH:mm:ss WRN] ⚠️ [COOKIE MISSING] ListStockCards has NO Cookie header!
```

**Çözüm**: `EnsureAuthenticatedAsync` çağrılıyor mu kontrol et. Loglarda `Login SUCCESS` görmelisin.

---

## 📊 Test Sonrası Rapor Şablonu

Test sonrası bana şu bilgileri paylaş:

### 1. Log Snippet (İlk 50 satır)

```log
[Test başladıktan sonraki loglar]
...
```

### 2. Response Body

```json
{
  "isSuccess": true/false,
  "message": "...",
  "sent": X,
  "skipped": Y,
  "duplicates": Z
}
```

### 3. Aradığımız Pattern'ler Bulundu mu?

- [ ] `[CACHE WARMING]` görüldü
- [ ] `[STRUTS SYNC]` 500ms delay görüldü
- [ ] `[COOKIE PRESENT]` görüldü
- [ ] `[CACHE HIT]` veya `[CACHE MISS]` görüldü
- [ ] `[SAFETY CHECK PASSED]` görüldü (eğer yeni ürün varsa)

### 4. Beklenmeyen Loglar

```log
[Garip veya beklenmeyen herhangi bir log]
```

---

## 🎯 Sonraki Adımlar

**Eğer Test Başarılı Olursa**:

1. ✅ Defansif programlama çalıştı doğrula
2. ✅ 10 ürünle tekrar test et
3. ✅ 50 ürünle gerçek sync yap
4. ✅ Production'a deploy

**Eğer Fail-Fast Tetiklenirse**:

1. ❌ Cache warming hala patlıyor demektir
2. ❌ Struts timing fix yeterli değil (1000ms dene)
3. ❌ Luca API'de daha derin bir sorun var
4. ❌ Alternative stratejiye geç (cache'siz çalış, her ürün için live check)

---

## ⚠️ ÖNEMLİ UYARILAR

1. **Frontend Açma**: Test süresince hiçbir tarayıcı sekmesi açma!
2. **Tek Test**: Aynı anda birden fazla Postman request atma!
3. **Log Takibi**: Backend terminalinde logları canlı izle!
4. **Timeout**: İlk test 1-2 dakika sürebilir (cache warming), sabırlı ol!

---

**Hazır mısın?** Şimdi adımları sırayla uygula ve temiz logları bana gönder! 🚀
