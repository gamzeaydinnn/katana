# Katana Temizlik Scriptleri - Hızlı Başlangıç

## 🎯 Ne İçin Kullanılır?

Katana'daki gereksiz ürünleri temizlemek ve siparişleri sıfırlamak için. Siparişleri 0'dan tekrar onaylayıp düzeltmek istediğinizde kullanın.

## 🚀 Hızlı Kullanım

### 1. Analiz Et (Güvenli)

```powershell
.\test-katana-cleanup-analysis.ps1
```

Ne var ne yok gösterir. Hiçbir şey değiştirmez.

### 2. Hepsini Temizle (Önerilen)

```powershell
# Önce test et (DRY RUN)
.\test-katana-full-cleanup.ps1

# Sonra gerçekten temizle
.\test-katana-full-cleanup.ps1 -DryRun:$false
```

### 3. Siparişleri Tekrar Onayla

Admin panelden siparişleri tekrar onaylayın. Ürünler otomatik olarak Katana'ya gönderilecek.

## 📋 Tüm Scriptler

| Script                               | Ne Yapar             | Güvenli mi? |
| ------------------------------------ | -------------------- | ----------- |
| `test-katana-cleanup-analysis.ps1`   | Analiz yapar         | ✅ Evet     |
| `test-katana-cleanup-delete-all.ps1` | Katana'dan siler     | ⚠️ Hayır    |
| `test-katana-cleanup-reset.ps1`      | Siparişleri sıfırlar | ⚠️ Hayır    |
| `test-katana-full-cleanup.ps1`       | Hepsini yapar        | ⚠️ Hayır    |

## ⚠️ Önemli

- ✅ Tüm scriptler varsayılan olarak **DRY RUN** modunda (güvenli)
- ⚠️ Gerçek işlem için `-DryRun:$false` ekleyin
- 🔒 Onay istenir (Force olmadıkça)
- 📄 JSON raporlar oluşturulur

## 📖 Detaylı Rehber

Detaylı kullanım için: [KATANA_CLEANUP_GUIDE.md](KATANA_CLEANUP_GUIDE.md)

## 🔧 Gereksinimler

- Backend çalışıyor olmalı (port 5055)
- Admin yetkisi (`admin` / `Katana2025!`)

## 💡 İpuçları

1. Her zaman önce analiz yapın
2. Her zaman önce DRY RUN yapın
3. JSON raporları kontrol edin
4. Şüphe varsa Force kullanmayın

---

**Hızlı Yardım:**

```powershell
# Sadece bak, hiçbir şey yapma
.\test-katana-cleanup-analysis.ps1

# Test et (güvenli)
.\test-katana-full-cleanup.ps1

# Gerçekten yap (dikkat!)
.\test-katana-full-cleanup.ps1 -DryRun:$false
```
