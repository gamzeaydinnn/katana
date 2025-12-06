# 🚀 Migration Script'leri - Buradan Başla!

## ⚡ Tek Komut ile Tüm Migration'ları Uygula

```powershell
.\run-all-migrations.ps1
```

**Bu kadar basit!** 🎉

---

## 📖 Ne Yapar?

Bu script otomatik olarak:

1. ✅ Docker'ın çalıştığını kontrol eder
2. ✅ Database container'ını başlatır
3. ✅ Database'in hazır olmasını bekler
4. ✅ EF Core migration'larını uygular
5. ✅ 9 adet SQL script'ini sırayla çalıştırır
6. ✅ Backend'i yeniden başlatır
7. ✅ Sonuç raporunu gösterir

---

## 🎯 Hızlı Başlangıç

### Adım 1: Docker'ı Başlat

Docker Desktop'ı açın ve çalıştığından emin olun.

### Adım 2: Migration'ları Uygula

```powershell
.\run-all-migrations.ps1
```

### Adım 3: Sonuçları Kontrol Et

```powershell
.\quick-fix-check.ps1
```

**Hepsi bu kadar!** ✨

---

## 📊 Başarılı Çıktı Örneği

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
  ... (7 more scripts)

Restarting backend...
OK: Backend restarted

=== SUMMARY ===
  Total: 9
  Success: 9
  Issues: 0

SUCCESS: All migrations completed!

Next: Run .\quick-fix-check.ps1
```

---

## 🔧 Sorun mu Yaşıyorsun?

### Docker Çalışmıyor

```
ERROR: Docker is not running!
```

**Çözüm:** Docker Desktop'ı başlat

### Database Bağlanamıyor

```
ERROR: Database timeout
```

**Çözüm:**

```powershell
docker-compose down
docker-compose up -d
Start-Sleep -Seconds 15
.\run-all-migrations.ps1
```

### Script Bulunamıyor

```
WARNING: some_script.sql may have issues
```

**Çözüm:** `db/` klasöründe SQL dosyalarının olduğundan emin ol

---

## 📚 Daha Fazla Bilgi

### Diğer Script'ler

- **`check-migration-status.ps1`** - Migration durumunu kontrol et
- **`test-auto-migrations.ps1`** - Sistem hazırlığını test et
- **`auto-apply-all-migrations.ps1`** - Gelişmiş özelliklerle migration (tracking, backup)

### Dokümantasyon

- **`QUICK_MIGRATION_GUIDE.md`** - Hızlı kılavuz
- **`MIGRATION_SCRIPTS_SUMMARY.md`** - Tüm script'lerin özeti
- **`MIGRATIONS_README.md`** - Detaylı dokümantasyon

---

## 💡 İpuçları

### Günlük Kullanım

```powershell
# Her gün sadece bunu çalıştır
.\run-all-migrations.ps1
```

### Sorun Giderme

```powershell
# Logları kontrol et
docker-compose logs backend --tail=50
docker-compose logs db --tail=50
```

### Container Yönetimi

```powershell
# Durumu gör
docker-compose ps

# Yeniden başlat
docker-compose restart

# Durdur
docker-compose down

# Başlat
docker-compose up -d
```

---

## ✅ Checklist

Migration'ları uygulamadan önce:

- [ ] Docker Desktop çalışıyor mu?
- [ ] `db/` klasöründe SQL dosyaları var mı?
- [ ] `docker-compose.yml` dosyası mevcut mu?

Migration'ları uyguladıktan sonra:

- [ ] "SUCCESS" mesajı gördün mü?
- [ ] Backend çalışıyor mu? (`docker-compose ps`)
- [ ] API test edildi mi? (`.\quick-fix-check.ps1`)

---

## 🎉 Başarılı!

Migration'lar başarıyla uygulandıysa:

1. Backend API'si çalışıyor olmalı
2. Database tabloları oluşturulmuş olmalı
3. Mapping verileri yüklenmiş olmalı

**Sonraki adım:** Uygulamanı test et!

```powershell
.\quick-fix-check.ps1
```

---

## 🆘 Yardım

Sorun yaşıyorsan:

1. **Logları kontrol et:**

   ```powershell
   docker-compose logs backend
   docker-compose logs db
   ```

2. **Container'ları yeniden başlat:**

   ```powershell
   docker-compose restart
   ```

3. **Temiz başlangıç:**

   ```powershell
   docker-compose down
   docker-compose up -d
   .\run-all-migrations.ps1
   ```

4. **Dokümantasyona bak:**
   - `QUICK_MIGRATION_GUIDE.md`
   - `MIGRATION_SCRIPTS_SUMMARY.md`

---

## 📞 Özet

**Tek yapman gereken:**

```powershell
.\run-all-migrations.ps1
```

**Bu kadar!** 🚀

Daha fazla bilgi için diğer dokümantasyon dosyalarına göz at.

---

**Son Güncelleme:** 2024-12-07  
**Versiyon:** 1.0.0  
**Yazar:** Kiro AI Assistant

**Kolay gelsin!** 💪
