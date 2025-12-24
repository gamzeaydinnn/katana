# Hızlı Başlangıç Rehberi - Veri Temizliği

## 🎯 Hedef

Luca'da hatalı stok kartlarını temizle → Siparişleri reset et → Yeni mantığı test et

## ⚡ 5 Dakikalık Özet

### Sorun

```
Luca'da: 287 hatalı kart (?, -V2, ABCABC)
Katana'da: 45 sipariş bu kartlara bağlı
Sonuç: Yeni mantık test edilemiyor
```

### Çözüm

```
1. Luca'da hatalı kartları sil
2. Siparişleri "gönderilmemiş" yap (IsSyncedToLuca = false)
3. Ürünleri inactive yap (IsActive = false)
4. Yeni mantığı test et
```

### Güvenlik

```
✓ Backup al
✓ Soft reset (silme değil)
✓ Geri dönüş mekanizması
✓ Audit log
```

---

## 📋 Yapılacaklar (Sırasıyla)

### Gün 1: Hazırlık (2 saat)

```bash
# 1. Backup al
BACKUP DATABASE [KatanaIntegration] TO DISK = 'C:\Backups\PreCleanup.bak'

# 2. Migration oluştur
dotnet ef migrations add AddSyncFlagsToSalesOrderLines
dotnet ef migrations add AddCleanupFlagsToProducts

# 3. Migration'ları uygula
dotnet ef database update

# 4. Servisleri implement et
# - DataCleanupService.cs
# - SoftResetService.cs
# - RollbackService.cs

# 5. API endpoint'lerini ekle
# - DataCleanupController.cs
```

### Gün 2: Analiz (1 saat)

```bash
# 1. Uygulamayı başlat
dotnet run

# 2. Dashboard'u aç
# GET http://localhost:5000/api/admin/cleanup/preview

# 3. İstatistikleri incele
# - Kaç hatalı kart?
# - Kaç sipariş etkilendi?
# - Veri kalitesi skoru?

# 4. Müşteriye rapor sun
```

### Gün 3: Temizlik (2 saat)

```bash
# 1. Müşteri onayını al
# "Devam etmemi onaylıyor musunuz?"

# 2. Temizliği başlat
# POST http://localhost:5000/api/admin/cleanup/execute
# Body: { "adminConfirmation": true }

# 3. İşlemi izle
# - Luca'da kartlar siliniyor
# - Siparişler reset ediliyor
# - Ürünler inactive yapılıyor

# 4. Audit log'u kontrol et
SELECT * FROM DataCleanupAudit ORDER BY PerformedAt DESC
```

### Gün 4: Doğrulama (1 saat)

```bash
# 1. Luca'da kontrol et
# - Hatalı kartlar silindi mi?

# 2. Katana'da kontrol et
SELECT * FROM SalesOrderLines WHERE IsSyncedToLuca = 0

# 3. Yeni mantığı test et
# - Temiz verilerle gruplandırma çalışıyor mu?

# 4. Başarı kriterleri
# ✓ Hatalı kartlar silindi
# ✓ Siparişler reset edildi
# ✓ Yeni mantık çalışıyor
# ✓ Müşteri memnun
```

---

## 🔧 Kod Şablonları

### Migration Şablonu

```csharp
// Migrations/20240101_AddSyncFlags.cs
public partial class AddSyncFlags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsSyncedToLuca",
            table: "SalesOrderLines",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "LukaErrorLog",
            table: "SalesOrderLines",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsSyncedToLuca", table: "SalesOrderLines");
        migrationBuilder.DropColumn(name: "LukaErrorLog", table: "SalesOrderLines");
    }
}
```

### Service Şablonu

```csharp
// DataCleanupService.cs
public async Task<List<BadStockCardInfo>> IdentifyBadStockCardsAsync()
{
    var allCards = await _lucaService.ListStockCardsAsync();
    var badCards = new List<BadStockCardInfo>();

    foreach (var card in allCards)
    {
        var issues = new List<string>();

        // ? karakteri
        if (card.StokAdi?.Contains('?') == true)
            issues.Add("CharacterEncoding");

        // -V2, -V3
        if (Regex.IsMatch(card.StokKodu ?? "", @"-V\d+$"))
            issues.Add("Versioning");

        // ABCABC
        if (IsConcatenationError(card.StokKodu))
            issues.Add("Concatenation");

        if (issues.Any())
            badCards.Add(new BadStockCardInfo
            {
                SkartId = card.SkartId,
                StokKodu = card.StokKodu,
                Issues = issues
            });
    }

    return badCards;
}
```

