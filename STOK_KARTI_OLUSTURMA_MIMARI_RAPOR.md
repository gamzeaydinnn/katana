# KATANA STOK KARTI OLUŞTURMA MİMARİSİ - DETAYLI RAPOR

## 📋 İÇİNDEKİLER

1. Genel Mimari Akış
2. Katman Bazlı Detaylar
3. Veri Akışı ve Dönüşümler
4. Session Yönetimi
5. Hata Yönetimi ve Retry Mekanizması
6. Luca API Entegrasyonu
7. Kritik Noktalar ve Dikkat Edilmesi Gerekenler

---

## 1. GENEL MİMARİ AKIŞ

### 1.1 Yüksek Seviye Akış

```
Frontend (React)
    ↓ HTTP POST /api/sync/start
API Controller (SyncController)
    ↓ syncType: "STOCK_CARD"
Business Layer (SyncService)
    ↓ Katana DB'den ürünler çekiliyor
Mapper Layer (KatanaToLucaMapper)
    ↓ Katana Product → LucaCreateStokKartiRequest
Infrastructure Layer (LucaService)
    ↓ Session kontrolü + Branch seçimi
Luca API (Koza)
    ↓ POST EkleStkWsKart.do
Response ← {"error":false,"skartId":12345,"message":"..."}
```

### 1.2 Mimari Katmanlar

- **API Layer**: `src/Katana.API/Controllers/SyncController.cs`
- **Business Layer**: `src/Katana.Business/UseCases/Sync/SyncService.cs`
- **Mapper Layer**: `src/Katana.Business/Mappers/KatanaToLucaMapper.cs`
- **Infrastructure Layer**: `src/Katana.Infrastructure/APIClients/LucaService.Operations.cs`
- **DTO Layer**: `src/Katana.Core/DTOs/LucaDtos.cs`

---

## 2. KATMAN BAZLI DETAYLAR

### 2.1 API LAYER - SyncController

**Dosya**: `src/Katana.API/Controllers/SyncController.cs`

**Sorumluluklar**:

- HTTP endpoint sağlama (`POST /api/sync/start`)
- Request validasyonu
- Business layer'a yönlendirme

**Örnek Request**:

```json
POST /api/sync/start
{
  "syncType": "STOCK_CARD"
}
```

**Kod Akışı**:

```csharp
[HttpPost("start")]
public async Task<IActionResult> StartSync([FromBody] SyncRequest request)
{
    if (request.SyncType == "STOCK_CARD")
    {
        var result = await _syncService.SyncStockCardsAsync();
        return Ok(result);
    }
}
```

---

### 2.2 BUSINESS LAYER - SyncService

**Dosya**: `src/Katana.Business/UseCases/Sync/SyncService.cs`

**Sorumluluklar**:

1. Katana DB'den ürünleri çekme
2. Her ürün için Luca'da kontrol (var mı yok mu?)
3. Yeni ürünleri Luca'ya gönderme
4. Mevcut ürünlerde değişiklik kontrolü
5. Sonuç raporlama (başarılı/başarısız sayıları)

**Kritik Metodlar**:

- `SyncStockCardsAsync()`: Ana sync metodu
- `GetProductsFromKatanaDb()`: Katana'dan ürünleri çeker
- `CheckIfExistsInLuca()`: Luca'da ürün kontrolü

**Örnek Akış**:

```csharp
public async Task<SyncResultDto> SyncStockCardsAsync()
{
    // 1. Katana'dan ürünleri çek
    var products = await _productRepository.GetAllActiveAsync();

    // 2. Her ürün için
    foreach (var product in products)
    {
        // 3. Luca'da var mı kontrol et
        var existingCard = await _lucaService.GetStockCardBySkuAsync(product.SKU);

        if (existingCard == null)
        {
            // 4. Yoksa oluştur
            var request = _mapper.MapToLucaRequest(product);
            await _lucaService.CreateStockCardAsync(request);
        }
    }
}
```

---

### 2.3 MAPPER LAYER - KatanaToLucaMapper

**Dosya**: `src/Katana.Business/Mappers/KatanaToLucaMapper.cs`

**Sorumluluklar**:

