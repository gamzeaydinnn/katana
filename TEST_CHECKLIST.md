# 🚀 TEST KONTROL LİSTESİ: Katana → Luca Stok Kartı Senkronizasyonu

## PRE-TEST HAZIRLIK

### ✅ Ortam Hazırı Kontrol
- [ ] Docker konteynerler çalışıyor mu? 
  ```bash
  docker ps | grep katana
  ```
- [ ] Backend API healthy mi?
  ```bash
  curl http://localhost:8080/api/health
  ```
- [ ] Frontend çalışıyor mu?
  ```bash
  http://localhost:3000
  ```
- [ ] Luca'ya bağlantı var mı?
  ```bash
  Backend logs: docker-compose logs api 2>&1 | grep "Luca"
  ```

---

## TEST 1: İLK SENKRONIZASYON (TEMIZ DURUM)

### Amaç
Tüm ürünler başarıyla Luca'ya gönderilmeli

### Test Adımları

1. **Admin Panel'i Aç**
   - [ ] Tarayıcıda: http://localhost:3000
   - [ ] Admin panele giriş yap
   - [ ] Stok Kartları Senkronizasyonu tab'ına tıkla

2. **Luca'daki Test Kayıtlarını Temizle (İf varsa)**
   - [ ] Luca Admin → Stok Kartları
   - [ ] "PRD-" ile başlayan tüm kartları sil
   - [ ] NOT: Eğer Luca arayüzü kapalıysa, bu adımı atlayabilirsiniz

3. **Senkronizasyonu Başlat**
   - [ ] "Senkronize Et" butonu'na tıkla
   - [ ] Başlatma zamanı not et: __________
   - [ ] Bekleme süresi: ~2-3 dakika

4. **Sonuçları Kontrol Et**
   - [ ] Admin Panel'de sync status'ü kontrol et
   - [ ] Beklenen sonuç:
     ```
     ✅ Başarılı: ~50
     ❌ Başarısız: 0
     ⚠️ Duplicate: 0
     ⏭️ Atlanan: 0
     ```

5. **Backend Loglarını Kontrol Et**
   ```bash
   # Terminal'de çalıştır:
   docker-compose logs api 2>&1 | grep -E "Stok kartı oluşturuldu|PRD-" | head -20
   ```
   - [ ] Ürün oluşturma logları görüyorum
   - [ ] Örnek: `✅ Stok kartı oluşturuldu: PRD-001`

### ✅ TEST 1 SONUÇ: ___________ (BAŞARILI/BAŞARISIZ)

---

## TEST 2: DUPLICATE DETECTION (AYNI ÜRÜNLERI TEKRAR GÖNDER)

### Amaç
Duplicate'lar tespit edilip atlanmalı, sistem kırılmayacak

### Test Adımları

1. **Aynı Senkronizasyonu 2. Kez Çalıştır**
   - [ ] Admin Panel'de "Senkronize Et" butonu'na tekrar tıkla
   - [ ] Başlatma zamanı not et: __________
   - [ ] Bekleme süresi: ~2-3 dakika

2. **Sonuçları Kontrol Et**
   - [ ] Admin Panel'de sync status'ü kontrol et
   - [ ] Beklenen sonuç:
     ```
     ✅ Başarılı: 0
     ❌ Başarısız: 0
     ⚠️ Duplicate: ~50  ← BURADA! Tümü duplicate olmalı
     ⏭️ Atlanan: 0
     ```

3. **Backend Loglarını Kontrol Et**
   ```bash
   # Terminal'de çalıştır:
   docker-compose logs api 2>&1 | grep -E "Duplicate tespit|daha önce kullanılmış|already exists" | head -20
   ```
   - [ ] Duplicate detection logları görüyorum
   - [ ] Örnek: `⚠️ Duplicate tespit edildi: PRD-001`

4. **Başarısız Kayıt Olmaması Kontrol Et**
   - [ ] Admin Panel → Başarısız Kayıtlar
   - [ ] Beklenen: 0 kayıt (veya en fazla bir kaç)

### ✅ TEST 2 SONUÇ: ___________ (BAŞARILI/BAŞARISIZ)

---

## TEŞHIS VE SORUN GİDERME

### Log Taraması

```bash
# Tüm senkronizasyon loglarını göster
docker-compose logs api 2>&1 | grep -i "sync\|senkronizasyon"

# Hata loglarını göster
docker-compose logs api 2>&1 | grep -i "error\|exception"

# Luca bağlantı loglarını göster
docker-compose logs api 2>&1 | grep -i "luca"

# Duplicate detection loglarını göster
docker-compose logs api 2>&1 | grep -i "duplicate\|daha önce"
```

### API Kontrolleri

```bash
# Sync status'ü API'den kontrol et
curl http://localhost:8080/api/Sync/status

# Başarısız kayıtları kontrol et
curl http://localhost:8080/api/adminpanel/failed-records-anon

# Mapping'leri kontrol et
curl http://localhost:8080/api/Mapping/category-mappings
```

---

## TEST SONU

### Özet Tablosu

| Test | Başarılı | Başarısız | Duplicate | Atlanan | Sonuç |
|------|----------|-----------|-----------|---------|-------|
| TEST 1 | ___ | ___ | ___ | ___ | ✅/❌ |
| TEST 2 | ___ | ___ | ___ | ___ | ✅/❌ |

### Notlar

_________________________________________________

_________________________________________________

### Gerekirse Yardımcı Bilgiler

- **API URL**: http://localhost:8080
- **Frontend URL**: http://localhost:3000
- **Database**: SQL Server @ localhost:1433
- **Luca URL**: Konfigürasyon'da

---

**Test Tarihi**: _______________

**Test Eden**: _______________

**Onay**: _______________
