# Veri Mapping Kılavuzu

## Genel Bakış

Bu kılavuz, Katana MRP/ERP sistemi ile Luca Koza muhasebe sistemi arasındaki veri eşleştirmelerinin nasıl yapılandırılacağını açıklar.

## Mapping Tipleri

### 1. SKU_ACCOUNT - Ürün Hesap Kodu Eşleştirmesi

Katana'daki ürün SKU kodlarını Luca'daki muhasebe hesap kodlarıyla eşleştirir.

| Katana SKU | Luca Hesap Kodu | Kategori   | Açıklama              |
| ---------- | --------------- | ---------- | --------------------- |
| PRD-001    | 600.10          | Elektronik | Akıllı Telefon        |
| PRD-002    | 600.11          | Elektronik | Tablet                |
| PRD-003    | 600.20          | Giyim      | T-Shirt               |
| PRD-004    | 600.21          | Giyim      | Pantolon              |
| DEFAULT    | 600.01          | -          | Varsayılan hesap kodu |

#### API ile Yönetim

```bash
# Yeni SKU mapping oluştur
curl -X POST "https://api.yourcompany.com/api/mapping" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "mappingType": "SKU_ACCOUNT",
    "sourceValue": "PRD-005",
    "targetValue": "600.30",
    "description": "Ev Aletleri Kategorisi",
    "isActive": true
  }'

# Mevcut mapping güncelle
curl -X PUT "https://api.yourcompany.com/api/mapping/1" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "targetValue": "600.31",
    "description": "Güncellenmiş Ev Aletleri",
    "isActive": true
  }'
```

### 2. LOCATION_WAREHOUSE - Lokasyon Depo Eşleştirmesi

Katana'daki stok lokasyonlarını Luca'daki depo kodlarıyla eşleştirir.

| Katana Lokasyon | Luca Depo Kodu | Açıklama           |
| --------------- | -------------- | ------------------ |
| MAIN_WAREHOUSE  | MAIN           | Ana Depo           |
| SECONDARY_STORE | SEC            | İkincil Depo       |
| RETAIL_SHOP_1   | RT01           | Perakende Mağaza 1 |
| RETAIL_SHOP_2   | RT02           | Perakende Mağaza 2 |
| ONLINE_STOCK    | ONLINE         | Online Stok        |
| DEFAULT         | MAIN           | Varsayılan depo    |

#### API ile Yönetim

```bash
# Yeni lokasyon mapping oluştur
curl -X POST "https://api.yourcompany.com/api/mapping" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "mappingType": "LOCATION_WAREHOUSE",
    "sourceValue": "NEW_BRANCH",
    "targetValue": "BR03",
    "description": "Yeni Şube Deposu",
    "isActive": true
  }'
```

## Mapping Stratejileri

### 1. Otomatik Mapping

Sistem, tanımlanmamış SKU'lar veya lokasyonlar için otomatik olarak varsayılan değerleri kullanır:

- Bilinmeyen SKU → `600.01` (Genel Satış Hesabı)
- Bilinmeyen Lokasyon → `MAIN` (Ana Depo)

### 2. Toplu Mapping

Çok sayıda mapping kaydını toplu olarak oluşturmak için CSV import özelliği kullanılabilir:

```csv
MappingType,SourceValue,TargetValue,Description,IsActive
SKU_ACCOUNT,PRD-100,600.40,Kitap Kategorisi,true
SKU_ACCOUNT,PRD-101,600.41,Dergi Kategorisi,true
SKU_ACCOUNT,PRD-102,600.42,Gazete Kategorisi,true
LOCATION_WAREHOUSE,BRANCH_A,BRA,A Şubesi,true
LOCATION_WAREHOUSE,BRANCH_B,BRB,B Şubesi,true
```

### 3. Dinamik Mapping

Kategori bazlı otomatik mapping için kural tanımlama:

```json
{
  "rules": [
    {
      "condition": "category_id == 1",
      "accountCode": "600.10",
      "description": "Elektronik ürünler"
    },
    {
      "condition": "category_id == 2",
      "accountCode": "600.20",
      "description": "Giyim ürünleri"
    },
    {
      "condition": "price > 1000",
      "accountCode": "600.50",
      "description": "Yüksek değerli ürünler"
    }
  ]
}
```

## Mapping Doğrulama

### 1. Veri Tutarlılığı

Mapping kayıtları oluşturulurken şu kontroller yapılır:

- **Tekil Kayıt**: Aynı `mappingType` ve `sourceValue` kombinasyonu sadece bir kez bulunabilir
- **Geçerli Format**: Hesap kodları Luca formatına uygun olmalıdır (örn: 600.XX)
- **Aktif Durum**: Sadece aktif mapping kayıtları senkronizasyonda kullanılır

### 2. Test Etme

Mapping'lerin doğru çalıştığını test etmek için:

```bash
# Test sync işlemi
curl -X POST "https://api.yourcompany.com/api/sync/stock?fromDate=2024-01-01" \
  -H "Authorization: Bearer <token>"

# Sonuçları kontrol et
curl -X GET "https://api.yourcompany.com/api/reports/last" \
  -H "Authorization: Bearer <token>"
```

## Troubleshooting

### Yaygın Sorunlar

1. **"Unknown SKU" Hatası**

   - Çözüm: SKU_ACCOUNT mapping tablosuna eksik SKU'yu ekleyin

2. **"Invalid Account Code" Hatası**

   - Çözüm: Luca sistemindeki geçerli hesap kodlarını kontrol edin

3. **"Warehouse Not Found" Hatası**
   - Çözüm: LOCATION_WAREHOUSE mapping tablosuna eksik lokasyonu ekleyin

### Mapping Performansı

- Mapping tablosu cache'lenir, değişiklikler 5 dakika içinde aktif olur
- Büyük toplu işlemlerde mapping performansını artırmak için batch size'ı ayarlayın
- Kullanılmayan mapping kayıtlarını deaktive edin

## Best Practices

1. **Standart Kodlama**: Hesap kodları için şirket standartlarını kullanın
2. **Dokümantasyon**: Her mapping için açıklayıcı description yazın
3. **Backup**: Mapping değişikliklerinden önce backup alın
4. **Test**: Production'da değişiklik yapmadan önce test ortamında deneyin
5. **Monitoring**: Başarısız mapping'leri düzenli kontrol edin

## Örnek Mapping Senaryoları

### Senaryo 1: Yeni Ürün Kategorisi

Şirket yeni bir ürün kategorisi (Spor Malzemeleri) ekledi:

```bash
# 1. Yeni hesap kodu mapping'i oluştur
curl -X POST "https://api.yourcompany.com/api/mapping" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "mappingType": "SKU_ACCOUNT",
    "sourceValue": "SPORT-*",
    "targetValue": "600.60",
    "description": "Spor Malzemeleri",
    "isActive": true
  }'

# 2. Test sync yap
curl -X POST "https://api.yourcompany.com/api/sync/stock" \
  -H "Authorization: Bearer <token>"
```

### Senaryo 2: Depo Reorganizasyonu

Şirket depo yapısını yeniden organize etti:

```bash
# 1. Eski mapping'leri deaktive et
curl -X PUT "https://api.yourcompany.com/api/mapping/old-warehouse-id" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"isActive": false}'

# 2. Yeni mapping'leri oluştur
curl -X POST "https://api.yourcompany.com/api/mapping" \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "mappingType": "LOCATION_WAREHOUSE",
    "sourceValue": "NEW_CENTRAL_WAREHOUSE",
    "targetValue": "CENTRAL",
    "description": "Yeni Merkez Depo",
    "isActive": true
  }'
```
