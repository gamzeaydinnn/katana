# 🚀 TEST SENARYOLARı - ÖZET

## Hızlı Başlangıç

### TEST 1: İlk Senkronizasyon
```bash
1. Admin Panel açınız: http://localhost:3000
2. "Stok Kartları Senkronizasyonu" tab'ına gidiniz
3. "Senkronize Et" butonuna tıklayınız
4. Beklenen: ✅ 50/50 başarılı, ⚠️ 0 duplicate
```

### TEST 2: Duplicate Detection
```bash
1. Aynı tab'ında "Senkronize Et" butonuna TEKRAR tıklayınız
2. Beklenen: ✅ 0 başarılı, ⚠️ 50/50 duplicate
```

---

## Detaylı Test Akışı

### TEST 1: İLK SENKRONIZASYON (TEMIZ DURUM)

**Senaryo**: Tüm ürünler Luca'ya gönderilmeli

**Başlangıç Durumu**:
- Katana'da: ~50 ürün
- Luca'da: Boş (temiz)

**Test Adımları**:
1. Admin Panel'i aç
2. Luca stok kartlarını temizle (varsa)
3. "Senkronize Et" tıkla
4. 2-3 dakika bekle
5. Sonuçları kontrol et

**Beklenen Sonuç**:
```
✅ Başarılı:    50/50
❌ Başarısız:    0
⚠️  Duplicate:    0
⏭️  Atlanan:      0
```

**Backend Log Örneği**:
```
[14:32:45] INF] SendStockCardsAsync: Başlatıldı, 50 ürün gönderilecek
[14:32:46] INF] ✅ Stok kartı oluşturuldu: PRD-001 (skartId: 12345)
[14:32:47] INF] ✅ Stok kartı oluşturuldu: PRD-002 (skartId: 12346)
...
[14:33:45] INF] SendStockCardsAsync: Tamamlandı
[14:33:45] INF] 📊 Sonuçlar: Başarılı=50, Başarısız=0, Duplicate=0
```

**Kontrol Noktaları**:
- [ ] Admin Panel'de 50 başarılı gözüküyor
- [ ] Backend log'da "Stok kartı oluşturuldu" mesajları var
- [ ] Başarısız kayıt yok

---

### TEST 2: DUPLICATE DETECTION (AYNI ÜRÜNLERI TEKRAR GÖNDER)

**Senaryo**: Aynı ürünler tekrar gönderildiğinde, sistem:
- Duplicate'ları tespit eder
- Luca hatası almaz
- İşlemi başarılı olarak işaretler

**Başlangıç Durumu**:
- Katana'da: ~50 ürün (TEST 1 sonrası)
- Luca'da: 50 ürün (TEST 1 sonrası)

**Test Adımları**:
1. Aynı Admin Panel'de
2. "Senkronize Et" tıkla (2. kez)
3. 2-3 dakika bekle
4. Sonuçları kontrol et

**Beklenen Sonuç**:
```
✅ Başarılı:    0
❌ Başarısız:    0
⚠️  Duplicate:    50/50  ← ÖNEMLI!
⏭️  Atlanan:      0
```

**Backend Log Örneği**:
```
[14:35:00] INF] SendStockCardsAsync: Başlatıldı, 50 ürün gönderilecek
[14:35:01] INF] 🔍 ListStockCardsAsync: Luca'dan stok kartları getiriliyor...
[14:35:02] INF] 🔍 Luca'da stok kartı aranıyor: PRD-001
[14:35:02] INF] ✅ Stok kartı bulundu: PRD-001 (skartId: 12345)
[14:35:02] INF] ℹ️  Değişiklik yok, atlanıyor: PRD-001
[14:35:02] INF] ⚠️  Duplicate tespit edildi (değişiklik yok): PRD-001
...
[14:36:00] INF] SendStockCardsAsync: Tamamlandı
[14:36:00] INF] 📊 Sonuçlar: Başarılı=0, Başarısız=0, Duplicate=50
```

**Kontrol Noktaları**:
- [ ] Admin Panel'de 0 başarılı, 50 duplicate gözüküyor
- [ ] Backend log'da "Duplicate tespit edildi" mesajları var
- [ ] Başarısız kayıt yok (ÖNEMLI!)
- [ ] Luca'dan hata almıyor

---

## Log Komutları

### Senkronizasyon Loglarını Görmek

```bash
# Son 50 senkronizasyon logunu göster
docker-compose logs api 2>&1 | tail -50

# "Stok kartı" loglarını filtrele
docker-compose logs api 2>&1 | grep "Stok kartı"

# Duplicate loglarını filtrele
docker-compose logs api 2>&1 | grep -i "duplicate\|daha önce\|already"

# Hata loglarını filtrele
docker-compose logs api 2>&1 | grep -i "error\|exception"

# Real-time logları takip et
docker-compose logs -f api | grep -i "sync\|stok kartı"
```

### API ile Kontrol

```bash
# Sync status'ü kontrol et
curl http://localhost:8080/api/Sync/status | jq '.'

# Başarısız kayıtları listele
curl http://localhost:8080/api/adminpanel/failed-records-anon | jq '.records | length'

# Kategori mapping'lerini kontrol et
curl http://localhost:8080/api/Mapping/category-mappings | jq '.totalCount'

# Luca kategorilerini kontrol et
curl http://localhost:8080/api/Mapping/luca-categories | jq '.categories | length'
```

---

## Sorun Giderme

### Eğer TEST 1 Başarısız Olursa

```bash
# 1. Backend loglarını kontrol et
docker-compose logs api 2>&1 | grep -i "error\|exception" | tail -20

# 2. Luca bağlantısını kontrol et
docker-compose logs api 2>&1 | grep -i "luca\|session" | tail -10

# 3. Database'i kontrol et
curl http://localhost:8080/api/adminpanel/db-check

# 4. Backend'i restart et
docker-compose restart api
```

### Eğer TEST 2'de Duplicate Algılanmazsa

```bash
# 1. ListStockCardsAsync loglarını kontrol et
docker-compose logs api 2>&1 | grep "ListStockCardsAsync"

# 2. Luca'da gerçekten ürün var mı kontrol et
# (Luca Admin'e login edip kontrol et)

# 3. Backend'i restart edip TEST 1'i tekrar çalıştır
docker-compose restart api
```

---

## Başarı Kriterleri

### ✅ TEST 1 BAŞARILI
- [ ] Sonuçlar: ✅ ~50, ❌ 0, ⚠️ 0
- [ ] Backend log'da başarılı loglar var
- [ ] Başarısız kayıt yok

### ✅ TEST 2 BAŞARILI
- [ ] Sonuçlar: ✅ 0, ❌ 0, ⚠️ ~50
- [ ] Backend log'da "Duplicate tespit edildi" logları var
- [ ] Başarısız kayıt yok (KRITIK!)
- [ ] Luca'dan hata almıyor

---

## Test Komut Dosyası

Python script'i otomatikleştirmek için:

```bash
# Script'i çalıştır
python3 scripts/test-sync-scenarios.py

# Sonuçları oku
cat test_sync_results.json
```

---

**Not**: Tüm test adımları bu listeyi izleyerek manuel olarak da yapılabilir!