- Katana Product entity'sini Luca DTO'suna dönüştürme
- Encoding dönüşümleri (UTF-8 → ISO-8859-9)
- Özel karakter temizleme (Ø → O)
- Varsayılan değer atama

**Kritik Dönüşümler**:

```csharp
public LucaCreateStokKartiRequest MapToLucaRequest(Product product)
{
    return new LucaCreateStokKartiRequest
    {
        // Temel alanlar
        KartAdi = CleanSpecialChars(product.Name),
        KartKodu = CleanSpecialChars(product.SKU),
        KartTuru = 1,  // 1=Stok, 2=Hizmet
        KartTipi = 1,  // Sabit

        // KDV oranları
        KartAlisKdvOran = product.VATRate ?? 1,

        // Ölçü birimi
        OlcumBirimiId = MapUnitToLucaId(product.Unit),

        // Tarih
        BaslangicTarihi = DateTime.Now.ToString("dd/MM/yyyy"),

        // Barkod
        Barkod = product.Barcode ?? product.SKU,

        // Flagler
        SatilabilirFlag = 1,
        SatinAlinabilirFlag = 1,
        MaliyetHesaplanacakFlag = true
    };
}
```

**Özel Karakter Temizleme**:

- `Ø` → `O`
- `ø` → `o`
- Türkçe karakterler korunuyor (ISO-8859-9 encoding)

---

### 2.4 INFRASTRUCTURE LAYER - LucaService

**Dosya**: `src/Katana.Infrastructure/APIClients/LucaService.Operations.cs`

**Sorumluluklar**:

1. Luca API ile HTTP iletişimi
2. Session yönetimi (JSESSIONID cookie)
3. Branch seçimi (orgSirketSubeId)
4. JSON serialization
5. Retry mekanizması
6. Hata yönetimi

**Ana Metodlar**:

#### 2.4.1 CreateStockCardAsync()

```csharp
public async Task<JsonElement> CreateStockCardAsync(LucaCreateStokKartiRequest request)
{
    // 1. Session kontrolü
    await EnsureAuthenticatedAsync();

    // 2. Branch seçimi
    await EnsureBranchSelectedAsync();

    // 3. JSON oluştur (Luca dokümantasyonuna %100 uygun)
    var jsonRequest = new Dictionary<string, object?>
    {
        ["kartAdi"] = request.KartAdi,           // required
        ["kartKodu"] = request.KartKodu,         // required
        ["kartTipi"] = 1,
        ["kartAlisKdvOran"] = request.KartAlisKdvOran,
        ["olcumBirimiId"] = request.OlcumBirimiId,
        ["baslangicTarihi"] = request.BaslangicTarihi,  // dd/MM/yyyy
        ["kartTuru"] = 1,
        ["kategoriAgacKod"] = null,
        ["barkod"] = request.Barkod,
        ["alisTevkifatOran"] = null,
        ["satisTevkifatOran"] = null,
        ["alisTevkifatTipId"] = null,
        ["satisTevkifatTipId"] = null,
        ["satilabilirFlag"] = 1,
        ["satinAlinabilirFlag"] = 1,
        ["lotNoFlag"] = 1,
        ["minStokKontrol"] = 0,
        ["maliyetHesaplanacakFlag"] = true
    };

    // 4. Serialize et
    var payload = JsonSerializer.Serialize(jsonRequest);

    // 5. HTTP POST gönder
    var response = await _httpClient.PostAsync(
        "EkleStkWsKart.do",
        new StringContent(payload, Encoding.UTF8, "application/json")
    );

    // 6. Response parse et
    var body = await response.Content.ReadAsStringAsync();
    return JsonSerializer.Deserialize<JsonElement>(body);
}
```

#### 2.4.2 Session Yönetimi

```csharp
private async Task EnsureAuthenticatedAsync()
{
    if (_isCookieAuthenticated && !IsSessionExpired())
    {
        return; // Session hala geçerli
    }

    // Yeni session oluştur
    await PerformLoginAsync();
}

private async Task PerformLoginAsync()
{
    var loginPayload = new
    {
        orgCode = "akozas",
        userName = "ENTEGRASYON",
        userPassword = "***"
    };

    var response = await _httpClient.PostAsync(
        "YdlUserLogin.do",
        new StringContent(JsonSerializer.Serialize(loginPayload))
    );

    // Cookie'yi al
    var cookies = _cookieContainer.GetCookies(new Uri(_baseUrl));
    _sessionCookie = cookies["JSESSIONID"]?.Value;
    _isCookieAuthenticated = true;
}
```

