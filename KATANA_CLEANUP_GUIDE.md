# Katana Temizlik Rehberi

Bu rehber, Katana'daki gereksiz ürünleri temizlemek ve siparişleri sıfırlamak için kullanılır.

## 📋 Genel Bakış

Sistem, onaylı siparişlerden Katana'ya gönderilen ürünleri analiz eder, siler ve siparişleri sıfırlar. Bu sayede temiz bir başlangıç yapabilirsiniz.

## 🔧 Kullanılabilir Scriptler

### 1. Analiz Scripti

**Dosya:** `test-katana-cleanup-analysis.ps1`

Katana'ya gönderilmiş tüm ürünleri analiz eder ve rapor verir.

```powershell
.\test-katana-cleanup-analysis.ps1
```

**Çıktı:**

- Onaylı sipariş sayısı
- Katana'ya gönderilen ürün sayısı
- Benzersiz SKU sayısı
- Tekrarlanan SKU'lar
- Tüm SKU listesi
- JSON rapor dosyası: `katana-cleanup-analysis-result.json`

**Özellikler:**

- ✅ Güvenli (sadece okuma)
- ✅ Hiçbir veri değiştirmez
- ✅ Detaylı rapor

---

### 2. Katana'dan Silme Scripti

**Dosya:** `test-katana-cleanup-delete-all.ps1`

Katana'daki TÜM sipariş ürünlerini siler.

#### Dry Run (Simülasyon)

```powershell
.\test-katana-cleanup-delete-all.ps1
# veya
.\test-katana-cleanup-delete-all.ps1 -DryRun
```

#### Gerçek Silme

```powershell
.\test-katana-cleanup-delete-all.ps1 -DryRun:$false
```

#### Onaysız Silme (Dikkatli!)

```powershell
.\test-katana-cleanup-delete-all.ps1 -DryRun:$false -Force
```

**Çıktı:**

- Silme istatistikleri
- Başarılı/başarısız sayıları
- Hata detayları
- JSON rapor dosyası: `katana-cleanup-delete-result.json`

**⚠️ UYARI:**

- Bu işlem geri alınamaz!
- Ürünler Katana'dan kalıcı olarak silinir
- Varsayılan olarak DRY RUN modunda çalışır

---

### 3. Sipariş Sıfırlama Scripti

**Dosya:** `test-katana-cleanup-reset.ps1`

Tüm onaylı siparişleri "Pending" durumuna geri alır.

#### Dry Run (Simülasyon)

```powershell
.\test-katana-cleanup-reset.ps1
# veya
.\test-katana-cleanup-reset.ps1 -DryRun
```

#### Gerçek Sıfırlama

```powershell
.\test-katana-cleanup-reset.ps1 -DryRun:$false
```

#### Onaysız Sıfırlama (Dikkatli!)

```powershell
.\test-katana-cleanup-reset.ps1 -DryRun:$false -Force
```

**Ne Yapar:**

- Sipariş durumunu `Approved` → `Pending` yapar
- `ApprovedDate`, `ApprovedBy`, `SyncStatus` temizler
- Tüm `KatanaOrderId` değerlerini siler
- Tüm `OrderMapping` kayıtlarını siler

**Çıktı:**

- Sıfırlanan sipariş sayısı
- Etkilenen satır sayısı
- Silinen mapping sayısı
- JSON rapor dosyası: `katana-cleanup-reset-result.json`

**⚠️ UYARI:**

- Bu işlem geri alınamaz!
- Siparişler baştan onaylanmalıdır
- Varsayılan olarak DRY RUN modunda çalışır

---

### 4. Tam Temizlik Scripti (Hepsi Bir Arada)

**Dosya:** `test-katana-full-cleanup.ps1`

Tüm işlemleri sırayla yapar:

1. Analiz
2. Katana'dan silme
3. Sipariş sıfırlama

#### Dry Run (Simülasyon)

```powershell
.\test-katana-full-cleanup.ps1
# veya
.\test-katana-full-cleanup.ps1 -DryRun
```

#### Gerçek Temizlik

```powershell
.\test-katana-full-cleanup.ps1 -DryRun:$false
```

#### Onaysız Temizlik (Dikkatli!)

```powershell
.\test-katana-full-cleanup.ps1 -DryRun:$false -Force
```

**Çıktı:**

- Tüm işlemlerin özeti
- Her adımın detaylı raporu
- 3 adet JSON rapor dosyası:
  - `katana-full-cleanup-analysis.json`
  - `katana-full-cleanup-delete.json`
  - `katana-full-cleanup-reset.json`

**⚠️ UYARI:**

- En kapsamlı temizlik işlemi
- Tüm veriler kalıcı olarak temizlenir
- Varsayılan olarak DRY RUN modunda çalışır

---

## 🎯 Önerilen İş Akışı

### Senaryo 1: İlk Kez Temizlik

```powershell
# 1. Mevcut durumu analiz et
.\test-katana-cleanup-analysis.ps1

# 2. Dry run ile test et
.\test-katana-full-cleanup.ps1

# 3. Sonuçları kontrol et ve gerçek temizlik yap
.\test-katana-full-cleanup.ps1 -DryRun:$false

# 4. Siparişleri admin panelden tekrar onayla
```

### Senaryo 2: Sadece Katana'yı Temizle

