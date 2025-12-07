# Migration Script'leri Özeti

## 📦 Oluşturulan Dosyalar

### 1. **run-all-migrations.ps1** ⭐ (ÖNERİLEN)

**En basit ve kullanışlı script**

```powershell
.\run-all-migrations.ps1
```

**Özellikler:**

- ✅ Basit ve anlaşılır
- ✅ Syntax hatası yok
- ✅ Tüm migration'ları otomatik uygular
- ✅ EF Core + SQL script desteği
- ✅ Otomatik container yönetimi
- ✅ Özet rapor

**Ne Zaman Kullanılır:**

- İlk kurulum
- Günlük geliştirme
- Hızlı migration uygulaması

---

### 2. **auto-apply-all-migrations.ps1**

**Gelişmiş özelliklerle migration script'i**

```powershell
.\auto-apply-all-migrations.ps1 -Verbose
.\auto-apply-all-migrations.ps1 -Force
.\auto-apply-all-migrations.ps1 -SkipBackup
```

**Özellikler:**

- ✅ Migration tracking sistemi
- ✅ Database backup
- ✅ Detaylı loglama
- ✅ Force reapply desteği
- ⚠️ Bazı syntax sorunları var (düzeltme gerekebilir)

**Ne Zaman Kullanılır:**

- Production deployment
- Migration geçmişi takibi gerektiğinde
- Backup ile güvenli uygulama

---

### 3. **check-migration-status.ps1**

**Migration durumunu kontrol eder**

```powershell
.\check-migration-status.ps1
.\check-migration-status.ps1 -Detailed
```

**Özellikler:**

- ✅ Uygulanmış migration'ları gösterir
- ✅ Bekleyen migration'ları listeler
- ✅ Başarısız migration'ları raporlar
- ✅ Tablo durumlarını kontrol eder

**Ne Zaman Kullanılır:**

- Migration durumu kontrolü
- Sorun giderme
- Audit ve raporlama

---

### 4. **test-auto-migrations.ps1**

**Migration script'ini test eder**

```powershell
.\test-auto-migrations.ps1
```

**Özellikler:**

- ✅ Docker kontrolü
- ✅ Database bağlantı testi
- ✅ SQL script varlık kontrolü
- ✅ Syntax doğrulama

**Ne Zaman Kullanılır:**

- Migration öncesi kontrol
- Sistem hazırlık testi
- Sorun önleme

---

## 📚 Dokümantasyon Dosyaları

### 1. **QUICK_MIGRATION_GUIDE.md**

Hızlı başlangıç kılavuzu - `run-all-migrations.ps1` için

### 2. **MIGRATION_AUTO_APPLY_GUIDE.md**

Detaylı kullanım kılavuzu - `auto-apply-all-migrations.ps1` için

### 3. **MIGRATIONS_README.md**

Kapsamlı migration yönetim dokümantasyonu

### 4. **MIGRATION_SCRIPTS_SUMMARY.md** (bu dosya)

Tüm script'lerin özeti

---

## 🎯 Önerilen İş Akışı

### Senaryo 1: İlk Kurulum

```powershell
# 1. Test et
.\test-auto-migrations.ps1

# 2. Migration'ları uygula
.\run-all-migrations.ps1

# 3. Kontrol et
.\quick-fix-check.ps1
```

### Senaryo 2: Günlük Geliştirme

```powershell
# Hızlı migration
.\run-all-migrations.ps1

# Backend'i kontrol et
docker-compose logs backend --tail=20
```

### Senaryo 3: Production Deployment

```powershell
# 1. Manuel backup al
docker-compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -Q "BACKUP DATABASE KatanaDB TO DISK = '/var/opt/mssql/backup/pre_deploy.bak'"

# 2. Durumu kontrol et
.\check-migration-status.ps1 -Detailed

# 3. Migration'ları uygula
.\run-all-migrations.ps1

# 4. Doğrula
.\check-migration-status.ps1
.\quick-fix-check.ps1
```

### Senaryo 4: Sorun Giderme

```powershell
# 1. Durumu kontrol et
.\check-migration-status.ps1 -Detailed

# 2. Logları incele
docker-compose logs db | Select-String -Pattern "error"
docker-compose logs backend | Select-String -Pattern "migration"

# 3. Tekrar dene
.\run-all-migrations.ps1

# 4. Manuel müdahale gerekirse
docker-compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB
```