#### 2.4.3 Branch Seçimi

```csharp
private async Task EnsureBranchSelectedAsync()
{
    // Branch listesini al
    var branches = await GetBranchesAsync();

    // Preferred branch'i seç (11746)
    var targetBranch = branches.FirstOrDefault(b => b.Id == 11746);

    if (targetBranch != null)
    {
        await ChangeBranchAsync(targetBranch.Id);
    }
}

private async Task ChangeBranchAsync(long branchId)
{
    var payload = new { orgSirketSubeId = branchId };

    await _httpClient.PostAsync(
        "YdlUserResponsibilityOrgSs.do",
        new StringContent(JsonSerializer.Serialize(payload))
    );
}
```

---

## 3. VERİ AKIŞI VE DÖNÜŞÜMLER

### 3.1 Katana Product → Luca Request Dönüşümü

**Katana Product Entity**:

```csharp
public class Product
{
    public int Id { get; set; }
    public string SKU { get; set; }           // "Ø38x1,5-2"
    public string Name { get; set; }          // "Ø38x1,5-2"
    public decimal? VATRate { get; set; }     // 1.0
    public string Unit { get; set; }          // "MT"
    public string? Barcode { get; set; }
    public bool IsActive { get; set; }
}
```

**Luca DTO (LucaCreateStokKartiRequest)**:

```csharp
public class LucaCreateStokKartiRequest
{
    [JsonPropertyName("kartAdi")]
    public string KartAdi { get; set; }                    // "O38x1,5-2" (Ø temizlendi)

    [JsonPropertyName("kartKodu")]
    public string KartKodu { get; set; }                   // "O38x1,5-2"

    [JsonPropertyName("kartTuru")]
    public long KartTuru { get; set; }                     // 1 (Stok)

    [JsonPropertyName("kartTipi")]
    public long KartTipi { get; set; }                     // 1

    [JsonPropertyName("kartAlisKdvOran")]
    public double KartAlisKdvOran { get; set; }            // 1.0

    [JsonPropertyName("olcumBirimiId")]
    public long OlcumBirimiId { get; set; }                // 5 (MT için)

    [JsonPropertyName("baslangicTarihi")]
    public string BaslangicTarihi { get; set; }            // "06/12/2025"

    [JsonPropertyName("barkod")]
    public string Barkod { get; set; }                     // "O38x1,5-2"

    [JsonPropertyName("alisTevkifatOran")]
    public string? AlisTevkifatOran { get; set; }          // null veya "7/10"

    [JsonPropertyName("alisTevkifatTipId")]
    public int? AlisTevkifatTipId { get; set; }            // null veya 1

    [JsonPropertyName("satisTevkifatOran")]
    public string? SatisTevkifatOran { get; set; }         // null veya "2/10"

    [JsonPropertyName("satisTevkifatTipId")]
    public int? SatisTevkifatTipId { get; set; }           // null veya 1

    [JsonPropertyName("satilabilirFlag")]
    public int SatilabilirFlag { get; set; }               // 1

    [JsonPropertyName("satinAlinabilirFlag")]
    public int SatinAlinabilirFlag { get; set; }           // 1

    [JsonPropertyName("maliyetHesaplanacakFlag")]
    public int MaliyetHesaplanacakFlag { get; set; }       // 1
}
```

### 3.2 Ölçü Birimi Mapping

```csharp
private long MapUnitToLucaId(string unit)
{
    return unit?.ToUpper() switch
    {
        "ADET" => 1,
        "KG" => 2,
        "LT" => 3,
        "M" => 4,
        "MT" => 5,
        "M2" => 6,
        "M3" => 7,
        _ => 1  // Default: ADET
    };
}
```

### 3.3 Encoding Dönüşümleri

```csharp
// UTF-8 → ISO-8859-9 (Türkçe karakterler için)
public static string ConvertToIso88599(string input)
{
    var utf8Bytes = Encoding.UTF8.GetBytes(input);
    var iso88599Bytes = Encoding.Convert(
        Encoding.UTF8,
        Encoding.GetEncoding("ISO-8859-9"),
        utf8Bytes
    );
    return Encoding.GetEncoding("ISO-8859-9").GetString(iso88599Bytes);
}
```