```powershell
# 1. Analiz
.\test-katana-cleanup-analysis.ps1

# 2. Dry run
.\test-katana-cleanup-delete-all.ps1

# 3. Gerçek silme
.\test-katana-cleanup-delete-all.ps1 -DryRun:$false
```

### Senaryo 3: Sadece Siparişleri Sıfırla

```powershell
# 1. Dry run
.\test-katana-cleanup-reset.ps1

# 2. Gerçek sıfırlama
.\test-katana-cleanup-reset.ps1 -DryRun:$false
```

---

## 🔒 Güvenlik Özellikleri

### Varsayılan Güvenlik

- ✅ Tüm scriptler varsayılan olarak **DRY RUN** modunda
- ✅ Gerçek işlemler için `-DryRun:$false` gerekli
- ✅ Onay istemi (Force olmadıkça)
- ✅ Detaylı loglama
- ✅ JSON raporlar

### Onay Mekanizması

```powershell
# Onay istenir
.\test-katana-cleanup-delete-all.ps1 -DryRun:$false

# Onay istenmez (dikkatli!)
.\test-katana-cleanup-delete-all.ps1 -DryRun:$false -Force
```

---

## 📊 Rapor Dosyaları

Tüm scriptler JSON formatında detaylı raporlar oluşturur:

| Script       | Rapor Dosyası                          |
| ------------ | -------------------------------------- |
| Analiz       | `katana-cleanup-analysis-result.json`  |
| Silme        | `katana-cleanup-delete-result.json`    |
| Sıfırlama    | `katana-cleanup-reset-result.json`     |
| Tam Temizlik | 3 adet rapor (yukarıdaki tüm raporlar) |

---

## 🚨 Önemli Notlar

### ⚠️ Dikkat Edilmesi Gerekenler

1. **Backend Çalışıyor Olmalı**

   - Backend 5055 portunda çalışmalı
   - `docker-compose up` veya benzeri

2. **Admin Yetkisi Gerekli**

   - Scriptler admin kullanıcısı ile çalışır
   - Varsayılan: `admin` / `Katana2025!`

3. **Geri Alınamaz İşlemler**

   - Silme ve sıfırlama işlemleri geri alınamaz
   - Mutlaka önce DRY RUN yapın

4. **Sıralı İşlem**
   - Önce Katana'dan silin
   - Sonra siparişleri sıfırlayın
   - Veya `test-katana-full-cleanup.ps1` kullanın

### ✅ En İyi Pratikler

1. **Her Zaman Önce Analiz**

   ```powershell
   .\test-katana-cleanup-analysis.ps1
   ```

2. **Her Zaman Önce Dry Run**

   ```powershell
   .\test-katana-full-cleanup.ps1  # DRY RUN
   ```

3. **Raporları Kontrol Et**

   - JSON dosyalarını inceleyin
   - Beklenmeyen durum var mı kontrol edin

4. **Yedek Alın** (Opsiyonel)
   - Kritik veriler için database yedek alın

---

## 🔧 Sorun Giderme

### Backend'e Bağlanamıyor

```
✗ Giriş başarısız: ...
```

**Çözüm:**

- Backend'in çalıştığından emin olun
- Port 5055'in açık olduğunu kontrol edin
- `docker ps` ile container'ı kontrol edin

### Login Hatası

```
✗ Giriş başarısız: 401 Unauthorized
```

**Çözüm:**

- Kullanıcı adı/şifre doğru mu kontrol edin
- Script içinde: `admin` / `Katana2025!`

### Silme Başarısız

```
✗ Silme işlemi başarısız: ...
```

**Çözüm:**

- Katana API'nin erişilebilir olduğunu kontrol edin
- Rate limit hatası varsa bekleyin
- JSON raporunda hata detaylarını inceleyin

---

## 📞 Destek

Sorun yaşarsanız:

1. JSON rapor dosyalarını kontrol edin
2. Backend loglarını inceleyin: `docker logs katana-backend`
3. Script çıktısını kaydedin

---

## 🎓 Örnek Kullanım

### Tam Temizlik Örneği

```powershell
# Terminal'i açın
cd C:\Users\GAMZE\Desktop\katana

# 1. Mevcut durumu gör
.\test-katana-cleanup-analysis.ps1

# Çıktı:
# ═══════════════════════════════════════
# KATANA ÜRÜN ANALİZ RAPORU
# ═══════════════════════════════════════
#
# 📊 GENEL İSTATİSTİKLER
#   • Onaylı Sipariş Sayısı      : 15
#   • Katana'ya Gönderilen Ürün  : 45
#   • Benzersiz SKU Sayısı        : 30
#   • Tekrarlanan SKU Sayısı      : 5

# 2. Dry run ile test et
.\test-katana-full-cleanup.ps1

# Çıktı kontrol et, her şey OK ise:

# 3. Gerçek temizlik
.\test-katana-full-cleanup.ps1 -DryRun:$false

# Onay ver: evet

# 4. Tamamlandı!
# Şimdi admin panelden siparişleri tekrar onaylayabilirsiniz
```

---

## 📝 Değişiklik Geçmişi

- **v1.0** - İlk sürüm
  - Analiz scripti
  - Silme scripti
  - Sıfırlama scripti
  - Tam temizlik scripti
  - Türkçe arayüz
  - DRY RUN desteği
  - JSON raporlama

---

**Son Güncelleme:** 2024
**Yazar:** Katana Integration Team
