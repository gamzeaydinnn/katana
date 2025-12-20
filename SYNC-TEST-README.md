# Katana Senkronizasyon Test Scripti

## Genel Bakış

Bu script, Katana entegrasyon sisteminin fatura ve müşteri senkronizasyonunu kapsamlı bir şekilde test eder. Senkronizasyon ekranını açmadan önce sistemin hazır olduğundan emin olmak için kullanılır.

## Özellikler

✅ **Müşteri Senkronizasyonu Testi**
- Müşteri kayıtlarının Katana'dan çekilmesi
- Luca'ya gönderilmesi
- Başarı/hata oranları

✅ **Fatura Senkronizasyonu Testi**
- Fatura kayıtlarının Katana'dan çekilmesi
- Variant SKU çözümleme kontrolü
- Luca'ya gönderilmesi
- Başarı/hata oranları

✅ **Katana API Rate Limit Kontrolü**
- 429 hatalarının tespiti
- Variant SKU çözümleme başarı oranı
- Retry mekanizması kontrolü

✅ **Luca Authentication Kontrolü**
- Session/branch problemlerinin tespiti
- Authentication hatalarının raporlanması

✅ **Detaylı Raporlama**
- Renkli konsol çıktısı
- Test istatistikleri
- Başarı oranları
- Log dosyası oluşturma

## Gereksinimler

- macOS (veya Linux)
- Docker (katana-api-1 container çalışıyor olmalı)
- `jq` (JSON parsing için)
- `curl`

### jq Kurulumu

```bash
# Homebrew ile
brew install jq

# veya MacPorts ile
sudo port install jq
```

## Kullanım

### 1. Scripti Çalıştırılabilir Yapın

```bash
chmod +x test-sync-comprehensive.sh
```

### 2. Scripti Çalıştırın

```bash
./test-sync-comprehensive.sh
```

### 3. Sonuçları İnceleyin

Script otomatik olarak:
- API sağlık kontrolü yapar
- Login olur
- Müşteri sync testi yapar
- Fatura sync testi yapar
- Sync durumunu kontrol eder
- Katana API rate limit kontrolü yapar
- Luca authentication kontrolü yapar
- Özet rapor sunar

## Çıktı Örnekleri

### Başarılı Test

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
TEST SONUÇLARI ÖZETİ
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Test İstatistikleri:
  Toplam Test: 15
  Başarılı: 15
  Başarısız: 0
  Uyarı: 0

Başarı Oranı: %100

✓ TÜM TESTLER BAŞARILI!
Senkronizasyon ekranını açabilirsiniz.
```

### Uyarılı Test

```
Test İstatistikleri:
  Toplam Test: 15
  Başarılı: 13
  Başarısız: 0
  Uyarı: 2

Başarı Oranı: %86

⚠ TESTLER BAŞARILI AMA UYARILAR VAR
Uyarıları kontrol edin, ardından senkronizasyon ekranını açabilirsiniz.
```

### Başarısız Test

```
Test İstatistikleri:
  Toplam Test: 12
  Başarılı: 7
  Başarısız: 5
  Uyarı: 6

Başarı Oranı: %58

✗ BAZI TESTLER BAŞARISIZ
Lütfen hataları düzeltin ve tekrar test edin.

Log dosyası: sync-test-20251220-101812.log
```

## Log Dosyaları

Her test çalıştırmasında otomatik olarak bir log dosyası oluşturulur:

```
sync-test-YYYYMMDD-HHMMSS.log
```

Bu dosya:
- Tüm test adımlarını
- API response'larını
- Hata detaylarını
- Timestamp'leri içerir

## Test Kriterleri

### Müşteri Sync

- ✅ API erişilebilir olmalı
- ✅ Login başarılı olmalı
- ✅ En az 1 müşteri kaydı bulunmalı
- ✅ Başarı oranı %80'in üzerinde olmalı

### Fatura Sync

- ✅ API erişilebilir olmalı
- ✅ Login başarılı olmalı
- ✅ En az 1 fatura kaydı bulunmalı
- ✅ Variant SKU çözümleme çalışmalı
- ✅ 429 Rate Limit hatası olmamalı
- ✅ Başarı oranı %80'in üzerinde olmalı

### Katana API

- ✅ 429 Rate Limit hatası olmamalı
- ✅ Variant SKU çözümleme %90'ın üzerinde başarılı olmalı
- ✅ Retry mekanizması çalışmalı

### Luca Authentication

- ⚠️ Session/branch hataları olmamalı (uyarı)
- ⚠️ "Login olunmalı" hatası olmamalı (uyarı)

## Sorun Giderme

### "API erişilemiyor" Hatası

```bash
# Docker container'ın çalıştığını kontrol edin
docker ps | grep katana-api

# Container'ı başlatın
docker-compose up -d
```

### "Login başarısız" Hatası

- `test-sync-comprehensive.sh` dosyasındaki `PASSWORD` değişkenini kontrol edin
- Doğru şifre: `Katana2025!`

### "jq: command not found" Hatası

```bash
# jq'yu kurun
brew install jq
```

### Luca Authentication Hataları

Bu hatalar genellikle:
- Luca session timeout
- Branch selection problemi
- Luca API değişiklikleri

nedeniyle oluşur. Bu durumda:
1. Luca credentials'ları kontrol edin
2. LucaService session yönetimini gözden geçirin
3. Branch selection mantığını kontrol edin

## Katana Rate Limit Fix Doğrulaması

Script, yaptığımız rate limit fix'inin çalıştığını doğrular:

✅ **Beklenen Davranış:**
- 429 hatası olmamalı
- Variant SKU çözümleme %90+ başarılı olmalı
- "✅ Resolved X/Y variant SKUs successfully" logu görülmeli

❌ **Sorunlu Davranış:**
- "429 Too Many Requests" hatası
- Variant SKU çözümleme başarısız
- Uzun bekleme süreleri

## Senkronizasyon Ekranını Açma Kararı

### ✅ Açabilirsiniz:
- Tüm testler başarılı
- Veya sadece uyarılar var (Luca auth hariç kritik hata yok)
- Başarı oranı %80+

### ❌ Açmayın:
- Kritik testler başarısız
- API erişilemiyor
- Login çalışmıyor
- Başarı oranı %50'nin altında

### ⚠️ Dikkatli Açın:
- Luca authentication uyarıları var
- Başarı oranı %50-80 arası
- Bazı kayıtlar senkronize edilemiyor

## Gelişmiş Kullanım

### Sadece Belirli Testleri Çalıştırma

Script'i düzenleyerek sadece istediğiniz testleri çalıştırabilirsiniz:

```bash
# Sadece müşteri testi
# test_invoice_sync satırını comment out edin

# Sadece fatura testi
# test_customer_sync satırını comment out edin
```

### Farklı API URL

```bash
# Script içinde API_URL değişkenini değiştirin
API_URL="http://production-server:8080"
```

### Farklı Credentials

```bash
# Script içinde USERNAME ve PASSWORD değişkenlerini değiştirin
USERNAME="your-username"
PASSWORD="your-password"
```

## Katkıda Bulunma

Script'i geliştirmek için:
1. Yeni test fonksiyonları ekleyin
2. Daha detaylı kontroller yapın
3. Raporlamayı iyileştirin

## Lisans

Bu script Katana Integration projesi kapsamındadır.
