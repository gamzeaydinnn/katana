# Otomatik Migration Uygulama Kılavuzu

## Genel Bakış

`auto-apply-all-migrations.ps1` scripti, tüm eksik database migration'larını otomatik olarak tespit edip uygular.

## Özellikler

✅ **Otomatik Tespit**: Hangi migration'ların uygulandığını takip eder
✅ **Güvenli**: Varsayılan olarak database backup alır
✅ **Akıllı**: Zaten uygulanmış migration'ları tekrar uygulamaz
✅ **Kapsamlı**: Hem EF Core hem de custom SQL script'leri uygular
✅ **Detaylı Raporlama**: Her adımın durumunu gösterir

## Hızlı Başlangıç

### Basit Kullanım

```powershell
.\auto-apply-all-migrations.ps1
```

Bu komut:

1. Docker'ın çalıştığını kontrol eder
2. Database container'ını başlatır
3. Database backup alır
4. EF Core migration'larını uygular
5. Custom SQL script'lerini uygular
6. Backend'i yeniden başlatır

### Parametreler

#### `-Force`

Zaten uygulanmış migration'ları tekrar uygular

```powershell
.\auto-apply-all-migrations.ps1 -Force
```

#### `-SkipBackup`

Backup almadan migration'ları uygular (hızlı test için)

```powershell
.\auto-apply-all-migrations.ps1 -SkipBackup
```

#### `-Verbose`

Detaylı çıktı gösterir (hata ayıklama için)

```powershell
.\auto-apply-all-migrations.ps1 -Verbose
```

#### Kombinasyon

```powershell
.\auto-apply-all-migrations.ps1 -Force -Verbose
```

## Migration Takip Sistemi

Script, `__MigrationHistory` tablosunda hangi migration'ların uygulandığını takip eder.

### Migration Durumunu Görüntüleme

Script sonunda otomatik olarak gösterilir, veya manuel olarak:

```sql
SELECT * FROM __MigrationHistory
ORDER BY AppliedAt DESC
```

### Belirli Bir Migration'ı Kontrol Etme

```sql
SELECT * FROM __MigrationHistory
WHERE ScriptName = 'create_product_luca_mappings.sql'
```

## Uygulanan SQL Script'ler

Script şu sırayla SQL dosyalarını uygular:

1. `create_product_luca_mappings.sql` - Luca mapping tablosu
2. `create_product_luca_mappings_table.sql` - Mapping tablo yapısı
3. `populate_initial_mappings.sql` - İlk mapping verileri
4. `insert_category_mappings.sql` - Kategori mapping'leri
5. `apply_category_mappings.sql` - Kategori mapping uygulaması
6. `apply_category_mappings_fixed.sql` - Düzeltilmiş kategori mapping
7. `update_mapping_266220.sql` - Özel mapping güncellemesi
8. `update_mapping_266220_fix.sql` - Mapping düzeltmesi
9. `update_mapping_266220_dbo.sql` - DBO schema düzeltmesi

## Sorun Giderme

### Docker Çalışmıyor

```
❌ Docker is not running. Please start Docker Desktop.
```

**Çözüm**: Docker Desktop'ı başlatın

### Database Container Başlamıyor

```
❌ Failed to start database container
```

**Çözüm**:

```powershell
docker-compose down
docker-compose up -d db
```

### Migration Başarısız Oluyor

```
❌ Failed: some_script.sql
```

**Çözüm**:

1. Verbose modda çalıştırın:

   ```powershell
   .\auto-apply-all-migrations.ps1 -Verbose
   ```

2. Database loglarını kontrol edin:

   ```powershell
   docker-compose logs db
   ```

3. Script'i manuel olarak test edin:
   ```powershell
   Get-Content db/some_script.sql | docker-compose exec -T db sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB
   ```

### Backup Başarısız Oluyor

```
⚠️  Backup failed, but continuing...
```

**Not**: Script backup başarısız olsa bile devam eder. Kritik bir ortamda çalışıyorsanız, manuel backup alın:

```powershell
# Manuel backup
docker-compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -Q "BACKUP DATABASE KatanaDB TO DISK = '/var/opt/mssql/backup/manual_backup.bak'"
```

## Manuel Migration Yönetimi

### Belirli Bir Migration'ı Tekrar Uygulama

```sql
-- Migration kaydını sil
DELETE FROM __MigrationHistory
WHERE ScriptName = 'some_script.sql';

-- Script'i tekrar çalıştır
```

```powershell
.\auto-apply-all-migrations.ps1
```

### Tüm Migration'ları Sıfırlama

```sql
-- DİKKAT: Bu tüm migration geçmişini siler!
TRUNCATE TABLE __MigrationHistory;
```