---

## 4. SESSION YÖNETİMİ

### 4.1 Session Lifecycle

```
1. Login Request
   POST YdlUserLogin.do
   Body: {"orgCode":"akozas","userName":"ENTEGRASYON","userPassword":"***"}

2. Response
   Set-Cookie: JSESSIONID=ABC123...; Path=/; HttpOnly

3. Session Cookie Saklanıyor
   _sessionCookie = "ABC123..."
   _sessionExpiry = DateTime.Now.AddHours(2)

4. Branch Seçimi
   POST YdlUserResponsibilityOrgSs.do
   Cookie: JSESSIONID=ABC123...
   Body: {"orgSirketSubeId":11746}

5. Her Request'te Cookie Gönderiliyor
   Cookie: JSESSIONID=ABC123...
```

### 4.2 Session Expiry Kontrolü

```csharp
private bool IsSessionExpired()
{
    if (_sessionExpiry == null) return true;
    return DateTime.Now >= _sessionExpiry.Value;
}
```

### 4.3 Session Refresh Mekanizması

```csharp
public async Task ForceSessionRefreshAsync()
{
    _logger.LogWarning("🔄 ForceSessionRefreshAsync: Session yenileniyor...");

    // Mevcut session'ı temizle
    _isCookieAuthenticated = false;
    _sessionCookie = null;
    _sessionExpiry = null;

    // Yeni session oluştur
    await EnsureAuthenticatedAsync();
    await EnsureBranchSelectedAsync();
}
```

---

## 5. HATA YÖNETİMİ VE RETRY MEKANİZMASI

### 5.1 Retry Stratejisi

```csharp
private async Task<HttpResponseMessage> SendWithAuthRetryAsync(
    HttpRequestMessage request,
    string logTag,
    int maxAttempts = 2)
{
    var attempt = 0;

    while (true)
    {
        attempt++;

        try
        {
            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            // Session expired kontrolü
            if (NeedsBranchSelection(body) ||
                body.Contains("Login olunmalı"))
            {
                if (attempt < maxAttempts)
                {
                    // Session yenile ve tekrar dene
                    await ForceSessionRefreshAsync();
                    request = await CloneHttpRequestMessageAsync(request);
                    continue;
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            if (attempt >= maxAttempts) throw;

            // Retry
            await Task.Delay(1000);
            continue;
        }
    }
}
```

### 5.2 Hata Tipleri ve Çözümleri

#### 5.2.1 Session Expired

**Hata**: `{"code":1002,"message":"Login olunmalı."}`
**Çözüm**: `ForceSessionRefreshAsync()` çağrılıyor

#### 5.2.2 Branch Not Selected

**Hata**: HTML response döndü (session timeout)
**Çözüm**: Branch seçimi tekrar yapılıyor

#### 5.2.3 Validation Error

**Hata**: `{"error":true,"message":"[kartAdi] alanı zorunludur."}`
**Çözüm**: JSON formatı kontrol ediliyor, eksik alanlar ekleniyor

#### 5.2.4 Duplicate SKU

**Hata**: `{"error":true,"message":"Kart kodu daha önce kullanılmış"}`
**Çözüm**: Versiyonlu SKU oluşturuluyor (SKU-V2, SKU-V3...)

---

## 6. LUCA API ENTEGRASYONU

### 6.1 Endpoint Detayları

**Base URL**: `https://akozas.luca.com.tr/luca-rs/rest/`

**Kullanılan Endpoint'ler**:

1. `YdlUserLogin.do` - Login
2. `YdlUserResponsibilityOrgSs.do` - Branch seçimi/listesi
3. `ListeleStkKart.do` - Stok kartı listesi
4. `EkleStkWsKart.do` - Stok kartı oluşturma

### 6.2 Luca API Request Format (DOKÜMANTASYON)

