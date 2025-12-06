# Migration Dosyaları İndeksi

## 📁 Oluşturulan Tüm Dosyalar

### 🚀 Çalıştırılabilir Script'ler

| Dosya                             | Boyut   | Açıklama                                         | Durum                   |
| --------------------------------- | ------- | ------------------------------------------------ | ----------------------- |
| **run-all-migrations.ps1**        | 3.7 KB  | **ÖNERİLEN** - Basit ve hızlı migration script'i | ✅ Hazır                |
| **check-migration-status.ps1**    | 7.0 KB  | Migration durumunu kontrol eder                  | ✅ Hazır                |
| **test-auto-migrations.ps1**      | 5.0 KB  | Sistem hazırlığını test eder                     | ✅ Hazır                |
| **auto-apply-all-migrations.ps1** | 14.5 KB | Gelişmiş migration script'i (tracking + backup)  | ⚠️ Syntax sorunları var |
| **auto-apply-migrations-v2.ps1**  | 9.3 KB  | Gelişmiş script v2                               | ⚠️ Syntax sorunları var |

### 📚 Dokümantasyon Dosyaları

| Dosya                             | Boyut    | Açıklama                                     |
| --------------------------------- | -------- | -------------------------------------------- |
| **START_HERE_MIGRATIONS.md**      | 4.6 KB   | **BURADAN BAŞLA** - Hızlı başlangıç kılavuzu |
| **QUICK_MIGRATION_GUIDE.md**      | 3.9 KB   | `run-all-migrations.ps1` için kılavuz        |
| **MIGRATION_SCRIPTS_SUMMARY.md**  | 7.4 KB   | Tüm script'lerin karşılaştırmalı özeti       |
| **MIGRATIONS_README.md**          | 8.1 KB   | Kapsamlı migration yönetim dokümantasyonu    |
| **MIGRATION_AUTO_APPLY_GUIDE.md** | 8.4 KB   | Gelişmiş script için detaylı kılavuz         |
| **MIGRATION_FILES_INDEX.md**      | Bu dosya | Tüm dosyaların indeksi                       |

---

## 🎯 Hangi Dosyayı Okumalıyım?

### Hızlı Başlangıç İçin

👉 **START_HERE_MIGRATIONS.md**

### Basit Migration İçin

👉 **QUICK_MIGRATION_GUIDE.md**

### Tüm Script'leri Karşılaştırmak İçin

👉 **MIGRATION_SCRIPTS_SUMMARY.md**

### Detaylı Bilgi İçin

👉 **MIGRATIONS_README.md**

---

## 🚀 Hangi Script'i Çalıştırmalıyım?

### İlk Kurulum ve Günlük Kullanım

```powershell
.\run-all-migrations.ps1
```

✅ **ÖNERİLEN** - Basit, hızlı, güvenilir

### Migration Durumu Kontrolü

```powershell
.\check-migration-status.ps1
```

✅ Hangi migration'ların uygulandığını gösterir

### Sistem Hazırlık Testi

```powershell
.\test-auto-migrations.ps1
```

✅ Migration öncesi kontroller

### Gelişmiş Özellikler (Tracking + Backup)

```powershell
.\auto-apply-all-migrations.ps1
```

⚠️ Syntax sorunları var, düzeltme gerekebilir

---

## 📊 Dosya Durumları

### ✅ Kullanıma Hazır

- `run-all-migrations.ps1` - **ÖNERİLEN**
- `check-migration-status.ps1`
- `test-auto-migrations.ps1`
- Tüm dokümantasyon dosyaları

### ⚠️ Dikkat Gerektirir

- `auto-apply-all-migrations.ps1` - Syntax sorunları var
- `auto-apply-migrations-v2.ps1` - Syntax sorunları var

---

## 🔄 Tipik İş Akışı

### 1. İlk Kurulum

```powershell
# Adım 1: Dokümantasyonu oku
# START_HERE_MIGRATIONS.md

# Adım 2: Sistemi test et
.\test-auto-migrations.ps1

# Adım 3: Migration'ları uygula
.\run-all-migrations.ps1

# Adım 4: Durumu kontrol et
.\check-migration-status.ps1
```