### API Endpoint Şablonu

```csharp
// DataCleanupController.cs
[HttpGet("preview")]
public async Task<ActionResult<DataCleanupDashboardDto>> PreviewCleanup()
{
    var badCards = await _cleanupService.IdentifyBadStockCardsAsync();

    return Ok(new DataCleanupDashboardDto
    {
        Statistics = new CleanupStatistics
        {
            BadStockCards = badCards.Count,
            EncodingIssues = badCards.Count(c => c.Issues.Contains("CharacterEncoding")),
            VersioningIssues = badCards.Count(c => c.Issues.Contains("Versioning"))
        },
        BadStockCards = badCards
    });
}

[HttpPost("execute")]
public async Task<ActionResult<CleanupExecutionResult>> ExecuteCleanup(
    [FromBody] CleanupExecutionRequest request)
{
    if (!request.AdminConfirmation)
        return BadRequest("Admin onayı gerekli");

    var badCards = await _cleanupService.IdentifyBadStockCardsAsync();
    return Ok(await _cleanupService.DeleteBadStockCardsAsync(badCards));
}
```

---

## 🚨 Kritik Noktalar

### ❌ YAPMA

```
❌ Backup almadan silme
❌ Hard delete (DELETE FROM)
❌ Admin onayı almadan işlem yapma
❌ Audit log tutmadan işlem yapma
❌ Geri dönüş mekanizması olmadan başlama
```

### ✅ YAP

```
✅ Backup al (BACKUP DATABASE)
✅ Soft reset (IsActive = false)
✅ Admin onayı al (Preview göster)
✅ Audit log tut (Her işlem kaydedilsin)
✅ Geri dönüş planı hazırla (Rollback service)
```

---

## 📊 Başarı Göstergeleri

```
Başlamadan Önce:
- Hatalı Kartlar: 287
- Etkilenen Siparişler: 45
- Veri Kalitesi: 94.7%

Temizlikten Sonra:
- Hatalı Kartlar: 0
- Etkilenen Siparişler: 0
- Veri Kalitesi: 100%
```

---

## 🆘 Sorun Giderme

### Sorun: Luca API'si bağlantı hatası veriyor

```csharp
// Çözüm: Retry mekanizması ekle
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, retryAttempt =>
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

await retryPolicy.ExecuteAsync(() =>
    _lucaService.DeleteStockCardAsync(cardId));
```

### Sorun: Temizlik yarıda kaldı

```csharp
// Çözüm: Geri dönüş yap
await _rollbackService.RollbackCleanupAsync(startTime);
```

### Sorun: Siparişler hala sync edilmiş görünüyor

```sql
-- Çözüm: Flagları kontrol et
SELECT * FROM SalesOrderLines
WHERE IsSyncedToLuca = 1 AND SKU LIKE '%?%'
```

---

## 📞 Yardım Gerekirse

1. **Audit Log'u Kontrol Et**

   ```sql
   SELECT * FROM DataCleanupAudit
   WHERE Status = 'FAILED'
   ORDER BY PerformedAt DESC
   ```

2. **Backup'tan Geri Dön**

   ```sql
   RESTORE DATABASE [KatanaIntegration]
   FROM DISK = 'C:\Backups\PreCleanup.bak'
   ```

3. **Rollback Service'i Çalıştır**
   ```csharp
   await _rollbackService.RollbackCleanupAsync(startTime);
   ```

---

## ✨ Sonuç

Bu rehberi takip ederek:

- ✅ Hatalı veriler temizlenir
- ✅ Siparişler yeniden gönderilir
- ✅ Yeni mantık test edilir
- ✅ Müşteri memnun olur
- ✅ Sistem stabil kalır

**Başarılar!** 🚀