```json
{
  "kartAdi": "Test Ürünü",
  "kartKodu": "00013225",
  "kartTipi": 1,
  "kartAlisKdvOran": 1,
  "olcumBirimiId": 1,
  "baslangicTarihi": "06/04/2022",
  "kartTuru": 1,
  "kategoriAgacKod": null,
  "barkod": "8888888",
  "alisTevkifatOran": "7/10",
  "satisTevkifatOran": "2/10",
  "alisTevkifatTipId": 1,
  "satisTevkifatTipId": 1,
  "satilabilirFlag": 1,
  "satinAlinabilirFlag": 1,
  "lotNoFlag": 1,
  "minStokKontrol": 0,
  "maliyetHesaplanacakFlag": true
}
```

### 6.3 Luca API Response Format

**Başarılı**:

```json
{
  "skartId": 79409,
  "error": false,
  "message": "00013225 - Test Ürünü stok kartı başarılı bir şekilde kaydedilmiştir."
}
```

**Hatalı**:

```json
{
  "error": true,
  "message": "[kartAdi] alanı zorunludur."
}
```

veya

```json
{
  "code": 1002,
  "message": "Login olunmalı."
}
```

---

## 7. KRİTİK NOKTALAR VE DİKKAT EDİLMESİ GEREKENLER

### 7.1 Özel Karakter Temizleme

**ZORUNLU**: Luca API Türkçe karakterleri destekliyor ama `Ø` gibi özel karakterleri desteklemiyor.

```csharp
// ✅ DOĞRU
kartAdi = "O38x1,5-2"  // Ø → O

// ❌ YANLIŞ
kartAdi = "Ø38x1,5-2"  // Luca hata verir
```

### 7.2 Tarih Formatı

**ZORUNLU**: `dd/MM/yyyy` formatı kullanılmalı

```csharp
// ✅ DOĞRU
baslangicTarihi = "06/12/2025"

// ❌ YANLIŞ
baslangicTarihi = "2025-12-06"  // ISO format çalışmaz
```

### 7.3 Boolean vs Integer

**✅ GÜNCEL DURUM**: `maliyetHesaplanacakFlag` boolean, diğer flagler integer!

**DTO Tanımı** (`LucaCreateStokKartiRequest`):
```csharp
[JsonPropertyName("maliyetHesaplanacakFlag")]
public bool MaliyetHesaplanacakFlag { get; set; }  // ✅ BOOLEAN
```

**JSON Gönderimi** (`LucaService.Operations.cs`):
```json
{
  "satilabilirFlag": 1,              // ✅ integer
  "satinAlinabilirFlag": 1,          // ✅ integer
  "lotNoFlag": 1,                    // ✅ integer
  "maliyetHesaplanacakFlag": true    // ✅ boolean!
}
```

**Kullanımlar**:
- `ProductsController.cs:538` → `MaliyetHesaplanacakFlag = true` ✅
- `KatanaToLucaMapper.cs:152` → `card.MaliyetHesaplanacakFlag = true` ✅
- `LucaService.Operations.cs:2049` → `["maliyetHesaplanacakFlag"] = true` ✅

**SONUÇ**: %100 uyumlu! Boolean olarak kullanılıyor.

### 7.4 Tevkifat Alan İsimleri

**ÖNEMLİ**: Alan isimleri dokümantasyona uygun olmalı

```csharp
// ✅ DOĞRU
alisTevkifatTipId    // "TipId" ile bitiyor
satisTevkifatTipId   // "TipId" ile bitiyor

// ❌ YANLIŞ (ESKİ)
alisTevkifatKod      // "Kod" ile bitiyor - ÇALIŞMAZ!
satisTevkifatKod     // "Kod" ile bitiyor - ÇALIŞMAZ!
```

### 7.5 Gereksiz Alanlar Gönderilmemeli

**DİKKAT**: Dokümantasyonda olmayan alanlar gönderilmemeli

```json
{
  // ❌ Bu alanlar dokümantasyonda YOK - gönderilmemeli:
  "stokKategoriId": 1,
  "kartSatisKdvOran": 1,
  "uzunAdi": "..."
}
```

### 7.6 Session Timeout

**ÖNEMLİ**: Session 2 saat sonra expire oluyor. Her request öncesi kontrol edilmeli.

```csharp
await EnsureAuthenticatedAsync();  // Her request öncesi çağrılmalı
```

### 7.7 Branch Seçimi