```powershell
.\auto-apply-all-migrations.ps1 -Force
```

### Migration Tablosunu Silme

```sql
DROP TABLE __MigrationHistory;
```

Script bir sonraki çalıştırmada tabloyu yeniden oluşturur.

## Yeni SQL Script Ekleme

Script'e yeni bir SQL dosyası eklemek için:

1. SQL dosyasını `db/` klasörüne ekleyin
2. `auto-apply-all-migrations.ps1` dosyasını düzenleyin
3. `$SQL_SCRIPTS` array'ine yeni dosyayı ekleyin:

```powershell
$SQL_SCRIPTS = @(
    "create_product_luca_mappings.sql",
    # ... diğer script'ler ...
    "yeni_script.sql"  # Yeni eklenen
)
```

## Best Practices

### Geliştirme Ortamı

```powershell
# Hızlı test için backup'sız
.\auto-apply-all-migrations.ps1 -SkipBackup
```

### Production Ortamı

```powershell
# Önce manuel backup al
docker-compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -Q "BACKUP DATABASE KatanaDB TO DISK = '/var/opt/mssql/backup/pre_migration_backup.bak'"

# Sonra migration'ları uygula
.\auto-apply-all-migrations.ps1 -Verbose

# Sonuçları kontrol et
.\quick-fix-check.ps1
```

### CI/CD Pipeline

```powershell
# Otomatik deployment için
.\auto-apply-all-migrations.ps1 -SkipBackup -Verbose
if ($LASTEXITCODE -ne 0) {
    Write-Error "Migration failed!"
    exit 1
}
```

## Çıktı Örnekleri

### Başarılı Çalıştırma

```
=== AUTO-APPLY ALL MIGRATIONS ===

1. Checking Docker...
✅ Docker is running

2. Ensuring database is running...
✅ Database is ready

3. Checking database exists...

4. Creating database backup...
✅ Database backed up to: /var/opt/mssql/backup/KatanaDB_20231207_143022.bak

5. Creating migration tracking table...
✅ Migration tracking table ready

=== APPLYING CUSTOM SQL SCRIPTS ===

ℹ️  Applying: create_product_luca_mappings.sql
✅ Applied: create_product_luca_mappings.sql

ℹ️  Already applied: populate_initial_mappings.sql (use -Force to reapply)

=== MIGRATION SUMMARY ===

  Total Scripts: 9
  ✅ Success: 7
  ❌ Failed: 0
  ⏭️  Skipped: 2

✅ All migrations completed successfully!
```

### Hatalı Çalıştırma

```
❌ Failed: some_script.sql
   Error: Invalid object name 'NonExistentTable'

=== MIGRATION SUMMARY ===

  Total Scripts: 9
  ✅ Success: 6
  ❌ Failed: 1
  ⏭️  Skipped: 2

⚠️  Some migrations failed. Review the errors above.
```

## İlgili Komutlar

```powershell
# Migration sonrası kontrol
.\quick-fix-check.ps1

# Backend loglarını görüntüle
docker-compose logs backend

# Database loglarını görüntüle
docker-compose logs db

# Tüm container'ları yeniden başlat
docker-compose restart

# Database'e bağlan
docker-compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB
```

## Sık Sorulan Sorular

### Q: Script her çalıştırmada migration'ları tekrar uygular mı?

**A**: Hayır. Script `__MigrationHistory` tablosunu kontrol eder ve sadece uygulanmamış migration'ları çalıştırır. `-Force` parametresi ile bu davranışı değiştirebilirsiniz.

### Q: Backup nereye kaydediliyor?

**A**: Container içinde `/var/opt/mssql/backup/` klasörüne kaydedilir. Docker volume ile host'a map edilmişse oradan erişebilirsiniz.

### Q: EF Core migration'ları nasıl uygulanıyor?

**A**: Script `dotnet ef database update` komutunu backend container içinde çalıştırır.

### Q: Script başarısız olursa ne olur?

**A**: Script hata mesajlarını gösterir ve exit code 1 ile çıkar. Backup aldıysanız, database'i geri yükleyebilirsiniz.

### Q: Production'da kullanabilir miyim?

**A**: Evet, ama önce test ortamında deneyin ve manuel backup aldığınızdan emin olun.

## Destek

Sorun yaşarsanız:

1. `-Verbose` parametresi ile detaylı log alın
2. `docker-compose logs` ile container loglarını kontrol edin
3. `__MigrationHistory` tablosunu inceleyin
4. Gerekirse migration'ları manuel olarak uygulayın

## Changelog

### v1.0.0 (2024-12-07)

- İlk sürüm
- Otomatik migration tespit ve uygulama
- Migration takip sistemi
- Backup desteği
- Verbose logging
