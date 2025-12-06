# Hızlı Migration Kılavuzu

## 🚀 Tek Komutla Tüm Migration'ları Uygula

```powershell
.\run-all-migrations.ps1
```

Bu script:

- ✅ Docker'ı kontrol eder
- ✅ Container'ları başlatır
- ✅ Database'in hazır olmasını bekler
- ✅ EF Core migration'larını uygular
- ✅ Tüm SQL script'lerini sırayla uygular
- ✅ Backend'i yeniden başlatır
- ✅ Özet rapor gösterir

## 📋 Uygulanan Migration'lar

Script şu migration'ları sırayla uygular:

1. `create_product_luca_mappings.sql`
2. `create_product_luca_mappings_table.sql`
3. `populate_initial_mappings.sql`
4. `insert_category_mappings.sql`
5. `apply_category_mappings.sql`
6. `apply_category_mappings_fixed.sql`
7. `update_mapping_266220.sql`
8. `update_mapping_266220_fix.sql`
9. `update_mapping_266220_dbo.sql`

## 💡 Kullanım Örnekleri

### İlk Kurulum

```powershell
# Docker'ı başlat
# Sonra migration'ları çalıştır
.\run-all-migrations.ps1

# Sonuçları kontrol et
.\quick-fix-check.ps1
```

### Güncellemeler

```powershell
# Yeni migration'lar varsa
.\run-all-migrations.ps1

# Backend loglarını kontrol et
docker-compose logs backend
```

## 🔍 Sorun Giderme

### Docker Çalışmıyor

```
ERROR: Docker is not running!
```

**Çözüm**: Docker Desktop'ı başlatın

### Database Timeout

```
ERROR: Database timeout
```

**Çözüm**:

```powershell
docker-compose down
docker-compose up -d db
Start-Sleep -Seconds 15
.\run-all-migrations.ps1
```

### Migration Başarısız

```
WARNING: some_script.sql may have issues
```

**Çözüm**:

```powershell
# Logları kontrol et
docker-compose logs db | Select-String -Pattern "error"

# Manuel olarak uygula
Get-Content db/some_script.sql | docker-compose exec -T db sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB
```

## 📊 Çıktı Örneği

```
=== RUNNING ALL MIGRATIONS ===

Checking Docker...
OK: Docker is running

Starting containers...
OK: Containers started

Waiting for database...
OK: Database is ready

Applying EF Core migrations...
OK: EF migrations applied

Applying SQL scripts...
  Applying: create_product_luca_mappings.sql
  OK: create_product_luca_mappings.sql
  Applying: populate_initial_mappings.sql
  OK: populate_initial_mappings.sql
  ...

Restarting backend...
OK: Backend restarted

=== SUMMARY ===
  Total: 9
  Success: 9
  Issues: 0

SUCCESS: All migrations completed!

Next: Run .\quick-fix-check.ps1
```

## 🔗 İlgili Script'ler

- `run-all-migrations.ps1` - Ana migration script'i (basit ve hızlı)
- `auto-apply-all-migrations.ps1` - Gelişmiş versiyon (tracking ile)
- `check-migration-status.ps1` - Migration durumunu kontrol eder
- `test-auto-migrations.ps1` - Ön kontroller yapar

## ⚡ Hızlı Komutlar

```powershell
# Migration'ları uygula
.\run-all-migrations.ps1

# Durumu kontrol et
docker-compose ps

# Backend logları
docker-compose logs backend --tail=50

# Database'e bağlan
docker-compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB

# Tüm container'ları yeniden başlat
docker-compose restart
```

## 📝 Notlar

- Script her çalıştırmada tüm migration'ları tekrar uygulamaya çalışır
- Zaten uygulanmış migration'lar genellikle hata vermez (idempotent)
- Backup almak isterseniz önce manuel backup alın
- Production'da kullanmadan önce test ortamında deneyin

## 🎯 Sonraki Adımlar

Migration'lar başarılı olduktan sonra:

1. Backend'in çalıştığını kontrol edin:

   ```powershell
   docker-compose logs backend
   ```

2. API'yi test edin:

   ```powershell
   .\quick-fix-check.ps1
   ```

3. Luca entegrasyonunu test edin:
   ```powershell
   .\test-luca-direct.ps1
   ```

---

**Son Güncelleme:** 2024-12-07  
**Versiyon:** 1.0.0
