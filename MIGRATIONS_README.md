# Database Migration Yönetimi

Bu klasörde database migration'larını otomatik olarak yönetmek için gerekli tüm araçlar bulunmaktadır.

## 🚀 Hızlı Başlangıç

```powershell
# 1. Önce test et
.\test-auto-migrations.ps1

# 2. Migration'ları uygula
.\auto-apply-all-migrations.ps1

# 3. Durumu kontrol et
.\check-migration-status.ps1
```

## 📋 Mevcut Script'ler

### 1. `auto-apply-all-migrations.ps1` ⭐

**Ana migration script'i** - Tüm eksik migration'ları otomatik uygular

```powershell
# Basit kullanım
.\auto-apply-all-migrations.ps1

# Tüm migration'ları tekrar uygula
.\auto-apply-all-migrations.ps1 -Force

# Backup almadan uygula (test için)
.\auto-apply-all-migrations.ps1 -SkipBackup

# Detaylı çıktı
.\auto-apply-all-migrations.ps1 -Verbose

# Kombinasyon
.\auto-apply-all-migrations.ps1 -Force -Verbose
```

**Özellikler:**

- ✅ Otomatik migration tespit
- ✅ Database backup
- ✅ EF Core migration desteği
- ✅ Custom SQL script desteği
- ✅ Migration takip sistemi
- ✅ Detaylı hata raporlama

### 2. `check-migration-status.ps1`

Migration durumunu kontrol eder

```powershell
# Basit kontrol
.\check-migration-status.ps1

# Detaylı bilgi (tablo sayıları vs.)
.\check-migration-status.ps1 -Detailed
```

**Gösterir:**

- ✅ Uygulanmış migration'lar
- ⏳ Bekleyen migration'lar
- ❌ Başarısız migration'lar
- 📊 Özet istatistikler

### 3. `test-auto-migrations.ps1`

Migration script'ini çalıştırmadan önce test eder

```powershell
.\test-auto-migrations.ps1
```

**Kontrol eder:**

- Docker durumu
- Database bağlantısı
- SQL script varlığı
- Script syntax'ı

## 📁 SQL Script'ler

`db/` klasöründeki SQL dosyaları şu sırayla uygulanır:

1. ✅ `create_product_luca_mappings.sql`
2. ✅ `create_product_luca_mappings_table.sql`
3. ✅ `populate_initial_mappings.sql`
4. ✅ `insert_category_mappings.sql`
5. ✅ `apply_category_mappings.sql`
6. ✅ `apply_category_mappings_fixed.sql`
7. ✅ `update_mapping_266220.sql`
8. ✅ `update_mapping_266220_fix.sql`
9. ✅ `update_mapping_266220_dbo.sql`

## 🔄 Tipik İş Akışı

### Yeni Ortam Kurulumu

```powershell
# 1. Docker'ı başlat
docker-compose up -d

# 2. Test et
.\test-auto-migrations.ps1

# 3. Migration'ları uygula
.\auto-apply-all-migrations.ps1 -Verbose

# 4. Kontrol et
.\check-migration-status.ps1 -Detailed

# 5. Backend'i test et
.\quick-fix-check.ps1
```

### Günlük Geliştirme

```powershell
# Hızlı migration kontrolü
.\check-migration-status.ps1

# Yeni migration varsa uygula
.\auto-apply-all-migrations.ps1 -SkipBackup
```

### Production Deployment

```powershell
# 1. Manuel backup al
docker-compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -Q "BACKUP DATABASE KatanaDB TO DISK = '/var/opt/mssql/backup/pre_deploy.bak'"

# 2. Migration durumunu kontrol et
.\check-migration-status.ps1 -Detailed

# 3. Migration'ları uygula (verbose mode)
.\auto-apply-all-migrations.ps1 -Verbose

# 4. Sonuçları doğrula
.\check-migration-status.ps1 -Detailed

# 5. Uygulama testleri
.\quick-fix-check.ps1
```

## 🔍 Migration Takip Sistemi

Migration'lar `__MigrationHistory` tablosunda takip edilir:

```sql
-- Tüm migration'ları göster
SELECT * FROM __MigrationHistory
ORDER BY AppliedAt DESC;

-- Başarılı migration'lar
SELECT * FROM __MigrationHistory
WHERE Success = 1;

-- Başarısız migration'lar
SELECT * FROM __MigrationHistory
WHERE Success = 0;

-- Belirli bir script'i kontrol et
SELECT * FROM __MigrationHistory
WHERE ScriptName = 'some_script.sql';
```

## 🛠️ Sorun Giderme

### Problem: Docker çalışmıyor

```powershell
# Docker Desktop'ı başlat
# Sonra tekrar dene
.\test-auto-migrations.ps1
```

### Problem: Database bağlantısı yok