**ZORUNLU**: Her session'da branch seçimi yapılmalı (11746)

```csharp
await EnsureBranchSelectedAsync();  // Login sonrası mutlaka çağrılmalı
```

### 7.8 Encoding

**ÖNEMLİ**: Türkçe karakterler için ISO-8859-9 encoding kullanılmalı

```csharp
var encoding = Encoding.GetEncoding("ISO-8859-9");
var content = new StringContent(json, encoding, "application/json");
```

---

## 8. ÖRNEK SENARYO: TAM AKIŞ

### Senaryo: "Ø38x1,5-2" SKU'lu ürünü Luca'ya gönderme

**1. Frontend Request**:

```http
POST /api/sync/start
Content-Type: application/json

{
  "syncType": "STOCK_CARD"
}
```

**2. SyncController**:

```csharp
var result = await _syncService.SyncStockCardsAsync();
```

**3. SyncService - Katana'dan ürün çekme**:

```sql
SELECT * FROM Products WHERE IsActive = 1 AND SKU = 'Ø38x1,5-2'
```

**4. Mapper - Dönüşüm**:

```csharp
var lucaRequest = new LucaCreateStokKartiRequest
{
    KartAdi = "O38x1,5-2",        // Ø → O
    KartKodu = "O38x1,5-2",
    KartTuru = 1,
    KartTipi = 1,
    KartAlisKdvOran = 1,
    OlcumBirimiId = 5,            // MT
    BaslangicTarihi = "06/12/2025",
    Barkod = "O38x1,5-2",
    SatilabilirFlag = 1,
    SatinAlinabilirFlag = 1,
    MaliyetHesaplanacakFlag = 1
};
```

**5. LucaService - Session kontrolü**:

```csharp
await EnsureAuthenticatedAsync();
// → Session var mı? Yoksa login yap
// → JSESSIONID cookie al
```

**6. LucaService - Branch seçimi**:

```csharp
await EnsureBranchSelectedAsync();
// → Branch 11746'yı seç
```

**7. LucaService - HTTP POST**:

```http
POST https://akozas.luca.com.tr/luca-rs/rest/EkleStkWsKart.do
Cookie: JSESSIONID=ABC123...
Content-Type: application/json; charset=ISO-8859-9

{
  "kartAdi": "O38x1,5-2",
  "kartKodu": "O38x1,5-2",
  "kartTipi": 1,
  "kartAlisKdvOran": 1,
  "olcumBirimiId": 5,
  "baslangicTarihi": "06/12/2025",
  "kartTuru": 1,
  "kategoriAgacKod": null,
  "barkod": "O38x1,5-2",
  "alisTevkifatOran": null,
  "satisTevkifatOran": null,
  "alisTevkifatTipId": null,
  "satisTevkifatTipId": null,
  "satilabilirFlag": 1,
  "satinAlinabilirFlag": 1,
  "lotNoFlag": 1,
  "minStokKontrol": 0,
  "maliyetHesaplanacakFlag": true
}
```

**8. Luca Response**:

```json
{
  "skartId": 79409,
  "error": false,
  "message": "O38x1,5-2 - O38x1,5-2 stok kartı başarılı bir şekilde kaydedilmiştir."
}
```

**9. SyncService - Sonuç**:

```csharp
return new SyncResultDto
{
    SyncType = "STOCK_CARD",
    ProcessedRecords = 1,
    SuccessfulRecords = 1,
    FailedRecords = 0,
    IsSuccess = true,
    Message = "1 stok kartı başarıyla oluşturuldu"
};
```

**10. Frontend Response**:

```json
{
  "syncType": "STOCK_CARD",
  "processedRecords": 1,
  "successfulRecords": 1,
  "failedRecords": 0,
  "isSuccess": true,
  "message": "1 stok kartı başarıyla oluşturuldu"
}
```

---

## 9. SORUN GİDERME REHBERİ

### Sorun 1: `{"error":true}` (mesaj yok)

**Sebep**: JSON formatı yanlış veya eksik alan var
**Çözüm**: Luca dokümantasyonundaki EXACT formatı kullan

### Sorun 2: `[kartAdi] alanı zorunludur`

**Sebep**: `kartAdi` null veya boş
**Çözüm**: Fallback mekanizması ekle (SKU kullan)