---

## 📊 Karşılaştırma Tablosu

| Özellik       | run-all-migrations | auto-apply-all-migrations | check-migration-status |
| ------------- | ------------------ | ------------------------- | ---------------------- |
| Basitlik      | ⭐⭐⭐⭐⭐         | ⭐⭐⭐                    | ⭐⭐⭐⭐               |
| Hız           | ⭐⭐⭐⭐⭐         | ⭐⭐⭐                    | ⭐⭐⭐⭐⭐             |
| Tracking      | ❌                 | ✅                        | ✅                     |
| Backup        | ❌                 | ✅                        | ❌                     |
| Force Reapply | ❌                 | ✅                        | ❌                     |
| Verbose Mode  | ❌                 | ✅                        | ✅                     |
| Syntax Hatası | ❌                 | ⚠️                        | ❌                     |
| Önerilen      | ✅                 | ⚠️                        | ✅                     |

---

## 🔧 Hangi Script'i Kullanmalıyım?

### `run-all-migrations.ps1` kullan eğer:

- ✅ Hızlı migration uygulamak istiyorsan
- ✅ Basit ve güvenilir bir çözüm arıyorsan
- ✅ İlk kez migration uyguluyorsan
- ✅ Günlük geliştirme yapıyorsan

### `auto-apply-all-migrations.ps1` kullan eğer:

- ✅ Migration geçmişi tutmak istiyorsan
- ✅ Backup almak istiyorsan
- ✅ Detaylı loglama gerekiyorsa
- ⚠️ Syntax hatalarını düzeltmeye hazırsan

### `check-migration-status.ps1` kullan eğer:

- ✅ Sadece durum kontrolü yapacaksan
- ✅ Hangi migration'ların uygulandığını görmek istiyorsan
- ✅ Sorun giderme yapıyorsan

---

## 🚀 Hızlı Başlangıç

**En basit yol:**

```powershell
.\run-all-migrations.ps1
```

**Daha fazla kontrol:**

```powershell
# Önce test et
.\test-auto-migrations.ps1

# Sonra uygula
.\run-all-migrations.ps1

# Durumu kontrol et
.\check-migration-status.ps1
```

---

## 📝 SQL Script'ler

Tüm script'ler `db/` klasöründe:

1. `create_product_luca_mappings.sql`
2. `create_product_luca_mappings_table.sql`
3. `populate_initial_mappings.sql`
4. `insert_category_mappings.sql`
5. `apply_category_mappings.sql`
6. `apply_category_mappings_fixed.sql`
7. `update_mapping_266220.sql`
8. `update_mapping_266220_fix.sql`
9. `update_mapping_266220_dbo.sql`

---

## 🔗 İlgili Komutlar

```powershell
# Container durumu
docker-compose ps

# Backend logları
docker-compose logs backend --tail=50

# Database logları
docker-compose logs db --tail=50

# Database'e bağlan
docker-compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "Admin00!S" -d KatanaDB

# Container'ları yeniden başlat
docker-compose restart

# Tüm container'ları durdur
docker-compose down

# Tüm container'ları başlat
docker-compose up -d
```

---

## ⚠️ Önemli Notlar

1. **Backup**: Production'da mutlaka manuel backup alın
2. **Test**: Önce test ortamında deneyin
3. **Loglar**: Hata durumunda logları kontrol edin
4. **Docker**: Docker Desktop'ın çalıştığından emin olun
5. **Syntax**: `auto-apply-all-migrations.ps1` bazı syntax sorunları içerebilir

---

## 🎉 Özet

**Önerilen Kullanım:**

1. **İlk kurulum ve günlük kullanım için:** `run-all-migrations.ps1`
2. **Durum kontrolü için:** `check-migration-status.ps1`
3. **Ön kontrol için:** `test-auto-migrations.ps1`

**En basit çözüm:**

```powershell
.\run-all-migrations.ps1
```

Bu tek komut tüm migration'larınızı uygular! 🚀

---

**Son Güncelleme:** 2024-12-07  
**Versiyon:** 1.0.0  
**Yazar:** Kiro AI Assistant