```powershell
# Container'ları yeniden başlat
docker-compose down
docker-compose up -d

# 10 saniye bekle
Start-Sleep -Seconds 10

# Tekrar dene
.\auto-apply-all-migrations.ps1
```

### Problem: Migration başarısız oluyor

```powershell
# Detaylı log al
.\auto-apply-all-migrations.ps1 -Verbose

# Database loglarını kontrol et
docker-compose logs db | Select-String -Pattern "error" -Context 2

# Backend loglarını kontrol et
docker-compose logs backend | Select-String -Pattern "migration" -Context 2
```

### Problem: Migration tekrar uygulanmıyor

```powershell
# Force flag ile tekrar uygula
.\auto-apply-all-migrations.ps1 -Force

# Veya migration kaydını sil
docker-compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB -Q "DELETE FROM __MigrationHistory WHERE ScriptName = 'some_script.sql'"
```

### Problem: Backup başarısız

```powershell
# Backup klasörünü kontrol et
docker-compose exec db ls -la /var/opt/mssql/backup/

# Backup'sız devam et (sadece test için!)
.\auto-apply-all-migrations.ps1 -SkipBackup
```

## 📊 Migration Durumu Örnekleri

### Başarılı Durum

```
=== MIGRATION STATUS CHECK ===

✅ Database connection OK
✅ Migration tracking table exists

=== APPLIED MIGRATIONS ===

ScriptName                              | AppliedAt           | Status
----------------------------------------|---------------------|--------
apply_category_mappings_fixed.sql       | 2024-12-07 14:30:22 | Success
populate_initial_mappings.sql           | 2024-12-07 14:30:15 | Success
create_product_luca_mappings.sql        | 2024-12-07 14:30:10 | Success

=== SUMMARY ===

  Total Migrations: 9
  ✅ Successful: 9
  ❌ Failed: 0

=== PENDING MIGRATIONS ===

✅ No pending migrations - all up to date!
```

### Bekleyen Migration'lar

```
=== PENDING MIGRATIONS ===

⚠️  Found 3 pending migration(s):

  - update_mapping_266220.sql
  - update_mapping_266220_fix.sql
  - update_mapping_266220_dbo.sql

ℹ️  Run: .\auto-apply-all-migrations.ps1 to apply pending migrations
```

## 🔐 Güvenlik Notları

- ⚠️ Script'ler `sa` kullanıcısı ve şifresini içerir
- ⚠️ Production'da farklı credentials kullanın
- ⚠️ Script'leri version control'e commit etmeden önce şifreleri değiştirin
- ✅ Her zaman production'da backup alın

## 📝 Yeni Migration Ekleme

1. SQL dosyasını `db/` klasörüne ekle
2. `auto-apply-all-migrations.ps1` dosyasını aç
3. `$SQL_SCRIPTS` array'ine ekle:

```powershell
$SQL_SCRIPTS = @(
    # ... mevcut script'ler ...
    "yeni_migration.sql"  # Yeni eklenen
)
```

4. Test et:

```powershell
.\test-auto-migrations.ps1
.\auto-apply-all-migrations.ps1 -Verbose
```

## 🔗 İlgili Dosyalar

- `MIGRATION_AUTO_APPLY_GUIDE.md` - Detaylı kullanım kılavuzu
- `db/README_apply_category_mappings.md` - Kategori mapping'leri hakkında
- `apply-migrations.ps1` - Eski migration script'i (deprecated)
- `apply-migrations-simple.ps1` - Basit migration script'i (deprecated)

## 📞 Destek

Sorun yaşarsanız:

1. `-Verbose` flag ile çalıştırın
2. `check-migration-status.ps1 -Detailed` ile durumu kontrol edin
3. Docker loglarını inceleyin: `docker-compose logs`
4. `__MigrationHistory` tablosunu kontrol edin

## 🎯 Best Practices

✅ **DO:**

- Her zaman önce test ortamında deneyin
- Production'da backup alın
- Migration'ları version control'de tutun
- Migration'ları sıralı numaralandırın
- Verbose mode kullanarak log alın

❌ **DON'T:**

- Production'da `-SkipBackup` kullanmayın
- Migration'ları manuel olarak düzenlemeyin
- `__MigrationHistory` tablosunu manuel olarak değiştirmeyin
- Başarısız migration'ları görmezden gelmeyin

## 📈 Gelecek Geliştirmeler

- [ ] Rollback desteği
- [ ] Migration versiyonlama
- [ ] Otomatik test suite
- [ ] Email bildirimleri
- [ ] Slack entegrasyonu
- [ ] Migration diff görüntüleme
- [ ] Paralel migration desteği

---

**Son Güncelleme:** 2024-12-07  
**Versiyon:** 1.0.0  
**Yazar:** Kiro AI Assistant
