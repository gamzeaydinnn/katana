# TODO: Frontend Güncellemeleri - Luca Ürün Alanları

## Görev: Backend'deki yeni Luca alanlarını frontend'e ekle

### Tamamlanan Adımlar:
- [x] Backend DTO'larını incele (LucaDtos.cs - LucaProductUpdateDto)
- [x] Frontend dosyalarını tara (LucaProducts.tsx, DataCorrectionPanel.tsx)
- [x] Eksik alanları belirle: kartAdi, kartTuru, olcumBirimiId, kartKodu, kategoriAgacKod, uzunAdi

### Kalan Adımlar:
- [ ] LucaProduct arayüzünü güncelle (her iki dosyada)
- [ ] Düzenleme diyaloglarına yeni alanları ekle (kartKodu, kartAdi, kategoriAgacKod, olcumBirimiId)
- [ ] handleSaveProduct/handleLucaSave fonksiyonlarını güncelle (updateDto'ya yeni alanları ekle)
- [ ] Frontend'i build et ve test et
- [ ] Backend API'nin yeni alanları kabul ettiğini doğrula

### Dosyalar:
- frontend/katana-web/src/components/Admin/LucaProducts.tsx
- frontend/katana-web/src/components/Admin/DataCorrectionPanel.tsx

### Notlar:
- Alanlar opsiyonel olarak eklenecek
- UI olarak basit TextField/NumberField kullanılacak
- Sadece dolu alanlar backend'e gönderilecek