### 2. Günlük Geliştirme

```powershell
# Tek komut yeterli
.\run-all-migrations.ps1
```

### 3. Sorun Giderme

```powershell
# Durumu kontrol et
.\check-migration-status.ps1 -Detailed

# Logları incele
docker-compose logs backend
docker-compose logs db

# Tekrar dene
.\run-all-migrations.ps1
```

---

## 📝 SQL Script'ler

Tüm SQL script'leri `db/` klasöründe:

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

## 🎓 Öğrenme Yolu

### Seviye 1: Başlangıç

1. `START_HERE_MIGRATIONS.md` oku
2. `run-all-migrations.ps1` çalıştır
3. Başarılı! 🎉

### Seviye 2: Orta

1. `QUICK_MIGRATION_GUIDE.md` oku
2. `check-migration-status.ps1` kullan
3. Sorun giderme öğren

### Seviye 3: İleri

1. `MIGRATION_SCRIPTS_SUMMARY.md` oku
2. Tüm script'leri karşılaştır
3. Kendi script'ini yaz

### Seviye 4: Uzman

1. `MIGRATIONS_README.md` oku
2. `MIGRATION_AUTO_APPLY_GUIDE.md` oku
3. Gelişmiş özellikleri kullan

---

## 🔗 Hızlı Linkler

### Dokümantasyon

- [Buradan Başla](START_HERE_MIGRATIONS.md)
- [Hızlı Kılavuz](QUICK_MIGRATION_GUIDE.md)
- [Script Özeti](MIGRATION_SCRIPTS_SUMMARY.md)
- [Detaylı README](MIGRATIONS_README.md)

### Script'ler

- [Ana Script](run-all-migrations.ps1) - **ÖNERİLEN**
- [Durum Kontrolü](check-migration-status.ps1)
- [Test Script](test-auto-migrations.ps1)

---

## 💡 Önemli Notlar

### ✅ Yapılması Gerekenler

- Docker Desktop'ı çalıştır
- `run-all-migrations.ps1` kullan
- Sonuçları kontrol et
- Logları incele

### ❌ Yapılmaması Gerekenler

- Production'da backup almadan migration uygulama
- Syntax sorunu olan script'leri kullanma
- Docker olmadan çalıştırmaya çalışma
- Hata mesajlarını görmezden gelme

---

## 🆘 Yardım

### Sorun Yaşıyorsan

1. **İlk olarak:**

   - `START_HERE_MIGRATIONS.md` oku
   - Docker'ın çalıştığını kontrol et

2. **Sonra:**

   - `test-auto-migrations.ps1` çalıştır
   - Hata mesajlarını oku

3. **Hala sorun varsa:**

   - `QUICK_MIGRATION_GUIDE.md` sorun giderme bölümüne bak
   - Docker loglarını kontrol et

4. **Son çare:**
   - Container'ları yeniden başlat
   - Temiz kurulum yap

---

## 📈 Versiyon Geçmişi

### v1.0.0 (2024-12-07)

- ✅ İlk sürüm
- ✅ 5 çalıştırılabilir script
- ✅ 6 dokümantasyon dosyası
- ✅ Kapsamlı kılavuzlar
- ⚠️ Bazı script'lerde syntax sorunları

---

## 🎯 Özet

**En basit yol:**

```powershell
.\run-all-migrations.ps1
```

**Daha fazla bilgi:**

- `START_HERE_MIGRATIONS.md` oku

**Sorun mu var:**

- `QUICK_MIGRATION_GUIDE.md` sorun giderme bölümü

**Tüm detaylar:**

- `MIGRATION_SCRIPTS_SUMMARY.md` karşılaştırma tablosu

---

**Son Güncelleme:** 2024-12-07  
**Toplam Dosya:** 11 (5 script + 6 dokümantasyon)  
**Toplam Boyut:** ~75 KB  
**Durum:** ✅ Kullanıma Hazır

**Kolay gelsin!** 🚀