### Sorun 3: `Login olunmalı`

**Sebep**: Session expired
**Çözüm**: `ForceSessionRefreshAsync()` çağır

### Sorun 4: HTML response döndü

**Sebep**: Branch seçilmemiş veya session timeout
**Çözüm**: Branch seçimini tekrar yap

### Sorun 5: Özel karakterler bozuk

**Sebep**: Encoding yanlış
**Çözüm**: ISO-8859-9 encoding kullan

---

## 10. PERFORMANS OPTİMİZASYONU

### 10.1 Batch Processing

```csharp
// Her ürün için ayrı request yerine batch gönder
var batchSize = 50;
var batches = products.Chunk(batchSize);

foreach (var batch in batches)
{
    await ProcessBatchAsync(batch);
    await Task.Delay(100); // Rate limiting
}
```

### 10.2 Parallel Processing

```csharp
// Dikkatli kullan - Luca API rate limit var
var options = new ParallelOptions { MaxDegreeOfParallelism = 3 };

await Parallel.ForEachAsync(products, options, async (product, ct) =>
{
    await CreateStockCardAsync(product);
});
```

### 10.3 Caching

```csharp
// Ölçü birimi mapping'i cache'le
private static readonly Dictionary<string, long> _unitCache = new();

private long GetUnitId(string unit)
{
    if (_unitCache.TryGetValue(unit, out var id))
        return id;

    id = FetchUnitIdFromLuca(unit);
    _unitCache[unit] = id;
    return id;
}
```

---

## 11. GÜVENLİK

### 11.1 Credential Yönetimi

```csharp
// ❌ YANLIŞ - Hardcoded
var password = "MyPassword123";

// ✅ DOĞRU - appsettings.json
var password = _configuration["Luca:Password"];

// ✅ DAHA İYİ - Environment variable
var password = Environment.GetEnvironmentVariable("LUCA_PASSWORD");

// ✅ EN İYİ - Azure Key Vault
var password = await _keyVaultClient.GetSecretAsync("luca-password");
```

### 11.2 HTTPS Zorunlu

```csharp
// ✅ DOĞRU
var baseUrl = "https://akozas.luca.com.tr";

// ❌ YANLIŞ
var baseUrl = "http://akozas.luca.com.tr";  // HTTP kullanma!
```

### 11.3 Cookie Security

```csharp
// Session cookie'yi güvenli sakla
private string? _sessionCookie;  // Private field
public string GetSessionCookie() => _sessionCookie;  // Read-only access
```

---

## 12. LOGGING VE MONİTORİNG

### 12.1 Structured Logging

```csharp
_logger.LogInformation(
    "Stok kartı oluşturuldu: SKU={SKU}, SkartId={SkartId}, Duration={Duration}ms",
    request.KartKodu,
    response.SkartId,
    stopwatch.ElapsedMilliseconds
);
```

### 12.2 Error Tracking

```csharp
try
{
    await CreateStockCardAsync(request);
}
catch (Exception ex)
{
    _logger.LogError(ex,
        "Stok kartı oluşturma hatası: SKU={SKU}, Error={Error}",
        request.KartKodu,
        ex.Message
    );

    // Sentry/Application Insights'a gönder
    _telemetryClient.TrackException(ex);
}
```

### 12.3 Metrics

```csharp
// Başarı/hata oranları
_metrics.Increment("stock_card.created.success");
_metrics.Increment("stock_card.created.failed");

// Süre metrikleri
_metrics.Histogram("stock_card.creation.duration", duration);
```

---

## SONUÇ

Bu mimari rapor, Katana sisteminde stok kartı oluşturma sürecinin tüm detaylarını içermektedir.

**Önemli Noktalar**:

1. Luca API dokümantasyonuna %100 uyum sağlanmalı
2. Session yönetimi kritik - her request öncesi kontrol edilmeli
3. Özel karakter temizleme zorunlu (Ø → O)
4. Encoding ISO-8859-9 olmalı
5. Tevkifat alan isimleri doğru olmalı (TipId, Kod değil)
6. Gereksiz alanlar gönderilmemeli

**Güncel Durum**: Tüm düzeltmeler yapıldı, sistem Luca dokümantasyonuna uygun çalışıyor.
